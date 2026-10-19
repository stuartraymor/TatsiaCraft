using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using TasiaCraft;
public static class AudioStitchTest {
 static int passed; static string root,log; static readonly JavaScriptSerializer json=new JavaScriptSerializer();
 static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object Field(Editor e,string name){return typeof(Editor).GetField(name,flags).GetValue(e);}
 static void SetField(Editor e,string name,object value){typeof(Editor).GetField(name,flags).SetValue(e,value);}
 static object Invoke(Editor e,string name,params object[] args){return typeof(Editor).GetMethod(name,flags).Invoke(e,args);}
 static Project P(Editor e){return (Project)Field(e,"project");}
 static int Undos(Editor e){return ((Stack<string>)Field(e,"undo")).Count;}
 static void Require(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static void Near(double a,double b,string label){Require(Math.Abs(a-b)<1e-6,label+" ("+a+" vs "+b+")");}
 static void Reject(Action a,string label){bool rejected=false;try{a();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}catch(InvalidDataException){rejected=true;}Require(rejected,label);}
 static Project Clone(Project p){return json.Deserialize<Project>(json.Serialize(p));}
 static Clip Text(string id,double start,double duration,int track){return new Clip{Id=id,Text=id,Start=start,Duration=duration,Track=track};}
 static Clip C(Project p,string id){return p.Clips.First(c=>c.Id==id);}
 static double Rms(string file,double from=0,double length=0){string raw=Path.Combine(root,"audio.pcm");string range=length>0?" -ss "+from.ToString(System.Globalization.CultureInfo.InvariantCulture)+" -t "+length.ToString(System.Globalization.CultureInfo.InvariantCulture):"";Engine.Run("ffmpeg","-y -v error"+range+" -i "+Engine.Quote(file)+" -vn -ac 1 -ar 48000 -f s16le "+Engine.Quote(raw));byte[] b=File.ReadAllBytes(raw);double sum=0;for(int i=0;i+1<b.Length;i+=2){double v=BitConverter.ToInt16(b,i)/32768.0;sum+=v*v;}return Math.Sqrt(sum/Math.Max(1,b.Length/2));}
 static byte[] Frame(string file,double time){string raw=Path.Combine(root,"frame.rgb");Engine.Run("ffmpeg","-y -v error -ss "+time.ToString(System.Globalization.CultureInfo.InvariantCulture)+" -i "+Engine.Quote(file)+" -frames:v 1 -pix_fmt rgb24 -f rawvideo "+Engine.Quote(raw));return File.ReadAllBytes(raw);}
 static double Difference(byte[] a,byte[] b){double d=0;for(int i=0;i<a.Length;i++)d+=Math.Abs(a[i]-b[i]);return d/a.Length;}
 static Media av;

 static Project AvProject(){var p=new Project{Width=320,Height=180,TrackCount=3};p.Media.Add(av);p.Clips.Add(new Clip{Id="V",MediaId=av.Id,Duration=3,Volume=.7});p.Clips.Add(new Clip{Id="T",Text="Title",Duration=1,Track=1});return p;}

 static void SilenceTests(){
  var p=AvProject();var v=C(p,"V");Require(RippleEditing.HasPlayableAudio(p,v)&&!RippleEditing.HasPlayableAudio(p,C(p,"T")),"only clips with audio can be silenced");
  string loud=Path.Combine(root,"loud.mp4");Engine.Render(p,loud,null);double level=Rms(loud);Require(level>.01,"fixture export is audible");
  Require(RippleEditing.SetSilenced(p,new[]{v,C(p,"T")},true)==1,"Silence Audio changes the audio clip only");Require(v.Silenced&&v.Volume==.7,"Silence Audio keeps the clip gain");Require(RippleEditing.SetSilenced(p,new[]{v},true)==0,"silencing again reports no change");
  string quiet=Path.Combine(root,"quiet.mp4");Engine.Render(p,quiet,null);Require(Rms(quiet)<.0001,"silenced clip exports no audio");Require(Difference(Frame(loud,1),Frame(quiet,1))<2,"silenced clip keeps its picture");Require(Math.Abs(Engine.Probe(quiet).Duration-3)<.1,"silenced export keeps duration");
  Require(RippleEditing.SetSilenced(p,new[]{v},false)==1&&!v.Silenced,"Restore Audio clears silence");string back=Path.Combine(root,"restored.mp4");Engine.Render(p,back,null);Require(Math.Abs(Rms(back)-level)/level<.03,"restored clip exports at its original level");
  RippleEditing.SetSilenced(p,new[]{v},true);string path=Path.Combine(root,"silenced.tasiaproj");Engine.Save(p,path);Require(Engine.Load(path).Clips.First(c=>c.Id=="V").Silenced,"silence state saves and reopens");
  var legacy=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(path));foreach(Dictionary<string,object> clip in (System.Collections.ArrayList)legacy["Clips"])clip.Remove("Silenced");File.WriteAllText(path,json.Serialize(legacy));Require(Engine.Load(path).Clips.All(c=>!c.Silenced),"legacy clips load unsilenced");
  var separated=AvProject();var audio=RippleEditing.SeparateAudioVideo(separated,new[]{C(separated,"V")})[0];Require(RippleEditing.SetSilenced(separated,new[]{C(separated,"V")},true)==0,"video-only half has no audio to silence");Require(RippleEditing.SetSilenced(separated,new[]{audio},true)==1,"audio-only half can be silenced");
  var locked=AvProject();locked.LockedTracks.Add(0);string state=json.Serialize(locked);Reject(()=>RippleEditing.SetSilenced(locked,new[]{C(locked,"V")},true),"silencing on a locked track rejected");Require(json.Serialize(locked)==state,"locked silence is atomic");
 }

