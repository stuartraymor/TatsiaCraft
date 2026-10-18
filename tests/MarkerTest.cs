using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation;
using TasiaCraft;
public static class MarkerTest {
 static int passed; static string root,log; static readonly JavaScriptSerializer json=new JavaScriptSerializer();
 static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object Field(Editor e,string name){return typeof(Editor).GetField(name,flags).GetValue(e);}
 static void SetField(Editor e,string name,object value){typeof(Editor).GetField(name,flags).SetValue(e,value);}
 static object Invoke(Editor e,string name,params object[] args){return typeof(Editor).GetMethod(name,flags).Invoke(e,args);}
 static Project P(Editor e){return (Project)Field(e,"project");}
 static int Undos(Editor e){return ((Stack<string>)Field(e,"undo")).Count;}
 static void Require(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static void Near(double a,double b,string label){Require(Math.Abs(a-b)<1e-6,label+" ("+a+" vs "+b+")");}
 static void Reject(Action a,string label){bool rejected=false;try{a();}catch(ArgumentException){rejected=true;}catch(InvalidDataException){rejected=true;}Require(rejected,label);}
 static Project Clone(Project p){return json.Deserialize<Project>(json.Serialize(p));}
 static IEnumerable<DependencyObject> Tree(DependencyObject node){yield return node;int n=VisualTreeHelper.GetChildrenCount(node);for(int i=0;i<n;i++)foreach(var d in Tree(VisualTreeHelper.GetChild(node,i)))yield return d;}
 static void Key(Editor e,Key key){var args=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(e),0,key){RoutedEvent=Keyboard.PreviewKeyDownEvent};Invoke(e,"Keys",e,args);}
 static byte[] Frame(string video,double time){string raw=Path.Combine(root,"frame.rgb");Engine.Run("ffmpeg","-y -v error -ss "+time.ToString(System.Globalization.CultureInfo.InvariantCulture)+" -i "+Engine.Quote(video)+" -frames:v 1 -pix_fmt rgb24 -f rawvideo "+Engine.Quote(raw));return File.ReadAllBytes(raw);}
 static List<Dictionary<string,object>> Chapters(string video){var root=json.Deserialize<Dictionary<string,object>>(Engine.Run("ffprobe","-v error -show_chapters -of json "+Engine.Quote(video)));return ((System.Collections.ArrayList)root["chapters"]).Cast<Dictionary<string,object>>().ToList();}
 static string Title(Dictionary<string,object> chapter){return (string)((Dictionary<string,object>)chapter["tags"])["title"];}
 static double Seconds(Dictionary<string,object> chapter,string key){return double.Parse((string)chapter[key],System.Globalization.CultureInfo.InvariantCulture);}

