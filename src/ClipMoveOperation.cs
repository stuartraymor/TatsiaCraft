using System;
using System.Collections.Generic;
using System.Linq;
namespace TasiaCraft {
 public sealed class ClipMoveResult {
  internal Action apply;internal Func<bool> changes;
  public readonly double DeltaX,DeltaY,Seconds;public readonly int Tracks;
  internal ClipMoveResult(double x,double y,double seconds,int tracks){DeltaX=x;DeltaY=y;Seconds=seconds;Tracks=tracks;}
  public bool Changes {get{return changes();}}
  public void Apply(){apply();}
 }
 // Snapshot-based unit translation: constrain one shared delta, never each
 // member independently. This preserves spacing, timing gaps and track offsets.
 public sealed class ClipMoveOperation {
  sealed class State {public Clip Clip;public double X,Y,EndX,EndY,Start;public int Track;public bool Visual,Animated;}
  readonly Project project;readonly List<State> states;
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  void CheckLocks(){if(states.Any(s=>project.LockedTracks.Contains(s.Clip.Track)))throw new InvalidOperationException("A selected track is locked.");}
  void CheckDestinationTracks(int lane){if(states.Any(s=>project.LockedTracks.Contains(s.Track+lane)))throw new InvalidOperationException("A destination track is locked.");}
  public ClipMoveOperation(Project project,IEnumerable<Clip> members){
   if(project==null||members==null)throw new ArgumentNullException();Engine.Validate(project,false);this.project=project;
   var ids=new HashSet<string>(members.Select(c=>c==null?null:c.Id));if(ids.Count==0||ids.Contains(null)||ids.Any(id=>!project.Clips.Any(c=>c.Id==id)))throw new ArgumentException("Choose existing clips to move.");
   states=project.Clips.Where(c=>ids.Contains(c.Id)).Select(c=>new State{Clip=c,X=c.X,Y=c.Y,EndX=c.EndX,EndY=c.EndY,Start=c.Start,Track=c.Track,Animated=c.Animate,Visual=!string.IsNullOrEmpty(c.Text)||project.Media.First(m=>m.Id==c.MediaId).Kind!="audio"}).ToList();CheckLocks();
  }
  public ClipMoveResult EvaluateCanvas(double dx,double dy){
   CheckLocks();if(!Finite(dx)||!Finite(dy))throw new ArgumentException("Move coordinates must be finite.");var visual=states.Where(s=>s.Visual).ToList();
   if(visual.Count==0){dx=dy=0;}else{
    dx=Math.Max(visual.Max(s=>-32768-Math.Min(s.X,s.Animated?s.EndX:s.X)),Math.Min(visual.Min(s=>32768-Math.Max(s.X,s.Animated?s.EndX:s.X)),dx));
    dy=Math.Max(visual.Max(s=>-32768-Math.Min(s.Y,s.Animated?s.EndY:s.Y)),Math.Min(visual.Min(s=>32768-Math.Max(s.Y,s.Animated?s.EndY:s.Y)),dy));
   }
   double x=dx,y=dy;var result=new ClipMoveResult(x,y,0,0);
   result.changes=()=>visual.Any(s=>Math.Abs(s.Clip.X-s.X-x)>1e-9||Math.Abs(s.Clip.Y-s.Y-y)>1e-9||s.Animated&&(Math.Abs(s.Clip.EndX-s.EndX-x)>1e-9||Math.Abs(s.Clip.EndY-s.EndY-y)>1e-9));
   result.apply=()=>{CheckLocks();foreach(var s in visual){s.Clip.X=s.X+x;s.Clip.Y=s.Y+y;if(s.Animated){s.Clip.EndX=s.EndX+x;s.Clip.EndY=s.EndY+y;}}};return result;
  }
  public ClipMoveResult EvaluateTimeline(double seconds,int tracks){
   CheckLocks();if(!Finite(seconds))throw new ArgumentException("Time delta must be finite.");double delta=Math.Max(-states.Min(s=>s.Start),Math.Min(86400-states.Max(s=>s.Start+s.Clip.Duration),seconds));int lane=Math.Max(-states.Min(s=>s.Track),Math.Min(project.TrackCount-1-states.Max(s=>s.Track),tracks));
   CheckDestinationTracks(lane);var result=new ClipMoveResult(0,0,delta,lane);result.changes=()=>states.Any(s=>Math.Abs(s.Clip.Start-s.Start-delta)>1e-9||s.Clip.Track!=s.Track+lane);
   result.apply=()=>{CheckLocks();CheckDestinationTracks(lane);foreach(var s in states){s.Clip.Start=s.Start+delta;s.Clip.Track=s.Track+lane;}};return result;
  }
 }
}
