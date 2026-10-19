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
using System.Windows.Media.Imaging;
using TasiaCraft;
public static class RippleTest {
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
 static Clip Text(string name,double start,double duration,int track){return new Clip{Id=name,Text=name,Start=start,Duration=duration,Track=track};}
 static Clip C(Project p,string id){return p.Clips.First(c=>c.Id==id);}
 static double Rms(string file){string raw=Path.Combine(root,"audio.pcm");Engine.Run("ffmpeg","-y -v error -i "+Engine.Quote(file)+" -vn -ac 1 -ar 48000 -f s16le "+Engine.Quote(raw));byte[] b=File.ReadAllBytes(raw);double sum=0;for(int i=0;i+1<b.Length;i+=2){double v=BitConverter.ToInt16(b,i)/32768.0;sum+=v*v;}return Math.Sqrt(sum/Math.Max(1,b.Length/2));}
 static byte[] Frame(string file,double time){string raw=Path.Combine(root,"frame.rgb");Engine.Run("ffmpeg","-y -v error -ss "+time.ToString(System.Globalization.CultureInfo.InvariantCulture)+" -i "+Engine.Quote(file)+" -frames:v 1 -pix_fmt rgb24 -f rawvideo "+Engine.Quote(raw));return File.ReadAllBytes(raw);}
 static double Difference(byte[] a,byte[] b){double d=0;for(int i=0;i<a.Length;i++)d+=Math.Abs(a[i]-b[i]);return d/a.Length;}
 static byte[] Preview(LivePreview preview,Project p,double time){preview.SetProject(p);preview.Seek(time);preview.UpdateLayout();var bitmap=new RenderTargetBitmap(p.Width,p.Height,96,96,PixelFormats.Pbgra32);bitmap.Render(preview.Stage);var pixels=new byte[p.Width*p.Height*4];bitmap.CopyPixels(pixels,p.Width*4,0);return pixels;}

 static Project Timeline(){var p=new Project{Width=320,Height=180,TrackCount=3};p.Clips.Add(Text("A",0,2,0));p.Clips.Add(Text("B",2,1,0));p.Clips.Add(Text("C",5,1,0));p.Clips.Add(Text("D",1,3,1));p.Clips.Add(Text("E",6,1,2));return p;}

 static void RippleDeleteTests(){
  var p=Timeline();Require(RippleEditing.RippleDelete(p,new[]{C(p,"B")}),"ripple delete reports change");Require(!p.Clips.Any(c=>c.Id=="B"),"ripple delete removes the clip");Near(C(p,"C").Start,4,"later clip on same track moves left by the deleted length");Near(C(p,"A").Start,0,"earlier clip on same track is unchanged");Near(C(p,"D").Start,1,"clip on another track is unchanged");Near(C(p,"E").Start,6,"later clip on another track is unchanged");
  p=Timeline();RippleEditing.RippleDelete(p,new[]{C(p,"A"),C(p,"C")});Near(C(p,"B").Start,0,"deleting earlier media closes its gap");Require(p.Clips.Count(c=>c.Track==0)==1,"multiple deletions on one track");
  p=Timeline();RippleEditing.RippleDelete(p,new[]{C(p,"B"),C(p,"D")});Near(C(p,"C").Start,4,"multi-track ripple closes track 1 gap");Near(C(p,"E").Start,6,"untouched track keeps timing with multi-track ripple");
  p=Timeline();p.Clips.Add(Text("F",2.5,2,0));RippleEditing.RippleDelete(p,new[]{C(p,"B")});Near(C(p,"F").Start,2,"overlapping clip starting inside the gap moves only by the removed part");Near(C(p,"C").Start,4,"clip after overlap moves by the full deleted length");
  p=Timeline();p.Clips.Add(Text("G",2.5,1,0));RippleEditing.RippleDelete(p,new[]{C(p,"B"),C(p,"G")});Near(C(p,"C").Start,3.5,"overlapping deleted clips close their union only");
  p=Timeline();p.LockedTracks.Add(0);string locked=json.Serialize(p);Reject(()=>RippleEditing.RippleDelete(p,new[]{C(p,"B")}),"ripple delete on locked track rejected");Require(json.Serialize(p)==locked,"locked ripple delete is atomic");
  p=Timeline();Reject(()=>RippleEditing.RippleDelete(p,new[]{Text("X",0,1,0)}),"unknown clip rejected");Require(!RippleEditing.RippleDelete(p,new Clip[0]),"empty selection reports no change");
  p=Timeline();ClipGroups.Create(p,new[]{C(p,"B"),C(p,"D")},"Group1");RippleEditing.RippleDelete(p,new[]{C(p,"B")});Require(p.Groups.Count==0,"ripple delete prunes a group left with one member");Engine.Validate(p,false);Require(true,"ripple-deleted project validates");
  p=Timeline();TimelineMarkers.Add(p,5,"Kept");RippleEditing.RippleDelete(p,new[]{C(p,"B")});Near(p.Markers[0].Time,5,"markers keep their time on ripple delete");
 }