 static void ModelTests(){
  var p=new Project{Width=320,Height=180,Fps=30};p.Clips.Add(new Clip{Text="Base",Duration=4});
  var a=TimelineMarkers.Add(p,1.234);Near(a.Time,37/30.0,"marker time aligns to nearest frame");Require(a.Name=="Marker 1","first marker is named Marker 1");
  var b=TimelineMarkers.Add(p,.5);Require(b.Name=="Marker 2","second marker is named Marker 2");Require(p.Markers[0]==b&&p.Markers[1]==a,"markers stay sorted by time");
  Require(TimelineMarkers.Add(p,1.234+.01)==a&&p.Markers.Count==2,"adding at an occupied frame returns the existing marker");
  TimelineMarkers.Remove(p,b.Id);Require(TimelineMarkers.Add(p,2).Name=="Marker 2","next name reuses the lowest free number");
  Require(TimelineMarkers.Rename(p,a.Id,"  Intro α = ; # \\ 日本  "),"rename reports change");Require(a.Name=="Intro α = ; # \\ 日本","rename trims and keeps Unicode and metadata characters");
  Require(!TimelineMarkers.Rename(p,a.Id,a.Name+" "),"unchanged normalized rename reports no change");
  string state=json.Serialize(p);Reject(()=>TimelineMarkers.Rename(p,a.Id,"  "),"blank rename rejected");Reject(()=>TimelineMarkers.Rename(p,a.Id,null),"null rename rejected");Reject(()=>TimelineMarkers.Rename(p,a.Id,new string('x',201)),"overlong rename rejected");Reject(()=>TimelineMarkers.Rename(p,a.Id,"two\nlines"),"line break rename rejected");Reject(()=>TimelineMarkers.Rename(p,"missing","Name"),"unknown marker rename rejected");Require(json.Serialize(p)==state,"rejected renames are atomic");
  Require(TimelineMarkers.Rename(p,a.Id,new string('x',200)),"maximum length name accepted");TimelineMarkers.Rename(p,a.Id,"Intro");
  var c=p.Markers.Last();Require(TimelineMarkers.Move(p,c.Id,3.01),"move reports change");Near(c.Time,90/30.0,"move aligns to frame");Reject(()=>TimelineMarkers.Move(p,c.Id,a.Time),"move onto another marker rejected");Require(!TimelineMarkers.Move(p,c.Id,3),"move to same frame reports no change");Reject(()=>TimelineMarkers.Move(p,c.Id,double.NaN),"NaN move rejected");
  Require(TimelineMarkers.Next(p,0,1)==a&&TimelineMarkers.Next(p,a.Time,1)==c&&TimelineMarkers.Next(p,c.Time,1)==null,"next marker search");Require(TimelineMarkers.Next(p,3.5,-1)==c&&TimelineMarkers.Next(p,a.Time,-1)==null,"previous marker search");
  Near(Engine.Length(p),4,"markers inside clips do not change duration");TimelineMarkers.Add(p,6);Near(Engine.Length(p),6,"marker past last clip extends duration");p.Markers.RemoveAt(2);
  Reject(()=>TimelineMarkers.Add(p,-1),"negative marker time rejected");Reject(()=>TimelineMarkers.Add(p,double.PositiveInfinity),"infinite marker time rejected");Reject(()=>TimelineMarkers.Add(p,86401),"marker past maximum timeline rejected");
  Engine.Validate(p,false);Require(true,"valid marker project validates");
  Action<Action<Project>,string> bad=(change,label)=>{var q=Clone(p);change(q);Reject(()=>Engine.Validate(q,false),label);};
  bad(q=>q.Markers[0].Time=double.NaN,"NaN marker in project rejected");bad(q=>q.Markers[0].Time=-1,"negative marker in project rejected");bad(q=>q.Markers[1].Id=q.Markers[0].Id,"duplicate marker ID rejected");bad(q=>q.Markers[0].Id=q.Clips[0].Id,"marker ID colliding with clip rejected");bad(q=>q.Markers[0].Name=" Intro","untrimmed marker name in file rejected");bad(q=>q.Markers[0].Name="","empty marker name in file rejected");bad(q=>q.Markers.Add(null),"null marker rejected");
  string path=Path.Combine(root,"markers.tasiaproj");Engine.Save(p,path);var loaded=Engine.Load(path);Require(json.Serialize(loaded.Markers)==json.Serialize(p.Markers),"markers save and reopen exactly");
  var legacy=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(path));legacy.Remove("Markers");File.WriteAllText(path,json.Serialize(legacy));var old=Engine.Load(path);Require(old.Markers!=null&&old.Markers.Count==0,"legacy project without markers loads empty marker list");
  string meta=TimelineMarkers.ChapterMetadata(p,4);Require(meta.StartsWith(";FFMETADATA1\n")&&meta.Contains("title=Intro\n")&&meta.Contains("START=1233\nEND=3000"),"chapter metadata spans each marker to the next");Require(TimelineMarkers.ChapterMetadata(p,1)==null,"markers at or after export end produce no chapters");
  TimelineMarkers.Rename(p,a.Id,"A=B;C#D\\E");Require(TimelineMarkers.ChapterMetadata(p,4).Contains("title=A\\=B\\;C\\#D\\\\E\n"),"chapter titles escape FFmpeg metadata characters");
 }

 static void ExportTests(){
  string red=Path.Combine(root,"red.png");Engine.Run("ffmpeg","-y -v error -f lavfi -i color=red:s=320x180 -frames:v 1 "+Engine.Quote(red));var media=Engine.Probe(red);
  var p=new Project{Width=320,Height=180,Fps=30};p.Media.Add(media);p.Clips.Add(new Clip{MediaId=media.Id,Duration=3,NativeSize=true});
  string plain=Path.Combine(root,"no-markers.mp4");Engine.Render(p,plain,null);Require(Chapters(plain).Count==0,"export without markers has no chapters");
  TimelineMarkers.Add(p,0,"Opening");TimelineMarkers.Add(p,1,"Scene α = 2");TimelineMarkers.Add(p,2.5,"Wrap; up");
  string marked=Path.Combine(root,"markers.mp4");Engine.Render(p,marked,null);var chapters=Chapters(marked);
  Require(chapters.Count==3,"export writes one chapter per marker");Require(Title(chapters[0])=="Opening"&&Title(chapters[1])=="Scene α = 2"&&Title(chapters[2])=="Wrap; up","chapter titles match marker names including Unicode and escaped characters");
  Near(Seconds(chapters[0],"start_time"),0,"first chapter starts at its marker");Near(Seconds(chapters[1],"start_time"),1,"second chapter starts at its marker");Near(Seconds(chapters[0],"end_time"),1,"chapter ends at next marker");Near(Seconds(chapters[2],"end_time"),3,"last chapter ends at export end");
  Require(Math.Abs(Engine.Probe(marked).Duration-3)<.1,"markers inside the timeline keep export duration");
  Require(Frame(plain,1.5).SequenceEqual(Frame(marked,1.5)),"markers leave exported pixels unchanged");
  TimelineMarkers.Add(p,5,"Tail");string tail=Path.Combine(root,"marker-tail.mp4");Engine.Render(p,tail,null);Require(Math.Abs(Engine.Probe(tail).Duration-5)<.1,"marker past last clip extends export with the timeline");var tailFrame=Frame(tail,4.5);Require(tailFrame.All(v=>v<20),"extended timeline tail renders black");Require(Chapters(tail).Count==3,"marker at export end does not create an empty chapter");
 }

 static void EditorTests(Editor editor){
  Invoke(editor,"HideHome");var p=P(editor);p.Width=320;p.Height=180;p.Fps=30;p.Clips.Add(new Clip{Text="Track 1",Duration=4,Track=0});p.Clips.Add(new Clip{Text="Track 2",Start=1,Duration=2,Track=1});Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();SetField(editor,"dirty",false);
  var timeline=(Canvas)Field(editor,"timeline");Func<string,FrameworkElement> byId=automationId=>Tree(timeline).OfType<FrameworkElement>().FirstOrDefault(x=>AutomationProperties.GetAutomationId(x)==automationId);
  Require(!(bool)Field(editor,"markerTrackVisible")&&byId("Timeline.MarkerTray")==null,"marker tray is hidden by default");Require(byId("Timeline.MarkerTrayToggle")!=null,"tray toggle button is present");
  double firstTop=Canvas.GetTop(Tree(timeline).OfType<Border>().First(b=>b.ToolTip is string&&((string)b.ToolTip).StartsWith("Track 1")));
  Invoke(editor,"ToggleMarkerTrack");Require((bool)Field(editor,"markerTrackVisible")&&byId("Timeline.MarkerTray")!=null,"Ctrl+M command shows marker tray");
  double shifted=Canvas.GetTop(Tree(timeline).OfType<Border>().First(b=>b.ToolTip is string&&((string)b.ToolTip).StartsWith("Track 1")));Near(shifted-firstTop,26,"showing the tray moves tracks down by the tray height");
  Invoke(editor,"ToggleMarkerTrack");Require(!(bool)Field(editor,"markerTrackVisible"),"Ctrl+M command hides marker tray");Require(Undos(editor)==0&&!(bool)Field(editor,"dirty"),"tray visibility is not an undoable edit");
  Invoke(editor,"Seek",1.5);Require((bool)Invoke(editor,"AddMarkerAt",1.5),"Add Timeline Marker adds a marker");Require(P(editor).Markers.Count==1&&P(editor).Markers[0].Name=="Marker 1","Add Timeline Marker names the first marker Marker 1");Require(Undos(editor)==1&&(bool)Field(editor,"dirty"),"adding a marker is one undo checkpoint");
  Require((bool)Field(editor,"markerTrackVisible"),"adding a marker shows the tray");string id=P(editor).Markers[0].Id;Require((string)Field(editor,"selectedMarkerId")==id,"new marker is selected");
  var props=(StackPanel)Field(editor,"properties");Require(props.Children.OfType<TextBlock>().First().Text=="Marker","selected marker shows Marker properties");var nameBox=props.Children.OfType<TextBox>().Single();Require(AutomationProperties.GetAutomationId(nameBox)=="Marker.Name"&&nameBox.Text=="Marker 1","Marker name field shows the name");
  Require(byId("Marker."+id)!=null,"marker chip is drawn in the tray");Near(Canvas.GetLeft(byId("Marker."+id)),110+1.5*(double)Field(editor,"zoom"),"marker chip sits at its time");
  Require(!(bool)Invoke(editor,"AddMarkerAt",1.5+.005)&&P(editor).Markers.Count==1&&Undos(editor)==1,"adding at an occupied frame selects without a new checkpoint");
  Require((bool)Invoke(editor,"RenameMarkerTo",id,"  Chapter One  ")&&P(editor).Markers[0].Name=="Chapter One"&&Undos(editor)==2,"rename is one checkpoint and trims");Require((bool)Invoke(editor,"RenameMarkerTo",id,"Chapter One")&&Undos(editor)==2,"unchanged rename adds no checkpoint");
  string before=json.Serialize(P(editor));Require(!(bool)Invoke(editor,"RenameMarkerTo",id," ")&&json.Serialize(P(editor))==before&&Undos(editor)==2,"invalid rename leaves project and undo unchanged");
  Invoke(editor,"Undo");Require(P(editor).Markers[0].Name=="Marker 1","undo restores previous marker name");Invoke(editor,"Redo");Require(P(editor).Markers[0].Name=="Chapter One","redo reapplies rename");
  Invoke(editor,"AddMarkerAt",3.0);Invoke(editor,"AddMarkerAt",.5);Require(P(editor).Markers.Select(m=>m.Name).SequenceEqual(new[]{"Marker 2","Chapter One","Marker 1"}),"markers stay time ordered with sequential names");
  Invoke(editor,"Seek",0.0);Invoke(editor,"JumpToMarker",1);Near((double)Field(editor,"head"),.5,"Next Marker jumps to first marker");Invoke(editor,"JumpToMarker",1);Near((double)Field(editor,"head"),1.5,"Next Marker jumps to second marker");Invoke(editor,"JumpToMarker",-1);Near((double)Field(editor,"head"),.5,"Previous Marker jumps back");Invoke(editor,"JumpToMarker",-1);Near((double)Field(editor,"head"),.5,"Previous Marker stops at first marker");
  SetField(editor,"zoom",50.0);var targets=((IEnumerable<double>)Invoke(editor,"TimelineSnapTargets")).ToList();Require(targets.Contains(1.5)&&targets.Contains(.5),"timeline snap targets include markers");Near((double)Invoke(editor,"SnapTime",1.55,targets),1.5,"times within 8 px snap to a marker");Near((double)Invoke(editor,"SnapTime",1.7,targets),1.7,"times beyond 8 px do not snap");
  string third=P(editor).Markers.Last().Id;Invoke(editor,"SelectMarker",third);Near((double)Field(editor,"head"),3,"selecting a marker moves the playhead to it");Invoke(editor,"SelectClip",P(editor).Clips[0],false,false);Require(Field(editor,"selectedMarkerId")==null,"selecting a clip clears marker selection");
  Invoke(editor,"SelectMarker",third);int undoBefore=Undos(editor);Key(editor,System.Windows.Input.Key.Delete);Require(P(editor).Markers.All(m=>m.Id!=third)&&Undos(editor)==undoBefore+1,"Delete key removes the selected marker in one step");Require(Field(editor,"selectedMarkerId")==null,"deleted marker is deselected");
  Invoke(editor,"Undo");Require(P(editor).Markers.Any(m=>m.Id==third),"undo restores deleted marker");
  int clips=P(editor).Clips.Count;Invoke(editor,"RemoveAllMarkers");Require(P(editor).Markers.Count==0&&P(editor).Clips.Count==clips,"Remove All Markers removes markers only");Invoke(editor,"Undo");Require(P(editor).Markers.Count==3,"undo restores all markers");
  Invoke(editor,"AddMarkerAt",7.0);Near((double)Invoke(editor,"Length"),7,"editor duration extends to a later marker");Invoke(editor,"Seek",10.0);Near((double)Field(editor,"head"),7,"playhead can reach the marker-extended end");
  var menu=((Grid)Field(editor,"editorRoot")).Children.OfType<Menu>().Single();Func<ItemsControl,string,MenuItem> find=(m,h)=>m.Items.OfType<MenuItem>().First(i=>((string)i.Header).Replace("_","")==h);
  var markers=find(find(menu,"Modify"),"Markers");Require(markers.Items.OfType<MenuItem>().Select(i=>(string)i.Header).SequenceEqual(new[]{"Add Timeline Marker","Remove All Markers"}),"Modify > Markers wording matches reference");Require(find(markers,"Add Timeline Marker").InputGestureText=="Shift+M","Add Timeline Marker shows Shift+M");
  var jump=find(find(menu,"Edit"),"Jump To");Require(find(jump,"Next Marker").InputGestureText=="Ctrl+]"&&find(jump,"Previous Marker").InputGestureText=="Ctrl+[","Jump To marker shortcuts match reference");Require(find(jump,"Beginning").InputGestureText=="Ctrl+Home"&&find(jump,"End").InputGestureText=="Ctrl+End","Jump To beginning/end shortcuts match reference");
  Require(find(find(menu,"View"),"Show Markers").InputGestureText=="Ctrl+M","View Show Markers shows Ctrl+M");Require(find(find(menu,"Edit"),"Deselect All").InputGestureText=="Ctrl+Shift+D","Deselect All shows Ctrl+Shift+D");
  string path=Path.Combine(root,"editor-markers.tasiaproj");Invoke(editor,"SaveProjectTo",path);var reopened=Engine.Load(path);Require(json.Serialize(reopened.Markers)==json.Serialize(P(editor).Markers),"editor save writes markers");
  SetField(editor,"dirty",false);
 }

 [STAThread] public static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var editor=new Editor{Width=1389,Height=812,WindowStartupLocation=WindowStartupLocation.Manual,Left=-20000,Top=0,ShowInTaskbar=false};
  editor.Loaded+=async(s,e)=>{int code=0;try{ModelTests();ExportTests();await Task.Delay(200);EditorTests(editor);File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);}catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);code=1;}finally{SetField(editor,"dirty",false);((LivePreview)Field(editor,"live")).Dispose();editor.Close();app.Shutdown(code);}};
  editor.Show();app.Run();
 }
}
