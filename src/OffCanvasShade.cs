using System.Windows;
using System.Windows.Media;
namespace TasiaCraft {
 // Editing decoration only. The project-sized export contains no shaded area.
 public sealed class OffCanvasShade : FrameworkElement {
  static readonly Brush shade=MakeShade();
  Geometry outside;Rect lastView=Rect.Empty,lastProject=Rect.Empty;
  static Brush MakeShade(){var brush=new SolidColorBrush(Color.FromArgb(192,28,35,39));brush.Freeze();return brush;}
  public OffCanvasShade(){IsHitTestVisible=false;Focusable=false;ClipToBounds=false;}
  internal void UpdateBounds(Rect view,Rect project){
   if(view==lastView&&project==lastProject)return;lastView=view;lastProject=project;
   outside=new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(view),new RectangleGeometry(project));outside.Freeze();InvalidateVisual();
  }
  protected override void OnRender(DrawingContext drawing){base.OnRender(drawing);if(outside!=null)drawing.DrawGeometry(shade,null,outside);}
 }
}