 static void StitchTests(){
  var p=AvProject();string original=json.Serialize(C(p,"V"));var right=RippleEditing.SplitAt(p,C(p,"V"),1.2);Require(right!=null&&p.Clips.Count==3,"split creates a second clip");Near(right.In,1.2,"split keeps source continuity");
  string split=Path.Combine(root,"split.mp4");Engine.Render(p,split,null);
  Require(RippleEditing.Stitch(p,new[]{C(p,"V"),right})==1,"stitch joins two continuous clips");Require(p.Clips.Count==2&&json.Serialize(C(p,"V"))==original,"stitched clip equals the original clip exactly");
  string stitched=Path.Combine(root,"stitched.mp4");Engine.Render(p,stitched,null);foreach(double t in new[]{.5,1.5,2.5})Require(Difference(Frame(split,t),Frame(stitched,t))<2,"stitched export matches split export at "+t+" s");Require(Math.Abs(Rms(split)-Rms(stitched))<.005,"stitched export audio matches");
  p=AvProject();var b=RippleEditing.SplitAt(p,C(p,"V"),1);var c=RippleEditing.SplitAt(p,b,2);Require(RippleEditing.Stitch(p,new[]{c,C(p,"V"),b})==2&&json.Serialize(C(p,"V"))==original,"three pieces stitch back into one in any selection order");
  p=AvProject();b=RippleEditing.SplitAt(p,C(p,"V"),1);b.Start+=.5;Require(RippleEditing.Stitch(p,new[]{C(p,"V"),b})==0,"clips with a gap are not stitched");
  p=AvProject();b=RippleEditing.SplitAt(p,C(p,"V"),1);b.In+=.3;Require(RippleEditing.Stitch(p,new[]{C(p,"V"),b})==0,"clips that skip source time are not stitched");
  p=AvProject();b=RippleEditing.SplitAt(p,C(p,"V"),1);b.Volume=.2;Require(RippleEditing.Stitch(p,new[]{C(p,"V"),b})==0,"clips with different settings are not stitched");
  p=AvProject();b=RippleEditing.SplitAt(p,C(p,"V"),1);b.Track=2;Require(RippleEditing.Stitch(p,new[]{C(p,"V"),b})==0,"clips on different tracks are not stitched");
  p=AvProject();b=RippleEditing.SplitAt(p,C(p,"V"),1);Require(RippleEditing.Stitch(p,new[]{C(p,"V")})==0,"a single selected piece does not stitch with an unselected neighbour");
  p=AvProject();b=RippleEditing.SplitAt(p,C(p,"V"),1);p.LockedTracks.Add(0);string state=json.Serialize(p);Reject(()=>RippleEditing.Stitch(p,new[]{C(p,"V"),b}),"stitch on a locked track rejected");Require(json.Serialize(p)==state,"locked stitch is atomic");
  p=AvProject();ClipGroups.Create(p,new[]{C(p,"V"),C(p,"T")},"Group1");b=RippleEditing.SplitAt(p,C(p,"V"),1);Require(p.Groups[0].ClipIds.Contains(b.Id),"split piece joins the group");Require(RippleEditing.Stitch(p,new[]{C(p,"V"),b})==1&&p.Groups[0].ClipIds.Count==2&&!p.Groups[0].ClipIds.Contains(b.Id),"stitch removes the joined piece from its group");Engine.Validate(p,false);Require(true,"stitched grouped project validates");
 }

