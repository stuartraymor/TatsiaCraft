using System;
using System.Windows;
namespace TasiaCraft {
 public sealed class CanvasCropMoveResult {
  public readonly double X,Y,CropLeft,CropTop,CropRight,CropBottom;
  internal CanvasCropMoveResult(double x,double y,double left,double top,double right,double bottom){X=x;Y=y;CropLeft=left;CropTop=top;CropRight=right;CropBottom=bottom;}
  public void ApplyTo(Clip clip){if(clip==null)throw new ArgumentNullException("clip");clip.X=X;clip.Y=Y;clip.CropLeft=CropLeft;clip.CropTop=CropTop;clip.CropRight=CropRight;clip.CropBottom=CropBottom;}
 }
 // Move source media beneath a fixed visible aperture. Dimensions are the
 // rendered unscaled layer size, after fitting, as used by the compositor.
 public sealed class CanvasCropMoveOperation {
  readonly double x,y,left,top,right,bottom,width,height,co,si;
  readonly Point down;
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  static void CheckPointer(Point p){if(!Finite(p.X)||!Finite(p.Y)||Math.Abs(p.X)>1e9||Math.Abs(p.Y)>1e9)throw new ArgumentException("Crop move pointer is outside supported coordinates");}
  public CanvasCropMoveOperation(Clip clip,double sourceWidth,double sourceHeight,Point pointerDown){
   if(clip==null)throw new ArgumentNullException("clip");CheckPointer(pointerDown);
   if(!Finite(sourceWidth)||!Finite(sourceHeight)||sourceWidth<.01||sourceHeight<.01||sourceWidth>65536||sourceHeight>65536)throw new ArgumentException("Invalid crop move dimensions");
   if(!Finite(clip.Scale)||clip.Scale<.01||clip.Scale>8||!Finite(clip.X)||!Finite(clip.Y)||Math.Abs(clip.X)>32768||Math.Abs(clip.Y)>32768||!Finite(clip.Rotation))throw new ArgumentException("Invalid initial crop move transform");
   left=clip.CropLeft;top=clip.CropTop;right=clip.CropRight;bottom=clip.CropBottom;
   if(!Finite(left)||!Finite(top)||!Finite(right)||!Finite(bottom)||left<0||top<0||right<0||bottom<0||left+right>=1||top+bottom>=1)throw new ArgumentException("Invalid crop move boundaries");
   x=clip.X;y=clip.Y;width=sourceWidth*clip.Scale;height=sourceHeight*clip.Scale;double angle=(clip.Rotation%360)*Math.PI/180;co=Math.Cos(angle);si=Math.Sin(angle);down=pointerDown;
  }
  public CanvasCropMoveResult Evaluate(Point pointer){
   CheckPointer(pointer);Vector delta=pointer-down;
   double u=Math.Max(-right*width,Math.Min(left*width,delta.X*co+delta.Y*si));
   double v=Math.Max(-bottom*height,Math.Min(top*height,-delta.X*si+delta.Y*co));
   double dx=u*co-v*si,dy=u*si+v*co;
   // Shorten the whole movement at the position boundary, keeping the aperture
   // fixed instead of independently clipping the source translation afterward.
   double fraction=1;
   if(dx>0)fraction=Math.Min(fraction,(32768-x)/dx);else if(dx<0)fraction=Math.Min(fraction,(-32768-x)/dx);
   if(dy>0)fraction=Math.Min(fraction,(32768-y)/dy);else if(dy<0)fraction=Math.Min(fraction,(-32768-y)/dy);
   u*=fraction;v*=fraction;dx*=fraction;dy*=fraction;
   return new CanvasCropMoveResult(x+dx,y+dy,Math.Max(0,left-u/width),Math.Max(0,top-v/height),Math.Max(0,right+u/width),Math.Max(0,bottom+v/height));
  }
 }
}
