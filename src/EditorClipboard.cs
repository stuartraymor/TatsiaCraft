using System;
using System.Collections.Generic;
using System.Linq;
namespace TasiaCraft {
 public partial class Editor {
  public sealed class ClipClipboardBatch {
   public List<Clip> Clips=new List<Clip>();public List<ClipGroup> Groups=new List<ClipGroup>();
  }
  List<Clip> ClipboardMembers(){
   var members=SelectedClips();
   return ClipGroups.Expand(project,members,openedGroupId);
  }
  ClipClipboardBatch BuildClipboardBatch(List<Clip> members){
   var ids=new HashSet<string>(members.Select(c=>c.Id));var batch=new ClipClipboardBatch();
   batch.Clips=json.Deserialize<List<Clip>>(json.Serialize(members));
   // In an opened group, selection is its editable members rather than a
   // nested group unit, even when all members happen to be selected.
   if(openedGroupId==null)batch.Groups=json.Deserialize<List<ClipGroup>>(json.Serialize(project.Groups.Where(g=>g.ClipIds.All(ids.Contains)).ToList()));
   return batch;
  }
  void CopySelection(){if(busy)return;try{var members=ClipboardMembers();if(members.Count==0)return;clipboard=json.Serialize(BuildClipboardBatch(members));Info("Copied "+members.Count+" clips");}catch(Exception error){Info(error.Message);}}
  void CutSelection(){
   if(busy)return;try{
    var members=ClipboardMembers();if(members.Count==0)return;
    if(members.Any(c=>project.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Unlock every selected track before cutting.");
    string payload=json.Serialize(BuildClipboardBatch(members));var ids=new HashSet<string>(members.Select(c=>c.Id));
    Checkpoint();clipboard=payload;project.Clips.RemoveAll(c=>ids.Contains(c.Id));ClipGroups.Prune(project);
    if(openedGroupId!=null&&!project.Groups.Any(g=>g.Id==openedGroupId))openedGroupId=null;
    selectedClipIds.Clear();selected=null;Refresh();Info("Cut "+members.Count+" clips");
   }catch(Exception error){Info(error.Message);}
  }
  void PasteSelection(bool duplicate){
   if(busy)return;try{
    ClipClipboardBatch batch;
    if(duplicate){var members=ClipboardMembers();if(members.Count==0)return;if(members.Any(c=>project.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Unlock every selected track before duplicating.");batch=BuildClipboardBatch(members);}
    else{if(string.IsNullOrEmpty(clipboard))return;batch=json.Deserialize<ClipClipboardBatch>(clipboard);}
    if(batch==null||batch.Clips==null||batch.Clips.Count==0||batch.Groups==null)throw new InvalidOperationException("The copied selection is empty.");
    var clips=batch.Clips;double firstStart=clips.Min(c=>c.Start),shift=duplicate?clips.Max(c=>c.Start+c.Duration)-firstStart:head-firstStart;
    int minTrack=clips.Min(c=>c.Track),maxTrack=clips.Max(c=>c.Track);
    if(maxTrack-minTrack>=project.TrackCount)throw new InvalidOperationException("The copied track span does not fit this project.");
    int lane=Math.Max(-minTrack,Math.Min(project.TrackCount-1-maxTrack,0));var remap=new Dictionary<string,string>();
    foreach(var clip in clips){string id=Guid.NewGuid().ToString("N");remap.Add(clip.Id,id);clip.Id=id;clip.Start+=shift;clip.Track+=lane;if(project.LockedTracks.Contains(clip.Track))throw new InvalidOperationException("A destination track is locked.");}
    var groups=new List<ClipGroup>();foreach(var group in batch.Groups)groups.Add(new ClipGroup{Id=Guid.NewGuid().ToString("N"),Name=group.Name,ClipIds=group.ClipIds.Select(id=>remap[id]).ToList()});
    var opened=openedGroupId==null?null:project.Groups.FirstOrDefault(g=>g.Id==openedGroupId);
    if(openedGroupId!=null&&opened==null)throw new InvalidOperationException("The opened group no longer exists.");
    if(opened!=null&&project.Clips.Any(c=>opened.ClipIds.Contains(c.Id)&&project.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Unlock every group member track before pasting into the group.");
    if(opened!=null&&groups.Count>0)throw new InvalidOperationException("Close the current group before pasting a group unit.");
    var candidate=json.Deserialize<Project>(json.Serialize(project));candidate.Clips.AddRange(clips);candidate.Groups.AddRange(groups);
    if(opened!=null)candidate.Groups.First(g=>g.Id==opened.Id).ClipIds.AddRange(clips.Select(c=>c.Id));Engine.Validate(candidate,false);
    Checkpoint();project.Clips.AddRange(clips);project.Groups.AddRange(groups);if(opened!=null)opened.ClipIds.AddRange(clips.Select(c=>c.Id));
    selectedClipIds.Clear();foreach(var clip in clips)selectedClipIds.Add(clip.Id);selected=clips.Last();Refresh();Info((duplicate?"Duplicated ":"Pasted ")+clips.Count+" clips");
   }catch(Exception error){Info(error.Message);}
  }
 }
}
