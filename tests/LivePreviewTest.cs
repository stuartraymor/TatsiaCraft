using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TasiaCraft;
public class LivePreviewTest {
 static string root,log;static int passed;static LivePreview current;
 static void Require(bool value,string label){if(!value)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static byte[] Pixels(LivePreview preview){preview.UpdateLayout();var bitmap=new RenderTargetBitmap(640,360,96,96,PixelFormats.Pbgra32);bitmap.Render(preview);var pixels=new byte[640*360*4];bitmap.CopyPixels(pixels,640*4,0);return pixels;}
 static int Index(int x,int y){var p=current.Stage.TransformToAncestor(current).Transform(new Point(x/2.0,y/2.0));return((int)Math.Round(p.Y)*640+(int)Math.Round(p.X))*4;}
 [STAThread]public static void Main(string[] args){
  root=args[0];log=Path.Combine(root,"artifacts","live-preview-test.txt");File.WriteAllText(log,"");
  var app=new Application();var preview=new LivePreview();current=preview;var window=new Window{Title="TasiaCraft isolated live compositor regression",Width=656,Height=399,Content=preview,ResizeMode=ResizeMode.NoResize};
  window.Loaded+=async(s,e)=>{try{
   string fixture=Path.Combine(root,"artifacts","final-engine");var red=Engine.Probe(Path.Combine(fixture,"red.png"));var blue=Engine.Probe(Path.Combine(fixture,"blue.png"));
   var p=new Project{Width=320,Height=180};p.Media.Add(red);p.Media.Add(blue);var lower=new Clip{MediaId=red.Id,Duration=3};var upper=new Clip{MediaId=blue.Id,Start=.5,Duration=1,Track=1,Scale=.5,Opacity=.5};p.Clips.Add(lower);p.Clips.Add(upper);
   preview.SetProject(p);preview.Seek(1);await Task.Delay(200);var pixels=Pixels(preview);int middle=Index(320,180),edge=Index(20,180);
   Require(pixels[middle]>80&&pixels[middle]<180&&pixels[middle+2]>80&&pixels[middle+2]<180,"paused multilayer opacity");Require(pixels[edge+2]>220&&pixels[edge]<30,"fit scaled layer bounds");
   preview.Seek(.2);await Task.Delay(80);pixels=Pixels(preview);Require(pixels[middle+2]>220&&pixels[middle]<30,"seek removes inactive upper layer immediately");
   preview.SetViewZoom(1);preview.UpdateLayout();var physical=preview.Stage.TransformToAncestor(preview);double width=(physical.Transform(new Point(320,0))-physical.Transform(new Point(0,0))).Length;double dpi=PresentationSource.FromVisual(preview).CompositionTarget.TransformToDevice.M11;Require(Math.Abs(width*dpi-320)<2,"100 percent viewport zoom uses physical pixels");Require(upper.Scale==.5,"viewport zoom does not change clip scale");preview.SetViewZoom(0);preview.UpdateLayout();
   preview.SetSelection(lower);preview.SetViewZoom(2);await Task.Delay(80);pixels=Pixels(preview);int arm=Index(680,180);Require(pixels[arm]>120&&pixels[arm+1]>80&&pixels[arm+2]<100,"rotation arm visible outside project bounds for full canvas clip");preview.SetSelection(null);preview.SetViewZoom(0);preview.UpdateLayout();
   preview.Seek(.8);p.HiddenTracks.Add(1);preview.Refresh();await Task.Delay(80);pixels=Pixels(preview);middle=Index(320,180);Require(pixels[middle+2]>220&&pixels[middle]<30,"hidden upper video track omitted from composition");p.HiddenTracks.Remove(1);preview.Refresh();preview.SetSelection(lower);p.LockedTracks.Add(0);preview.Refresh();Require(preview.Stage.Children.OfType<Canvas>().First().Children.Count==0,"locked selection hides editing grips");typeof(LivePreview).GetMethod("StartDrag",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(preview,new object[]{"move",new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left)});Require(!preview.Stage.IsMouseCaptured,"locked track rejects canvas editing gesture");p.LockedTracks.Clear();preview.SetSelection(null);preview.Seek(.2);
   upper.Start=0;upper.NativeSize=true;upper.Scale=.25;upper.Opacity=1;upper.X=70;upper.Y=30;upper.Rotation=90;preview.Refresh();await Task.Delay(80);pixels=Pixels(preview);Require(pixels[Index(460,240)]>220&&pixels[Index(460,300)]>220&&pixels[Index(520,240)+2]>220,"native image move rotation bounds");
   upper.Effect="grayscale";preview.Refresh();await Task.Delay(1800);pixels=Pixels(preview);int center=Index(460,240);Require(Math.Abs(pixels[center]-pixels[center+2])<8&&pixels[center]>5,"asynchronous source effect grayscale preview");
   preview.Play();await Task.Delay(350);Require(preview.Position>.45&&preview.Position<1,"master clock advances without timeline render");preview.Pause();double paused=preview.Position;await Task.Delay(180);Require(Math.Abs(preview.Position-paused)<.001,"pause freezes master clock");
   var video=Engine.Probe(Path.Combine(fixture,"fixture.mp4"));var vp=new Project{Width=320,Height=180};vp.Media.Add(video);vp.Clips.Add(new Clip{MediaId=video.Id,Duration=1,In=1,Speed=2});preview.SetProject(vp);preview.Seek(.2);await Task.Delay(1200);
   var decoder=((Border)preview.Stage.Children.OfType<Border>().First()).Child as MediaElement;Require(decoder!=null&&decoder.NaturalVideoWidth==320,"native video decoder opened");Require(Math.Abs(decoder.Position.TotalSeconds-1.4)<.25,"paused video trim and speed source mapping");
   preview.Play();await Task.Delay(250);Require(Math.Abs(decoder.Position.TotalSeconds-(1+preview.Position*2))<.35,"live video speed synchronization");
   preview.Pause();preview.Seek(.25);vp.Clips[0].Effect="grayscale";vp.Clips[0].AudioEffect="noise reduction";vp.Clips[0].Volume=2;preview.Refresh();await Task.Delay(2200);
   decoder=((Border)preview.Stage.Children.OfType<Border>().First()).Child as MediaElement;Require(decoder!=null&&decoder.Source.LocalPath.Contains("TasiaCraft-source-preview")&&decoder.NaturalVideoWidth==320,"video visual audio boosted gain source proxy decoded");Require(Math.Abs(decoder.Volume-1)<.01,"boosted proxy gain not applied twice");
   vp.Clips[0].Volume=.25;vp.Clips[0].Effect=null;vp.Clips[0].AudioEffect=null;vp.Clips[0].FadeIn=.5;preview.Refresh();await Task.Delay(450);decoder=((Border)preview.Stage.Children.OfType<Border>().First()).Child as MediaElement;File.AppendAllText(log,"DIAGNOSTIC native source after 450ms: width="+decoder.NaturalVideoWidth+" volume="+decoder.Volume+" position="+preview.Position+Environment.NewLine);for(int readyAttempt=0;readyAttempt<100&&decoder.NaturalVideoWidth==0;readyAttempt++)await Task.Delay(50);Require(decoder.NaturalVideoWidth==320,"native source decoder reopened after removing effect proxy");Require(Math.Abs(decoder.Volume-.125)<.01,"live gain and clip fade at paused position");
   preview.Seek(.9);preview.Play();await Task.Delay(350);Require(!preview.IsPlaying&&Math.Abs(preview.Position-1)<.01,"playback stops exactly at timeline end");
   preview.Seek(.2);preview.Play();await Task.Delay(80);vp.HiddenTracks.Add(0);preview.Refresh();Require(decoder.Source==null&&preview.Stage.Children.OfType<Border>().Count()==0,"hidden audio video track closes and mutes decoder");preview.Seek(.8);Require(Math.Abs(preview.Position-.8)<.01,"hidden tracks preserve timeline length");preview.Pause();
   preview.Dispose();Require(!preview.IsPlaying&&preview.Stage.Children.OfType<Border>().Count()==0,"dispose releases decoders");
   string lifeSource=Path.Combine(root,"artifacts","lifecycle-"+Guid.NewGuid().ToString("N")+".mp4");File.Copy(video.Path,lifeSource);var lm=Engine.Probe(lifeSource);var lp=new Project{Width=320,Height=180};lp.Media.Add(lm);lp.Clips.Add(new Clip{MediaId=lm.Id,Duration=1,Effect="sharpen",AudioEffect="normalize"});var life=new LivePreview();life.SetProject(lp);
   var field=typeof(LivePreview).GetField("proxyProcesses",BindingFlags.NonPublic|BindingFlags.Instance);var owned=(IDictionary)field.GetValue(life);Process running=null;string partial=null;
   for(int retry=0;retry<100&&running==null;retry++){lock(owned){foreach(DictionaryEntry pair in owned){running=(Process)pair.Key;partial=(string)pair.Value;break;}}if(running==null)await Task.Delay(5);}
   Require(running!=null,"lifecycle fixture starts an owned source processing process");int pid=running.Id;life.Dispose();bool ended;try{using(var check=Process.GetProcessById(pid))ended=check.HasExited;}catch(ArgumentException){ended=true;}Require(ended,"dispose terminates owned source proxy process");Require(!File.Exists(partial),"dispose removes partial source proxy output");await Task.Delay(100);Require(life.Stage.Children.OfType<Border>().Count()==0,"disposed proxy continuation does not recreate layers");File.Delete(lifeSource);
   File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);app.Shutdown(0);
  }catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);preview.Dispose();app.Shutdown(1);}};
  app.Run(window);
 }
}




