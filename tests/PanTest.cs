using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using TasiaCraft;

public static class PanTest {
 static string log;static int passed,beforeEdits;static LivePreview live;
 static void Require(bool value,string label){if(!value)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 static Point Origin(){live.UpdateLayout();return live.Stage.TransformToAncestor(live).Transform(new Point(0,0));}
 static double Width(){var t=live.Stage.TransformToAncestor(live);return (t.Transform(new Point(live.Stage.Width,0))-t.Transform(new Point(0,0))).Length;}
 static bool Near(double a,double b){return Math.Abs(a-b)<.05;}
 static bool Offset(double x,double y){return Near(live.ViewOffset.X,x)&&Near(live.ViewOffset.Y,y);}
 static int Handles(){return live.Stage.Children.OfType<Canvas>().Sum(c=>c.Children.Count);}
 static void Invalid(double x,double y,string label){var prior=live.ViewOffset;bool rejected=false;try{live.SetViewOffset(x,y);}catch(ArgumentException){rejected=true;}Require(rejected,label+" rejects invalid offset");Require(live.ViewOffset==prior,label+" preserves prior view offset");}
 [STAThread]public static void Main(string[] args){string root=args[0];Directory.CreateDirectory(root);log=Path.Combine(root,"results.txt");File.WriteAllText(log,"");var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};live=new LivePreview();var host=new Grid();host.Children.Add(live);var window=new Window{Title="TatsiaCraft isolated canvas pan acceptance",Width=940,Height=740,Content=host};Window detached=null;window.Loaded+=async(s,e)=>{try{
  string image=Path.Combine(root,"source.png");using(var bitmap=new System.Drawing.Bitmap(320,180)){using(var g=System.Drawing.Graphics.FromImage(bitmap)){g.Clear(System.Drawing.Color.DarkOrange);}bitmap.Save(image,System.Drawing.Imaging.ImageFormat.Png);}
  var media=new Media{Path=image,Name="source.png",Kind="image",Duration=5,Width=320,Height=180};var project=new Project{Width=320,Height=180};project.Media.Add(media);var clip=new Clip{MediaId=media.Id,Duration=5,NativeSize=true};project.Clips.Add(clip);live.BeforeEdit+=()=>beforeEdits++;live.SetProject(project);live.Seek(1);live.SetSelection(clip);live.SetViewZoom(1);await Task.Delay(100);string original=new JavaScriptSerializer().Serialize(project);
  Require(!live.PanMode&&Offset(0,0),"new canvas starts in centered select mode");Require(Handles()>0,"selected image starts with editing handles");live.SetPanMode(true);Require(live.PanMode&&Handles()==0,"pan mode hides selection editing handles");
  foreach(double zoom in new[]{1.0,2.0}){live.SetViewZoom(zoom);live.ResetViewOffset();await Task.Delay(50);Point baseline=Origin();double width=Width();live.SetViewOffset(50,30);await Task.Delay(50);Point shifted=Origin();Require(Near(shifted.X-baseline.X,50)&&Near(shifted.Y-baseline.Y,30),"view translation is 50 by 30 logical pixels at "+(zoom*100)+" percent");Require(Near(Width(),width),"pan preserves canvas dimensions at "+(zoom*100)+" percent");Require(Offset(50,30),"view reports exact offset at "+(zoom*100)+" percent");live.ResetViewOffset();Point reset=Origin();Require(Offset(0,0)&&Near(reset.X,baseline.X)&&Near(reset.Y,baseline.Y),"reset restores centered origin at "+(zoom*100)+" percent");}
  live.SetViewOffset(50,30);live.SetProject(project);Require(Offset(50,30),"same-project refresh retains view offset");live.Refresh();Require(Offset(50,30),"source refresh retains view offset");live.SetViewZoom(1);Require(Offset(50,30),"zoom change retains logical view offset");
  host.Children.Remove(live);detached=new Window{Title="TatsiaCraft isolated pan detach",Width=940,Height=740,Content=live};detached.Show();await Task.Delay(80);Require(Offset(50,30)&&Window.GetWindow(live)==detached,"detach retains compositor view offset");detached.Content=null;host.Children.Add(live);detached.Close();detached=null;await Task.Delay(80);Require(Offset(50,30)&&Window.GetWindow(live)==window,"reattach retains compositor view offset");
  live.SetPanMode(false);Require(!live.PanMode&&Handles()>0,"select mode restores editing handles");live.SetCropMode(true);Require(live.CropMode,"crop mode enabled for source image");live.SetPanMode(true);Require(live.PanMode&&!live.CropMode&&Handles()==0,"pan mode disables crop mode");live.SetCropMode(true);Require(live.CropMode&&!live.PanMode,"crop mode disables pan mode");live.SetCropMode(false);
  Invalid(double.NaN,0,"NaN X");Invalid(0,double.NaN,"NaN Y");Invalid(double.PositiveInfinity,0,"infinite X");Invalid(0,double.NegativeInfinity,"infinite Y");Require(beforeEdits==0,"view pan reset mode zoom and detach create no document edits");Require(new JavaScriptSerializer().Serialize(project)==original,"all viewport operations preserve complete serialized project");Require(clip.X==0&&clip.Y==0&&clip.Scale==1&&clip.CropLeft==0&&clip.CropTop==0&&clip.CropRight==0&&clip.CropBottom==0,"clip position scale and crop remain unchanged");
  live.SetViewOffset(-37,22);var next=new Project{Width=320,Height=180};live.SetProject(next);Require(Offset(0,0),"opening a different project centers view");Require(beforeEdits==0,"new-project centering creates no document edit checkpoint");live.Dispose();File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);app.Shutdown(0);
 }catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);if(detached!=null)detached.Close();live.Dispose();app.Shutdown(1);}};app.Run(window);}
}