 static void InsertTests(){
  var p=new Project{Width=320,Height=180,TrackCount=3};p.Clips.Add(Text("A",0,2,0));p.Clips.Add(Text("B",2,1,0));p.Clips.Add(Text("D",1,3,1));p.Clips.Add(Text("E",6,1,2));p.LockedTracks.Add(2);TimelineMarkers.Add(p,2.5,"Later");TimelineMarkers.Add(p,1,"Earlier");
  RippleEditing.RippleInsert(p,1.5,1);var a2=p.Clips.First(c=>c.Id!="A"&&c.Track==0&&c.Text=="A");
  Near(C(p,"A").Duration,1.5,"clip crossing the insert point is split");Near(a2.Start,2.5,"right part of split clip moves after the inserted time");Near(a2.Duration,.5,"right part keeps remaining length");
  Near(C(p,"B").Start,3,"later clip moves right by inserted time");var d2=p.Clips.First(c=>c.Id!="D"&&c.Text=="D");Near(C(p,"D").Duration,.5,"crossing clip on another track is split too");Near(d2.Start,2.5,"other track right part moves");
  Near(C(p,"E").Start,6,"locked track is not moved");Require(p.Markers.First(m=>m.Name=="Later").Time==3.5&&p.Markers.First(m=>m.Name=="Earlier").Time==1,"markers after the insert point move; earlier markers stay");
  Engine.Validate(p,false);Require(true,"ripple-inserted project validates");
  p=new Project{TrackCount=2};p.Clips.Add(Text("A",0,2,0));p.Clips.Add(Text("B",3,1,1));RippleEditing.RippleInsert(p,0,2);Near(C(p,"A").Start,2,"insert at zero moves clips starting at zero");Near(C(p,"B").Start,5,"insert at zero moves every clip");Require(p.Clips.Count==2,"insert at a clip edge does not split");
  Reject(()=>RippleEditing.RippleInsert(p,1,0),"zero-length insert rejected");Reject(()=>RippleEditing.RippleInsert(p,1,-1),"negative insert rejected");Reject(()=>RippleEditing.RippleInsert(p,double.NaN,1),"NaN insert point rejected");
  var v=AvProject();string before=Path.Combine(root,"pre-insert.mp4");Engine.Render(v,before,null);RippleEditing.RippleInsert(v,1,1);string after=Path.Combine(root,"post-insert.mp4");Engine.Render(v,after,null);
  Require(Math.Abs(Engine.Probe(after).Duration-4)<.1,"insert lengthens the export");Require(Difference(Frame(before,.5),Frame(after,.5))<2,"picture before the insert point is unchanged");Require(Difference(Frame(before,1.5),Frame(after,2.5))<3,"picture after the insert point is delayed by the inserted time");
  Require(Rms(after,1.1,.8)<.001,"inserted time is silent");Require(Rms(after,2.1,.8)>.01,"audio resumes after the inserted time");
 }

