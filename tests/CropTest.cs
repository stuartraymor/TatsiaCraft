using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TasiaCraft;

public static class CropTest {
 static string root,log;static int passed;static LivePreview preview;
 static void Require(bool value,string label){if(!value)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static void Reject(Action action,string label){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Require(rejected,label);}
 static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
 static void Fixture(string path,int w,int h,bool split){using(var b=new System.Drawing.Bitmap(w,h)){using(var g=System.Drawing.Graphics.FromImage(b)){g.Clear(split?System.Drawing.Color.Red:System.Drawing.Color.Lime);if(split)g.FillRectangle(System.Drawing.Brushes.Blue,w/2,0,w/2,h);}b.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
 static byte[] PreviewPixels(){preview.UpdateLayout();var target=new RenderTargetBitmap(240,160,96,96,PixelFormats.Pbgra32);target.Render(preview.Stage);var result=new byte[240*160*4];target.CopyPixels(result,240*4,0);return result;}
 static byte[] ExportPixels(Project p,string name){string output=Path.Combine(root,name+".mp4"),png=Path.Combine(root,name+".png");Engine.Render(p,output,null);Engine.Run("ffmpeg","-y -v error -ss 0.2 -i "+Engine.Quote(output)+" -frames:v 1 -threads 1 "+Engine.Quote(png));using(var b=new System.Drawing.Bitmap(png)){Require(b.Width==240&&b.Height==160,name+" export decoded dimensions");var result=new byte[240*160*4];for(int y=0;y<160;y++)for(int x=0;x<240;x++){var c=b.GetPixel(x,y);int n=(y*240+x)*4;result[n]=c.B;result[n+1]=c.G;result[n+2]=c.R;result[n+3]=c.A;}return result;}}
 static bool Blue(byte[] p,int x,int y){int n=(y*240+x)*4;return p[n]>180&&p[n+1]<65&&p[n+2]<65;}
 static bool Green(byte[] p,int x,int y){int n=(y*240+x)*4;return p[n+1]>180&&p[n]<65&&p[n+2]<65;}
 static void CheckCrop(byte[] pixels,string label){Require(Blue(pixels,160,80),label+" retained blue stays at original source placement");Require(Green(pixels,80,80),label+" cropped left reveals lower green track");Require(Green(pixels,160,48),label+" cropped top reveals lower track");Require(Green(pixels,192,80),label+" cropped right reveals lower track");Require(Green(pixels,160,116),label+" cropped bottom reveals lower track");}
 static void CheckRotation(byte[] pixels,string label){Require(Blue(pixels,120,125),label+" crop rotates about original full-source center");Require(Green(pixels,120,40),label+" removed source half stays transparent after rotation");Require(Green(pixels,155,120),label+" rotated retained bounds preserve original pivot");}
 [STAThread]public static void Main(string[] args){root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");var app=new Application();preview=new LivePreview();var window=new Window{Title="TatsiaCraft isolated crop acceptance test",Width=500,Height=360,Content=preview};window.Loaded+=async(s,e)=>{try{
  string source=Path.Combine(root,"split.png"),background=Path.Combine(root,"green.png");Fixture(source,160,80,true);Fixture(background,240,160,false);string original=Hash(source);var m=Engine.Probe(source);var green=Engine.Probe(background);var p=new Project{Width=240,Height=160};p.Media.Add(m);p.Media.Add(green);p.Clips.Add(new Clip{MediaId=green.Id,Duration=1});var c=new Clip{MediaId=m.Id,Duration=1,Track=1,NativeSize=true,CropLeft=.5,CropTop=.25,CropRight=.125,CropBottom=.125};p.Clips.Add(c);Engine.Validate(p,true);
  preview.SetProject(p);preview.Seek(.2);await Task.Delay(150);CheckCrop(PreviewPixels(),"image preview");CheckCrop(ExportPixels(p,"image-crop"),"image export");Require(Hash(source)==original,"crop preview/export preserve source bytes");
  string saved=Path.Combine(root,"crop.tasia");Engine.Save(p,saved);var loaded=Engine.Load(saved).Clips[1];Require(loaded.CropLeft==.5&&loaded.CropTop==.25&&loaded.CropRight==.125&&loaded.CropBottom==.125,"save/reopen preserves all four crop sides");var serializer=new JavaScriptSerializer();var legacyData=serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText(saved));foreach(Dictionary<string,object> entry in (ArrayList)legacyData["Clips"])foreach(string key in new[]{"CropLeft","CropTop","CropRight","CropBottom"})entry.Remove(key);string legacy=serializer.Serialize(legacyData);string legacyFile=Path.Combine(root,"legacy.tasia");File.WriteAllText(legacyFile,legacy);var defaults=Engine.Load(legacyFile).Clips[1];Require(defaults.CropLeft==0&&defaults.CropTop==0&&defaults.CropRight==0&&defaults.CropBottom==0,"legacy project defaults to uncropped source");
  c.Rotation=90;preview.Refresh();await Task.Delay(100);CheckRotation(PreviewPixels(),"rotated preview");CheckRotation(ExportPixels(p,"rotated-crop"),"rotated export");c.Rotation=0;
  string video=Path.Combine(root,"split.mp4");Engine.Run("ffmpeg","-y -v error -loop 1 -i "+Engine.Quote(source)+" -t 1 -c:v libopenh264 -pix_fmt yuv420p "+Engine.Quote(video));var vm=Engine.Probe(video);p.Media.Add(vm);c.MediaId=vm.Id;CheckCrop(ExportPixels(p,"video-crop"),"video export");c.MediaId=m.Id;
  c.CropLeft=double.NaN;Reject(()=>Engine.Validate(p,false),"NaN crop rejected");c.CropLeft=double.PositiveInfinity;Reject(()=>Engine.Validate(p,false),"infinite crop rejected");c.CropLeft=-.01;Reject(()=>Engine.Validate(p,false),"negative crop rejected");c.CropLeft=.9;Reject(()=>Engine.Validate(p,false),"horizontal overcrop rejected");c.CropLeft=.5;c.CropTop=.9;Reject(()=>Engine.Validate(p,false),"vertical overcrop rejected");c.CropTop=.25;
  c.CropRight=0;c.CropLeft=.999;Reject(()=>Engine.Validate(p,false),"less than one source pixel remaining rejected");c.CropLeft=159.0/160;Engine.Validate(p,false);Require(true,"one full source pixel remaining accepted");c.CropLeft=.5;c.CropRight=.125;
  var text=new Clip{Text="crop unsupported",Duration=1,Track=2,CropLeft=.1};p.Clips.Add(text);Reject(()=>Engine.Validate(p,false),"text crop rejected");p.Clips.Remove(text);string audio=Path.Combine(root,"tone.wav");Engine.Run("ffmpeg","-y -v error -f lavfi -i sine=frequency=440:duration=1 "+Engine.Quote(audio));var am=Engine.Probe(audio);p.Media.Add(am);var ac=new Clip{MediaId=am.Id,Duration=1,Track=2,CropLeft=.1};p.Clips.Add(ac);Reject(()=>Engine.Validate(p,false),"audio crop rejected");p.Clips.Remove(ac);Require(Hash(source)==original,"all crop operations preserve original source bytes");
  preview.Dispose();File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);app.Shutdown(0);
 }catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);preview.Dispose();app.Shutdown(1);}};app.Run(window);}
}
