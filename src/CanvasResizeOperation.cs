using System;
using System.Collections.Generic;
using System.Windows;
namespace TasiaCraft {
 // Independent gesture geometry: anchor the opposite visible crop handle,
 // while keeping the original source center as the scale/rotation pivot.
 // Width/height inputs are the rendered unscaled layer dimensions after any fit.
 public sealed class CanvasResizeResult {
  public readonly double Scale,X,Y;
  internal CanvasResizeResult(double scale,double x,double y){Scale=scale;X=x;Y=y;}
  public void ApplyTo(Clip clip){if(clip==null)throw new ArgumentNullException("clip");clip.Scale=Scale;clip.X=X;clip.Y=Y;}
 }
 public sealed class CanvasResizeSnapResult {
  public readonly CanvasResizeResult Result;public readonly double? GuideX,GuideY;
  internal CanvasResizeSnapResult(CanvasResizeResult result,double? x,double? y){Result=result;GuideX=x;GuideY=y;}
 }
 public sealed class CanvasResizeOperation {
  readonly double oldScale,oldX,oldY,projectWidth,projectHeight,minRatio,maxRatio;
  readonly Point pointerDown,center,anchor;
  readonly Vector diagonal;
  readonly Rect initialBounds;
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  static void CheckPointer(Point p){if(!Finite(p.X)||!Finite(p.Y)||Math.Abs(p.X)>1e9||Math.Abs(p.Y)>1e9)throw new ArgumentException("Resize pointer is outside supported coordinates");}
  static void Restrict(double offset,double direction,ref double low,ref double high){
   if(Math.Abs(direction)<1e-12)return;
   double a=(-32768-offset)/direction,b=(32768-offset)/direction;
   low=Math.Max(low,Math.Min(a,b));high=Math.Min(high,Math.Max(a,b));
  }
  static Point RotatePoint(double x,double y,double angle,Point origin){double co=Math.Cos(angle),si=Math.Sin(angle);return new Point(origin.X+x*co-y*si,origin.Y+x*si+y*co);}
  public CanvasResizeOperation(Clip clip,double sourceWidth,double sourceHeight,double projectWidth,double projectHeight,int handleX,int handleY,Point pointerDown){
   if(clip==null)throw new ArgumentNullException("clip");CheckPointer(pointerDown);
   if(handleX<0||handleX>2||handleY<0||handleY>2||handleX==1&&handleY==1)throw new ArgumentException("Choose an edge or corner resize handle");
   if(!Finite(sourceWidth)||!Finite(sourceHeight)||!Finite(projectWidth)||!Finite(projectHeight)||sourceWidth<.01||sourceHeight<.01||sourceWidth>65536||sourceHeight>65536||projectWidth<2||projectHeight<2||projectWidth>8192||projectHeight>8192)throw new ArgumentException("Invalid resize dimensions");
   if(!Finite(clip.Scale)||clip.Scale<.01||clip.Scale>8||!Finite(clip.X)||!Finite(clip.Y)||Math.Abs(clip.X)>32768||Math.Abs(clip.Y)>32768||!Finite(clip.Rotation))throw new ArgumentException("Invalid initial transform");
   if(!Finite(clip.CropLeft)||!Finite(clip.CropRight)||!Finite(clip.CropTop)||!Finite(clip.CropBottom)||clip.CropLeft<0||clip.CropRight<0||clip.CropTop<0||clip.CropBottom<0||clip.CropLeft+clip.CropRight>=1||clip.CropTop+clip.CropBottom>=1)throw new ArgumentException("Invalid resize crop");
   this.pointerDown=pointerDown;this.projectWidth=projectWidth;this.projectHeight=projectHeight;oldScale=clip.Scale;oldX=clip.X;oldY=clip.Y;
   center=new Point(projectWidth/2+oldX,projectHeight/2+oldY);
   double w=sourceWidth*oldScale,h=sourceHeight*oldScale;
   double[] xs={w*(clip.CropLeft-.5),w*(clip.CropLeft-clip.CropRight)/2,w*(.5-clip.CropRight)};
   double[] ys={h*(clip.CropTop-.5),h*(clip.CropTop-clip.CropBottom)/2,h*(.5-clip.CropBottom)};
   double angle=(clip.Rotation%360)*Math.PI/180;
   Point handle=RotatePoint(xs[handleX],ys[handleY],angle,center);
   Rect bounds=Rect.Empty;foreach(double x in new[]{xs[0],xs[2]})foreach(double y in new[]{ys[0],ys[2]})bounds.Union(RotatePoint(x,y,angle,center));initialBounds=bounds;
   anchor=RotatePoint(xs[2-handleX],ys[2-handleY],angle,center);diagonal=handle-anchor;
   if(diagonal.LengthSquared<1e-16)throw new ArgumentException("Resize handle span is too small");
   double low=.01/oldScale,high=8/oldScale;
   Restrict(anchor.X-projectWidth/2,center.X-anchor.X,ref low,ref high);
   Restrict(anchor.Y-projectHeight/2,center.Y-anchor.Y,ref low,ref high);
   minRatio=low;maxRatio=high;
  }
  public CanvasResizeResult Evaluate(Point pointer){
   CheckPointer(pointer);Vector delta=pointer-pointerDown;
   double ratio=1+Vector.Multiply(delta,diagonal)/diagonal.LengthSquared;
   ratio=Math.Max(minRatio,Math.Min(maxRatio,ratio));
   return AtRatio(ratio);
  }
  CanvasResizeResult AtRatio(double ratio){
   if(ratio==1)return new CanvasResizeResult(oldScale,oldX,oldY);
   Point next=anchor+(center-anchor)*ratio;
   return new CanvasResizeResult(Math.Max(.01,Math.Min(8,oldScale*ratio)),Math.Max(-32768,Math.Min(32768,next.X-projectWidth/2)),Math.Max(-32768,Math.Min(32768,next.Y-projectHeight/2)));
  }
  // Snap the one permitted scale degree of freedom rather than translating
  // the result, so the opposite visible handle remains exactly anchored.
  public CanvasResizeSnapResult EvaluateSnapped(Point pointer,IEnumerable<Rect> targets,double tolerance){
   var raw=Evaluate(pointer);if(targets==null)throw new ArgumentNullException("targets");if(!Finite(tolerance)||tolerance<0)throw new ArgumentException("Invalid resize snapping tolerance");
   double ratio=raw.Scale/oldScale,bestRatio=ratio,bestDistance=double.PositiveInfinity;double? guideX=null,guideY=null;var admitted=new List<Rect>();
   double[] xs={initialBounds.Left,initialBounds.Right,(initialBounds.Left+initialBounds.Right)/2},ys={initialBounds.Top,initialBounds.Bottom,(initialBounds.Top+initialBounds.Bottom)/2};
   foreach(var r in targets){
    if(r.IsEmpty||!Finite(r.X)||!Finite(r.Y)||!Finite(r.Width)||!Finite(r.Height)||!Finite(r.Right)||!Finite(r.Bottom)||r.Width<=0||r.Height<=0)throw new ArgumentException("Invalid resize snapping target");admitted.Add(r);
    for(int axis=0;axis<2;axis++)foreach(double target in axis==0?new[]{r.Left,r.Right,(r.Left+r.Right)/2}:new[]{r.Top,r.Bottom,(r.Top+r.Bottom)/2})foreach(double initial in axis==0?xs:ys){
     double origin=axis==0?anchor.X:anchor.Y,slope=initial-origin;if(Math.Abs(slope)<1e-9)continue;
     if(Math.Abs(target-(origin+slope*ratio))>tolerance+1e-9)continue;
     double candidate=(target-origin)/slope;if(candidate<minRatio-1e-12||candidate>maxRatio+1e-12)continue;candidate=Math.Max(minRatio,Math.Min(maxRatio,candidate));
     double distance=Math.Abs(candidate-ratio)*diagonal.Length;
     if(distance>tolerance+1e-9||distance>=bestDistance-1e-9)continue;
     bestRatio=candidate;bestDistance=distance;guideX=axis==0?(double?)target:null;guideY=axis==1?(double?)target:null;
    }
   }
   if(!double.IsPositiveInfinity(bestDistance))foreach(var r in admitted){
    if(!guideX.HasValue)foreach(double target in new[]{r.Left,r.Right,(r.Left+r.Right)/2})foreach(double initial in xs)if(Math.Abs(initial-anchor.X)>1e-9&&Math.Abs(anchor.X+(initial-anchor.X)*bestRatio-target)<1e-7)guideX=target;
    if(!guideY.HasValue)foreach(double target in new[]{r.Top,r.Bottom,(r.Top+r.Bottom)/2})foreach(double initial in ys)if(Math.Abs(initial-anchor.Y)>1e-9&&Math.Abs(anchor.Y+(initial-anchor.Y)*bestRatio-target)<1e-7)guideY=target;
   }
   return new CanvasResizeSnapResult(AtRatio(bestRatio),guideX,guideY);
  }
 }
}
