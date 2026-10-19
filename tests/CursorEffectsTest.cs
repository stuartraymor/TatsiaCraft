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
using System.Windows.Automation;
using TasiaCraft;
public static class CursorEffectsTest {
 static int passed; static string root,log; static readonly JavaScriptSerializer json=new JavaScriptSerializer();
 static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object Field(Editor e,string name){return typeof(Editor).GetField(name,flags).GetValue(e);}
 static void SetField(Editor e,string name,object value){typeof(Editor).GetField(name,flags).SetValue(e,value);}
 static object Invoke(Editor e,string name,params object[] args){return typeof(Editor).GetMethod(name,flags).Invoke(e,args);}
 static Project P(Editor e){return (Project)Field(e,"project");}
 static int Undos(Editor e){return ((Stack<string>)Field(e,"undo")).Count;}
 static void Require(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static void Near(double a,double b,string label,double tolerance){Require(Math.Abs(a-b)<tolerance,label+" ("+a.ToString("0.###")+" vs "+b.ToString("0.###")+")");}
 static void Reject(Action a,string label){bool rejected=false;try{a();}catch(ArgumentException){rejected=true;}catch(InvalidDataException){rejected=true;}Require(rejected,label);}
 static IEnumerable<DependencyObject> Tree(DependencyObject node){yield return node;int n=VisualTreeHelper.GetChildrenCount(node);for(int i=0;i<n;i++)foreach(var d in Tree(VisualTreeHelper.GetChild(node,i)))yield return d;}
 static Media screen; static string sidecar;
 static int[] Px(byte[] rgb,int x,int y){int i=(y*320+x)*3;return new int[]{rgb[i],rgb[i+1],rgb[i+2]};}
 static byte[] Export(Project p,string name,double t){string v=Path.Combine(root,name+".mp4");Engine.Render(p,v,null);string raw=Path.Combine(root,"f.rgb");Engine.Run("ffmpeg","-y -v error -ss "+t.ToString(System.Globalization.CultureInfo.InvariantCulture)+" -i "+Engine.Quote(v)+" -frames:v 1 -pix_fmt rgb24 -f rawvideo "+Engine.Quote(raw));return File.ReadAllBytes(raw);}
 static async Task<byte[]> Preview(LivePreview preview,Project p,double t){preview.SetProject(p);preview.Seek(t);for(int i=0;i<20;i++){await Task.Delay(100);preview.Seek(t);}preview.UpdateLayout();var bitmap=new RenderTargetBitmap(320,180,96,96,PixelFormats.Pbgra32);bitmap.Render(preview.Stage);var bgra=new byte[320*180*4];bitmap.CopyPixels(bgra,320*4,0);var rgb=new byte[320*180*3];for(int i=0;i<320*180;i++){rgb[i*3]=bgra[i*4+2];rgb[i*3+1]=bgra[i*4+1];rgb[i*3+2]=bgra[i*4];}return rgb;}
 static Project ScreenProject(params string[] effects){var p=new Project{Width=320,Height=180};p.Media.Add(screen);var c=new Clip{Id="S",MediaId=screen.Id,Duration=2};foreach(var e in effects)c.CursorEffects.Add(new CursorEffect{Name=e,Size=CursorEffects.DefaultSize(e)});p.Clips.Add(c);return p;}

 static void TrackTests(){
  var t=new CursorTrack{Width=320,Height=180};t.Samples.Add(new CursorSample{T=0,X=0,Y=0});t.Samples.Add(new CursorSample{T=1,X=100,Y=50});t.Samples.Add(new CursorSample{T=2,X=100,Y=50,Kind="hidden"});
  double x,y;string kind;Require(t.At(.5,0,out x,out y,out kind)&&Math.Abs(x-50)<1e-9&&Math.Abs(y-25)<1e-9,"cursor position interpolates between samples");Require(!t.At(2.5,0,out x,out y,out kind)&&kind=="hidden","hidden cursor reports not visible");
  var zig=new CursorTrack{Width=320,Height=180};for(int i=0;i<=60;i++)zig.Samples.Add(new CursorSample{T=i/30.0,X=100+(i%2==0?10:-10),Y=50});double raw,smooth;zig.At(1,0,out raw,out y,out kind);zig.At(1,.15,out smooth,out y,out kind);Require(Math.Abs(smooth-100)<Math.Abs(raw-100)/2,"Cursor Smoothing averages out jitter");
  string path=Path.Combine(root,"track.cursor.json");t.Save(path);var loaded=CursorTrack.Load(path);Require(loaded!=null&&loaded.Samples.Count==3&&loaded.Samples[2].Kind=="hidden","cursor data saves and loads");
  System.Threading.Thread.Sleep(20);t.Samples[1].X=90;t.Save(path);File.SetLastWriteTimeUtc(path,DateTime.UtcNow.AddSeconds(5));Require(CursorTrack.Load(path).Samples[1].X==90,"changed cursor data reloads");
  File.WriteAllText(Path.Combine(root,"bad.cursor.json"),"{\"Width\":0}");Require(CursorTrack.Load(Path.Combine(root,"bad.cursor.json"))==null,"invalid cursor data is ignored");Require(CursorTrack.Load(Path.Combine(root,"missing.json"))==null,"missing cursor data is ignored");
  var rec=new CursorRecorder();rec.Start();System.Threading.Thread.Sleep(150);rec.MarkVideoStart();System.Threading.Thread.Sleep(500);var got=rec.Stop();Require(got.Samples.Count>=1&&got.Width>0&&got.Height>0,"recorder samples the real cursor and desktop size");Require(got.Samples[0].T<0&&got.Samples[0].T>-.5,"recorder times are relative to the video start");got.Validate();Require(true,"recorded cursor data validates");
 }

 static void MapTests(){
  var p=ScreenProject();var c=p.Clips[0];Point pt;double px;Require(CursorEffects.Map(p,c,screen,0,100,50,out pt,out px)&&Math.Abs(pt.X-100)<1e-9&&Math.Abs(pt.Y-50)<1e-9&&Math.Abs(px-1)<1e-9,"full-frame clip maps source pixels one to one");
  c.Scale=.5;CursorEffects.Map(p,c,screen,0,100,50,out pt,out px);Near(pt.X,130,"scaled clip maps X",1e-9);Near(pt.Y,70,"scaled clip maps Y",1e-9);Near(px,.5,"scaled clip pixel size",1e-9);
  c.Rotation=90;CursorEffects.Map(p,c,screen,0,100,50,out pt,out px);Near(pt.X,180,"rotated clip maps X",1e-9);Near(pt.Y,60,"rotated clip maps Y",1e-9);
  c.Rotation=0;c.Scale=1;c.X=20;c.Y=-10;CursorEffects.Map(p,c,screen,0,100,50,out pt,out px);Require(Math.Abs(pt.X-120)<1e-9&&Math.Abs(pt.Y-40)<1e-9,"moved clip maps with position");
  c.CropLeft=.5;Require(!CursorEffects.Map(p,c,screen,0,100,50,out pt,out px),"cursor inside a cropped-out area is not drawn");
 }

 static async Task RenderTests(LivePreview preview){
  Func<byte[],int,int,bool> white=(f,x,y)=>{var q=Px(f,x,y);return q[0]>200&&q[1]>200&&q[2]>200;};Func<byte[],int,int,bool> tinted=(f,x,y)=>Px(f,x,y)[1]>100;Func<byte[],int,int,bool> red=(f,x,y)=>{var q=Px(f,x,y);return q[0]>180&&q[1]<60&&q[2]<60;};
  var plain=ScreenProject();var e=Export(plain,"plain",1);var v=await Preview(preview,plain,1);Require(white(e,102,60)&&red(e,250,150),"export draws the recorded cursor over the screen");Require(white(v,102,60)&&red(v,250,150),"preview draws the same cursor");
  var hidden=ScreenProject();hidden.Clips[0].CursorOpacity=0;Require(red(Export(hidden,"hidden",1),102,60),"cursor opacity 0 hides the cursor");
  var big=ScreenProject();big.Clips[0].CursorScale=2;e=Export(big,"big",1);Require(white(e,106,74)&&!white(Export(plain,"plain2",1),106,74),"cursor scale enlarges the cursor");
  var highlight=ScreenProject("Cursor Highlight");e=Export(highlight,"highlight",1);v=await Preview(preview,highlight,1);Require(Px(e,128,52)[1]>80&&red(e,250,150),"Cursor Highlight tints around the cursor in export");Require(Px(v,128,52)[1]>80,"Cursor Highlight matches in preview");
  var spot=ScreenProject("Cursor Spotlight");e=Export(spot,"spot",1);v=await Preview(preview,spot,1);Require(Px(e,250,170)[0]<150&&Px(e,150,50)[0]>200,"Cursor Spotlight dims away from the cursor in export");Require(Px(v,250,170)[0]<150&&Px(v,150,50)[0]>200,"Cursor Spotlight matches in preview");
  var color=ScreenProject("Cursor Color");e=Export(color,"color",1);Require(Px(e,102,60)[1]>150&&Px(e,102,60)[2]<100,"Cursor Color recolours the cursor");
  var shadow=ScreenProject("Cursor Shadow");e=Export(shadow,"shadow",1);Require(Px(e,109,62)[0]<150,"Cursor Shadow darkens beside the cursor");
  var glow=ScreenProject("Cursor Glow");e=Export(glow,"glow",1);Require(Px(e,98,60)[1]>120,"Cursor Glow outlines the cursor");
  var rings=ScreenProject("Left Click Rings");rings.Clips[0].CursorEffects[0].Color="#FFFFFF";e=Export(rings,"rings",.75);v=await Preview(preview,rings,.75);Require(tinted(e,218,120)&&red(e,200,120),"Left Click Rings draws an expanding ring after a click");Require(tinted(v,218,120),"Left Click Rings matches in preview");Require(red(Export(rings,"rings-late",1.5),218,120),"click effect ends after half a second");
  var right=ScreenProject("Right Click Rings");right.Clips[0].CursorEffects[0].Color="#FFFFFF";Require(red(Export(right,"right",.75),218,120),"right-click effects ignore left clicks");
  var target=ScreenProject("Left Click Target");target.Clips[0].CursorEffects[0].Color="#FFFFFF";e=Export(target,"target",.75);Require(Enumerable.Range(96,24).Any(y=>tinted(e,200,y)),"Left Click Target draws crosshairs");
  var scaled=ScreenProject();scaled.Clips[0].Scale=.5;e=Export(scaled,"scaled",1);v=await Preview(preview,scaled,1);Require(white(e,131,75)&&white(v,131,75),"cursor follows a scaled clip in export and preview");
  var fast=ScreenProject("Left Click Rings");fast.Clips[0].Speed=2;fast.Clips[0].Duration=1;fast.Clips[0].CursorEffects[0].Color="#FFFFFF";Require(tinted(Export(fast,"fast",.375),212,120),"click timing follows clip speed");
 }

 static void ValidationTests(){
  var p=ScreenProject("Cursor Highlight");Engine.Validate(p,true);Require(true,"cursor-effect project validates");
  Action<Action<Project>,string> bad=(change,label)=>{var q=json.Deserialize<Project>(json.Serialize(p));change(q);Reject(()=>Engine.Validate(q,false),label);};
  bad(q=>q.Clips[0].CursorEffects[0].Name="Kinetic Cursor","unsupported cursor effect rejected");bad(q=>q.Clips[0].CursorEffects[0].Size=0,"zero effect size rejected");bad(q=>q.Clips[0].CursorEffects[0].Color="notacolor","invalid colour rejected");bad(q=>q.Clips[0].CursorScale=10,"cursor scale above 400% rejected");bad(q=>q.Clips[0].CursorOpacity=-1,"negative cursor opacity rejected");bad(q=>q.Media[0].CursorPath=null,"cursor effects on media without cursor data rejected");
  string path=Path.Combine(root,"cursor.tasiaproj");Engine.Save(p,path);var loaded=Engine.Load(path);Require(loaded.Media[0].CursorPath==sidecar&&loaded.Clips[0].CursorEffects[0].Name=="Cursor Highlight","cursor data link and effects save and reopen");
  var legacy=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(path));foreach(Dictionary<string,object> clip in (System.Collections.ArrayList)legacy["Clips"]){clip.Remove("CursorEffects");clip.Remove("CursorScale");clip.Remove("CursorOpacity");}File.WriteAllText(path,json.Serialize(legacy));var old=Engine.Load(path);Require(old.Clips[0].CursorEffects.Count==0&&old.Clips[0].CursorScale==1&&old.Clips[0].CursorOpacity==1,"legacy clips load with default cursor settings");
 }

