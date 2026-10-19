using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace TasiaCraft {
 public static class MediaBatchInsertion {
  public static List<Clip> Plan(Project project,IEnumerable<Media> selection,double start,int track){
   if(project==null||project.Media==null||project.Clips==null||selection==null)throw new ArgumentException("Choose a valid project and media selection.");
   if(double.IsNaN(start)||double.IsInfinity(start)||start<0||start>86400||track<0||track>=project.TrackCount)throw new ArgumentException("Choose a valid insertion time and track.");
   if(project.LockedTracks!=null&&project.LockedTracks.Contains(track))throw new InvalidOperationException("Unlock the target track before inserting media.");
   var media=selection.Distinct().ToList();if(media.Any(m=>m==null||!project.Media.Contains(m)))throw new ArgumentException("The media selection is stale. Select current Media Bin entries.");
   var draft=new Project();draft.Media.AddRange(media);var clips=new List<Clip>();double time=start;
   foreach(var item in media){if(item.Kind!="audio"&&(item.Width<1||item.Height<1))throw new ArgumentException("Insertion requires known visual source dimensions.");if(!File.Exists(item.Path))throw new FileNotFoundException("Insertion source is missing. Relink the media first.",item.Path);double duration=item.Kind=="image"?5:item.Duration;var clip=new Clip{MediaId=item.Id,Start=time,Duration=duration,Track=0,NativeSize=true};draft.Clips.Add(clip);time+=duration;}
   // Validate independent clip data before changing the editor or undo history.
   Engine.Validate(draft,true);foreach(var clip in draft.Clips){clip.Track=track;clips.Add(clip);}return clips;
  }
 }
}
