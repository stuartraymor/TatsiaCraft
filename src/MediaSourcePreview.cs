using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace TasiaCraft {
 public sealed class MediaSourcePreview : Window {
  public LivePreview Viewer {get;private set;}
  public bool Released {get;private set;}
  readonly Project source; readonly Slider seek; readonly TextBlock clock; readonly Button play,stop,retry; readonly TextBlock status; string failure; bool updating;
  public static Project Snapshot(Media media){
   if(media==null)throw new ArgumentException("Select one media item to preview.");
   if(!File.Exists(media.Path))throw new FileNotFoundException("Preview source is missing. Relink the media first.",media.Path);
   var copy=new Media{Id=media.Id,Name=media.Name,Path=media.Path,Kind=media.Kind,Duration=media.Duration,HasAudio=media.HasAudio,Width=media.Width,Height=media.Height};
   if(double.IsNaN(copy.Duration)||double.IsInfinity(copy.Duration)||copy.Duration<=0||copy.Duration>86400)throw new ArgumentException("Preview requires a valid source duration of at most 24 hours.");
   if(copy.Kind!="audio"&&(copy.Width<1||copy.Height<1))throw new ArgumentException("Preview requires known source dimensions.");
   // The private project fits the original source; even canvas dimensions support the existing compositor.
   int width=copy.Kind=="audio"?640:Math.Min(8192,Math.Max(2,(copy.Width+1)/2*2));
   int height=copy.Kind=="audio"?360:Math.Min(8192,Math.Max(2,(copy.Height+1)/2*2));
   var result=new Project{Width=width,Height=height,TrackCount=1};result.Media.Add(copy);result.Clips.Add(new Clip{MediaId=copy.Id,Duration=copy.Duration,NativeSize=false});Engine.Validate(result,true);return result;
  }
  public MediaSourcePreview(Media media){
   source=Snapshot(media);Title="Preview - "+media.Name;Width=720;Height=490;MinWidth=380;MinHeight=260;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=new SolidColorBrush(Color.FromRgb(32,40,45));Foreground=Brushes.White;
   var root=new DockPanel{Margin=new Thickness(10)};Content=root;
   var controls=new StackPanel();DockPanel.SetDock(controls,Dock.Bottom);root.Children.Add(controls);
   var row=new WrapPanel{Orientation=Orientation.Horizontal};controls.Children.Add(row);
   play=Button("Play","MediaPreview.Play",Toggle);row.Children.Add(play);stop=Button("Stop","MediaPreview.Stop",()=>{if(failure!=null)return;Viewer.Pause();Viewer.Seek(0);UpdatePosition();});row.Children.Add(stop);retry=Button("Retry","MediaPreview.Retry",Retry);retry.IsEnabled=false;row.Children.Add(retry);
   clock=new TextBlock{Margin=new Thickness(12,8,8,8),VerticalAlignment=VerticalAlignment.Center};AutomationProperties.SetAutomationId(clock,"MediaPreview.Time");row.Children.Add(clock);row.Children.Add(Button("Close","MediaPreview.Close",()=>Close()));
   seek=new Slider{Minimum=0,Maximum=Engine.Length(source),SmallChange=1.0/source.Fps,LargeChange=1,Margin=new Thickness(4,8,4,8)};AutomationProperties.SetAutomationId(seek,"MediaPreview.Seek");AutomationProperties.SetName(seek,"Preview position in seconds");controls.Children.Add(seek);
   status=new TextBlock{Text=media.Kind=="audio"?"Audio source preview":"Source preview",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(4)};AutomationProperties.SetAutomationId(status,"MediaPreview.Status");controls.Children.Add(status);
   Viewer=new LivePreview{IsHitTestVisible=false};root.Children.Add(Viewer);Viewer.PositionChanged+=t=>UpdatePosition();Viewer.Status+=message=>{if(failure==null)status.Text=message;UpdatePosition();};Viewer.PlaybackError+=Failed;
   seek.ValueChanged+=(s,e)=>{if(!updating&&failure==null){Viewer.Seek(seek.Value);UpdatePosition();}};
   Loaded+=(s,e)=>Retry();Closed+=(s,e)=>{Viewer.Dispose();Released=true;};
   PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape){Close();e.Handled=true;}else if(e.Key==Key.Space&&!(Keyboard.FocusedElement is Slider)&&!(Keyboard.FocusedElement is Button)){Toggle();e.Handled=true;}};
  }
  static Button Button(string text,string id,Action action){var b=new Button{Content=text,Margin=new Thickness(3),Padding=new Thickness(10,5,10,5)};AutomationProperties.SetAutomationId(b,id);b.Click+=(s,e)=>action();return b;}
  void Failed(string message){failure=message;Viewer.Pause();status.Text=message;play.IsEnabled=stop.IsEnabled=seek.IsEnabled=false;retry.IsEnabled=true;UpdatePosition();}
  void Retry(){Viewer.Pause();Viewer.SetProject(null);failure=null;status.Text="Source preview";play.IsEnabled=stop.IsEnabled=seek.IsEnabled=true;retry.IsEnabled=false;try{Viewer.SetProject(source);Viewer.Seek(0);Viewer.SetViewZoom(0);}catch(Exception ex){Failed("Source preview failed: "+ex.Message);}UpdatePosition();}
  void Toggle(){if(failure!=null)return;if(Viewer.IsPlaying)Viewer.Pause();else Viewer.Play();UpdatePosition();}
  void UpdatePosition(){if(Viewer==null)return;updating=true;seek.Value=Viewer.Position;updating=false;clock.Text=TimeSpan.FromSeconds(Viewer.Position).ToString(@"hh\:mm\:ss\.fff")+" / "+TimeSpan.FromSeconds(Engine.Length(source)).ToString(@"hh\:mm\:ss\.fff");play.Content=Viewer.IsPlaying?"Pause":"Play";}
 }
}
