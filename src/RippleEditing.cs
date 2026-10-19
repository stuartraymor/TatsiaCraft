using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace TasiaCraft {
 // Ripple Delete, ripple move and Separate Audio and Video. See docs/RIPPLE-EVIDENCE.md for the reference behavior
 // and the independent rules chosen where the reference was not measured.
 public static class RippleEditing {
  static void Members(Project p,IEnumerable<Clip> clips,out List<Clip> list){if(clips==null)throw new ArgumentNullException("clips");list=clips.Distinct().ToList();if(list.Any(c=>c==null||!p.Clips.Contains(c)))throw new ArgumentException("Selection contains an unknown clip");}
  static List<double[]> Merge(IEnumerable<Clip> clips){var merged=new List<double[]>();foreach(var c in clips.OrderBy(x=>x.Start)){double a=c.Start,z=c.Start+c.Duration;if(merged.Count>0&&a<=merged[merged.Count-1][1])merged[merged.Count-1][1]=Math.Max(merged[merged.Count-1][1],z);else merged.Add(new[]{a,z});}return merged;}
  static double RemovedBefore(List<double[]> merged,double time){double total=0;foreach(var r in merged){if(r[0]>=time)break;total+=Math.Min(r[1],time)-r[0];}return total;}
  // Removes the clips and closes the time they occupied on each affected track. Other tracks are unchanged.
  public static bool RippleDelete(Project p,IEnumerable<Clip> clips){
   List<Clip> list;Members(p,clips,out list);if(list.Count==0)return false;if(list.Any(c=>p.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Track is locked.");
   var gaps=list.GroupBy(c=>c.Track).ToDictionary(g=>g.Key,g=>Merge(g));
   p.Clips.RemoveAll(list.Contains);ClipGroups.Prune(p);
   foreach(var c in p.Clips){List<double[]> merged;if(!gaps.TryGetValue(c.Track,out merged))continue;c.Start=Math.Max(0,c.Start-RemovedBefore(merged,c.Start));}
   return true;
  }
  // Selection plus every clip that starts at or after the selection's start on the same tracks, with whole groups.
  public static List<Clip> RippleFollowers(Project p,IEnumerable<Clip> selection){
   List<Clip> list;Members(p,selection,out list);if(list.Count==0)return list;
   var firstByTrack=list.GroupBy(c=>c.Track).ToDictionary(g=>g.Key,g=>g.Min(c=>c.Start));
   var followers=p.Clips.Where(c=>!list.Contains(c)&&firstByTrack.ContainsKey(c.Track)&&c.Start>=firstByTrack[c.Track]-1e-9);
   return ClipGroups.Expand(p,list.Concat(followers)).ToList();
  }
  public static bool CanSeparate(Project p,Clip c){if(c==null||!string.IsNullOrEmpty(c.Text)||!string.IsNullOrEmpty(c.Stream))return false;var m=p.Media.FirstOrDefault(x=>x.Id==c.MediaId);return m!=null&&m.Kind=="video"&&m.HasAudio;}
  // Splits each video-with-audio clip into a video-only clip and an audio-only clip on a new track inserted
  // directly above the source track, as observed in the reference. Returns the created audio clips.
  public static List<Clip> SeparateAudioVideo(Project p,IEnumerable<Clip> clips){
   List<Clip> list;Members(p,clips,out list);var sources=list.Where(c=>CanSeparate(p,c)).ToList();var created=new List<Clip>();if(sources.Count==0)return created;
   if(sources.Any(c=>p.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Track is locked.");
   var tracks=sources.Select(c=>c.Track).Distinct().OrderByDescending(t=>t).ToList();if(p.TrackCount+tracks.Count>64)throw new InvalidOperationException("A project can have at most 64 tracks.");
   foreach(int track in tracks){
    int inserted=track+1;foreach(var c in p.Clips)if(c.Track>=inserted)c.Track++;foreach(var states in new[]{p.HiddenTracks,p.LockedTracks,p.SnapDisabledTracks})for(int i=0;i<states.Count;i++)if(states[i]>=inserted)states[i]++;p.TrackCount++;
    foreach(var source in sources.Where(c=>c.Track==track)){
     var audio=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Clip>(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(source));audio.Id=Guid.NewGuid().ToString("N");audio.Track=inserted;audio.Stream="audio";source.Stream="video";source.AudioPoints=new List<AudioPoint>();
     audio.Effect=null;audio.Animate=false;audio.Rotation=0;audio.CropLeft=audio.CropTop=audio.CropRight=audio.CropBottom=0;audio.Scale=1;audio.X=audio.Y=0;audio.EndScale=1;audio.EndX=audio.EndY=0;audio.Opacity=1;audio.NativeSize=false;source.AudioEffect=null;
     p.Clips.Insert(p.Clips.IndexOf(source)+1,audio);var group=ClipGroups.ForClip(p,source);if(group!=null)group.ClipIds.Add(audio.Id);created.Add(audio);
    }
   }
   return created;
  }
  static readonly System.Web.Script.Serialization.JavaScriptSerializer json=new System.Web.Script.Serialization.JavaScriptSerializer();
  public static bool HasPlayableAudio(Project p,Clip c){if(c==null||!string.IsNullOrEmpty(c.Text)||c.Stream=="video")return false;var m=p.Media.FirstOrDefault(x=>x.Id==c.MediaId);return m!=null&&m.HasAudio;}
  // Silence Audio / Restore Audio: a per-clip flag that removes the clip's audio without changing its gain.
  public static int SetSilenced(Project p,IEnumerable<Clip> clips,bool silenced){List<Clip> list;Members(p,clips,out list);var targets=list.Where(c=>HasPlayableAudio(p,c)&&(silenced?!c.Silenced:c.Silenced||AudioEnvelope.Points(c).Count>0)).ToList();if(targets.Any(c=>p.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Track is locked.");foreach(var c in targets){c.Silenced=silenced;if(!silenced)AudioEnvelope.Clear(c);}return targets.Count;}
  // Splits a clip at a project time, keeping source continuity; the right part joins the clip's group.
  public static Clip SplitAt(Project p,Clip c,double time){double offset=time-c.Start;if(offset<=1e-9||offset>=c.Duration-1e-9)return null;var right=json.Deserialize<Clip>(json.Serialize(c));right.Id=Guid.NewGuid().ToString("N");right.Start=time;right.In+=offset*c.Speed;right.Duration-=offset;c.Duration=offset;c.FadeOut=0;right.FadeIn=0;c.FadeIn=Math.Min(c.FadeIn,c.Duration);right.FadeOut=Math.Min(right.FadeOut,right.Duration);p.Clips.Insert(p.Clips.IndexOf(c)+1,right);var group=ClipGroups.ForClip(p,c);if(group!=null)group.ClipIds.Add(right.Id);return right;}
  static bool Same(double a,double b){return Math.Abs(a-b)<1e-6;}
  static bool SameEnvelope(Clip a,Clip b){return json.Serialize(AudioEnvelope.Points(a))==json.Serialize(AudioEnvelope.Points(b));}
  static bool Continuous(Clip a,Clip b){return a.MediaId!=null&&a.MediaId==b.MediaId&&a.Track==b.Track&&a.Stream==b.Stream&&Same(a.Start+a.Duration,b.Start)&&Same(a.Speed,b.Speed)&&Same(a.In+a.Duration*a.Speed,b.In)&&!a.Animate&&!b.Animate&&a.NativeSize==b.NativeSize&&a.Silenced==b.Silenced&&a.Effect==b.Effect&&a.AudioEffect==b.AudioEffect&&Same(a.Volume,b.Volume)&&Same(a.Opacity,b.Opacity)&&Same(a.Scale,b.Scale)&&Same(a.X,b.X)&&Same(a.Y,b.Y)&&Same(a.Rotation,b.Rotation)&&Same(a.CropLeft,b.CropLeft)&&Same(a.CropTop,b.CropTop)&&Same(a.CropRight,b.CropRight)&&Same(a.CropBottom,b.CropBottom);}
  // Stitch Selected Media: joins selected clips that touch on a track and continue the same source, restoring one clip.
  public static int Stitch(Project p,IEnumerable<Clip> clips){
   List<Clip> list;Members(p,clips,out list);if(list.Any(c=>p.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Track is locked.");int joined=0;
   foreach(var track in list.GroupBy(c=>c.Track)){var ordered=track.OrderBy(c=>c.Start).ToList();for(int i=0;i+1<ordered.Count;){var a=ordered[i];var b=ordered[i+1];if(Continuous(a,b)&&ClipGroups.ForClip(p,a)==ClipGroups.ForClip(p,b)&&SameEnvelope(a,b)){a.Duration+=b.Duration;a.FadeOut=b.FadeOut;p.Clips.Remove(b);foreach(var g in p.Groups)g.ClipIds.Remove(b.Id);ordered.RemoveAt(i+1);joined++;}else i++;}}
   ClipGroups.Prune(p);return joined;
  }
  // Ripple Insert: adds blank time at a point on every unlocked track; crossing clips split there, later clips and markers move right.
  public static void RippleInsert(Project p,double at,double seconds){
   if(double.IsNaN(at)||double.IsInfinity(at)||at<0||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)throw new ArgumentException("Invalid ripple insert");
   foreach(var c in p.Clips.Where(c=>!p.LockedTracks.Contains(c.Track)&&c.Start<at-1e-9&&c.Start+c.Duration>at+1e-9).ToList())SplitAt(p,c,at);
   foreach(var c in p.Clips)if(!p.LockedTracks.Contains(c.Track)&&c.Start>=at-1e-9)c.Start+=seconds;
   foreach(var m in p.Markers)if(m.Time>=at-1e-9)m.Time+=seconds;TimelineMarkers.Sort(p);
  }
  public static void Validate(Project p){foreach(var c in p.Clips){if(string.IsNullOrEmpty(c.Stream))continue;if(c.Stream!="audio"&&c.Stream!="video")throw new InvalidDataException("Unknown clip stream");var m=p.Media.FirstOrDefault(x=>x.Id==c.MediaId);if(!string.IsNullOrEmpty(c.Text)||m==null||m.Kind!="video"||c.Stream=="audio"&&!m.HasAudio)throw new InvalidDataException("Separated stream requires matching video media");}}
 }
}
