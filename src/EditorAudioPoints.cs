using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Automation;
namespace TasiaCraft {
public partial class Editor {
 // Audio points: a volume line drawn over each audio clip. Double-click the line to add a point, drag a point to
 // change its time and level, right-click it to delete. The green and red playhead handles select a time range used
 // by Silence Audio / Restore Audio. See docs/AUDIO-POINTS-EVIDENCE.md.
 double? rangeStart,rangeEnd; Rectangle rangeStartHandle,rangeEndHandle;
 void FollowPlayheadHandles(){if(HasRange||rangeStartHandle==null)return;Canvas.SetLeft(rangeStartHandle,TrackHeaderWidth+head*zoom-19);Canvas.SetLeft(rangeEndHandle,TrackHeaderWidth+head*zoom+10);}
 bool HasRange{get{return rangeStart.HasValue&&rangeEnd.HasValue&&rangeEnd.Value-rangeStart.Value>1e-6;}}
 void ClearRange(){if(rangeStart==null&&rangeEnd==null)return;rangeStart=rangeEnd=null;DrawTimeline();}
 void SetRange(double from,double to){double a=TimelineMarkers.Frame(project,Math.Max(0,Math.Min(from,to))),z=TimelineMarkers.Frame(project,Math.Min(Length(),Math.Max(from,to)));rangeStart=a;rangeEnd=z;}
 double EnvelopeY(double level,double height){return 6+(1-level)*(height-12);}
 double EnvelopeLevel(double y,double height){return Math.Max(0,Math.Min(1,1-(y-6)/Math.Max(1,height-12)));}
 PointCollection EnvelopeLine(Clip c,double width,double height){
  var line=new PointCollection();line.Add(new Point(0,EnvelopeY(AudioEnvelope.Level(c,0),height)));
  foreach(var p in AudioEnvelope.Points(c)){double local=AudioEnvelope.Local(c,p.Time);if(local>0&&local<c.Duration)line.Add(new Point(local*zoom,EnvelopeY(p.Level,height)));}
  line.Add(new Point(width,EnvelopeY(AudioEnvelope.Level(c,c.Duration),height)));return line;
 }
 void DrawEnvelope(Clip c,double left,double top,double width,double height){
  if(!RippleEditing.HasPlayableAudio(project,c)||c.Silenced)return;
  var overlay=new Canvas{Width=width,Height=height,ClipToBounds=true};Canvas.SetLeft(overlay,left);Canvas.SetTop(overlay,top);AutomationProperties.SetAutomationId(overlay,"AudioPoints."+c.Id);timeline.Children.Add(overlay);
  bool locked=TrackLocked(c);var visible=new Polyline{Points=EnvelopeLine(c,width,height),Stroke=B("#5bd6a2"),StrokeThickness=1.5,IsHitTestVisible=false};overlay.Children.Add(visible);
  if(locked)return;
  var hit=new Polyline{Points=visible.Points,Stroke=Brushes.Transparent,StrokeThickness=9,Cursor=Cursors.Cross,ToolTip="Audio level • double-click to add an audio point"};overlay.Children.Add(hit);
  hit.MouseLeftButtonDown+=(s,e)=>{if(e.ClickCount!=2)return;e.Handled=true;double local=e.GetPosition(overlay).X/zoom;AddAudioPoint(c,local,AudioEnvelope.Level(c,local));};
  var points=AudioEnvelope.Points(c);
  for(int i=0;i<points.Count;i++){
   double local=AudioEnvelope.Local(c,points[i].Time);if(local<0||local>c.Duration)continue;int index=i;
   var dot=new Ellipse{Width=9,Height=9,Fill=B("#5bd6a2"),Stroke=Brushes.White,StrokeThickness=1,Cursor=Cursors.SizeAll,ToolTip=Math.Round(points[i].Level*100)+"%  •  "+FrameTime(c.Start+local)};AutomationProperties.SetAutomationId(dot,"AudioPoint."+c.Id+"."+i);Canvas.SetLeft(dot,local*zoom-4.5);Canvas.SetTop(dot,EnvelopeY(points[i].Level,height)-4.5);overlay.Children.Add(dot);
   bool dragging=false,changed=false;
   dot.MouseLeftButtonDown+=(s,e)=>{if(busy)return;e.Handled=true;dragging=true;changed=false;dot.CaptureMouse();};
   dot.MouseMove+=(s,e)=>{if(!dragging||!dot.IsMouseCaptured)return;var p=e.GetPosition(overlay);if(!changed){Checkpoint();changed=true;}AudioEnvelope.Move(c,index,p.X/zoom,EnvelopeLevel(p.Y,height));var moved=AudioEnvelope.Points(c)[index];double x=AudioEnvelope.Local(c,moved.Time)*zoom;Canvas.SetLeft(dot,x-4.5);Canvas.SetTop(dot,EnvelopeY(moved.Level,height)-4.5);visible.Points=hit.Points=EnvelopeLine(c,width,height);Info("Audio point "+Math.Round(moved.Level*100)+"%");};
   dot.MouseLeftButtonUp+=(s,e)=>{if(!dragging)return;dragging=false;dot.ReleaseMouseCapture();e.Handled=true;if(changed){live.Refresh();DrawTimeline();}};
   var menu=new ContextMenu();var remove=new MenuItem{Header="Delete Audio Point"};remove.Click+=(s,e)=>DeleteAudioPoint(c,index);menu.Items.Add(remove);dot.ContextMenu=menu;
  }
 }
 bool AddAudioPoint(Clip c,double local,double level){if(busy||TrackLocked(c)||!RippleEditing.HasPlayableAudio(project,c))return false;if(AudioEnvelope.Points(c).Count>=AudioEnvelope.MaxPoints){Info("This clip has the maximum number of audio points.");return false;}Checkpoint();AudioEnvelope.Add(c,local,level);live.Refresh();DrawTimeline();Info("Audio point added");return true;}
 void DeleteAudioPoint(Clip c,int index){if(busy||TrackLocked(c)||index<0||index>=AudioEnvelope.Points(c).Count)return;Checkpoint();AudioEnvelope.Remove(c,index);live.Refresh();DrawTimeline();}
 void RemoveAllAudioPoints(){if(busy)return;var clips=SelectedClips();if(clips.Count==0)clips=project.Clips.ToList();var targets=clips.Where(c=>AudioEnvelope.Points(c).Count>0).ToList();if(targets.Count==0){Info("No audio points to remove.");return;}if(targets.Any(TrackLocked)){Info("Track is locked.");return;}Checkpoint();foreach(var c in targets)AudioEnvelope.Clear(c);Refresh();Info("Removed audio points from "+targets.Count+" clip(s)");}
 // Range Silence/Restore: selected clips overlapping the range, or every audio clip overlapping it when nothing is selected.
 List<Clip> RangeTargets(){double a=rangeStart.Value,z=rangeEnd.Value;var clips=SelectedClips();if(clips.Count==0)clips=project.Clips.ToList();return clips.Where(c=>RippleEditing.HasPlayableAudio(project,c)&&c.Start<z-1e-9&&c.Start+c.Duration>a+1e-9).ToList();}
 bool ApplyRange(bool silence){
  var targets=RangeTargets();if(targets.Count==0){Info("No audio in the selected range.");return false;}if(targets.Any(TrackLocked)){Info("Track is locked.");return false;}
  var candidate=json.Deserialize<Project>(json.Serialize(project));bool any=false;foreach(var t in targets){var c=candidate.Clips.First(x=>x.Id==t.Id);any|=silence?AudioEnvelope.SilenceRange(c,rangeStart.Value-c.Start,rangeEnd.Value-c.Start):AudioEnvelope.RestoreRange(c,rangeStart.Value-c.Start,rangeEnd.Value-c.Start);}
  if(!any){Info(silence?"The range is already silent.":"No silenced audio in the range.");return false;}try{Engine.Validate(candidate,false);}catch(Exception e){Info(e.Message);return false;}
  Checkpoint();foreach(var c in targets){if(silence)AudioEnvelope.SilenceRange(c,rangeStart.Value-c.Start,rangeEnd.Value-c.Start);else AudioEnvelope.RestoreRange(c,rangeStart.Value-c.Start,rangeEnd.Value-c.Start);}Refresh();Info((silence?"Silenced ":"Restored ")+FrameTime(rangeStart.Value)+" – "+FrameTime(rangeEnd.Value)+" on "+targets.Count+" clip(s)");return true;
 }
 void DrawRange(){
  double headX=TrackHeaderWidth+head*zoom;
  if(HasRange){double a=TrackHeaderWidth+rangeStart.Value*zoom,z=TrackHeaderWidth+rangeEnd.Value*zoom;var band=new Rectangle{Width=Math.Max(1,z-a),Height=timeline.Height,Fill=new SolidColorBrush(Color.FromArgb(46,91,214,162)),IsHitTestVisible=false};AutomationProperties.SetAutomationId(band,"Timeline.Range");Canvas.SetLeft(band,a);timeline.Children.Add(band);}
  double startX=HasRange?TrackHeaderWidth+rangeStart.Value*zoom:headX,endX=HasRange?TrackHeaderWidth+rangeEnd.Value*zoom:headX;
  foreach(bool isStart in new[]{true,false}){
   bool start=isStart;var handle=new Rectangle{Width=9,Height=16,RadiusX=2,RadiusY=2,Fill=B(start?"#5bbf3a":"#d23b3b"),Cursor=Cursors.SizeWE,ToolTip=start?"Drag to set the selection start":"Drag to set the selection end"};AutomationProperties.SetAutomationId(handle,start?"Timeline.RangeStart":"Timeline.RangeEnd");Canvas.SetLeft(handle,(start?startX-19:endX+10));Canvas.SetTop(handle,1);timeline.Children.Add(handle);if(start)rangeStartHandle=handle;else rangeEndHandle=handle;
   bool drag=false;double anchor=0;
   handle.MouseLeftButtonDown+=(s,e)=>{Pause();drag=true;anchor=start?(rangeEnd??head):(rangeStart??head);handle.CaptureMouse();e.Handled=true;};
   handle.MouseMove+=(s,e)=>{if(!drag)return;double t=Math.Max(0,Math.Min(Length(),(e.GetPosition(timeline).X-TrackHeaderWidth)/zoom));SetRange(anchor,t);Info("Selection "+FrameTime(rangeStart.Value)+" – "+FrameTime(rangeEnd.Value));Canvas.SetLeft(handle,start?TrackHeaderWidth+rangeStart.Value*zoom-19:TrackHeaderWidth+rangeEnd.Value*zoom+10);};
   handle.MouseLeftButtonUp+=(s,e)=>{if(!drag)return;drag=false;handle.ReleaseMouseCapture();e.Handled=true;if(!HasRange)rangeStart=rangeEnd=null;DrawTimeline();};
  }
 }
}
}
