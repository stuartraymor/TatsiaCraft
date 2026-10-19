using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Automation;
using TasiaCraft;
public static class BehaviorTest {
 static int passed; static string root,log; static readonly JavaScriptSerializer json=new JavaScriptSerializer();
 static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object Field(Editor e,string name){return typeof(Editor).GetField(name,flags).GetValue(e);}
 static void SetField(Editor e,string name,object value){typeof(Editor).GetField(name,flags).SetValue(e,value);}
 static object Invoke(Editor e,string name,params object[] args){return typeof(Editor).GetMethod(name,flags).Invoke(e,args);}
 static Project P(Editor e){return (Project)Field(e,"project");}
 static int Undos(Editor e){return ((Stack<string>)Field(e,"undo")).Count;}
 static void Require(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static void Near(double a,double b,string label,double tolerance=1e-6){Require(Math.Abs(a-b)<tolerance,label+" ("+a.ToString("0.####")+" vs "+b.ToString("0.####")+")");}
 static void Reject(Action a,string label){bool rejected=false;try{a();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}catch(InvalidDataException){rejected=true;}Require(rejected,label);}
 static Project Clone(Project p){return json.Deserialize<Project>(json.Serialize(p));}
 static IEnumerable<DependencyObject> Tree(DependencyObject node){yield return node;int n=VisualTreeHelper.GetChildrenCount(node);for(int i=0;i<n;i++)foreach(var d in Tree(VisualTreeHelper.GetChild(node,i)))yield return d;}
 static Media red;
 static Project RedProject(string behavior,double inD=1,double outD=1){var p=new Project{Width=320,Height=180};p.Media.Add(red);var c=new Clip{Id="R",MediaId=red.Id,Duration=3,Scale=.5};if(behavior!=null)c.Behaviors.Add(new Behavior{Name=behavior,In=inD,Out=outD});p.Clips.Add(c);return p;}
 static byte[] Export(Project p,string name,double time){string video=Path.Combine(root,name+".mp4");Engine.Render(p,video,null);string raw=Path.Combine(root,"frame.rgb");Engine.Run("ffmpeg","-y -v error -ss "+time.ToString(System.Globalization.CultureInfo.InvariantCulture)+" -i "+Engine.Quote(video)+" -frames:v 1 -pix_fmt rgb24 -f rawvideo "+Engine.Quote(raw));return File.ReadAllBytes(raw);}
 static byte[] Preview(LivePreview preview,Project p,double time){preview.SetProject(p);preview.Seek(time);preview.UpdateLayout();var bitmap=new RenderTargetBitmap(p.Width,p.Height,96,96,PixelFormats.Pbgra32);bitmap.Render(preview.Stage);var bgra=new byte[p.Width*p.Height*4];bitmap.CopyPixels(bgra,p.Width*4,0);var rgb=new byte[p.Width*p.Height*3];for(int i=0;i<p.Width*p.Height;i++){rgb[i*3]=bgra[i*4+2];rgb[i*3+1]=bgra[i*4+1];rgb[i*3+2]=bgra[i*4];}return rgb;}
 static int R(byte[] rgb,int x,int y){return rgb[(y*320+x)*3];}

 static void ModelTests(){
  var c=new Clip{Duration=3};Action<string> set=name=>{c.Behaviors.Clear();c.Behaviors.Add(new Behavior{Name=name,In=1,Out=1});};
  set("Fade");Near(Behaviors.Evaluate(c,0,320,180).Alpha,0,"Fade starts transparent");Near(Behaviors.Evaluate(c,.5,320,180).Alpha,.5,"Fade In is linear");Near(Behaviors.Evaluate(c,1.5,320,180).Alpha,1,"Fade is opaque during");Near(Behaviors.Evaluate(c,2.5,320,180).Alpha,.5,"Fade Out is linear");Near(Behaviors.Evaluate(c,3,320,180).Alpha,0,"Fade ends transparent");
  set("Fly In");Near(Behaviors.Evaluate(c,0,320,180).X,-320,"Fly In starts one canvas width left");Near(Behaviors.Evaluate(c,.5,320,180).X,-160,"Fly In eases halfway");Near(Behaviors.Evaluate(c,1.5,320,180).X,0,"Fly In rests in place");Near(Behaviors.Evaluate(c,3,320,180).X,320,"Fly In exits right");
  set("Pop Up");Near(Behaviors.Evaluate(c,0,320,180).Scale,0,"Pop Up starts at zero size");Require(Behaviors.Evaluate(c,.7,320,180).Scale>1.05,"Pop Up overshoots");Near(Behaviors.Evaluate(c,1,320,180).Scale,1,"Pop Up settles at full size");Near(Behaviors.Evaluate(c,3,320,180).Scale,0,"Pop Up shrinks out");
  set("Scale");Near(Behaviors.Evaluate(c,.5,320,180).Scale,.5,"Scale eases in");Near(Behaviors.Evaluate(c,2.5,320,180).Scale,.5,"Scale eases out");
  set("Sliding");Near(Behaviors.Evaluate(c,0,320,180).Y,45,"Sliding starts a quarter canvas below");Near(Behaviors.Evaluate(c,.5,320,180).Alpha,.5,"Sliding fades in");Near(Behaviors.Evaluate(c,3,320,180).Y,-45,"Sliding exits upward");
  set("Pulsating");Near(Behaviors.Evaluate(c,.2,320,180).Scale,1.06,"Pulsating peaks at 106%");Near(Behaviors.Evaluate(c,.4,320,180).Scale,1,"Pulsating returns to 100%",1e-9);
  set("Tremble");Require(Math.Abs(Behaviors.Evaluate(c,.03,320,180).X)>0&&Math.Abs(Behaviors.Evaluate(c,.03,320,180).X)<=3,"Tremble shakes within 3 px");
  set("Drifting");Near(Behaviors.Evaluate(c,0,320,180).X,-6.4,"Drifting starts left");Near(Behaviors.Evaluate(c,3,320,180).X,6.4,"Drifting ends right");
  c.Behaviors.Clear();c.Behaviors.Add(new Behavior{Name="Fade",In=3,Out=3});Near(Behaviors.Evaluate(c,.75,320,180).Alpha,.5,"overlapping In/Out shrink proportionally");Near(Behaviors.Evaluate(c,1.5,320,180).Alpha,1,"shrunk phases meet in the middle");
  c.Behaviors[0].Enabled=false;Near(Behaviors.Evaluate(c,0,320,180).Alpha,1,"disabled behavior has no effect");
  c.Behaviors.Clear();c.Behaviors.Add(new Behavior{Name="Fade",In=1,Out=1});c.Behaviors.Add(new Behavior{Name="Scale",In=1,Out=1});var both=Behaviors.Evaluate(c,.5,320,180);Near(both.Alpha,.5,"stacked behaviors combine alpha");Near(both.Scale,.5,"stacked behaviors combine scale");
  var p=RedProject("Fade");Engine.Validate(p,true);Require(true,"behavior project validates");
  Action<Action<Project>,string> bad=(change,label)=>{var q=Clone(p);change(q);Reject(()=>Engine.Validate(q,false),label);};
  bad(q=>q.Clips[0].Behaviors[0].Name="Explode","unsupported behavior name rejected");bad(q=>q.Clips[0].Behaviors[0].In=-1,"negative In rejected");bad(q=>q.Clips[0].Behaviors[0].Out=double.NaN,"NaN Out rejected");bad(q=>q.Clips[0].Behaviors[0].In=61,"In above 60 s rejected");bad(q=>{for(int i=0;i<8;i++)q.Clips[0].Behaviors.Add(new Behavior{Name="Fade"});},"more than 8 behaviors rejected");bad(q=>q.Clips[0].Behaviors.Add(null),"null behavior rejected");
  var text=new Project();text.Clips.Add(new Clip{Text="Hi",Duration=2,Behaviors=new List<Behavior>{new Behavior{Name="Pop Up"}}});Engine.Validate(text,false);Require(true,"text clips accept behaviors");
  string path=Path.Combine(root,"behaviors.tasiaproj");Engine.Save(p,path);Require(json.Serialize(Engine.Load(path).Clips[0].Behaviors)==json.Serialize(p.Clips[0].Behaviors),"behaviors save and reopen");
  var legacy=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(path));foreach(Dictionary<string,object> clip in (System.Collections.ArrayList)legacy["Clips"])clip.Remove("Behaviors");File.WriteAllText(path,json.Serialize(legacy));Require(Engine.Load(path).Clips[0].Behaviors.Count==0,"legacy clips load without behaviors");
 }