 static async Task EditorTests(Editor editor){
  Invoke(editor,"HideHome");var p=ScreenProject();p.Clips.Add(new Clip{Id="T",Text="Title",Duration=2,Track=1});p.TrackCount=2;SetField(editor,"project",p);Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();SetField(editor,"dirty",false);
  Invoke(editor,"SelectClip",P(editor).Clips.First(c=>c.Id=="T"),false,false);Require(!(bool)Invoke(editor,"AddCursorEffect","Cursor Highlight")&&Undos(editor)==0,"cursor effects need a recording with cursor data");
  Invoke(editor,"SelectClip",P(editor).Clips.First(c=>c.Id=="S"),false,false);Require((bool)Invoke(editor,"AddCursorEffect","Cursor Highlight")&&Undos(editor)==1&&P(editor).Clips.First(c=>c.Id=="S").CursorEffects.Single().Name=="Cursor Highlight","adding a cursor effect is one checkpoint");
  Require(!(bool)Invoke(editor,"AddCursorEffect","Cursor Highlight")&&Undos(editor)==1,"the same effect is not added twice");
  var props=(StackPanel)Field(editor,"properties");Require(props.Children.OfType<TextBlock>().Any(t=>t.Text=="CURSOR")&&Tree(props).OfType<TextBox>().Any(b=>AutomationProperties.GetAutomationId(b)=="Cursor.Scale"),"Properties shows cursor scale, opacity and effects");
  var s=P(editor).Clips.First(c=>c.Id=="S");Require(!(bool)Invoke(editor,"EditCursor",s,(Action<Clip>)(c=>c.CursorScale=10))&&s.CursorScale==1&&Undos(editor)==1,"invalid cursor scale is rejected without a checkpoint");Require((bool)Invoke(editor,"EditCursor",s,(Action<Clip>)(c=>c.CursorScale=1.5))&&s.CursorScale==1.5&&Undos(editor)==2,"cursor scale edit is one checkpoint");
  Invoke(editor,"Undo");Require(P(editor).Clips.First(c=>c.Id=="S").CursorScale==1,"undo restores cursor scale");
  var nav=((string[])Field(editor,"NavigationTools")).ToList();Require(nav.IndexOf("Cursor Effects")==nav.IndexOf("Behaviors")+1&&nav.IndexOf("Audio Effects")==nav.IndexOf("Cursor Effects")+1,"Cursor Effects sits between Behaviors and Audio Effects, as in the reference");
  Invoke(editor,"Panel","Cursor Effects");var library=(StackPanel)Field(editor,"library");var tabs=Tree(library).OfType<Button>().Select(AutomationProperties.GetAutomationId).Where(i=>i!=null&&i.StartsWith("CursorTab.")).ToList();Require(tabs.SequenceEqual(new[]{"CursorTab.Cursor","CursorTab.LeftClick","CursorTab.RightClick"}),"panel has Cursor, Left Click and Right Click tabs");
  var names=Tree(library).OfType<Button>().Select(AutomationProperties.GetName).Where(n=>CursorEffects.Catalog.Contains(n)).ToList();Require(names.SequenceEqual(CursorEffects.CursorCatalog),"Cursor tab lists cursor effects");
  SetField(editor,"cursorTab","Left Click");Invoke(editor,"Panel","Cursor Effects");names=Tree((StackPanel)Field(editor,"library")).OfType<Button>().Select(AutomationProperties.GetName).Where(n=>CursorEffects.Catalog.Contains(n)).ToList();Require(names.SequenceEqual(new[]{"Left Click Burst 1","Left Click Rings","Left Click Target"}),"Left Click tab lists left-click effects");
  var menu=((Grid)Field(editor,"editorRoot")).Children.OfType<Menu>().Single();var modify=menu.Items.OfType<MenuItem>().First(i=>((string)i.Header).Replace("_","")=="Modify");var addEffect=modify.Items.OfType<MenuItem>().First(i=>(string)i.Header=="Add Effect");Require(addEffect.Items.OfType<MenuItem>().First(i=>(string)i.Header=="Cursor Effects").Items.Count==CursorEffects.Catalog.Count(),"Modify > Add Effect > Cursor Effects lists every effect");
  string clipPath=Path.Combine(root,"imported.mp4");File.Copy(screen.Path,clipPath,true);File.Copy(sidecar,CursorTrack.SidecarFor(clipPath),true);int before=P(editor).Media.Count;Invoke(editor,"Import",(object)new[]{clipPath});for(int i=0;i<100&&P(editor).Media.Count==before;i++)await Task.Delay(100);var imported=P(editor).Media.Last();Require(imported.CursorPath==CursorTrack.SidecarFor(clipPath),"importing a recording attaches its cursor data");
  Invoke(editor,"SetRecordContent",true);var record=(Button)Field(editor,"record");Require(AutomationProperties.GetName(record)=="Stop","record button shows Stop while recording");Invoke(editor,"SetRecordContent",false);Require(AutomationProperties.GetName(record)=="Record"&&record.Content is StackPanel,"record button returns to the icon and Record label");
  SetField(editor,"dirty",false);
 }