 static void EditorTests(Editor editor){
  Invoke(editor,"HideHome");SetField(editor,"project",AvProject());Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();SetField(editor,"dirty",false);
  Invoke(editor,"SelectClip",C(P(editor),"V"),false,false);Invoke(editor,"SilenceSelection",true);Require(C(P(editor),"V").Silenced&&Undos(editor)==1,"editor Silence Audio is one checkpoint");Invoke(editor,"SilenceSelection",true);Require(Undos(editor)==1,"editor repeat Silence adds no checkpoint");
  Invoke(editor,"SelectClip",C(P(editor),"V"),false,false);Invoke(editor,"SilenceSelection",false);Require(!C(P(editor),"V").Silenced&&Undos(editor)==2,"editor Restore Audio is one checkpoint");Invoke(editor,"Undo");Require(C(P(editor),"V").Silenced,"undo returns to silenced state");
  SetField(editor,"project",AvProject());Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();var right=RippleEditing.SplitAt(P(editor),C(P(editor),"V"),1.5);Invoke(editor,"Refresh");
  Invoke(editor,"SelectClip",C(P(editor),"V"),false,false);Invoke(editor,"SelectClip",right,true,false);Invoke(editor,"StitchSelection");Require(P(editor).Clips.Count==2&&Math.Abs(C(P(editor),"V").Duration-3)<1e-9&&Undos(editor)==1,"editor Stitch Selected Media rejoins with one checkpoint");
  Invoke(editor,"Undo");Require(P(editor).Clips.Count==3,"undo restores the split pieces");
  int undoBefore=Undos(editor);Invoke(editor,"SelectClip",C(P(editor),"T"),false,false);Invoke(editor,"SelectClip",C(P(editor),"V"),true,false);Invoke(editor,"StitchSelection");Require(Undos(editor)==undoBefore,"editor stitch with nothing to join adds no checkpoint");
  SetField(editor,"project",AvProject());Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();Require((bool)Invoke(editor,"RippleInsertAt",1.0,.5)&&P(editor).Clips.Count==3&&Undos(editor)==1,"editor Ripple Insert splits and inserts in one checkpoint");Invoke(editor,"Undo");Require(P(editor).Clips.Count==2&&Math.Abs(C(P(editor),"V").Duration-3)<1e-9,"undo removes the inserted time");
  P(editor).Clips[0].Start=86399;P(editor).Clips[0].Duration=.5;string state=json.Serialize(P(editor));int undoMark=Undos(editor);Require(!(bool)Invoke(editor,"RippleInsertAt",86398.0,10.0)&&json.Serialize(P(editor))==state&&Undos(editor)==undoMark,"Ripple Insert past the 24-hour limit is rejected atomically");Require(!(bool)Invoke(editor,"RippleInsertAt",86399.9,1.0)&&Undos(editor)==undoMark,"Ripple Insert after all media makes no change or checkpoint");
  var menu=((Grid)Field(editor,"editorRoot")).Children.OfType<Menu>().Single();var edit=menu.Items.OfType<MenuItem>().First(i=>((string)i.Header).Replace("_","")=="Edit");var labels=edit.Items.OfType<MenuItem>().Select(i=>(string)i.Header).ToList();
  Require(labels.IndexOf("Stitch Selected Media")==labels.IndexOf("Split Selected")+1&&labels.IndexOf("Separate Audio and Video")==labels.IndexOf("Stitch Selected Media")+1,"Edit menu orders Split, Stitch, Separate as in the reference");Require(edit.Items.OfType<MenuItem>().First(i=>(string)i.Header=="Stitch Selected Media").InputGestureText=="Ctrl+Alt+I","Stitch shows Ctrl+Alt+I");
  SetField(editor,"dirty",false);
 }

 [STAThread] public static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var editor=new Editor{Width=1389,Height=812,WindowStartupLocation=WindowStartupLocation.Manual,Left=-20000,Top=0,ShowInTaskbar=false};
  editor.Loaded+=async(s,e)=>{int code=0;try{string video=Path.Combine(root,"av.mp4");Engine.Run("ffmpeg","-y -v error -f lavfi -i testsrc2=size=320x180:rate=30 -f lavfi -i sine=frequency=440:sample_rate=48000 -t 3 -c:v libopenh264 -c:a aac "+Engine.Quote(video));av=Engine.Probe(video);
   SilenceTests();StitchTests();InsertTests();await Task.Delay(200);EditorTests(editor);File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);}catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);code=1;}finally{SetField(editor,"dirty",false);((LivePreview)Field(editor,"live")).Dispose();editor.Close();app.Shutdown(code);}};
  editor.Show();app.Run();
 }
}
