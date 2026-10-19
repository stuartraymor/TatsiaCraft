using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace TasiaCraft {
 public sealed class ProjectCanvas : Canvas {
  protected override AutomationPeer OnCreateAutomationPeer(){return new FrameworkElementAutomationPeer(this);}
 }
 // Independent timeline compositor. Playback never renders the timeline to a file.
 public sealed class LivePreview : UserControl, IDisposable {
  sealed class Layer {
   public Clip Clip; public Media Media; public Border Box; public MediaElement Decoder;
   public FrameworkElement Visual; public bool Ready,Failed,Running,Active; public double Width,Height; public string SourceKey; public double Gain=1; public int Z;
  }
  readonly Dictionary<string,Layer> layers=new Dictionary<string,Layer>();
  readonly Dictionary<string,Task<string>> proxies=new Dictionary<string,Task<string>>();
  readonly Dictionary<Process,string> proxyProcesses=new Dictionary<Process,string>();
  readonly DispatcherTimer timer=new DispatcherTimer(); readonly Stopwatch watch=new Stopwatch();
  readonly Canvas handles=new Canvas{IsHitTestVisible=true};
  readonly OffCanvasShade offCanvasShade=new OffCanvasShade();
  readonly Viewbox viewport=new Viewbox{Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly};
  readonly ScrollViewer scroller=new ScrollViewer{HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  Project project; Clip selection; readonly List<Clip> selections=new List<Clip>(); double position,origin; volatile bool disposed; bool editing;
  Point down; double oldX,oldY,oldScale,oldRotation; string gesture;
  CanvasResizeOperation resizeOperation;
  CanvasCropMoveOperation cropMoveOperation;
  ClipMoveOperation moveOperation;Rect moveBounds=Rect.Empty;
  readonly Border viewHost=new Border{ClipToBounds=false};
  readonly CanvasGuideOverlay alignmentGuides;
  public bool CanvasSnappingEnabled {get;private set;}
  void ClearGuides(){if(alignmentGuides!=null)alignmentGuides.SetGuides(null,null);}
  public void SetCanvasSnapping(bool enabled){CanvasSnappingEnabled=enabled;ClearGuides();}
  bool EditableSelection(){return selections.Count<=1&&project!=null&&selection!=null&&!selection.Animate&&!Hidden(selection)&&!Locked(selection)&&layers.ContainsKey(selection.Id)&&layers[selection.Id].Active&&(layers[selection.Id].Media==null||layers[selection.Id].Media.Kind!="audio");}
  CanvasSnapResult SnapMove(double x,double y){
   if(!CanvasSnappingEnabled||!EditableSelection())return new CanvasSnapResult(0,0,null,null);
   var layer=layers[selection.Id];var bounds=CanvasAlignment.Bounds(selection,layer.Box.Width,layer.Box.Height,project.Width,project.Height);bounds.Offset(x-selection.X,y-selection.Y);
   var snap=CanvasAlignment.Snap(bounds,SnapTargets(),SnapTolerance());
   bool validX=Math.Abs(x+snap.DeltaX)<=32768,validY=Math.Abs(y+snap.DeltaY)<=32768;
   return new CanvasSnapResult(validX?snap.DeltaX:0,validY?snap.DeltaY:0,validX?snap.GuideX:null,validY?snap.GuideY:null);
  }
  List<Rect> SnapTargets(){
   var targets=new List<Rect>{new Rect(0,0,project.Width,project.Height)};
   foreach(var other in layers.Values.OrderBy(l=>l.Z))if(!selections.Contains(other.Clip)&&other.Clip!=selection&&other.Active&&!Hidden(other.Clip)&&!other.Clip.Animate&&(other.Media==null||other.Media.Kind!="audio"))targets.Add(CanvasAlignment.Bounds(other.Clip,other.Box.Width,other.Box.Height,project.Width,project.Height));
   return targets;
  }
  double SnapTolerance(){var t=Stage.TransformToAncestor(this);double displayed=(t.Transform(new Point(1,0))-t.Transform(new Point(0,0))).Length;return 6/Math.Max(.00001,displayed);}
  CanvasResizeSnapResult SnapResize(Point pointer){return CanvasSnappingEnabled?resizeOperation.EvaluateSnapped(pointer,SnapTargets(),SnapTolerance()):new CanvasResizeSnapResult(resizeOperation.Evaluate(pointer),null,null);}
  public bool CanAlignSelection {get{return EditableSelection();}}
  public bool AlignSelection(string alignment){
   EndDrag(this,null);ClearGuides();if(!EditableSelection())return false;var layer=layers[selection.Id];var delta=CanvasAlignment.Align(CanvasAlignment.Bounds(selection,layer.Box.Width,layer.Box.Height,project.Width,project.Height),project.Width,project.Height,alignment);
   if(delta.LengthSquared<1e-16)return false;if(Math.Abs(selection.X+delta.X)>32768||Math.Abs(selection.Y+delta.Y)>32768)return false;
   Pause();if(BeforeEdit!=null)BeforeEdit();selection.X+=delta.X;selection.Y+=delta.Y;Update(false);if(AfterEdit!=null)AfterEdit();return true;
  }
  double oldCropLeft,oldCropTop,oldCropRight,oldCropBottom;
  public bool CropMode {get;private set;}
  readonly TranslateTransform viewTranslation=new TranslateTransform();
  bool panning;Point panDown;Vector panOrigin;
  public bool PanMode {get;private set;}
  public Vector ViewOffset {get{return new Vector(viewTranslation.X,viewTranslation.Y);}}
  public void SetViewOffset(double x,double y){if(double.IsNaN(x)||double.IsNaN(y)||double.IsInfinity(x)||double.IsInfinity(y))throw new ArgumentException("Viewport offset must be finite");ClearGuides();viewTranslation.X=Math.Max(-32768,Math.Min(32768,x));viewTranslation.Y=Math.Max(-32768,Math.Min(32768,y));UpdateOffCanvasShade();}
  public void ResetViewOffset(){SetViewOffset(0,0);}
  public void SetPanMode(bool enabled){EndDrag(this,null);ClearGuides();PanMode=enabled;if(enabled)CropMode=false;panning=false;if(IsMouseCaptured)ReleaseMouseCapture();Cursor=enabled?Cursors.Hand:Cursors.Arrow;DrawHandles();Say(enabled?"Pan: drag the workspace; clip properties and export stay unchanged":"Select canvas");}
  public Canvas Stage {get;private set;}
  public double ViewZoom {get;private set;}
  public double Position {get{return position;}}
  public bool IsPlaying {get;private set;}
  public event Action<double> PositionChanged;
  public event Action<string> Status;
  public event Action<string> PlaybackError;
  public event Action<Clip> Selecting;
  // Toggle means Ctrl or Shift; preserve means a normal click on an existing selection.
  public event Action<Clip,bool,bool> SelectionRequested;
  public event Action BeforeEdit,AfterEdit;
  public LivePreview() {
   Stage=new ProjectCanvas{Width=1280,Height=720,Background=Brushes.Black,ClipToBounds=false};
   AutomationProperties.SetAutomationId(Stage,"ProjectCanvas");AutomationProperties.SetName(Stage,"Project canvas");
   viewport.Child=Stage;viewport.RenderTransform=viewTranslation;viewport.ClipToBounds=false;var display=new Grid();alignmentGuides=new CanvasGuideOverlay(this);display.Children.Add(viewHost);display.Children.Add(alignmentGuides);viewHost.Child=viewport;Content=display;CanvasSnappingEnabled=true;Background=Brushes.Transparent;ClipToBounds=true;
   PreviewMouseLeftButtonDown+=(s,e)=>{if(!PanMode)return;panDown=e.GetPosition(this);panOrigin=ViewOffset;panning=true;CaptureMouse();e.Handled=true;};
   PreviewMouseMove+=(s,e)=>{if(!panning||e.LeftButton!=MouseButtonState.Pressed)return;Vector delta=e.GetPosition(this)-panDown;SetViewOffset(panOrigin.X+delta.X,panOrigin.Y+delta.Y);e.Handled=true;};
   PreviewMouseLeftButtonUp+=(s,e)=>{if(!panning)return;panning=false;ReleaseMouseCapture();e.Handled=true;};
   LostMouseCapture+=(s,e)=>{if(e.OriginalSource==this)panning=false;};
   Loaded+=(s,e)=>SetViewZoom(ViewZoom);
   Stage.Children.Add(handles); Panel.SetZIndex(handles,int.MaxValue);
   Stage.Children.Add(offCanvasShade);Panel.SetZIndex(offCanvasShade,int.MaxValue-1);
   LayoutUpdated+=(s,e)=>UpdateOffCanvasShade();SizeChanged+=(s,e)=>UpdateOffCanvasShade();scroller.ScrollChanged+=(s,e)=>UpdateOffCanvasShade();
   Stage.MouseLeftButtonDown+=(s,e)=>{if(e.OriginalSource==Stage){if(SelectionRequested!=null)SelectionRequested(null,false,false);else{SetSelection(null);if(Selecting!=null)Selecting(null);}}};
   Stage.MouseMove+=Drag; Stage.MouseLeftButtonUp+=EndDrag; Stage.LostMouseCapture+=(s,e)=>EndDrag(s,null);
   timer.Interval=TimeSpan.FromMilliseconds(1000.0/30);timer.Tick+=(s,e)=>Tick();
  }
  void Say(string message){if(Status!=null)Status(message);}
  void Fail(string message){Say(message);if(PlaybackError!=null)PlaybackError(message);}
  void UpdateOffCanvasShade(){
   if(disposed||Stage==null)return;
   if(project==null||ActualWidth<=0||ActualHeight<=0){offCanvasShade.Visibility=Visibility.Collapsed;return;}
   try{
    var bounds=TransformToDescendant(Stage).TransformBounds(new Rect(0,0,ActualWidth,ActualHeight));
    offCanvasShade.Width=Stage.Width;offCanvasShade.Height=Stage.Height;
    offCanvasShade.UpdateBounds(bounds,new Rect(0,0,Stage.Width,Stage.Height));offCanvasShade.Visibility=Visibility.Visible;
   }catch(InvalidOperationException){offCanvasShade.Visibility=Visibility.Collapsed;}
  }
  bool Hidden(Clip clip){return project!=null&&project.HiddenTracks!=null&&project.HiddenTracks.Contains(clip.Track);}
  bool Locked(Clip clip){return project!=null&&project.LockedTracks!=null&&project.LockedTracks.Contains(clip.Track);}
  double Length(){return project==null?0:Engine.Length(project);}
  public void SetProject(Project value){
   bool resume=IsPlaying;Pause();
   if(!object.ReferenceEquals(project,value)){EndDrag(this,null);ClearGuides();ClearLayers();project=value;selection=null;selections.Clear();ResetViewOffset();}
   if(project==null){position=0;handles.Children.Clear();return;}
   Stage.Width=project.Width;Stage.Height=project.Height;
   SetViewZoom(ViewZoom);
   var keep=new HashSet<string>(project.Clips.Select(c=>c.Id));
   foreach(string id in layers.Keys.Where(id=>!keep.Contains(id)||!object.ReferenceEquals(layers[id].Clip,project.Clips.First(c=>c.Id==id))||layers[id].Media!=project.Media.FirstOrDefault(m=>m.Id==layers[id].Clip.MediaId)).ToArray())Remove(id);
   Seek(position);if(resume)Play();
  }
  public void SetSelection(Clip value){SetSelections(value==null?new Clip[0]:new[]{value});}
  public void SetSelections(IEnumerable<Clip> values){
   EndDrag(this,null);ClearGuides();var ids=new HashSet<string>((values??new Clip[0]).Where(c=>c!=null).Select(c=>c.Id));selections.Clear();
   if(project!=null)selections.AddRange(project.Clips.Where(c=>ids.Contains(c.Id)));
   selection=selections.FirstOrDefault();if(selections.Count>1)CropMode=false;DrawHandles();
  }
  public void SetCropMode(bool enabled){SetPanMode(false);EndDrag(this,null);CropMode=enabled&&selections.Count<=1;DrawHandles();Say(CropMode?"Crop: drag edges to trim or drag media inside the fixed crop":"Select: drag to move or use transform handles");}
  // Viewport magnification never mutates clip transforms or export geometry.
  public void SetViewZoom(double zoom){
   ClearGuides();
   ViewZoom=double.IsNaN(zoom)?0:Math.Max(0,Math.Min(20,zoom));
   double dpiX=1,dpiY=1;var source=PresentationSource.FromVisual(this);if(source!=null&&source.CompositionTarget!=null){dpiX=source.CompositionTarget.TransformToDevice.M11;dpiY=source.CompositionTarget.TransformToDevice.M22;}
   if(ViewZoom==0){scroller.Content=null;viewHost.Child=null;viewport.Width=double.NaN;viewport.Height=double.NaN;viewport.MaxWidth=Stage.Width/dpiX;viewport.MaxHeight=Stage.Height/dpiY;viewport.StretchDirection=StretchDirection.DownOnly;viewHost.Child=viewport;}
   else{viewHost.Child=null;scroller.Content=null;viewport.MaxWidth=double.PositiveInfinity;viewport.MaxHeight=double.PositiveInfinity;viewport.StretchDirection=StretchDirection.Both;viewport.Width=Stage.Width*ViewZoom/dpiX;viewport.Height=Stage.Height*ViewZoom/dpiY;scroller.Content=viewport;viewHost.Child=scroller;}
   UpdateOffCanvasShade();
  }
  public void Refresh(){Update(true);}
  public void Seek(double time){
   ClearGuides();position=Math.Max(0,Math.Min(Length(),double.IsNaN(time)?0:time));origin=position;watch.Restart();
   Update(true);if(PositionChanged!=null)PositionChanged(position);
  }
  public void Play(){ClearGuides();if(disposed||project==null||Length()<=0)return;if(position>=Length()-.01)position=0;origin=position;watch.Restart();IsPlaying=true;timer.Start();Update(true);Say("Playing live timeline");}
  public void Pause(){ClearGuides();if(IsPlaying){position=Math.Min(Length(),origin+watch.Elapsed.TotalSeconds);IsPlaying=false;watch.Stop();}timer.Stop();foreach(var l in layers.Values)if(l.Decoder!=null){l.Decoder.Pause();l.Running=false;}DrawHandles();}
  void Tick(){position=Math.Min(Length(),origin+watch.Elapsed.TotalSeconds);Update(false);if(PositionChanged!=null)PositionChanged(position);if(position>=Length()){Pause();Say("Playback finished");}}
  void SelectFromPointer(Clip clip,MouseButtonEventArgs e){
   bool toggle=(Keyboard.Modifiers&(ModifierKeys.Control|ModifierKeys.Shift))!=0,preserve=!toggle&&selections.Contains(clip);
   if(SelectionRequested!=null)SelectionRequested(clip,toggle,preserve);
   else{if(!preserve){if(toggle){var next=selections.ToList();if(next.Contains(clip))next.Remove(clip);else next.Add(clip);SetSelections(next);}else SetSelection(clip);}if(Selecting!=null)Selecting(clip);}
   if(toggle){e.Handled=true;return;}
   if(selections.Contains(clip)){selection=clip;StartDrag("move",e);}else e.Handled=true;
  }
  Layer Create(Clip clip){
   var source=project.Media.FirstOrDefault(m=>m.Id==clip.MediaId);if(source!=null&&clip.Stream=="audio")source=new Media{Id=source.Id,Path=source.Path,Name=source.Name,Kind="audio",Duration=source.Duration,HasAudio=true};// separated audio plays its source through an audio-only layer
   var layer=new Layer{Clip=clip,Media=source};
   layer.SourceKey=SourceKey(clip,layer.Media);
   layer.Box=new Border{Background=Brushes.Transparent,RenderTransformOrigin=new Point(.5,.5)};
   layer.Box.MouseLeftButtonDown+=(s,e)=>SelectFromPointer(clip,e);
   Stage.Children.Add(layer.Box);layers[clip.Id]=layer;
   if(!string.IsNullOrEmpty(clip.Text)){
    var text=new TextBlock{Text=clip.Text,FontFamily=new FontFamily("Arial"),FontSize=48,Foreground=Brushes.White,TextWrapping=TextWrapping.NoWrap};
    text.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));layer.Width=text.DesiredSize.Width;layer.Height=text.DesiredSize.Height;layer.Visual=text;layer.Box.Child=text;layer.Ready=true;
   }else if(layer.Media!=null){
    if(NeedsProxy(clip,layer.Media))BeginProxy(layer);else OpenSource(layer,layer.Media.Path);
   }
   return layer;
  }
  static bool NeedsProxy(Clip c,Media m){return !string.IsNullOrEmpty(c.Effect)||m.HasAudio&&(!string.IsNullOrEmpty(c.AudioEffect)||c.Volume>1);}
  static string SourceKey(Clip c,Media m){return m==null?"text":m.Path+"|"+File.GetLastWriteTimeUtc(m.Path).Ticks+"|"+c.Effect+"|"+c.AudioEffect+"|"+(c.Volume>1?c.Volume:1).ToString(System.Globalization.CultureInfo.InvariantCulture);}
  void RunProxy(string args,string temp){
   var process=new Process{StartInfo=new ProcessStartInfo(Engine.Tool("ffmpeg"),"-nostdin "+args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true}};
   var errors=new StringBuilder();process.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)lock(errors){errors.AppendLine(e.Data);if(errors.Length>12000)errors.Remove(0,errors.Length-12000);}};
   try{
    lock(proxyProcesses){if(disposed)throw new OperationCanceledException("Preview closed");process.Start();proxyProcesses.Add(process,temp);}
    process.BeginErrorReadLine();process.StandardOutput.ReadToEnd();process.WaitForExit();
    if(disposed)throw new OperationCanceledException("Preview closed");
    if(process.ExitCode!=0)throw new Exception("Source preview processing failed: "+errors.ToString());
   }finally{lock(proxyProcesses)proxyProcesses.Remove(process);process.Dispose();}
  }
  async void BeginProxy(Layer layer){
   string key=layer.SourceKey;Media media=layer.Media;Clip c=layer.Clip;
   string effect=c.Effect,audio=c.AudioEffect;double gain=c.Volume>1?c.Volume:1;layer.Gain=gain;
   Task<string> task;
   lock(proxies){if(!proxies.TryGetValue(key,out task)){
    task=Task.Run(()=>{
     if(disposed)throw new OperationCanceledException("Preview closed");
     string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-","");
     string folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"TasiaCraft-source-preview");Directory.CreateDirectory(folder);
     string output=System.IO.Path.Combine(folder,hash+(media.Kind=="image"?".png":media.Kind=="audio"?".wav":".mp4"));
     if(File.Exists(output))return output;
     string temp=System.IO.Path.Combine(folder,hash+"-"+Guid.NewGuid().ToString("N")+System.IO.Path.GetExtension(output));
     string vf=effect=="grayscale"?"hue=s=0":effect=="blur"?"gblur=sigma=5":effect=="sharpen"?"unsharp":null;
     string af=audio=="normalize"?"loudnorm":audio=="noise reduction"?"afftdn":"anull";
     if(gain>1)af+=",volume="+gain.ToString(System.Globalization.CultureInfo.InvariantCulture);
     string args="-hide_banner -y -v error -threads 1 -i "+Engine.Quote(media.Path)+(vf==null?"":" -vf "+vf);
     if(media.Kind=="image")args+=" -frames:v 1 -threads 1";
     else if(media.Kind=="audio")args+=" -vn -af "+af+" -c:a pcm_s16le";
     else args+=" -c:v libopenh264 -b:v 6M -pix_fmt yuv420p"+(media.HasAudio?" -af "+af+" -c:a aac -b:a 192k":" -an")+" -movflags +faststart";
     try{RunProxy(args+" "+Engine.Quote(temp),temp);if(disposed)throw new OperationCanceledException("Preview closed");try{File.Move(temp,output);}catch(IOException){if(!File.Exists(output))throw;}return output;}finally{try{if(File.Exists(temp))File.Delete(temp);}catch(IOException){}}
    });proxies[key]=task;
   }}
   Say("Preparing source effect preview: "+media.Name);
   try{string path=await task;if(disposed||!layers.ContainsKey(c.Id)||!object.ReferenceEquals(layers[c.Id],layer))return;OpenSource(layer,path);Apply(layer,true);DrawHandles();Say("Source effect preview ready: "+media.Name);}
   catch(Exception ex){layer.Failed=true;lock(proxies)proxies.Remove(key);if(!disposed&&layers.ContainsKey(c.Id)&&object.ReferenceEquals(layers[c.Id],layer))Fail("Source effect preview failed: "+ex.Message);}
  }
  void OpenSource(Layer layer,string path){
   Clip clip=layer.Clip;
   if(layer.Media.Kind=="image"){
    try{var bitmap=new BitmapImage();using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();}bitmap.Freeze();layer.Width=bitmap.PixelWidth;layer.Height=bitmap.PixelHeight;layer.Visual=new Image{Source=bitmap,Stretch=Stretch.Fill};layer.Box.Child=layer.Visual;layer.Ready=true;}catch(Exception ex){layer.Failed=true;Fail("Image preview failed: "+ex.Message);}
   }else{
    layer.Width=layer.Media.Width;layer.Height=layer.Media.Height;
    var decoder=new MediaElement{LoadedBehavior=MediaState.Manual,UnloadedBehavior=MediaState.Manual,Stretch=Stretch.Fill,Volume=0,ScrubbingEnabled=true};
    layer.Decoder=decoder;layer.Visual=decoder;layer.Box.Child=decoder;
    decoder.MediaOpened+=(s,e)=>{if(disposed||!layers.ContainsKey(clip.Id)||!object.ReferenceEquals(layers[clip.Id],layer))return;layer.Ready=true;if(decoder.NaturalVideoWidth>0){layer.Width=decoder.NaturalVideoWidth;layer.Height=decoder.NaturalVideoHeight;}decoder.Pause();layer.Running=false;Apply(layer,true);DrawHandles();};
    decoder.MediaFailed+=(s,e)=>{if(disposed||!layers.ContainsKey(clip.Id)||!object.ReferenceEquals(layers[clip.Id],layer))return;layer.Failed=true;Fail("Live decoder cannot open "+layer.Media.Name+": "+e.ErrorException.Message+". Export may support this format.");};
    try{decoder.Source=new Uri(path);decoder.Play();decoder.Pause();}catch(Exception ex){layer.Failed=true;Fail("Live preview failed: "+ex.Message);}
   }
  }
  void Update(bool seek){
   if(disposed||project==null)return;
   int z=0;foreach(var c in project.Clips.OrderBy(c=>c.Track).ThenBy(c=>c.Start)){
    int order=z++;
    if(Hidden(c)){if(layers.ContainsKey(c.Id))Remove(c.Id);continue;}
    bool active=position>=c.Start&&position<c.Start+c.Duration;
    Layer l;if(layers.TryGetValue(c.Id,out l)&&l.SourceKey!=SourceKey(c,project.Media.FirstOrDefault(m=>m.Id==c.MediaId))){Remove(c.Id);l=null;}
    if(l==null){if(!active&&!(IsPlaying&&c.Start>position&&c.Start-position<.6))continue;l=Create(c);}
    l.Z=order;l.Active=active;Apply(l,seek);
   }
   // Release remote decoders to bound memory for large projects. The next clip is preopened.
   foreach(string id in layers.Keys.Where(id=>layers[id].Decoder!=null&&!layers[id].Active&&Math.Abs(layers[id].Clip.Start-position)>8).ToArray())Remove(id);
   DrawHandles();
  }
  static double Fade(Clip c,double t){double a=1;if(c.FadeIn>0)a*=Math.Min(1,Math.Max(0,t/c.FadeIn));if(c.FadeOut>0)a*=Math.Min(1,Math.Max(0,(c.Duration-t)/c.FadeOut));return a;}
  public bool Muted{get{return muted;}set{muted=value;Refresh();}} bool muted;
  void Apply(Layer l,bool seek){
   var c=l.Clip;double elapsed=Math.Max(0,Math.Min(c.Duration,position-c.Start));double k=c.Animate?elapsed/c.Duration:0;
   var behavior=Behaviors.Evaluate(c,elapsed,project.Width,project.Height);double scale=(c.Scale+(c.EndScale-c.Scale)*k)*behavior.Scale,x=c.X+(c.EndX-c.X)*k+behavior.X,y=c.Y+(c.EndY-c.Y)*k+behavior.Y;
   double w=l.Width>0?l.Width:project.Width,h=l.Height>0?l.Height:project.Height;
   bool text=!string.IsNullOrEmpty(c.Text),audio=l.Media!=null&&l.Media.Kind=="audio";
   if(text){var tb=(TextBlock)l.Visual;if(tb.Text!=c.Text){tb.Text=c.Text;tb.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));l.Width=tb.DesiredSize.Width;l.Height=tb.DesiredSize.Height;w=l.Width;h=l.Height;}}
   if(!text&&!c.NativeSize){double fit=Math.Min(project.Width/w,project.Height/h);w*=fit;h*=fit;}
   l.Box.Width=audio?1:w;l.Box.Height=audio?1:h;
   l.Box.Clip=!text&&!audio?new RectangleGeometry(new Rect(w*c.CropLeft,h*c.CropTop,Math.Max(0,w*(1-c.CropLeft-c.CropRight)),Math.Max(0,h*(1-c.CropTop-c.CropBottom)))):null;
   Canvas.SetLeft(l.Box,(project.Width-w)/2+x);Canvas.SetTop(l.Box,(project.Height-h)/2+y);
   var transforms=new TransformGroup();transforms.Children.Add(new ScaleTransform(scale,scale));transforms.Children.Add(new RotateTransform(text?0:c.Rotation));l.Box.RenderTransform=transforms;
   Panel.SetZIndex(l.Box,l.Z);
   l.Box.Opacity=l.Active&&!audio?Math.Max(0,Math.Min(1,c.Opacity))*Fade(c,elapsed)*behavior.Alpha:0;
   l.Box.IsHitTestVisible=l.Active&&!audio;
   if(l.Decoder!=null&&l.Ready&&!l.Failed){
    var d=l.Decoder;double target=c.In+elapsed*c.Speed;
    d.SpeedRatio=c.Speed;d.Volume=l.Active&&!Muted&&c.Stream!="video"&&!c.Silenced?Math.Min(1,Math.Max(0,c.Volume/l.Gain*Fade(c,elapsed)*AudioEnvelope.Level(c,elapsed))):0;
    if(seek||!l.Running||Math.Abs(d.Position.TotalSeconds-target)>.22)d.Position=TimeSpan.FromSeconds(target);
    if(IsPlaying&&l.Active){if(!l.Running){d.Play();l.Running=true;}}else if(l.Running){d.Pause();l.Running=false;}
   }
  }
  Rect VisibleLayerBounds(Layer layer){
   var member=layer.Clip;double w=layer.Box.Width,h=layer.Box.Height;
   var visible=new Rect(w*member.CropLeft,h*member.CropTop,w*(1-member.CropLeft-member.CropRight),h*(1-member.CropTop-member.CropBottom));
   return layer.Box.TransformToAncestor(Stage).TransformBounds(visible);
  }
  void DrawMultipleHandles(){
    handles.Children.Clear();if(project==null||IsPlaying||PanMode||selections.Any(Locked))return;
    // Layout must reflect the latest interpolated position and transform origin.
    Stage.UpdateLayout();
    foreach(var member in selections){
     Layer layer;if(Hidden(member)||!layers.TryGetValue(member.Id,out layer)||!layer.Active||layer.Media!=null&&layer.Media.Kind=="audio")continue;
     var bounds=VisibleLayerBounds(layer);var outline=new Canvas{Width=bounds.Width,Height=bounds.Height};
     Canvas.SetLeft(outline,bounds.X);Canvas.SetTop(outline,bounds.Y);handles.Children.Add(outline);
     var rect=new Rectangle{Width=bounds.Width,Height=bounds.Height,Stroke=Brushes.DeepSkyBlue,StrokeThickness=2,Fill=Brushes.Transparent,Cursor=Cursors.SizeAll};
     outline.Children.Add(rect);var clicked=member;rect.MouseLeftButtonDown+=(s,e)=>SelectFromPointer(clicked,e);
    }
  }
  void DrawHandles(){
   if(selections.Count>1){DrawMultipleHandles();return;}
   handles.Children.Clear();if(project==null||selection==null||IsPlaying||PanMode||Hidden(selection)||Locked(selection))return;
   Layer l;if(!layers.TryGetValue(selection.Id,out l)||!l.Active||l.Media!=null&&l.Media.Kind=="audio")return;
   double elapsed=Math.Max(0,Math.Min(selection.Duration,position-selection.Start)),k=selection.Animate?elapsed/selection.Duration:0;
   double scale=selection.Scale+(selection.EndScale-selection.Scale)*k,w=l.Box.Width*scale,h=l.Box.Height*scale;
   double cx=project.Width/2.0+selection.X+(selection.EndX-selection.X)*k,cy=project.Height/2.0+selection.Y+(selection.EndY-selection.Y)*k;
   double rotation=string.IsNullOrEmpty(selection.Text)?selection.Rotation:0;
   var outline=new Canvas{Width=w,Height=h,RenderTransform=new RotateTransform(rotation,w/2,h/2)};
   Canvas.SetLeft(outline,cx-w/2);Canvas.SetTop(outline,cy-h/2);handles.Children.Add(outline);
   double left=w*selection.CropLeft,top=h*selection.CropTop,right=w*(1-selection.CropRight),bottom=h*(1-selection.CropBottom);
   var rect=new Rectangle{Width=right-left,Height=bottom-top,Stroke=Brushes.DeepSkyBlue,StrokeThickness=2,Fill=Brushes.Transparent,Cursor=Cursors.SizeAll};Canvas.SetLeft(rect,left);Canvas.SetTop(rect,top);outline.Children.Add(rect);var outlined=selection;rect.MouseLeftButtonDown+=(s,e)=>SelectFromPointer(outlined,e);
   bool canCrop=string.IsNullOrEmpty(selection.Text)&&l.Media!=null&&l.Media.Width>0&&l.Media.Height>0;
   foreach(var pos in new[]{new[]{0,0},new[]{1,0},new[]{2,0},new[]{0,1},new[]{2,1},new[]{0,2},new[]{1,2},new[]{2,2}}){
    int ix=pos[0],iy=pos[1];string kind=CropMode?"crop"+(ix==0?"-left":ix==2?"-right":"")+(iy==0?"-top":iy==2?"-bottom":""):"scale-"+ix+"-"+iy;
    if(CropMode&&!canCrop)continue;Shape grip=CropMode?(Shape)new Rectangle():(Shape)new Ellipse();grip.Width=grip.Height=12;grip.Fill=Brushes.White;grip.Stroke=Brushes.DeepSkyBlue;grip.StrokeThickness=2;grip.Cursor=ix==1?Cursors.SizeNS:iy==1?Cursors.SizeWE:ix==iy?Cursors.SizeNWSE:Cursors.SizeNESW;Canvas.SetLeft(grip,(ix==0?left:ix==2?right:(left+right)/2)-6);Canvas.SetTop(grip,(iy==0?top:iy==2?bottom:(top+bottom)/2)-6);outline.Children.Add(grip);grip.MouseLeftButtonDown+=(s,e)=>StartDrag(kind,e);
   }
   if(!CropMode&&string.IsNullOrEmpty(selection.Text)){
    var stem=new Line{X1=w,Y1=h/2,X2=w+28,Y2=h/2,Stroke=Brushes.DeepSkyBlue,StrokeThickness=2,IsHitTestVisible=false};outline.Children.Add(stem);
    var rotate=new Ellipse{Width=14,Height=14,Fill=Brushes.White,Stroke=Brushes.DeepSkyBlue,StrokeThickness=2,Cursor=Cursors.Hand};Canvas.SetLeft(rotate,w+21);Canvas.SetTop(rotate,h/2-7);outline.Children.Add(rotate);rotate.MouseLeftButtonDown+=(s,e)=>StartDrag("rotate",e);
   }
  }
  bool CanMoveSelection(){
   if(project==null||selection==null||selections.Count==0||selections.Any(Locked))return false;
   return selections.Any(member=>{Layer layer;return !Hidden(member)&&layers.TryGetValue(member.Id,out layer)&&layer.Active&&(layer.Media==null||layer.Media.Kind!="audio");});
  }
  void StartDrag(string kind,MouseButtonEventArgs e){
   bool multiple=selections.Count>1;
   if(multiple){if(kind!="move"||!CanMoveSelection())return;}else if(!EditableSelection())return;
   if(CropMode&&kind=="move"){Layer sourceLayer=layers[selection.Id];if(sourceLayer.Media==null||sourceLayer.Media.Kind=="audio"||!string.IsNullOrEmpty(selection.Text))return;kind="crop-move";}
   var window=Window.GetWindow(this);if(window!=null){window.Focus();Keyboard.Focus(window);}Pause();Stage.UpdateLayout();
   gesture=kind;down=e.GetPosition(Stage);oldX=selection.X;oldY=selection.Y;oldScale=selection.Scale;oldRotation=selection.Rotation;
   oldCropLeft=selection.CropLeft;oldCropTop=selection.CropTop;oldCropRight=selection.CropRight;oldCropBottom=selection.CropBottom;
   resizeOperation=null;cropMoveOperation=null;moveOperation=null;moveBounds=Rect.Empty;ClearGuides();
   if(kind=="move"){
    try{moveOperation=new ClipMoveOperation(project,selections);}catch(InvalidOperationException){gesture=null;return;}
    foreach(var member in selections){Layer layer;if(Hidden(member)||!layers.TryGetValue(member.Id,out layer)||!layer.Active||layer.Media!=null&&layer.Media.Kind=="audio")continue;moveBounds.Union(VisibleLayerBounds(layer));}
   }
   if(kind=="crop-move"){var sourceLayer=layers[selection.Id];cropMoveOperation=new CanvasCropMoveOperation(selection,sourceLayer.Box.Width,sourceLayer.Box.Height,down);}
   if(kind.StartsWith("scale-")){Layer resizeLayer;if(!layers.TryGetValue(selection.Id,out resizeLayer))return;var parts=kind.Split('-');resizeOperation=new CanvasResizeOperation(selection,resizeLayer.Box.Width,resizeLayer.Box.Height,project.Width,project.Height,int.Parse(parts[1]),int.Parse(parts[2]),down);}
   editing=false;Stage.CaptureMouse();e.Handled=true;
  }
  void Drag(object sender,MouseEventArgs e){
   if(gesture==null||selection==null||e.LeftButton!=MouseButtonState.Pressed)return;
   if(selections.Count>1?!CanMoveSelection():Hidden(selection)||selections.Any(Locked)){EndDrag(sender,null);return;}
   Point p=e.GetPosition(Stage);if(!editing&&(p-down).Length<2)return;
   if(gesture=="crop-move"&&cropMoveOperation!=null){
    var result=cropMoveOperation.Evaluate(p);
    if(Math.Abs(result.X-selection.X)<1e-9&&Math.Abs(result.Y-selection.Y)<1e-9){e.Handled=true;return;}
    if(!editing){if(BeforeEdit!=null)BeforeEdit();editing=true;}result.ApplyTo(selection);Update(false);e.Handled=true;return;
   }
   if(gesture=="move"&&moveOperation!=null){
    var result=moveOperation.EvaluateCanvas(p.X-down.X,p.Y-down.Y);double? guideX=null,guideY=null;
    if(CanvasSnappingEnabled&&!moveBounds.IsEmpty){
     var bounds=moveBounds;bounds.Offset(result.DeltaX,result.DeltaY);var snap=CanvasAlignment.Snap(bounds,SnapTargets(),SnapTolerance());
     var snapped=moveOperation.EvaluateCanvas(result.DeltaX+snap.DeltaX,result.DeltaY+snap.DeltaY);
     if(Math.Abs(snapped.DeltaX-result.DeltaX-snap.DeltaX)<1e-8)guideX=snap.GuideX;
     if(Math.Abs(snapped.DeltaY-result.DeltaY-snap.DeltaY)<1e-8)guideY=snap.GuideY;result=snapped;
    }
    if(!result.Changes){alignmentGuides.SetGuides(guideX,guideY);e.Handled=true;return;}
    if(!editing){if(BeforeEdit!=null)BeforeEdit();editing=true;}result.Apply();Update(false);alignmentGuides.SetGuides(guideX,guideY);e.Handled=true;return;
   }
   if(gesture.StartsWith("scale-")&&resizeOperation!=null){
    var snap=SnapResize(p);var result=snap.Result;
    if(Math.Abs(result.Scale-selection.Scale)<1e-10&&Math.Abs(result.X-selection.X)<1e-9&&Math.Abs(result.Y-selection.Y)<1e-9){alignmentGuides.SetGuides(snap.GuideX,snap.GuideY);e.Handled=true;return;}
    if(!editing){if(BeforeEdit!=null)BeforeEdit();editing=true;}result.ApplyTo(selection);Update(false);alignmentGuides.SetGuides(snap.GuideX,snap.GuideY);e.Handled=true;return;
   }
   if(!editing){if(BeforeEdit!=null)BeforeEdit();editing=true;}
   if(gesture.StartsWith("crop")){
    Layer layer;if(!layers.TryGetValue(selection.Id,out layer)||layer.Media==null)return;
    double a=-oldRotation*Math.PI/180,dx=p.X-down.X,dy=p.Y-down.Y;
    double ux=(dx*Math.Cos(a)-dy*Math.Sin(a))/Math.Max(.01,layer.Box.Width*oldScale),uy=(dx*Math.Sin(a)+dy*Math.Cos(a))/Math.Max(.01,layer.Box.Height*oldScale);
    double minX=Math.Min(1,2.0/layer.Media.Width),minY=Math.Min(1,2.0/layer.Media.Height);
    if(gesture.Contains("left"))selection.CropLeft=Math.Max(0,Math.Min(1-oldCropRight-minX,oldCropLeft+ux));
    if(gesture.Contains("right"))selection.CropRight=Math.Max(0,Math.Min(1-oldCropLeft-minX,oldCropRight-ux));
    if(gesture.Contains("top"))selection.CropTop=Math.Max(0,Math.Min(1-oldCropBottom-minY,oldCropTop+uy));
    if(gesture.Contains("bottom"))selection.CropBottom=Math.Max(0,Math.Min(1-oldCropTop-minY,oldCropBottom-uy));
   }else{Point center=new Point(project.Width/2.0+oldX,project.Height/2.0+oldY);double angle=Math.Atan2(p.Y-center.Y,p.X-center.X)-Math.Atan2(down.Y-center.Y,down.X-center.X);selection.Rotation=oldRotation+angle*180/Math.PI;if((Keyboard.Modifiers&ModifierKeys.Shift)!=0)selection.Rotation=Math.Round(selection.Rotation/15)*15;}
   Update(false);e.Handled=true;
  }
  void EndDrag(object sender,MouseButtonEventArgs e){if(gesture==null)return;gesture=null;resizeOperation=null;cropMoveOperation=null;moveOperation=null;moveBounds=Rect.Empty;ClearGuides();bool changed=editing;editing=false;if(Stage.IsMouseCaptured)Stage.ReleaseMouseCapture();if(changed&&AfterEdit!=null)AfterEdit();if(e!=null)e.Handled=true;}
  void Remove(string id){var l=layers[id];if(l.Decoder!=null){l.Decoder.Close();l.Decoder.Source=null;}Stage.Children.Remove(l.Box);layers.Remove(id);}
  void ClearLayers(){foreach(string id in layers.Keys.ToArray())Remove(id);}
  public void Dispose(){
   KeyValuePair<Process,string>[] running;
   lock(proxyProcesses){if(disposed)return;disposed=true;running=proxyProcesses.ToArray();foreach(var pair in running)try{if(!pair.Key.HasExited)pair.Key.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}
   foreach(var pair in running){try{pair.Key.WaitForExit(1000);}catch(InvalidOperationException){}try{if(File.Exists(pair.Value))File.Delete(pair.Value);}catch(IOException){}catch(UnauthorizedAccessException){}}
   ClearGuides();panning=false;if(IsMouseCaptured)ReleaseMouseCapture();Pause();timer.Stop();ClearLayers();handles.Children.Clear();offCanvasShade.Visibility=Visibility.Collapsed;
  }
 }
}
