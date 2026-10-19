using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;
using TasiaCraft;
public static class NarrationTest {
 static int passed; static string root,log; static readonly JavaScriptSerializer json=new JavaScriptSerializer();
 static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object Field(Editor e,string name){return typeof(Editor).GetField(name,flags).GetValue(e);}
 static void SetField(Editor e,string name,object value){typeof(Editor).GetField(name,flags).SetValue(e,value);}
 static object Invoke(Editor e,string name,params object[] args){return typeof(Editor).GetMethod(name,flags).Invoke(e,args);}
 static Project P(Editor e){return (Project)Field(e,"project");}
 static int Undos(Editor e){return ((Stack<string>)Field(e,"undo")).Count;}
 static void Require(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static void Near(double a,double b,string label,double tolerance){Require(Math.Abs(a-b)<tolerance,label+" ("+a.ToString("0.###")+" vs "+b.ToString("0.###")+")");}
 static IEnumerable<DependencyObject> Tree(DependencyObject node){yield return node;int n=VisualTreeHelper.GetChildrenCount(node);for(int i=0;i<n;i++)foreach(var d in Tree(VisualTreeHelper.GetChild(node,i)))yield return d;}
 const string Tone="-re -f lavfi -i sine=frequency=440:sample_rate=48000";
 static double Rms(string file,double from=0,double length=0){string raw=Path.Combine(root,"pcm.raw");string range=length>0?" -ss "+from.ToString(System.Globalization.CultureInfo.InvariantCulture)+" -t "+length.ToString(System.Globalization.CultureInfo.InvariantCulture):"";Engine.Run("ffmpeg","-y -v error"+range+" -i "+Engine.Quote(file)+" -vn -ac 1 -ar 48000 -f s16le "+Engine.Quote(raw));byte[] b=File.ReadAllBytes(raw);double sum=0;for(int i=0;i+1<b.Length;i+=2){double v=BitConverter.ToInt16(b,i)/32768.0;sum+=v*v;}return Math.Sqrt(sum/Math.Max(1,b.Length/2));}

 static async Task CaptureTests(){
  string listing="[in#0] \"USB2.0 FHD UVC WebCam\" (video)\n[in#0]   Alternative name \"@device_pnp_x\"\n[in#0] \"Microphone Array (Realtek(R) Audio)\" (audio)\n[in#0] \"audio-capture-device\" (audio)\n[in#0] \"Microphone Array (Realtek(R) Audio)\" (audio)";
  Require(NarrationDevices.Parse(listing).SequenceEqual(new[]{"Microphone Array (Realtek(R) Audio)","audio-capture-device"}),"microphone list keeps audio devices once, in order");Require(NarrationDevices.Parse(null).Count==0,"empty listing has no microphones");
  Require(NarrationDevices.InputArgs("Mic (A)")=="-f dshow -audio_buffer_size 50 -i \"audio=Mic (A)\"","microphone input is quoted for DirectShow");
  string full=Path.Combine(root,"full.wav");var levels=new List<double>();var capture=new NarrationCapture(Tone,full,1);capture.Level+=db=>{lock(levels)levels.Add(db);};var clock=System.Diagnostics.Stopwatch.StartNew();await capture.StartAsync();Require(capture.IsReady,"capture reports ready once audio flows");await Task.Delay(1500);double elapsed=clock.Elapsed.TotalSeconds;await capture.StopAsync();capture.Dispose();
  Require(levels.Count>=8,"level meter receives readings about every 100 ms ("+levels.Count+")");double typical;lock(levels)typical=levels.Skip(2).Average();Require(typical>-30&&typical<-12,"meter reads the tone level in dBFS ("+typical.ToString("0.0")+")");
  var media=Engine.Probe(full);Require(media.Kind=="audio"&&media.HasAudio,"recording is an audio file");Near(media.Duration,elapsed,"recording length matches real time",.6);
  string half=Path.Combine(root,"half.wav");capture=new NarrationCapture(Tone,half,.5);await capture.StartAsync();await Task.Delay(800);await capture.StopAsync();capture.Dispose();Near(Rms(half)/Rms(full),.5,"input level slider scales the recording",.05);
  var monitor=new NarrationCapture(Tone,null,1);int monitorLevels=0;monitor.Level+=db=>monitorLevels++;await monitor.StartAsync();await Task.Delay(500);monitor.Dispose();Require(monitorLevels>2,"monitor shows levels without recording");
  bool rejected=false;try{new NarrationCapture(Tone,null,-1);}catch(ArgumentException){rejected=true;}Require(rejected,"negative input level rejected");
  var broken=new NarrationCapture("-f dshow -i \"audio=No Such Microphone 123\"",Path.Combine(root,"none.wav"),1);bool failed=false;try{await broken.StartAsync();}catch(InvalidOperationException){failed=true;}Require(failed,"a missing microphone fails to start with an error");
 }

 static async Task EditorTests(Editor editor){
  Invoke(editor,"HideHome");var p=new Project{Width=320,Height=180,TrackCount=2};p.Clips.Add(new Clip{Id="T",Text="Slide",Duration=4});SetField(editor,"project",p);Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();SetField(editor,"dirty",false);SetField(editor,"narrationInputOverride",Tone);
  var nav=((string[])Field(editor,"NavigationTools")).ToList();Require(nav.IndexOf("Narration")==nav.IndexOf("Audio Effects")+1&&nav.IndexOf("Captions")==nav.IndexOf("Narration")+1,"Narration sits between Audio Effects and Captions, as in the reference");
  Invoke(editor,"Panel","Narration");var library=(StackPanel)Field(editor,"library");var ids=Tree(library).OfType<FrameworkElement>().Select(AutomationProperties.GetAutomationId).ToList();
  Require(new[]{"Narration.Device","Narration.Meter","Narration.Gain","Narration.Mute","Narration.Record","Narration.Script"}.All(ids.Contains),"panel has microphone, meter, level, mute, record and script controls");
  Require((string)((Button)Tree(library).OfType<Button>().First(b=>AutomationProperties.GetAutomationId(b)=="Narration.Record")).Content=="Start Voice Recording","record button uses reference wording");Require((string)Tree(library).OfType<CheckBox>().First().Content=="Mute timeline during recording"&&Tree(library).OfType<CheckBox>().First().IsChecked==true,"mute option uses reference wording and is on by default");Require(Tree(library).OfType<TextBlock>().Any(t=>t.Text=="Paste or type script text here"),"script box shows the reference placeholder");
  var script=Tree(library).OfType<TextBox>().First(b=>AutomationProperties.GetAutomationId(b)=="Narration.Script");script.Text="Welcome to the tour.";Require(P(editor).NarrationScript=="Welcome to the tour."&&(bool)Field(editor,"dirty"),"typing a script stores it in the project");
  string path=Path.Combine(root,"script.tasiaproj");Engine.Save(P(editor),path);Require(Engine.Load(path).NarrationScript=="Welcome to the tour.","script saves and reopens");var big=new Project{NarrationScript=new string('x',100001)};bool tooLong=false;try{Engine.Validate(big,false);}catch(InvalidDataException){tooLong=true;}Require(tooLong,"over-long script rejected");
  SetField(editor,"dirty",false);
  Invoke(editor,"Seek",1.0);var live=(LivePreview)Field(editor,"live");Require(await (Task<bool>)Invoke(editor,"StartNarration"),"Start Voice Recording begins recording");Require(live.Muted,"timeline is muted while recording");Require((bool)Field(editor,"playing"),"timeline plays while recording");
  Require(Field(editor,"narrationRecording")!=null&&!(await (Task<bool>)Invoke(editor,"StartNarration")),"a second start is ignored while recording");
  await Task.Delay(1500);int tracks=P(editor).TrackCount;Require(await (Task<bool>)Invoke(editor,"StopNarration"),"Stop Voice Recording finishes the take");Require(!live.Muted&&!(bool)Field(editor,"playing"),"stopping unmutes and pauses the timeline");
  var take=P(editor).Clips.Last();var media=P(editor).Media.First(m=>m.Id==take.MediaId);Require(P(editor).TrackCount==tracks+1&&take.Track==P(editor).TrackCount-1,"the take goes on a new top track");Near(take.Start,1,"the take starts where the playhead was",1e-9);Near(take.In,NarrationCapture.Window,"the take skips the 100 ms lead-in",1e-9);Near(take.Duration,media.Duration-NarrationCapture.Window,"the take uses the rest of the recording",1e-6);Require(media.Kind=="audio"&&take.Duration>1,"the take is an audio clip over one second long");
  Require(Undos(editor)==1&&((List<Clip>)Invoke(editor,"SelectedClips")).Single()==take,"placing the take is one checkpoint and selects it");Engine.Validate(P(editor),true);Require(true,"project with narration validates");
  string video=Path.Combine(root,"narration.mp4");Engine.Render(P(editor),video,null);Require(Rms(video,1.4,.8)>.01,"exported video contains the narration after its start");Require(Rms(video,.1,.7)<.001,"export is silent before the narration start");
  Invoke(editor,"Undo");Require(P(editor).TrackCount==tracks&&P(editor).Clips.Count==1,"undo removes the take and its track");Invoke(editor,"Redo");Require(P(editor).Clips.Count==2,"redo restores the take");
  SetField(editor,"narrationMute",false);Require(await (Task<bool>)Invoke(editor,"StartNarration"),"recording again works");Require(!live.Muted,"timeline stays audible when mute is off");await Task.Delay(500);await (Task<bool>)Invoke(editor,"StopNarration");
  Invoke(editor,"Panel","Media");Require(Field(editor,"narrationMonitor")==null,"leaving the Narration tool stops the microphone monitor");
  SetField(editor,"dirty",false);
 }

 [STAThread] public static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var editor=new Editor{Width=1389,Height=812,WindowStartupLocation=WindowStartupLocation.Manual,Left=-20000,Top=0,ShowInTaskbar=false};
  editor.Loaded+=async(s,e)=>{int code=0;try{await CaptureTests();await Task.Delay(200);await EditorTests(editor);File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);}catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);code=1;}finally{SetField(editor,"dirty",false);Invoke(editor,"StopNarrationMonitor");((LivePreview)Field(editor,"live")).Dispose();editor.Close();app.Shutdown(code);}};
  editor.Show();app.Run();
 }
}
