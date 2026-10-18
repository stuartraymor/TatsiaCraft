using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TasiaCraft {
 // Groups are a single-level selection relationship, never replacement media.
 public class ClipGroup {
  public string Id=Guid.NewGuid().ToString("N"),Name="Group";
  public List<string> ClipIds=new List<string>();
 }

 public static class ClipGroups {
  static void Collections(Project project){
   if(project==null)throw new ArgumentNullException("project");
   if(project.Clips==null)throw new InvalidDataException("Project clips are missing");
   if(project.Groups==null)project.Groups=new List<ClipGroup>();
  }
  static bool ValidName(string name){return !string.IsNullOrWhiteSpace(name)&&name.Length<=200;}
  public static void Validate(Project project){
   Collections(project);
   var clips=new HashSet<string>(project.Clips.Where(c=>c!=null).Select(c=>c.Id));
   var groupIds=new HashSet<string>();var grouped=new HashSet<string>();
   foreach(var group in project.Groups){
    if(group==null||string.IsNullOrWhiteSpace(group.Id)||!groupIds.Add(group.Id)||clips.Contains(group.Id))throw new InvalidDataException("Invalid, duplicate or colliding group identity");
    if(!ValidName(group.Name))throw new InvalidDataException("Group name must contain 1 through 200 characters");
    if(group.ClipIds==null||group.ClipIds.Count<2)throw new InvalidDataException("A group requires at least two clip members");
    var members=new HashSet<string>();
    foreach(string id in group.ClipIds){
     if(string.IsNullOrEmpty(id)||!clips.Contains(id)||!members.Add(id)||!grouped.Add(id))throw new InvalidDataException("Group members must be distinct existing clips in only one group; nested groups are not supported");
    }
   }
  }
  public static ClipGroup ForClip(Project project,Clip clip){
   Collections(project);if(clip==null)return null;
   return project.Groups.FirstOrDefault(g=>g!=null&&g.ClipIds!=null&&g.ClipIds.Contains(clip.Id));
  }
  static List<Clip> Canonical(Project project,IEnumerable<Clip> values){
   if(values==null)throw new ArgumentNullException("members");
   var canonical=project.Clips.ToDictionary(c=>c.Id);var ids=new HashSet<string>();
   foreach(var clip in values){if(clip==null||string.IsNullOrEmpty(clip.Id)||!canonical.ContainsKey(clip.Id))throw new ArgumentException("Group selection must reference existing project clips");ids.Add(clip.Id);}
   return project.Clips.Where(c=>ids.Contains(c.Id)).ToList();
  }
  public static List<Clip> Expand(Project project,IEnumerable<Clip> seeds,string openedGroupId=null){
   Validate(project);var selection=Canonical(project,seeds);var ids=new HashSet<string>(selection.Select(c=>c.Id));
   if(openedGroupId!=null){
    var opened=project.Groups.FirstOrDefault(g=>g.Id==openedGroupId);
    if(opened==null)throw new ArgumentException("Opened group does not exist","openedGroupId");
    ids.IntersectWith(opened.ClipIds);
   }else{
    foreach(var group in project.Groups)if(group.ClipIds.Any(ids.Contains))ids.UnionWith(group.ClipIds);
   }
   return project.Clips.Where(c=>ids.Contains(c.Id)).ToList();
  }
  public static ClipGroup Create(Project project,IEnumerable<Clip> members,string name){
   Validate(project);if(!ValidName(name))throw new ArgumentException("Group name must contain 1 through 200 characters","name");
   var selected=Canonical(project,members);if(selected.Count<2)throw new ArgumentException("Select at least two distinct clips","members");
   if(selected.Any(c=>ForClip(project,c)!=null))throw new InvalidOperationException("A selected clip already belongs to a group; nested groups are not supported");
   if(project.LockedTracks!=null&&selected.Any(c=>project.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Locked track clips cannot be grouped");
   var group=new ClipGroup{Name=name,ClipIds=selected.Select(c=>c.Id).ToList()};
   project.Groups.Add(group);return group;
  }
  public static bool Rename(Project project,string groupId,string name){
   Validate(project);var group=project.Groups.FirstOrDefault(g=>g.Id==groupId);if(group==null)throw new ArgumentException("Choose an existing group.");
   name=(name??"").Trim();if(name.Length==0||name.Length>200)throw new ArgumentException("Use a group name containing 1 to 200 characters.");
   if(project.Clips.Any(c=>group.ClipIds.Contains(c.Id)&&project.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Unlock every member track before renaming the group.");
   if(group.Name==name)return false;group.Name=name;return true;
  }
  public static bool Ungroup(Project project,IEnumerable<Clip> members){
   Validate(project);var selected=new HashSet<string>(Canonical(project,members).Select(c=>c.Id));
   var groups=project.Groups.Where(g=>g.ClipIds.Any(selected.Contains)).ToList();if(groups.Count==0)return false;
   var affected=new HashSet<string>(groups.SelectMany(g=>g.ClipIds));
   if(project.LockedTracks!=null&&project.Clips.Any(c=>affected.Contains(c.Id)&&project.LockedTracks.Contains(c.Track)))throw new InvalidOperationException("Groups containing locked track clips cannot be ungrouped");
   foreach(var group in groups)project.Groups.Remove(group);return true;
  }
  public static void Prune(Project project){
   Collections(project);var existing=new HashSet<string>(project.Clips.Where(c=>c!=null).Select(c=>c.Id));
   if(project.Groups.Any(g=>g==null||g.ClipIds==null))throw new InvalidDataException("Invalid group collections");
   foreach(var group in project.Groups)group.ClipIds.RemoveAll(id=>!existing.Contains(id));
   project.Groups.RemoveAll(g=>g.ClipIds.Count<2);
  }
 }
}
