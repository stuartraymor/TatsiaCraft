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
public static class AudioPointsTest {
 static int passed; static string root,log; static readonly JavaScriptSerializer json=new JavaScriptSerializer();
 static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object Field(Editor e,string name){return typeof(Editor).GetField(name,flags).GetValue(e);}
 static void SetField(Editor e,string name,object value){typeof(Editor).GetField(name,flags).SetValue(e,value);}
 static object Invoke(Editor e,string name,params object[] args){return typeof(Editor).GetMethod(name,flags).Invoke(e,args);}
 static Project P(Editor e){return (Project)Field(e,"project");}
 static int Undos(Editor e){return ((Stack<string>)Field(e,"undo")).Count;}
 static void Require(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static void Near(double a,double b,string label,double tolerance=1e-6){Require(Math.Abs(a-b)<tolerance,label+" ("+a+" vs "+b+")");}
 static void Reject(Action a,string label){bool rejected=false;try{a();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}catch(InvalidDataException){rejected=true;}Require(rejected,label);}
 static Project Clone(Project p){return json.Deserialize<Project>(json.Serialize(p));}
 static IEnumerable<DependencyObject> Tree(DependencyObject node){yield return node;int n=VisualTreeHelper.GetChildrenCount(node);for(int i=0;i<n;i++)foreach(var d in Tree(VisualTreeHelper.GetChild(node,i)))yield return d;}
 static double Rms(string file,double from,double length){string raw=Path.Combine(root,"window.pcm");Engine.Run("ffmpeg","-y -v error -ss "+from.ToString(System.Globalization.CultureInfo.InvariantCulture)+" -t "+length.ToString(System.Globalization.CultureInfo.InvariantCulture)+" -i "+Engine.Quote(file)+" -vn -ac 1 -ar 48000 -f s16le "+Engine.Quote(raw));byte[] b=File.ReadAllBytes(raw);double sum=0;for(int i=0;i+1<b.Length;i+=2){double v=BitConverter.ToInt16(b,i)/32768.0;sum+=v*v;}return Math.Sqrt(sum/Math.Max(1,b.Length/2));}
 static Media tone;
 static Project ToneProject(){var p=new Project{Width=320,Height=180,TrackCount=2};p.Media.Add(tone);p.Clips.Add(new Clip{Id="A",MediaId=tone.Id,Duration=3});return p;}

 static void ModelTests(){
  var p=ToneProject();var c=p.Clips[0];Near(AudioEnvelope.Level(c,1),1,"no points means full level");
  AudioEnvelope.Add(c,1,1);AudioEnvelope.Add(c,2,0);Near(AudioEnvelope.Level(c,1.5),.5,"level interpolates linearly between points");Near(AudioEnvelope.Level(c,.2),1,"level holds before the first point");Near(AudioEnvelope.Level(c,2.8),0,"level holds after the last point");
  AudioEnvelope.Add(c,2,.25);Require(AudioEnvelope.Points(c).Count==2&&AudioEnvelope.Points(c)[1].Level==.25,"adding at an existing time replaces the point");
  AudioEnvelope.Add(c,5,2);Near(AudioEnvelope.Points(c).Last().Time,3,"added point clamps to the clip end");Near(AudioEnvelope.Points(c).Last().Level,1,"added level clamps to 100%");AudioEnvelope.Remove(c,2);
  AudioEnvelope.Move(c,0,2.5,-1);Require(AudioEnvelope.Points(c)[0].Time<AudioEnvelope.Points(c)[1].Time,"moving a point cannot pass its neighbour");Near(AudioEnvelope.Points(c)[0].Level,0,"moved level clamps to 0%");
  Reject(()=>AudioEnvelope.Move(c,9,1,1),"moving an unknown point is rejected");Require(AudioEnvelope.Remove(c,0)&&!AudioEnvelope.Remove(c,9),"remove reports whether a point was removed");
  Engine.Validate(p,false);Require(true,"enveloped project validates");
  Action<Action<Project>,string> bad=(change,label)=>{var q=Clone(p);change(q);Reject(()=>Engine.Validate(q,false),label);};
  bad(q=>q.Clips[0].AudioPoints[0].Level=1.5,"level above 100% rejected");bad(q=>q.Clips[0].AudioPoints[0].Time=double.NaN,"NaN point time rejected");bad(q=>q.Clips[0].AudioPoints.Add(new AudioPoint{Time=0,Level=1}),"out-of-order points rejected");bad(q=>q.Clips.Add(new Clip{Text="T",Duration=1,AudioPoints=new List<AudioPoint>{new AudioPoint{Time=0,Level=0}}}),"audio points on text rejected");bad(q=>q.Clips[0].AudioPoints.Add(null),"null point rejected");
  string path=Path.Combine(root,"points.tasiaproj");Engine.Save(p,path);Require(json.Serialize(Engine.Load(path).Clips[0].AudioPoints)==json.Serialize(c.AudioPoints),"audio points save and reopen exactly");
  var legacy=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(path));foreach(Dictionary<string,object> clip in (System.Collections.ArrayList)legacy["Clips"])clip.Remove("AudioPoints");File.WriteAllText(path,json.Serialize(legacy));Require(Engine.Load(path).Clips[0].AudioPoints.Count==0,"legacy clips load without audio points");
 }