 static void RenderTests(LivePreview preview){
  // Red 320x180 image at 50% scale: a 160x90 block centred on a 320x180 canvas, so x 80..240, y 45..135 at rest.
  var rest=RedProject(null);var restFrame=Export(rest,"rest",1.5);Require(R(restFrame,100,90)>200&&R(restFrame,60,90)<30,"reference block renders at rest");
  var fly=RedProject("Fly In");var e=Export(fly,"flyin",.5);var v=Preview(preview,fly,.5);Require(R(e,30,90)>200&&R(e,200,90)<30,"export Fly In is halfway in at 0.5 s");Require(R(v,30,90)>200&&R(v,200,90)<30,"preview Fly In matches export");
  var e2=Export(fly,"flyin-rest",1.5);Require(R(e2,100,90)>200&&R(e2,60,90)<30,"export Fly In rests in place during");
  var fade=RedProject("Fade");e=Export(fade,"fade",.5);v=Preview(preview,fade,.5);Require(R(e,160,90)>100&&R(e,160,90)<160,"export Fade is half transparent at 0.5 s");Require(Math.Abs(R(v,160,90)-R(e,160,90))<25,"preview Fade matches export");e=Export(fade,"fade-out",2.5);Require(R(e,160,90)>100&&R(e,160,90)<160,"export Fade Out is half transparent at 2.5 s");
  var pop=RedProject("Pop Up");e=Export(pop,"popup",.5);v=Preview(preview,pop,.5);Require(R(e,241,90)>200&&R(e,252,90)<30,"export Pop Up overshoots past its rest width");Require(R(v,241,90)>200&&R(v,252,90)<30,"preview Pop Up matches export");
  var scale=RedProject("Scale");e=Export(scale,"scale",.5);v=Preview(preview,scale,.5);Require(R(e,190,90)>200&&R(e,210,90)<30,"export Scale is at half size at 0.5 s");Require(R(v,190,90)>200&&R(v,210,90)<30,"preview Scale matches export");
  var slide=RedProject("Sliding");e=Export(slide,"sliding",.5);v=Preview(preview,slide,.5);Require(R(e,160,60)<30&&R(e,160,150)>80,"export Sliding is below its rest position at 0.5 s");Require(R(v,160,60)<30&&R(v,160,150)>80,"preview Sliding matches export");
  var rotated=RedProject("Pop Up");rotated.Clips[0].Rotation=30;e=Export(rotated,"rotated",1.5);Require(R(e,160,90)>200,"behaviors work with rotated clips");
  var animated=RedProject("Pulsating");animated.Clips[0].Animate=true;animated.Clips[0].EndScale=.5;e=Export(animated,"animated",1.5);Require(R(e,160,90)>200,"behaviors work with transform animation");
  var text=new Project{Width=320,Height=180};text.Clips.Add(new Clip{Text="Behavior",Duration=3,Behaviors=new List<Behavior>{new Behavior{Name="Fade",In=1,Out=1}}});var early=Export(text,"text",.2).Select(b=>(double)b).Sum();var mid=Export(text,"text-mid",1.5).Select(b=>(double)b).Sum();Require(mid>10000&&early<mid*.4,"text Fade behavior exports");
  var textPop=new Project{Width=320,Height=180};textPop.Clips.Add(new Clip{Text="Behavior",Duration=3,Behaviors=new List<Behavior>{new Behavior{Name="Scale",In=1,Out=1}}});var small=Export(textPop,"text-scale",.3).Select(b=>(double)b).Sum();var big=Export(textPop,"text-scale-mid",1.5).Select(b=>(double)b).Sum();Require(small<big*.5,"text Scale behavior exports smaller text early");
 }