 [STAThread] public static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var preview=new LivePreview();var host=new Window{Width=700,Height=460,Content=preview,Left=-20000,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.Manual,Title="TatsiaCraft isolated cursor acceptance"};
  host.Loaded+=async(s,e)=>{int code=0;Editor editor=null;try{
   string video=Path.Combine(root,"screen.mp4");Engine.Run("ffmpeg","-y -v error -f lavfi -i color=red:s=320x180:r=30:d=3 -c:v libopenh264 -pix_fmt yuv420p "+Engine.Quote(video));screen=Engine.Probe(video);
   var track=new CursorTrack{Width=320,Height=180};track.Samples.Add(new CursorSample{T=0,X=100,Y=50});track.Samples.Add(new CursorSample{T=3,X=100,Y=50});track.Clicks.Add(new CursorClick{T=.5,X=200,Y=120,Button="left"});sidecar=CursorTrack.SidecarFor(video);track.Save(sidecar);screen.CursorPath=sidecar;
   TrackTests();MapTests();await RenderTests(preview);ValidationTests();editor=new Editor{Width=1389,Height=812,WindowStartupLocation=WindowStartupLocation.Manual,Left=-20000,Top=0,ShowInTaskbar=false};editor.Show();await Task.Delay(300);await EditorTests(editor);File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);}catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);code=1;}finally{if(editor!=null){SetField(editor,"dirty",false);((LivePreview)Field(editor,"live")).Dispose();editor.Close();}preview.Dispose();host.Close();app.Shutdown(code);}};
  host.Show();app.Run();
 }
}
