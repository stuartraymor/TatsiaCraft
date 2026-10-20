using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace TasiaCraft {
 // A cursor or click effect on a screen-recording clip. Names follow the reference Cursor Effects tool (Cursor tab,
 // Left Click and Right Click tabs); drawing is independent. One Draw routine serves the live preview and export.
 public class CursorEffect { public string Name; public double Size=30; public string Color; }
 public static class CursorEffects {
  public static readonly string[] CursorCatalog={"Cursor Color","Cursor Glow","Cursor Highlight","Cursor Shadow","Cursor Smoothing","Cursor Spotlight"};
  public static readonly string[] ClickCatalog={"Left Click Burst 1","Left Click Rings","Left Click Target","Right Click Burst 1","Right Click Rings","Right Click Target"};
  public static IEnumerable<string> Catalog{get{return CursorCatalog.Concat(ClickCatalog);}}
  public const double ClickDuration=.5;
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  public static List<CursorEffect> Of(Clip c){if(c.CursorEffects==null)c.CursorEffects=new List<CursorEffect>();return c.CursorEffects;}
  public static string DefaultColor(string name){return name=="Cursor Highlight"?"#FFE14D":name=="Cursor Glow"?"#3CE36B":name=="Cursor Color"?"#FFD000":name.StartsWith("Left Click")?"#E53935":name=="Right Click Rings"?"#F2C94C":name.StartsWith("Right Click")?"#2F6BFF":"#000000";}
  public static double DefaultSize(string name){return name=="Cursor Spotlight"?90:name=="Cursor Highlight"?36:name=="Cursor Smoothing"?.15:name=="Cursor Glow"?6:30;}
  public static bool HasCursorData(Media m){return m!=null&&m.Kind=="video"&&!string.IsNullOrEmpty(m.CursorPath);}
  public static void Validate(Project p){foreach(var c in p.Clips){if(c.CursorEffects==null)c.CursorEffects=new List<CursorEffect>();if(!Finite(c.CursorScale)||c.CursorScale<.25||c.CursorScale>4||!Finite(c.CursorOpacity)||c.CursorOpacity<0||c.CursorOpacity>1)throw new InvalidDataException("Invalid cursor scale or opacity");if(c.CursorEffects.Count==0)continue;if(c.CursorEffects.Count>16)throw new InvalidDataException("Too many cursor effects");if(!HasCursorData(p.Media.FirstOrDefault(m=>m.Id==c.MediaId)))throw new InvalidDataException("Cursor effects need a recording with cursor data");foreach(var e in c.CursorEffects){Color unused;if(e==null||!Catalog.Contains(e.Name)||!Finite(e.Size)||e.Size<=0||e.Size>1000||!TryColor(e.Color??DefaultColor(e.Name),out unused))throw new InvalidDataException("Invalid cursor effect");}}}
  public static bool TryColor(string text,out Color color){color=Colors.Black;try{color=(Color)ColorConverter.ConvertFromString(text);return true;}catch{return false;}}
  static Color ColorOf(CursorEffect e){Color c;TryColor(e.Color??DefaultColor(e.Name),out c);return c;}
  // Maps a source-pixel point to project coordinates using the same geometry as the live preview and export.
  public static bool Map(Project p,Clip c,Media m,double local,double sx,double sy,out Point point,out double pixel){
   point=new Point();pixel=1;if(m==null||m.Width<1||m.Height<1)return false;
   if(sx<m.Width*c.CropLeft||sy<m.Height*c.CropTop||sx>m.Width*(1-c.CropRight)||sy>m.Height*(1-c.CropBottom))return false;
   double k=c.Animate&&c.Duration>0?Math.Max(0,Math.Min(1,local/c.Duration)):0;var b=Behaviors.Evaluate(c,local,p.Width,p.Height);
   double scale=(c.Scale+(c.EndScale-c.Scale)*k)*b.Scale,x=c.X+(c.EndX-c.X)*k+b.X,y=c.Y+(c.EndY-c.Y)*k+b.Y;
   double w=m.Width,h=m.Height;if(!c.NativeSize){double fit=Math.Min(p.Width/w,p.Height/h);w*=fit;h*=fit;}
   double cx=(p.Width-w)/2+x+w/2,cy=(p.Height-h)/2+y+h/2,dx=(sx/m.Width-.5)*w*scale,dy=(sy/m.Height-.5)*h*scale,a=(c.Rotation+b.Rotation)*Math.PI/180;
   point=new Point(cx+dx*Math.Cos(a)-dy*Math.Sin(a),cy+dx*Math.Sin(a)+dy*Math.Cos(a));pixel=w/m.Width*scale;return true;
  }
  static Geometry Arrow(){return Geometry.Parse("M0,0 L0,17 L4,13 L7,20 L10,19 L7,12 L12,12 Z");}
  static Geometry Shape(string kind){return kind=="ibeam"?Geometry.Parse("M-3,-8 L3,-8 M0,-8 L0,8 M-3,8 L3,8"):kind=="hand"?Geometry.Parse("M4,0 L7,0 L7,8 L10,7 L13,8 L15,9 L16,18 L13,22 L6,22 L1,14 L3,12 L4,13 Z"):kind=="wait"?new EllipseGeometry(new Point(0,0),8,8):Arrow();}
  // Draws the recorded cursor and its effects for one clip at a clip-local time, in project coordinates.
  public static void Draw(DrawingContext d,Project p,Clip c,CursorTrack track,double local){
   var m=p.Media.FirstOrDefault(x=>x.Id==c.MediaId);if(track==null||!HasCursorData(m))return;double source=c.In+local*c.Speed,ratio=m.Width/(double)track.Width,ratioY=m.Height/(double)track.Height;
   var effects=Of(c);var smoothing=effects.FirstOrDefault(e=>e.Name=="Cursor Smoothing");double cxs,cys;string kind;bool visible=track.At(source,smoothing==null?0:smoothing.Size,out cxs,out cys,out kind);
   Point at;double px;bool mapped=Map(p,c,m,local,cxs*ratio,cys*ratioY,out at,out px);double unit=Math.Max(.05,px*ratio);
   double clipAlpha=Math.Max(0,Math.Min(1,c.Opacity))*Behaviors.Evaluate(c,local,p.Width,p.Height).Alpha;if(clipAlpha<=0)return;d.PushOpacity(clipAlpha);
   if(mapped&&visible){
    foreach(var e in effects.Where(e=>e.Name=="Cursor Spotlight")){var hole=new EllipseGeometry(at,e.Size*unit,e.Size*unit);var dim=new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(0,0,p.Width,p.Height)),hole);var color=ColorOf(e);d.DrawGeometry(new SolidColorBrush(Color.FromArgb(150,color.R,color.G,color.B)),null,dim);}
    foreach(var e in effects.Where(e=>e.Name=="Cursor Highlight")){var color=ColorOf(e);d.DrawEllipse(new SolidColorBrush(Color.FromArgb(110,color.R,color.G,color.B)),null,at,e.Size*unit,e.Size*unit);}
   }
   foreach(var click in track.Clicks){double age=source-click.T;if(age<0||age>ClickDuration*c.Speed)continue;double f=age/(ClickDuration*c.Speed);string prefix=click.Button=="left"?"Left Click ":"Right Click ";Point cp;double cpx;if(!Map(p,c,m,local,click.X*ratio,click.Y*ratioY,out cp,out cpx))continue;double u=Math.Max(.05,cpx*ratio);
    foreach(var e in effects.Where(e=>e.Name.StartsWith(prefix))){var color=ColorOf(e);var brush=new SolidColorBrush(Color.FromArgb((byte)(255*(1-f)),color.R,color.G,color.B));var pen=new Pen(brush,Math.Max(1,3*u));double r=e.Size*u;
     if(e.Name.EndsWith("Rings"))d.DrawEllipse(null,pen,cp,r*(.2+.8*f),r*(.2+.8*f));
     else if(e.Name.EndsWith("Burst 1"))for(int i=0;i<8;i++){double a=i*Math.PI/4,r0=r*(.25+.5*f),r1=r0+r*.35;d.DrawLine(pen,new Point(cp.X+r0*Math.Cos(a),cp.Y+r0*Math.Sin(a)),new Point(cp.X+r1*Math.Cos(a),cp.Y+r1*Math.Sin(a)));}
     else if(e.Name.EndsWith("Target")){double tr=r*(1-.6*f);d.DrawEllipse(null,pen,cp,tr,tr);d.DrawLine(pen,new Point(cp.X-tr*1.3,cp.Y),new Point(cp.X+tr*1.3,cp.Y));d.DrawLine(pen,new Point(cp.X,cp.Y-tr*1.3),new Point(cp.X,cp.Y+tr*1.3));}
    }}
   if(mapped&&visible&&c.CursorOpacity>0){
    var geometry=Shape(kind).Clone();var tg=new TransformGroup();tg.Children.Add(new ScaleTransform(unit*c.CursorScale,unit*c.CursorScale));tg.Children.Add(new RotateTransform(c.Rotation+Behaviors.Evaluate(c,local,p.Width,p.Height).Rotation));tg.Children.Add(new TranslateTransform(at.X,at.Y));geometry.Transform=tg;
    var colorEffect=effects.FirstOrDefault(e=>e.Name=="Cursor Color");var fill=colorEffect!=null?new SolidColorBrush(ColorOf(colorEffect)):Brushes.White;
    d.PushOpacity(c.CursorOpacity);
    foreach(var e in effects.Where(e=>e.Name=="Cursor Shadow")){var shadow=geometry.Clone();var st=new TransformGroup();st.Children.Add(tg);st.Children.Add(new TranslateTransform(3*unit,3*unit));shadow.Transform=st;var color=ColorOf(e);d.DrawGeometry(new SolidColorBrush(Color.FromArgb(120,color.R,color.G,color.B)),null,shadow);}
    foreach(var e in effects.Where(e=>e.Name=="Cursor Glow")){var color=ColorOf(e);d.DrawGeometry(null,new Pen(new SolidColorBrush(Color.FromArgb(170,color.R,color.G,color.B)),e.Size*unit*c.CursorScale){LineJoin=PenLineJoin.Round},geometry);}
    if(kind=="ibeam")d.DrawGeometry(null,new Pen(Brushes.Black,3*unit*c.CursorScale),geometry);
    d.DrawGeometry(kind=="ibeam"?null:fill,new Pen(kind=="ibeam"?fill:Brushes.Black,(kind=="ibeam"?1.6:1.1)*unit*c.CursorScale){LineJoin=PenLineJoin.Round},geometry);
    d.Pop();
   }
   d.Pop();
  }
  // Renders the cursor layer for a clip to a transparent QuickTime RLE video at project size and frame rate.
  public static void RenderOverlay(Project p,Clip c,CursorTrack track,string output){
   int frames=Math.Max(1,(int)Math.Ceiling(c.Duration*p.Fps));Exception failure=null;
   var psi=new ProcessStartInfo(Engine.Tool("ffmpeg"),"-hide_banner -v error -y -f rawvideo -pix_fmt bgra -s "+p.Width+"x"+p.Height+" -r "+p.Fps+" -i - -c:v qtrle -pix_fmt argb "+Engine.Quote(output)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
   var thread=new Thread(()=>{
    var ffmpeg=Process.Start(psi);var errors=ffmpeg.StandardError.ReadToEndAsync();
    try{var stream=ffmpeg.StandardInput.BaseStream;var bitmap=new RenderTargetBitmap(p.Width,p.Height,96,96,PixelFormats.Pbgra32);var buffer=new byte[p.Width*p.Height*4];
     for(int i=0;i<frames;i++){var visual=new DrawingVisual();using(var d=visual.RenderOpen())Draw(d,p,c,track,i/(double)p.Fps);bitmap.Clear();bitmap.Render(visual);bitmap.CopyPixels(buffer,p.Width*4,0);
      for(int j=0;j<buffer.Length;j+=4){int a=buffer[j+3];if(a>0&&a<255){buffer[j]=(byte)Math.Min(255,buffer[j]*255/a);buffer[j+1]=(byte)Math.Min(255,buffer[j+1]*255/a);buffer[j+2]=(byte)Math.Min(255,buffer[j+2]*255/a);}}
      stream.Write(buffer,0,buffer.Length);}
     stream.Close();ffmpeg.WaitForExit();if(ffmpeg.ExitCode!=0)throw new Exception("Cursor overlay failed: "+errors.Result);}
    catch(Exception e){failure=e;try{if(!ffmpeg.HasExited)ffmpeg.Kill();}catch{}}
   });thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(failure!=null)throw failure;
  }
 }
}