 static void SourceTimeTests(){
  var p=ToneProject();var c=p.Clips[0];AudioEnvelope.Add(c,1,1);AudioEnvelope.Add(c,2,0);var levels=new[]{.5,1.25,1.75,2.5}.Select(t=>AudioEnvelope.Level(c,t)).ToArray();
  var right=RippleEditing.SplitAt(p,c,1.5);Require(new[]{.5,1.25}.Select(t=>AudioEnvelope.Level(c,t)).SequenceEqual(levels.Take(2))&&new[]{1.75,2.5}.Select(t=>AudioEnvelope.Level(right,t-1.5)).SequenceEqual(levels.Skip(2)),"split halves keep the same levels on the timeline");
  string before=json.Serialize(p.Clips[0].AudioPoints);Require(RippleEditing.Stitch(p,new[]{c,right})==1&&json.Serialize(c.AudioPoints)==before&&Math.Abs(c.Duration-3)<1e-9,"stitching enveloped halves restores the clip");
  c.Start+=.4;c.In+=.4;c.Duration-=.4;Near(AudioEnvelope.Level(c,1.6-.4),AudioEnvelope.LevelAtSource(c,1.6),"left trim keeps points attached to the source");Near(AudioEnvelope.Level(c,1.1),.5,"left-trimmed clip level matches source time");
  var fast=ToneProject().Clips[0];fast.Speed=2;fast.Duration=1;AudioEnvelope.Add(fast,.5,0);Near(AudioEnvelope.Points(fast)[0].Time,1,"points on a 2x clip are stored at source time");Near(AudioEnvelope.Level(fast,.25),0,"2x clip level uses source time");
  var s=ToneProject();var v=new Clip{Id="V",MediaId=s.Media[0].Id,Duration=1};s.Clips[0]=v;
  var inserted=ToneProject();AudioEnvelope.Add(inserted.Clips[0],0,1);AudioEnvelope.Add(inserted.Clips[0],2,0);RippleEditing.RippleInsert(inserted,1,1);var later=inserted.Clips.First(x=>x.Start>1.5);Near(AudioEnvelope.Level(later,.5),.25,"ripple insert keeps the envelope with the moved audio",1e-6);
 }

 static void RangeTests(){
  var c=ToneProject().Clips[0];Require(AudioEnvelope.SilenceRange(c,1,2),"range silence reports change");Near(AudioEnvelope.Level(c,1.5),0,"inside the range is silent");Near(AudioEnvelope.Level(c,.5),1,"before the range keeps full level");Near(AudioEnvelope.Level(c,2.5),1,"after the range keeps full level");Near(AudioEnvelope.Level(c,1),0,"range starts silent at its first frame");
  Require(AudioEnvelope.Points(c).Count==4,"range silence adds four points with ramps");
  var start=ToneProject().Clips[0];AudioEnvelope.SilenceRange(start,-1,1);Near(AudioEnvelope.Level(start,0),0,"range at clip start silences from the start");Require(AudioEnvelope.Points(start).Count==3,"range at clip start needs no lead-in point");
  var shaped=ToneProject().Clips[0];AudioEnvelope.Add(shaped,0,.5);AudioEnvelope.Add(shaped,3,.5);AudioEnvelope.SilenceRange(shaped,1,2);Near(AudioEnvelope.Level(shaped,.5),.5,"range silence preserves the existing level before");Near(AudioEnvelope.Level(shaped,2.5),.5,"range silence preserves the existing level after");
  Require(AudioEnvelope.RestoreRange(shaped,1,2),"range restore reports change");Near(AudioEnvelope.Level(shaped,1.5),.5,"range restore returns to the surrounding level");
  Require(AudioEnvelope.RestoreRange(c,1,2)&&AudioEnvelope.Points(c).Count==0,"restoring the only silenced range clears the envelope");Require(!AudioEnvelope.SilenceRange(c,2,2),"empty range does nothing");Require(!AudioEnvelope.RestoreRange(c,0,3),"restore with nothing to remove reports no change");
 }

