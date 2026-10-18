using System;
using System.Collections.Generic;
using System.Windows;
namespace TasiaCraft {
 public sealed class CanvasSnapResult {
  public readonly double DeltaX,DeltaY; public readonly double? GuideX,GuideY;
  internal CanvasSnapResult(double x,double y,double? gx,double? gy){DeltaX=x;DeltaY=y;GuideX=gx;GuideY=gy;}
 }
 // Independent alignment of visible, axis-aligned crop bounds.
 public static class CanvasAlignment {
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  static void Check(Rect r){if(r.IsEmpty||!Finite(r.X)||!Finite(r.Y)||!Finite(r.Width)||!Finite(r.Height)||!Finite(r.Right)||!Finite(r.Bottom)||r.Width<=0||r.Height<=0)throw new ArgumentException("Invalid alignment bounds");}
  public static Rect Bounds(Clip c,double width,double height,int projectWidth,int projectHeight){
   if(c==null)throw new ArgumentNullException("c");
   if(!Finite(width)||!Finite(height)||width<=0||height<=0||projectWidth<2||projectHeight<2||projectWidth>8192||projectHeight>8192||!Finite(c.Scale)||c.Scale<.01||c.Scale>8||!Finite(c.X)||!Finite(c.Y)||!Finite(c.Rotation))throw new ArgumentException("Invalid alignment transform");
   if(!Finite(c.CropLeft)||!Finite(c.CropRight)||!Finite(c.CropTop)||!Finite(c.CropBottom)||c.CropLeft<0||c.CropRight<0||c.CropTop<0||c.CropBottom<0||c.CropLeft+c.CropRight>=1||c.CropTop+c.CropBottom>=1)throw new ArgumentException("Invalid alignment crop");
   double a=(c.Rotation%360)*Math.PI/180,co=Math.Cos(a),si=Math.Sin(a);Rect r=Rect.Empty;
   foreach(double u in new[]{c.CropLeft-.5,.5-c.CropRight})foreach(double v in new[]{c.CropTop-.5,.5-c.CropBottom}){
    double x=u*width*c.Scale,y=v*height*c.Scale;r.Union(new Point(projectWidth/2.0+c.X+x*co-y*si,projectHeight/2.0+c.Y+x*si+y*co));
   }
   Check(r);return r;
  }
  public static CanvasSnapResult Snap(Rect moving,IEnumerable<Rect> targets,double tolerance){
   Check(moving);if(targets==null)throw new ArgumentNullException("targets");if(!Finite(tolerance)||tolerance<0)throw new ArgumentException("Invalid snapping tolerance");
   double dx=0,dy=0,bestX=tolerance,bestY=tolerance;double? gx=null,gy=null;
   foreach(var r in targets){Check(r);
    foreach(double target in new[]{r.Left,(r.Left+r.Right)/2,r.Right})foreach(double own in new[]{moving.Left,(moving.Left+moving.Right)/2,moving.Right}){double d=target-own;if(Math.Abs(d)<=tolerance&&(!gx.HasValue||Math.Abs(d)<bestX)){bestX=Math.Abs(d);dx=d;gx=target;}}
    foreach(double target in new[]{r.Top,(r.Top+r.Bottom)/2,r.Bottom})foreach(double own in new[]{moving.Top,(moving.Top+moving.Bottom)/2,moving.Bottom}){double d=target-own;if(Math.Abs(d)<=tolerance&&(!gy.HasValue||Math.Abs(d)<bestY)){bestY=Math.Abs(d);dy=d;gy=target;}}
   }
   return new CanvasSnapResult(dx,dy,gx,gy);
  }
  public static Vector Align(Rect moving,int projectWidth,int projectHeight,string alignment){
   Check(moving);if(projectWidth<2||projectHeight<2||projectWidth>8192||projectHeight>8192)throw new ArgumentException("Invalid project dimensions");
   switch(alignment){case "Left":return new Vector(-moving.Left,0);case "Horizontal center":return new Vector(projectWidth/2.0-(moving.Left+moving.Right)/2,0);case "Right":return new Vector(projectWidth-moving.Right,0);case "Top":return new Vector(0,-moving.Top);case "Vertical center":return new Vector(0,projectHeight/2.0-(moving.Top+moving.Bottom)/2);case "Bottom":return new Vector(0,projectHeight-moving.Bottom);default:throw new ArgumentException("Unknown canvas alignment");}
  }
 }
}