 static void RippleMoveTests(){
  var p=Timeline();var unit=RippleEditing.RippleFollowers(p,new[]{C(p,"B")});Require(unit.Select(c=>c.Id).OrderBy(x=>x).SequenceEqual(new[]{"B","C"}),"ripple move includes later media on the same track only");
  var move=new ClipMoveOperation(p,unit).EvaluateTimeline(1.5,0);move.Apply();Near(C(p,"B").Start,3.5,"ripple move moves the dragged clip");Near(C(p,"C").Start,6.5,"ripple move moves following media by the same amount");Near(C(p,"A").Start,0,"ripple move leaves earlier media");Near(C(p,"D").Start,1,"ripple move leaves other tracks");Require(C(p,"B").Track==0&&C(p,"C").Track==0,"ripple move keeps tracks");
  p=Timeline();move=new ClipMoveOperation(p,RippleEditing.RippleFollowers(p,new[]{C(p,"B")})).EvaluateTimeline(-10,0);move.Apply();Near(C(p,"B").Start,0,"ripple move left clamps at zero");Near(C(p,"C").Start,3,"ripple move left keeps the gap to following media");
  p=Timeline();ClipGroups.Create(p,new[]{C(p,"C"),C(p,"E")},"Group1");unit=RippleEditing.RippleFollowers(p,new[]{C(p,"B")});Require(unit.Any(c=>c.Id=="E"),"ripple move keeps grouped followers together");
  p=Timeline();unit=RippleEditing.RippleFollowers(p,new[]{C(p,"A"),C(p,"D")});Require(unit.Select(c=>c.Id).OrderBy(x=>x).SequenceEqual(new[]{"A","B","C","D"}),"multi-track ripple selection includes followers on each selected track");
  p=Timeline();unit=RippleEditing.RippleFollowers(p,new[]{C(p,"C")});Require(unit.Count==1,"last clip ripple move has no followers");
  p=Timeline();p.LockedTracks.Add(0);Reject(()=>new ClipMoveOperation(p,RippleEditing.RippleFollowers(p,new[]{C(p,"B")})),"ripple move on locked track rejected");
 }

