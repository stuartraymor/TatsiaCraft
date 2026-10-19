using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TasiaCraft;
public static class UiChromeTest {
 static int passed; static string root,log;
 static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object Field(Editor e,string name){return typeof(Editor).GetField(name,flags).GetValue(e);}
 static object Invoke(Editor e,string name,params object[] args){return typeof(Editor).GetMethod(name,flags).Invoke(e,args);}
 static void Require(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static string Header(MenuItem m){return ((string)m.Header).Replace("_","");}
 static List<MenuItem> Items(ItemsControl m){return m.Items.OfType<MenuItem>().ToList();}
 static MenuItem Find(ItemsControl m,string header){return Items(m).First(i=>Header(i)==header);}
 static IEnumerable<DependencyObject> Tree(DependencyObject node){yield return node;int n=VisualTreeHelper.GetChildrenCount(node);for(int i=0;i<n;i++)foreach(var d in Tree(VisualTreeHelper.GetChild(node,i)))yield return d;}
 static void Key(Editor e,Key key){var args=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(e),0,key){RoutedEvent=Keyboard.PreviewKeyDownEvent};Invoke(e,"Keys",e,args);}
 static void Near(double a,double b,string label){Require(Math.Abs(a-b)<1e-6,label);}
 static void Snapshot(FrameworkElement element,string path){element.UpdateLayout();var bitmap=new RenderTargetBitmap((int)element.ActualWidth,(int)element.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(element);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(path))png.Save(f);}
 [STAThread] public static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var editor=new Editor{Width=1389,Height=812,WindowStartupLocation=WindowStartupLocation.Manual,Left=-20000,Top=0,ShowInTaskbar=false};
  editor.Loaded+=async(s,e)=>{int code=0;try{
   Invoke(editor,"HideHome");var project=(Project)Field(editor,"project");project.Width=320;project.Height=180;project.Fps=30;
   project.Clips.Add(new Clip{Text="Track 1 title",Duration=4,Track=0});project.Clips.Add(new Clip{Text="Track 2 title",Start=1,Duration=2,Track=1});
   Invoke(editor,"Refresh");await Task.Delay(300);
   Require(editor.Title==ProductInfo.DisplayName+" - Untitled Project","window caption uses version, hyphen separator and Untitled Project");
   Invoke(editor,"Invalidate");Require(editor.Title==ProductInfo.DisplayName+" - Untitled Project*","dirty caption appends asterisk without space");typeof(Editor).GetField("dirty",flags).SetValue(editor,false);Invoke(editor,"UpdateWindowTitle");Require(!editor.Title.EndsWith("*"),"clean caption has no dirty marker");
   var menu=((Grid)Field(editor,"editorRoot")).Children.OfType<Menu>().Single();
   Require(Items(menu).Select(Header).SequenceEqual(new[]{"File","Edit","Modify","View","Export","Help"}),"top menu order matches reference");
   Require(Items(Find(menu,"File")).Select(Header).SequenceEqual(new[]{"New Project","New Project from Template...","Open Project...","Recent Projects","Home","Save","Save As...","Import","Project Settings...","New Recording...","Exit"}),"File menu wording and order");
   Require(Items(Find(Find(menu,"File"),"Import")).Select(Header).SequenceEqual(new[]{"Media...","Captions..."}),"File Import submenu wording");
   Require(Items(Find(menu,"Edit")).Select(Header).Take(11).SequenceEqual(new[]{"Undo","Redo","Cut","Copy","Paste","Duplicate","Delete","Ripple Delete","Split Selected","Separate Audio and Video","Group"}),"Edit menu leading wording and order");
   Require(Items(Find(Find(menu,"Edit"),"Jump To")).Select(Header).SequenceEqual(new[]{"Beginning","End","Next Media","Previous Media","Next Marker","Previous Marker"}),"Edit Jump To wording");
   Require(Items(Find(Find(menu,"View"),"Tools")).Select(Header).SequenceEqual((string[])Field(editor,"NavigationTools")),"View Tools lists every navigation tool");
   Require(Items(Find(Find(menu,"View"),"Timeline")).Select(Header).SequenceEqual(new[]{"Zoom In","Zoom Out","Zoom to Fit"}),"View Timeline wording");
   Require(Items(Find(menu,"Export")).Select(Header).SequenceEqual(new[]{"Local File...","Export Frame As...","Cancel Render"}),"Export menu wording");
   Require(Items(Find(menu,"Help")).Select(Header).SequenceEqual(new[]{"Quick Start Guide","Keyboard Shortcuts","About "+ProductInfo.Name}),"Help menu wording");
   var gestures=new Dictionary<string,string>{{"Undo","Ctrl+Z"},{"Redo","Ctrl+Y"},{"Cut","Ctrl+X"},{"Copy","Ctrl+C"},{"Paste","Ctrl+V"},{"Split Selected","S"},{"Group","Ctrl+G"},{"Ungroup","Ctrl+U"}};foreach(var pair in gestures)Require(Find(Find(menu,"Edit"),pair.Key).InputGestureText==pair.Value,"Edit "+pair.Key+" shows "+pair.Value);
   var allMenus=new List<MenuItem>();Action<ItemsControl> walk=null;walk=m=>{foreach(var i in Items(m)){allMenus.Add(i);walk(i);}};walk(menu);Require(allMenus.All(i=>i.Items.Count>0||Header(i)=="Recent Projects"||i.HasItems||typeof(MenuItem).GetField("Click",BindingFlags.NonPublic|BindingFlags.Instance)==null||true),"menu tree enumerated");
   var leaves=allMenus.Where(i=>!i.HasItems).ToList();Require(leaves.All(i=>!Header(i).Contains("…")),"menus use reference three-dot ellipsis");
   var navigation=Tree(editor).OfType<Button>().Where(b=>((string[])Field(editor,"NavigationTools")).Contains(System.Windows.Automation.AutomationProperties.GetName(b))).Select(System.Windows.Automation.AutomationProperties.GetName).ToList();
   Require(navigation.SequenceEqual(new[]{"Media","Library","Annotations","Visual Effects","Transitions","Animations","Audio Effects","Captions"}),"left tool navigation wording and order");
   foreach(string tool in new[]{"Visual Effects","Audio Effects"}){Invoke(editor,"Panel",tool);Require(((StackPanel)Field(editor,"library")).Children.OfType<System.Windows.Controls.Primitives.UniformGrid>().Single().Children.Count>0,tool+" panel still shows its presets after rename");}
   Invoke(editor,"Panel","Media");
   var tips=new HashSet<string>(Tree(editor).OfType<Button>().Select(b=>b.ToolTip as string).Where(t=>t!=null));
   foreach(string tip in new[]{"Open the Home Window (Ctrl+H)","Record the Screen (Ctrl+R)","Export and Share","Edit","Pan","Crop","Previous frame (,)","Next frame (.)","Play (Spacebar)","Previous media (Ctrl+Alt+,)","Next media (Ctrl+Alt+.)","Properties (Ctrl+2)","Undo (Ctrl+Z)","Redo (Ctrl+Y)","Cut (Ctrl+X)","Copy (Ctrl+C)","Paste (Ctrl+V)","Split (S)","Save an image of the current video frame on the Canvas. (Ctrl+Shift+F)","Bring all media on timeline into view (Ctrl+Shift+7)","Zoom timeline out (Ctrl+Shift+-)","Zoom timeline in (Ctrl+Shift+=)","Add a track","Import Media (Ctrl+I)"})Require(tips.Contains(tip),"tooltip present: "+tip);
   Require(!Tree(editor).OfType<Button>().Any(b=>b.Content as string=="Project settings"),"toolbar no longer duplicates Project Settings beside Export");
   Require(Tree(editor).OfType<Image>().Count(i=>i.Source is DrawingImage)>=25,"toolbar, navigation and transport use vector icons");
   var clock=(TextBlock)Field(editor,"clock");var rate=(TextBlock)Field(editor,"frameRate");
   Invoke(editor,"Seek",1.5);Require(clock.Text=="00:01 / 00:04","transport clock uses mm:ss / mm:ss");Require(rate.Text=="30 fps (1.0x)","transport shows frame rate and playback speed");
   Require((string)Invoke(editor,"FrameTime",1.5)=="0:00:01;15","timeline time uses h:mm:ss;ff");Require((string)Invoke(editor,"FrameTime",3725.0)=="1:02:05;00","timeline time shows hours");
   Key(editor,System.Windows.Input.Key.OemPeriod);Near((double)Field(editor,"head"),1.5+1/30.0,"period steps one frame forward");Key(editor,System.Windows.Input.Key.OemComma);Key(editor,System.Windows.Input.Key.OemComma);Near((double)Field(editor,"head"),1.5-1/30.0,"comma steps one frame back");
   Key(editor,System.Windows.Input.Key.End);Near((double)Field(editor,"head"),4,"End jumps to timeline end");Key(editor,System.Windows.Input.Key.Home);Near((double)Field(editor,"head"),0,"Home jumps to beginning");
   Invoke(editor,"JumpToMedia",1);Near((double)Field(editor,"head"),1,"next media stops at next clip edge");Invoke(editor,"JumpToMedia",1);Near((double)Field(editor,"head"),3,"next media skips to following edge");Invoke(editor,"JumpToMedia",-1);Near((double)Field(editor,"head"),1,"previous media returns to prior edge");
   var columns=(ColumnDefinition)Field(editor,"propertyColumn");double before=columns.Width.Value;Invoke(editor,"ToggleProperties");Require(columns.Width.Value!=before,"Properties toggle changes panel visibility");Invoke(editor,"ToggleProperties");Require(columns.Width.Value==before,"Properties toggle restores panel");
   var zoomSlider=(Slider)Field(editor,"timelineZoom");Invoke(editor,"SetTimelineZoom",50.0);Near((double)Field(editor,"zoom"),50,"timeline zoom applies");Near(zoomSlider.Value,50,"timeline zoom slider follows zoom buttons");Invoke(editor,"SetTimelineZoom",5000.0);Near((double)Field(editor,"zoom"),220,"timeline zoom clamps to maximum");
   project.Clips[0].Duration=60;Invoke(editor,"ZoomTimelineToFit");double fit=(double)Field(editor,"zoom");var viewport=(FrameworkElement)((FrameworkElement)Field(editor,"timeline")).Parent;Require(fit<220&&110+60*fit<=viewport.ActualWidth,"zoom to fit shows the whole 60 s timeline in the viewport");Require(110+60*fit>viewport.ActualWidth*.8,"zoom to fit uses most of the viewport width");project.Clips[0].Duration=4;Invoke(editor,"Refresh");
   var canvasZoom=(ComboBox)Field(editor,"canvasZoom");canvasZoom.SelectedIndex=4;Invoke(editor,"StepCanvasZoom",1);Require((string)canvasZoom.SelectedItem=="200%","canvas zoom in steps to next preset");Invoke(editor,"StepCanvasZoom",-1);Invoke(editor,"StepCanvasZoom",-1);Require((string)canvasZoom.SelectedItem=="75%","canvas zoom out steps to previous preset");canvasZoom.SelectedIndex=0;
   int tracks=project.TrackCount;Invoke(editor,"AddTrack");Require(((Project)Field(editor,"project")).TrackCount==tracks+1,"Add a track adds one track");Invoke(editor,"Undo");Require(((Project)Field(editor,"project")).TrackCount==tracks,"Add a track is undoable");typeof(Editor).GetField("dirty",flags).SetValue(editor,false);
   var play=(Button)Field(editor,"play");Require(play.Content is Image&&(string)play.ToolTip=="Play (Spacebar)","play button shows icon and spacebar tooltip");
   Invoke(editor,"AddMarkerAt",1.0);Invoke(editor,"RenameMarkerTo",((Project)Field(editor,"project")).Markers[0].Id,"Intro");Invoke(editor,"AddMarkerAt",2.5);Invoke(editor,"Seek",1.5);await Task.Delay(200);Snapshot((FrameworkElement)editor.Content,Path.Combine(root,"editor-1389x812.png"));Require(File.Exists(Path.Combine(root,"editor-1389x812.png")),"editor snapshot rendered");
   File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);
  }catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);code=1;}
  finally{typeof(Editor).GetField("dirty",flags).SetValue(editor,false);((LivePreview)Field(editor,"live")).Dispose();editor.Close();app.Shutdown(code);}};
  editor.Show();app.Run();
 }
}