 static void ExportTests(){
  var plain=ToneProject();string full=Path.Combine(root,"full.wav.mp4");Engine.Render(plain,full,null);double level=Rms(full,.2,.6);Require(level>.05,"fixture tone is audible");
  var env=ToneProject();AudioEnvelope.Add(env.Clips[0],1,1);AudioEnvelope.Add(env.Clips[0],2,0);string faded=Path.Combine(root,"envelope.mp4");Engine.Render(env,faded,null);
  Require(Math.Abs(Rms(faded,.2,.6)/level-1)<.05,"export keeps full level before the first point");Require(Rms(faded,2.2,.6)<.001,"export is silent after a 0% point");double mid=Rms(faded,1.45,.1)/level;Require(mid>.4&&mid<.6,"export level ramps linearly between points ("+mid.ToString("0.00")+")");
  var range=ToneProject();AudioEnvelope.SilenceRange(range.Clips[0],1,2);string silenced=Path.Combine(root,"range.mp4");Engine.Render(range,silenced,null);
  Require(Rms(silenced,1.1,.8)<.001,"range silence exports silence inside the range");Require(Math.Abs(Rms(silenced,.2,.6)/level-1)<.05&&Math.Abs(Rms(silenced,2.2,.6)/level-1)<.05,"range silence leaves audio outside the range");Require(Math.Abs(Engine.Probe(silenced).Duration-3)<.1,"range silence keeps duration");
  var offset=ToneProject();var c=offset.Clips[0];c.In=1;c.Duration=2;c.Start=.5;AudioEnvelope.Points(c).Add(new AudioPoint{Time=2,Level=0});AudioEnvelope.Points(c).Insert(0,new AudioPoint{Time=1.99,Level=1});string shifted=Path.Combine(root,"offset.mp4");Engine.Render(offset,shifted,null);Require(Math.Abs(Rms(shifted,.7,.6)/level-1)<.05&&Rms(shifted,1.7,.6)<.001,"export envelope follows source time with an In offset and start offset");
  var gain=ToneProject();gain.Clips[0].Volume=.5;AudioEnvelope.Add(gain.Clips[0],0,.5);string both=Path.Combine(root,"gain.mp4");Engine.Render(gain,both,null);Require(Math.Abs(Rms(both,.2,.6)/level-.25)<.03,"envelope level multiplies clip gain");
 }