 static void SeparateTests(LivePreview preview){
  string video=Path.Combine(root,"av.mp4");Engine.Run("ffmpeg","-y -v error -f lavfi -i testsrc2=size=320x180:rate=30 -f lavfi -i sine=frequency=440:sample_rate=48000 -t 3 -c:v libopenh264 -c:a aac "+Engine.Quote(video));var m=Engine.Probe(video);Require(m.Kind=="video"&&m.HasAudio,"fixture has video and audio");
  string red=Path.Combine(root,"red.png");Engine.Run("ffmpeg","-y -v error -f lavfi -i color=red:s=320x180 -frames:v 1 "+Engine.Quote(red));var image=Engine.Probe(red);
  var p=new Project{Width=320,Height=180,TrackCount=2};p.Media.Add(m);p.Media.Add(image);var v=new Clip{Id="V",MediaId=m.Id,Duration=3,Volume=.8,FadeIn=.2,AudioEffect="normalize",Scale=.9,X=10};var other=new Clip{Id="T",Text="Upper",Duration=2,Track=1};var still=new Clip{Id="I",MediaId=image.Id,Start=3,Duration=1};p.Clips.AddRange(new[]{v,other,still});p.HiddenTracks.Add(1);p.SnapDisabledTracks.Add(1);
  var original=Clone(p);original.HiddenTracks.Clear();string before=Path.Combine(root,"before.mp4");Engine.Render(original,before,null);
  Require(RippleEditing.CanSeparate(p,v)&&!RippleEditing.CanSeparate(p,other)&&!RippleEditing.CanSeparate(p,still),"only video clips with audio can be separated");
  var created=RippleEditing.SeparateAudioVideo(p,new[]{v,other,still});Require(created.Count==1,"one audio clip created");var a=created[0];
  Require(p.TrackCount==3,"a track is inserted");Require(v.Track==0&&a.Track==1&&other.Track==2,"audio goes on a new track directly above the source; higher tracks move up");Require(p.HiddenTracks.SequenceEqual(new[]{2})&&p.SnapDisabledTracks.SequenceEqual(new[]{2}),"track states move with their tracks");
  Require(v.Stream=="video"&&a.Stream=="audio","source becomes video-only and the new clip audio-only");Require(a.MediaId==v.MediaId&&a.Start==v.Start&&a.In==v.In&&a.Duration==v.Duration&&a.Speed==v.Speed,"audio clip keeps source timing");Require(a.Volume==.8&&a.FadeIn==.2&&a.AudioEffect=="normalize","audio clip keeps audio settings");Require(v.AudioEffect==null&&v.Scale==.9&&v.X==10,"video clip keeps visual settings and drops audio effect");Require(a.Id!=v.Id,"audio clip gets a new identity");
  Require(!RippleEditing.CanSeparate(p,v)&&!RippleEditing.CanSeparate(p,a),"separated clips cannot be separated again");Require(RippleEditing.SeparateAudioVideo(p,new[]{v}).Count==0,"separating again is a no-op");
  Engine.Validate(p,true);Require(true,"separated project validates");string path=Path.Combine(root,"separated.tasiaproj");Engine.Save(p,path);Require(json.Serialize(Engine.Load(path).Clips)==json.Serialize(p.Clips),"separated streams save and reopen");
  var legacy=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(path));foreach(Dictionary<string,object> clip in (System.Collections.ArrayList)legacy["Clips"])clip.Remove("Stream");File.WriteAllText(path,json.Serialize(legacy));Require(Engine.Load(path).Clips.All(c=>c.Stream==null),"legacy clips load with combined streams");
  Action<Action<Project>,string> bad=(change,label)=>{var q=Clone(p);change(q);Reject(()=>Engine.Validate(q,false),label);};
  bad(q=>q.Clips.First(c=>c.Id=="T").Stream="audio","stream on text clip rejected");bad(q=>q.Clips.First(c=>c.Id=="I").Stream="video","stream on image clip rejected");bad(q=>q.Clips.First(c=>c.Id=="V").Stream="both","unknown stream rejected");
  var visible=Clone(p);visible.HiddenTracks.Clear();string after=Path.Combine(root,"after.mp4");Engine.Render(visible,after,null);
  double rmsBefore=Rms(before),rmsAfter=Rms(after);Require(rmsBefore>.01&&Math.Abs(rmsAfter-rmsBefore)/rmsBefore<.05,"separated export audio level matches the combined clip");Require(Difference(Frame(before,1),Frame(after,1))<2,"separated export picture matches the combined clip");
  var noAudio=Clone(visible);noAudio.HiddenTracks.Add(1);string mutedPath=Path.Combine(root,"audio-hidden.mp4");Engine.Render(noAudio,mutedPath,null);Require(Rms(mutedPath)<.0001,"hiding the audio track silences the video clip");Require(Difference(Frame(before,1),Frame(mutedPath,1))<2,"hiding the audio track keeps the picture");
  var noVideo=Clone(visible);noVideo.HiddenTracks.Add(0);string blackPath=Path.Combine(root,"video-hidden.mp4");Engine.Render(noVideo,blackPath,null);Require(Math.Abs(Rms(blackPath)-rmsBefore)/rmsBefore<.05,"hiding the video track keeps separated audio");Require(Frame(blackPath,1).Take(320*3*40).All(x=>x<20),"hiding the video track removes the picture");
  var moved=Clone(visible);moved.Clips.First(c=>c.Stream=="audio").Start=1;string offset=Path.Combine(root,"audio-offset.mp4");Engine.Render(moved,offset,null);string head=Path.Combine(root,"head.mp4");Engine.Run("ffmpeg","-y -v error -i "+Engine.Quote(offset)+" -t 0.9 -vn -c:a pcm_s16le "+Engine.Quote(head+".wav"));Require(Rms(head+".wav")<.001,"separated audio moved later is silent before its new start");
  var p0=Preview(preview,visible,1);var p1=Preview(preview,original,1);Require(p0.Length==p1.Length,"preview renders both projects");double diff=0;for(int i=0;i<p0.Length;i++)diff+=Math.Abs(p0[i]-p1[i]);Require(diff/p0.Length<3,"separated preview picture matches the combined clip");
  var lockedProject=Clone(original);lockedProject.LockedTracks.Add(0);string lockedState=json.Serialize(lockedProject);Reject(()=>RippleEditing.SeparateAudioVideo(lockedProject,new[]{lockedProject.Clips[0]}),"separating on a locked track rejected");Require(json.Serialize(lockedProject)==lockedState,"locked separation is atomic");
  var full=Clone(original);full.TrackCount=64;Reject(()=>RippleEditing.SeparateAudioVideo(full,new[]{full.Clips[0]}),"separation beyond 64 tracks rejected");
  var grouped=Clone(original);grouped.HiddenTracks.Clear();ClipGroups.Create(grouped,new[]{grouped.Clips[0],grouped.Clips[1]},"Group1");var made=RippleEditing.SeparateAudioVideo(grouped,new[]{grouped.Clips[0]});Require(grouped.Groups[0].ClipIds.Contains(made[0].Id),"separated audio joins the source clip's group");Engine.Validate(grouped,false);Require(true,"grouped separation validates");
 }

 static void EditorTests(Editor editor){
  Invoke(editor,"HideHome");var p=Timeline();SetField(editor,"project",p);Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();SetField(editor,"dirty",false);
  Invoke(editor,"SelectClip",C(P(editor),"B"),false,false);Invoke(editor,"RippleDeleteSelection");Require(!P(editor).Clips.Any(c=>c.Id=="B")&&Math.Abs(C(P(editor),"C").Start-4)<1e-9,"editor Ripple Delete closes the gap");Require(Undos(editor)==1,"editor Ripple Delete is one checkpoint");
  Invoke(editor,"Undo");Require(P(editor).Clips.Any(c=>c.Id=="B")&&Math.Abs(C(P(editor),"C").Start-5)<1e-9,"undo restores ripple-deleted clip and timing");
  P(editor).LockedTracks.Add(0);Invoke(editor,"SelectClip",C(P(editor),"B"),false,false);int undoBefore=Undos(editor);string state=json.Serialize(P(editor));Invoke(editor,"RippleDeleteSelection");Require(json.Serialize(P(editor))==state&&Undos(editor)==undoBefore,"editor locked Ripple Delete makes no change or checkpoint");P(editor).LockedTracks.Clear();
  var menu=((Grid)Field(editor,"editorRoot")).Children.OfType<Menu>().Single();var edit=menu.Items.OfType<MenuItem>().First(i=>((string)i.Header).Replace("_","")=="Edit");var labels=edit.Items.OfType<MenuItem>().Select(i=>(string)i.Header).ToList();
  Require(labels.IndexOf("Ripple Delete")==labels.IndexOf("Delete")+1,"Edit menu lists Ripple Delete after Delete");Require(edit.Items.OfType<MenuItem>().First(i=>(string)i.Header=="Ripple Delete").InputGestureText=="Ctrl+Backspace","Ripple Delete shows Ctrl+Backspace");Require(labels.Contains("Separate Audio and Video"),"Edit menu lists Separate Audio and Video");
  string video=Path.Combine(root,"av.mp4");var m=Engine.Probe(video);var q=new Project{Width=320,Height=180,TrackCount=2};q.Media.Add(m);q.Clips.Add(new Clip{Id="V",MediaId=m.Id,Duration=3});q.Clips.Add(new Clip{Id="T",Text="Upper",Duration=1,Track=1});SetField(editor,"project",q);Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();
  Invoke(editor,"SelectClip",P(editor).Clips[0],false,false);Invoke(editor,"SeparateSelection");Require(P(editor).TrackCount==3&&P(editor).Clips.Count==3&&Undos(editor)==1,"editor Separate Audio and Video adds a track and clip in one checkpoint");
  var selected=(List<Clip>)Invoke(editor,"SelectedClips");Require(selected.Count==2&&selected.Any(c=>c.Stream=="audio")&&selected.Any(c=>c.Stream=="video"),"both separated halves are selected");
  Invoke(editor,"Undo");Require(P(editor).TrackCount==2&&P(editor).Clips.Count==2&&P(editor).Clips[0].Stream==null,"undo restores the combined clip and track count");
  Invoke(editor,"SelectClip",P(editor).Clips[1],false,false);Invoke(editor,"SeparateSelection");Require(P(editor).TrackCount==2&&Undos(editor)==0,"separating a text clip makes no change or checkpoint");
  SetField(editor,"dirty",false);
 }

 [STAThread] public static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var preview=new LivePreview();var host=new Window{Width=700,Height=460,Content=preview,Left=-20000,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.Manual,Title="TatsiaCraft isolated ripple acceptance"};
  host.Loaded+=async(s,e)=>{int code=0;Editor editor=null;try{RippleDeleteTests();RippleMoveTests();SeparateTests(preview);await Task.Delay(100);editor=new Editor{Width=1389,Height=812,WindowStartupLocation=WindowStartupLocation.Manual,Left=-20000,Top=0,ShowInTaskbar=false};editor.Show();await Task.Delay(300);EditorTests(editor);File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);}catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);code=1;}finally{if(editor!=null){SetField(editor,"dirty",false);((LivePreview)Field(editor,"live")).Dispose();editor.Close();}preview.Dispose();host.Close();app.Shutdown(code);}};
  host.Show();app.Run();
 }
}
