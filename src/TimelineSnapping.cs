using System;
using System.Linq;
using System.Collections.Generic;
namespace TasiaCraft {
 public sealed class TimelineSnapOptions { public bool MediaEdges=true,Playhead=true,Markers=true; }
 public static class TimelineSnapping {
  public static List<double> Targets(Project project,TimelineSnapOptions options,IEnumerable<string> excludedClips,double? head,string excludedMarker){
   if(project==null||project.Clips==null||project.Markers==null||options==null)throw new ArgumentException("Choose valid timeline snapping inputs.");
   var excluded=new HashSet<string>(excludedClips??new string[0]);var result=new List<double>();
   if(options.MediaEdges)result.AddRange(project.Clips.Where(c=>!excluded.Contains(c.Id)&&(project.SnapDisabledTracks==null||!project.SnapDisabledTracks.Contains(c.Track))).SelectMany(c=>new[]{c.Start,c.Start+c.Duration}));
   if(options.Markers)result.AddRange(project.Markers.Where(m=>m.Id!=excludedMarker).Select(m=>m.Time));
   if(options.Playhead&&head.HasValue)result.Add(head.Value);result.Add(0);return result.Distinct().ToList();
  }
  public static double Snap(double time,IEnumerable<double> targets,double zoom,bool bypass){
   if(double.IsNaN(time)||double.IsInfinity(time)||double.IsNaN(zoom)||double.IsInfinity(zoom)||zoom<=0||targets==null)throw new ArgumentException("Snap position and zoom must be finite and valid.");
   if(bypass)return time;double best=time,distance=8/zoom;foreach(double t in targets){if(double.IsNaN(t)||double.IsInfinity(t))throw new ArgumentException("Snap targets must be finite.");double d=Math.Abs(t-time);if(d<distance){distance=d;best=t;}}return best;
  }
 }
 public partial class Editor {
  readonly TimelineSnapOptions timelineSnapping=new TimelineSnapOptions();
  System.Windows.Controls.MenuItem TimelineSnappingMenu(){
   var menu=new System.Windows.Controls.MenuItem{Header="Timeline Snapping"};
   Action<string,Func<bool>,Action<bool>> add=(name,get,set)=>{var item=new System.Windows.Controls.MenuItem{Header=name,IsCheckable=true,IsChecked=get()};item.Click+=(s,e)=>set(item.IsChecked);menu.Items.Add(item);};
   add("Media Edges",()=>timelineSnapping.MediaEdges,v=>timelineSnapping.MediaEdges=v);add("Playhead",()=>timelineSnapping.Playhead,v=>timelineSnapping.Playhead=v);add("Markers",()=>timelineSnapping.Markers,v=>timelineSnapping.Markers=v);return menu;
  }
  IEnumerable<double> TimelineSnapTargets(){return TimelineSnapping.Targets(project,timelineSnapping,null,null,null);}
  double SnapTime(double time,IEnumerable<double> targets){return TimelineSnapping.Snap(time,targets,zoom,(System.Windows.Input.Keyboard.Modifiers&System.Windows.Input.ModifierKeys.Shift)!=0);}
  double SnapPlayheadTime(double time){return SnapTime(time,TimelineSnapTargets());}
  double SnapMarkerCreation(double time){return SnapTime(time,timelineSnapping.Playhead?new[]{head}:new double[0]);}
  double SnapMarkerTime(double time,string markerId){return SnapTime(time,TimelineSnapping.Targets(project,timelineSnapping,null,head,markerId));}
  double SnapClipStart(double time,List<Clip> clips){return SnapTime(time,clips.Any(c=>project.SnapDisabledTracks.Contains(c.Track))?(IEnumerable<double>)new double[0]:TimelineSnapping.Targets(project,timelineSnapping,clips.Select(c=>c.Id),head,null));}
 }
}
