using System;
using System.Windows;
using System.Windows.Media;
namespace TasiaCraft {
 // A sibling of the viewport: never part of the exported Stage visual.
 public sealed class CanvasGuideOverlay : FrameworkElement {
  readonly LivePreview owner;double? x,y;
  public CanvasGuideOverlay(LivePreview owner){this.owner=owner;IsHitTestVisible=false;ClipToBounds=true;}
  internal void SetGuides(double? gx,double? gy){x=gx;y=gy;InvalidateVisual();}
  protected override void OnRender(DrawingContext dc){
   base.OnRender(dc);if(!x.HasValue&&!y.HasValue)return;
   try{var t=owner.Stage.TransformToVisual(this);var pen=new Pen(Brushes.Gold,1){DashStyle=DashStyles.Dash};pen.Freeze();
    if(x.HasValue)dc.DrawLine(pen,t.Transform(new Point(x.Value,0)),t.Transform(new Point(x.Value,owner.Stage.Height)));
    if(y.HasValue)dc.DrawLine(pen,t.Transform(new Point(0,y.Value)),t.Transform(new Point(owner.Stage.Width,y.Value)));
   }catch(InvalidOperationException){}
  }
 }
}