 static void EditorTests(Editor editor){
  string favFile=Path.Combine(root,"favorites.json");if(File.Exists(favFile))File.Delete(favFile);SetField(editor,"favoritesPath",favFile);SetField(editor,"favorites",null);
  Invoke(editor,"HideHome");var p=RedProject(null);p.TrackCount=3;string tone=Path.Combine(root,"tone.wav");Engine.Run("ffmpeg","-y -v error -f lavfi -i sine=frequency=440:duration=2 "+Engine.Quote(tone));var t=Engine.Probe(tone);p.Media.Add(t);p.Clips.Add(new Clip{Id="A",MediaId=t.Id,Duration=2,Track=1});p.Clips.Add(new Clip{Id="T",Text="Title",Duration=2,Track=2});SetField(editor,"project",p);Invoke(editor,"Refresh");((Stack<string>)Field(editor,"undo")).Clear();SetField(editor,"dirty",false);
  Require(!(bool)Invoke(editor,"AddBehavior","Fade")&&Undos(editor)==0,"Add behavior needs a selected clip");
  Invoke(editor,"SelectClip",P(editor).Clips.First(c=>c.Id=="R"),false,false);Require((bool)Invoke(editor,"AddBehavior","Fly In")&&P(editor).Clips.First(c=>c.Id=="R").Behaviors.Single().Name=="Fly In"&&Undos(editor)==1,"Add behavior applies to the selected clip in one checkpoint");Require(((TextBlock)Field(editor,"status")).Text=="Behavior Added","reference-style Behavior Added notice");
  var r=P(editor).Clips.First(c=>c.Id=="R");Near(r.Behaviors[0].In,1,"default In is one second");
  var props=(StackPanel)Field(editor,"properties");Require(props.Children.OfType<TextBlock>().Any(b=>b.Text=="BEHAVIORS")&&Tree(props).OfType<TextBox>().Any(b=>AutomationProperties.GetAutomationId(b)=="Behavior.In.0"),"Properties shows the behavior with In and Out fields");
  Require((bool)Invoke(editor,"EditBehavior",r,0,(Action<Behavior>)(b=>b.In=.5))&&P(editor).Clips.First(c=>c.Id=="R").Behaviors[0].In==.5&&Undos(editor)==2,"editing In is one checkpoint");r=P(editor).Clips.First(c=>c.Id=="R");
  Require(!(bool)Invoke(editor,"EditBehavior",r,0,(Action<Behavior>)(b=>b.In=-1))&&r.Behaviors[0].In==.5&&Undos(editor)==2,"invalid In is rejected without a checkpoint");
  Require((bool)Invoke(editor,"EditBehavior",r,0,(Action<Behavior>)(b=>b.Enabled=false))&&!r.Behaviors[0].Enabled,"behavior can be turned off");
  Invoke(editor,"Undo");Invoke(editor,"Undo");Require(P(editor).Clips.First(c=>c.Id=="R").Behaviors[0].In==1,"undo restores behavior settings");r=P(editor).Clips.First(c=>c.Id=="R");
  Invoke(editor,"RemoveBehavior",r,0);Require(P(editor).Clips.First(c=>c.Id=="R").Behaviors.Count==0,"behavior can be removed");
  Invoke(editor,"SelectClip",P(editor).Clips.First(c=>c.Id=="A"),false,false);int marks=Undos(editor);Require(!(bool)Invoke(editor,"AddBehavior","Fade")&&Undos(editor)==marks,"audio clips cannot take behaviors");
  Invoke(editor,"SelectClip",P(editor).Clips.First(c=>c.Id=="T"),false,false);Require((bool)Invoke(editor,"AddBehavior","Pop Up"),"text clips take behaviors");
  P(editor).LockedTracks.Add(2);marks=Undos(editor);Invoke(editor,"SelectClip",P(editor).Clips.First(c=>c.Id=="T"),false,false);Require(!(bool)Invoke(editor,"AddBehavior","Fade")&&Undos(editor)==marks,"locked track blocks behaviors");P(editor).LockedTracks.Clear();
  Invoke(editor,"Panel","Behaviors");var library=(StackPanel)Field(editor,"library");var names=Tree(library).OfType<Button>().Select(AutomationProperties.GetName).Where(n=>Behaviors.Catalog.Contains(n)).ToList();Require(names.SequenceEqual(Behaviors.Catalog),"Behaviors panel lists every supported behavior");
  var menu=((Grid)Field(editor,"editorRoot")).Children.OfType<Menu>().Single();var modify=menu.Items.OfType<MenuItem>().First(i=>((string)i.Header).Replace("_","")=="Modify");var add=modify.Items.OfType<MenuItem>().First(i=>(string)i.Header=="Add Behavior");Require(add.Items.OfType<MenuItem>().Select(i=>(string)i.Header).SequenceEqual(Behaviors.Catalog),"Modify > Add Behavior lists the catalog");
  var nav=((string[])Field(editor,"NavigationTools")).ToList();Require(nav.IndexOf("Favorites")==0&&nav.IndexOf("Behaviors")==nav.IndexOf("Animations")+1,"Favorites leads and Behaviors follows Animations, as in the reference");
  Invoke(editor,"Panel","Favorites");Require(Tree(library).OfType<TextBlock>().Any(b=>b.Text=="Click the star icon to save callouts, effects, or other video enhancements to Favorites."),"empty Favorites shows the reference hint");
  Invoke(editor,"Panel","Behaviors");var star=Tree(library).OfType<Button>().First(b=>AutomationProperties.GetAutomationId(b)=="Favorite.Behaviors/FlyIn");Require((string)star.ToolTip=="Click to save to Favorites.","star tooltip uses reference wording");
  Invoke(editor,"ToggleFavorite","Behaviors/Fly In");Invoke(editor,"ToggleFavorite","Visual Effects/Blur");Require(File.Exists(favFile)&&File.ReadAllText(favFile).Contains("Behaviors/Fly In"),"favorites are saved for the user");
  SetField(editor,"favorites",null);Invoke(editor,"Panel","Favorites");var shown=Tree(library).OfType<Button>().Select(AutomationProperties.GetName).ToList();Require(shown.Contains("Fly In")&&shown.Contains("Blur")&&!shown.Contains("Fade"),"Favorites shows only starred presets after reload");
  var type=Tree(library).OfType<ComboBox>().First(b=>AutomationProperties.GetAutomationId(b)=="Favorites.Type");Require(((IEnumerable<string>)type.ItemsSource).SequenceEqual(new[]{"All","Visual Effects","Behaviors"}),"Type filter lists tool types that have favorites");
  type.SelectedItem="Behaviors";shown=Tree((StackPanel)Field(editor,"library")).OfType<Button>().Select(AutomationProperties.GetName).ToList();Require(shown.Contains("Fly In")&&!shown.Contains("Blur"),"Type filter narrows Favorites");
  Invoke(editor,"SelectClip",P(editor).Clips.First(c=>c.Id=="R"),false,false);var favTile=Tree((StackPanel)Field(editor,"library")).OfType<Button>().First(b=>AutomationProperties.GetName(b)=="Fly In");favTile.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Require(P(editor).Clips.First(c=>c.Id=="R").Behaviors.Any(b=>b.Name=="Fly In"),"clicking a favorite applies it");
  Invoke(editor,"ToggleFavorite","Behaviors/Fly In");Require(!File.ReadAllText(favFile).Contains("Behaviors/Fly In"),"unstarring removes a favorite");
  SetField(editor,"activePanel","Media");var args=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(editor),0,Key.F){RoutedEvent=Keyboard.PreviewKeyDownEvent};Invoke(editor,"Keys",editor,args);Require((string)Field(editor,"activePanel")=="Favorites","F opens Favorites");
  SetField(editor,"dirty",false);
 }

 [STAThread] public static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var preview=new LivePreview();var host=new Window{Width=700,Height=460,Content=preview,Left=-20000,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.Manual,Title="TatsiaCraft isolated behavior acceptance"};
  host.Loaded+=async(s,e)=>{int code=0;Editor editor=null;try{string path=Path.Combine(root,"red.png");Engine.Run("ffmpeg","-y -v error -f lavfi -i color=red:s=320x180 -frames:v 1 "+Engine.Quote(path));red=Engine.Probe(path);
   ModelTests();RenderTests(preview);editor=new Editor{Width=1389,Height=812,WindowStartupLocation=WindowStartupLocation.Manual,Left=-20000,Top=0,ShowInTaskbar=false};editor.Show();await Task.Delay(300);EditorTests(editor);File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);}catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);code=1;}finally{if(editor!=null){SetField(editor,"dirty",false);((LivePreview)Field(editor,"live")).Dispose();editor.Close();}preview.Dispose();host.Close();app.Shutdown(code);}};
  host.Show();app.Run();
 }
}