 static void EditorTests(Editor editor){
  Invoke(editor,"HideHome");var p=ToneProject();p.Clips.Add(new Clip{Id="B",MediaId=tone.Id,Start=0,Duration=3,Track=1});p.Clips.Add(new Clip{Id="T",Text="Title",Start=0,Duration=3,Track=1});p.Clips[2].Start=3.5;p.Clips[2].Duration=1;SetField(editor,"project",p);Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();SetField(editor,"dirty",false);
  var a=P(editor).Clips[0];Require((bool)Invoke(editor,"AddAudioPoint",a,1.0,.5)&&AudioEnvelope.Points(a).Count==1&&Undos(editor)==1,"editor adds an audio point in one checkpoint");
  var timeline=(Canvas)Field(editor,"timeline");Require(Tree(timeline).OfType<FrameworkElement>().Any(x=>AutomationProperties.GetAutomationId(x)=="AudioPoints.A"),"audio line is drawn over audio clips");Require(Tree(timeline).OfType<FrameworkElement>().Any(x=>AutomationProperties.GetAutomationId(x)=="AudioPoint.A.0"),"audio point handle is drawn");Require(!Tree(timeline).OfType<FrameworkElement>().Any(x=>AutomationProperties.GetAutomationId(x)=="AudioPoints.T"),"text clips have no audio line");
  Invoke(editor,"DeleteAudioPoint",a,0);Require(AudioEnvelope.Points(a).Count==0&&Undos(editor)==2,"editor deletes an audio point in one checkpoint");Invoke(editor,"Undo");Require(AudioEnvelope.Points(P(editor).Clips[0]).Count==1,"undo restores the deleted point");a=P(editor).Clips[0];
  P(editor).LockedTracks.Add(0);Invoke(editor,"Refresh");int marks=Undos(editor);Require(!(bool)Invoke(editor,"AddAudioPoint",P(editor).Clips[0],2.0,.1)&&Undos(editor)==marks,"locked track blocks audio point edits");P(editor).LockedTracks.Clear();Invoke(editor,"Refresh");
  Invoke(editor,"SetRange",1.0,2.0);Near((double)Field(editor,"rangeStart"),1,"range start is set");Near((double)Field(editor,"rangeEnd"),2,"range end is set");Invoke(editor,"DeselectAll");Invoke(editor,"SetRange",1.0,2.0);marks=Undos(editor);Invoke(editor,"SilenceSelection",true);
  Require(AudioEnvelope.Level(P(editor).Clips.First(c=>c.Id=="A"),1.5)==0&&AudioEnvelope.Level(P(editor).Clips.First(c=>c.Id=="B"),1.5)==0&&Undos(editor)==marks+1,"Silence Audio with a range and no selection silences every overlapping audio clip in one checkpoint");
  Require(AudioEnvelope.Level(P(editor).Clips.First(c=>c.Id=="B"),.5)==1,"range silence leaves audio outside the range");
  Invoke(editor,"SetRange",1.0,2.0);Invoke(editor,"SelectClip",P(editor).Clips.First(c=>c.Id=="B"),false,false);Invoke(editor,"SetRange",1.0,2.0);Invoke(editor,"SilenceSelection",false);Require(AudioEnvelope.Level(P(editor).Clips.First(c=>c.Id=="B"),1.5)==1&&AudioEnvelope.Level(P(editor).Clips.First(c=>c.Id=="A"),1.5)==0,"Restore Audio with a range and a selection restores only the selected clip");
  Invoke(editor,"ClearRange");Require(!(bool)typeof(Editor).GetProperty("HasRange",flags).GetValue(editor,null),"clicking the timeline clears the range");
  Invoke(editor,"SelectClip",P(editor).Clips.First(c=>c.Id=="A"),false,false);Invoke(editor,"SilenceSelection",false);Require(AudioEnvelope.Points(P(editor).Clips.First(c=>c.Id=="A")).Count==0,"Restore Audio without a range clears the clip's audio points");
  Invoke(editor,"AddAudioPoint",P(editor).Clips.First(c=>c.Id=="A"),1.0,.3);Invoke(editor,"AddAudioPoint",P(editor).Clips.First(c=>c.Id=="B"),1.0,.3);Invoke(editor,"DeselectAll");marks=Undos(editor);Invoke(editor,"RemoveAllAudioPoints");Require(P(editor).Clips.All(c=>AudioEnvelope.Points(c).Count==0)&&Undos(editor)==marks+1,"Modify > Remove > All Audio Points clears every clip in one checkpoint");
  var menu=((Grid)Field(editor,"editorRoot")).Children.OfType<Menu>().Single();var modify=menu.Items.OfType<MenuItem>().First(i=>((string)i.Header).Replace("_","")=="Modify");var remove=modify.Items.OfType<MenuItem>().First(i=>(string)i.Header=="Remove");Require(remove.Items.OfType<MenuItem>().Any(i=>(string)i.Header=="All Audio Points"),"Modify > Remove > All Audio Points uses reference wording");
  Require(Tree(timeline).OfType<FrameworkElement>().Any(x=>AutomationProperties.GetAutomationId(x)=="Timeline.RangeStart")&&Tree(timeline).OfType<FrameworkElement>().Any(x=>AutomationProperties.GetAutomationId(x)=="Timeline.RangeEnd"),"green and red range handles are drawn on the playhead");
  Invoke(editor,"SetRange",2.0,1.0);Invoke(editor,"DrawTimeline");Near((double)Field(editor,"rangeStart"),1,"reversed range is ordered");Require(Tree(timeline).OfType<FrameworkElement>().Any(x=>AutomationProperties.GetAutomationId(x)=="Timeline.Range"),"selected range is shaded on the timeline");
  SetField(editor,"dirty",false);
 }

 [STAThread] public static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var editor=new Editor{Width=1389,Height=812,WindowStartupLocation=WindowStartupLocation.Manual,Left=-20000,Top=0,ShowInTaskbar=false};
  editor.Loaded+=async(s,e)=>{int code=0;try{string wav=Path.Combine(root,"tone.wav");Engine.Run("ffmpeg","-y -v error -f lavfi -i sine=frequency=440:sample_rate=48000:duration=4 "+Engine.Quote(wav));tone=Engine.Probe(wav);
   ModelTests();SourceTimeTests();RangeTests();ExportTests();await Task.Delay(200);EditorTests(editor);File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);}catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);code=1;}finally{SetField(editor,"dirty",false);((LivePreview)Field(editor,"live")).Dispose();editor.Close();app.Shutdown(code);}};
  editor.Show();app.Run();
 }
}
