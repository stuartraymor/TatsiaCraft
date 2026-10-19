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
     var audio=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Clip>(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(source));audio.Id=Guid.NewGuid().ToString("N");audio.Track=inserted;audio.Stream="audio";source.Stream="video";
     audio.Effect=null;audio.Animate=false;audio.Rotation=0;audio.CropLeft=audio.CropTop=audio.CropRight=audio.CropBottom=0;audio.Scale=1;audio.X=audio.Y=0;audio.EndScale=1;audio.EndX=audio.EndY=0;audio.Opacity=1;audio.NativeSize=false;source.AudioEffect=null;
     p.Clips.Insert(p.Clips.IndexOf(source)+1,audio);var group=ClipGroups.ForClip(p,source);if(group!=null)group.ClipIds.Add(audio.Id);created.Add(audio);
    }
   }
   return created;
  }
  public static void Validate(Project p){foreach(var c in p.Clips){if(string.IsNullOrEmpty(c.Stream))continue;if(c.Stream!="audio"&&c.Stream!="video")throw new InvalidDataException("Unknown clip stream");var m=p.Media.FirstOrDefault(x=>x.Id==c.MediaId);if(!string.IsNullOrEmpty(c.Text)||m==null||m.Kind!="video"||c.Stream=="audio"&&!m.HasAudio)throw new InvalidDataException("Separated stream requires matching video media");}}
 }
}
