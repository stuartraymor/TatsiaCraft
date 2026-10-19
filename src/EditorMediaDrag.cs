using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace TasiaCraft {
 public sealed class MediaBinDragPayload {
  public const string Format="TatsiaCraft.MediaSelection";
  public Project Source {get;private set;}readonly Media[] media;
  public Media[] Media {get{return media.ToArray();}}
  public MediaBinDragPayload(Project source,Media[] selection){Source=source;media=selection==null?new Media[0]:selection.ToArray();}
  public static bool BeyondThreshold(Point origin,Point point,double horizontal,double vertical){return Math.Abs(point.X-origin.X)>=horizontal||Math.Abs(point.Y-origin.Y)>=vertical;}
 }
 public partial class Editor {
  // Own-source test seam: production always enters WPF's operating-system drag loop.
  Func<DependencyObject,IDataObject,DragDropEffects,DragDropEffects> mediaDragExecutor=null;
  Action<Point,bool> mediaDragMove;
  void WireMediaBinDrag(ListBox target){
   Point origin=new Point();bool pressed=false;Project source=null;ListBoxItem seed=null,deferred=null;
   target.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,new MouseButtonEventHandler((s,e)=>{pressed=false;deferred=null;if(busy||!object.ReferenceEquals(bin,target))return;seed=e.OriginalSource as ListBoxItem;if(seed==null||!target.Items.Contains(seed))seed=ItemsControl.ContainerFromElement(target,e.OriginalSource as DependencyObject) as ListBoxItem;if(seed==null||!(seed.Tag is Media))return;origin=e.GetPosition(target);source=project;pressed=true;
    if(Keyboard.Modifiers==ModifierKeys.None&&seed.IsSelected&&target.SelectedItems.Count>1){deferred=seed;target.Focus();e.Handled=true;}
   }),true);
   target.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent,new MouseButtonEventHandler((s,e)=>{pressed=false;if(deferred!=null&&object.ReferenceEquals(target,bin)&&object.ReferenceEquals(source,project)){target.SelectedItems.Clear();deferred.IsSelected=true;e.Handled=true;}deferred=null;}),true);
   mediaDragMove=(point,leftDown)=>{if(!leftDown){pressed=false;deferred=null;return;}if(!pressed||busy||!object.ReferenceEquals(source,project)||!object.ReferenceEquals(target,bin))return;if(!MediaBinDragPayload.BeyondThreshold(origin,point,SystemParameters.MinimumHorizontalDragDistance,SystemParameters.MinimumVerticalDragDistance))return;
    pressed=false;deferred=null;if(seed==null||!seed.IsSelected)return;var selected=target.Items.OfType<ListBoxItem>().Where(i=>i.IsSelected&&i.Tag is Media).Select(i=>(Media)i.Tag).ToArray();if(selected.Length==0)return;var data=new DataObject(MediaBinDragPayload.Format,new MediaBinDragPayload(source,selected));
    if(mediaDragExecutor!=null)mediaDragExecutor(target,data,DragDropEffects.Copy);else DragDrop.DoDragDrop(target,data,DragDropEffects.Copy);
   };
   var move=mediaDragMove;target.PreviewMouseMove+=(s,e)=>move(e.GetPosition(target),e.LeftButton==MouseButtonState.Pressed);
  }
  DragDropEffects MediaDragOverEffects(IDataObject data,Point point){
   if(busy||data==null||!data.GetDataPresent(MediaBinDragPayload.Format))return DragDropEffects.None;var payload=data.GetData(MediaBinDragPayload.Format) as MediaBinDragPayload;
   if(payload==null||!object.ReferenceEquals(payload.Source,project)||payload.Media.Length==0||payload.Media.Any(m=>m==null||!project.Media.Contains(m)))return DragDropEffects.None;
   if(double.IsNaN(point.X)||double.IsNaN(point.Y)||double.IsInfinity(point.X)||double.IsInfinity(point.Y)||zoom<=0||double.IsNaN(zoom)||double.IsInfinity(zoom))return DragDropEffects.None;
   int track=(int)Math.Max(0,Math.Min(project.TrackCount-1,project.TrackCount-1-Math.Floor((point.Y-TracksTop)/LaneHeight)));return project.LockedTracks.Contains(track)||(point.X-TrackHeaderWidth)/zoom>86400?DragDropEffects.None:DragDropEffects.Copy;
  }
  DragDropEffects DropMediaSelection(IDataObject data,Point point){
   if(busy||data==null||!data.GetDataPresent(MediaBinDragPayload.Format))return DragDropEffects.None;var payload=data.GetData(MediaBinDragPayload.Format) as MediaBinDragPayload;
   if(payload==null||!object.ReferenceEquals(payload.Source,project)){Info("Media drag belongs to another project. Select current Media Bin items.");return DragDropEffects.None;}
   if(double.IsNaN(point.X)||double.IsNaN(point.Y)||double.IsInfinity(point.X)||double.IsInfinity(point.Y)||double.IsNaN(zoom)||double.IsInfinity(zoom)||zoom<=0)return DragDropEffects.None;
   double time=Math.Max(0,(point.X-TrackHeaderWidth)/zoom);double lane=Math.Floor((point.Y-TracksTop)/LaneHeight);int track=(int)Math.Max(0,Math.Min(project.TrackCount-1,project.TrackCount-1-lane));
   return InsertMediaSelection(payload.Media,time,track)?DragDropEffects.Copy:DragDropEffects.None;
  }
 }
}
