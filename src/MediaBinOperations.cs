using System;
using System.Linq;
using System.Collections.Generic;
namespace TasiaCraft {
 public static class MediaBinOperations {
  public static List<Clip> TimelineReferences(Project project,IEnumerable<Media> selection){
   if(project==null||project.Media==null||project.Clips==null||selection==null)throw new ArgumentException("Choose a valid project and media selection.");
   var ids=new HashSet<string>(selection.Where(m=>m!=null&&project.Media.Contains(m)).Select(m=>m.Id));return project.Clips.Where(c=>c!=null&&ids.Contains(c.MediaId)).ToList();
  }
  public static List<Media> SourceReferences(Project project,IEnumerable<Clip> selection){
   if(project==null||project.Media==null||project.Clips==null||selection==null)throw new ArgumentException("Choose a valid project and clip selection.");
   var ids=new HashSet<string>(selection.Where(c=>c!=null&&project.Clips.Contains(c)&&string.IsNullOrEmpty(c.Text)).Select(c=>c.MediaId));return project.Media.Where(m=>m!=null&&ids.Contains(m.Id)).ToList();
  }
  public static List<Media> View(Project project,string filter,string sort,bool descending){
   if(project==null||project.Media==null)throw new ArgumentException("Choose a valid project.");
   if(!new[]{"All media","Video","Audio","Images"}.Contains(filter)||!new[]{"Type","Name"}.Contains(sort))throw new ArgumentException("Choose a supported media filter and sort field.");
   var items=project.Media.Where(m=>m!=null&&(filter=="All media"||filter=="Video"&&m.Kind=="video"||filter=="Audio"&&m.Kind=="audio"||filter=="Images"&&m.Kind=="image"));var comparer=StringComparer.OrdinalIgnoreCase;
   if(sort=="Name")return (descending?items.OrderByDescending(m=>m.Name??"",comparer):items.OrderBy(m=>m.Name??"",comparer)).ToList();
   return (descending?items.OrderByDescending(m=>m.Kind??"",comparer).ThenByDescending(m=>m.Name??"",comparer):items.OrderBy(m=>m.Kind??"",comparer).ThenBy(m=>m.Name??"",comparer)).ToList();
  }
  public static List<Media> Unused(Project project){if(project==null||project.Media==null||project.Clips==null)throw new ArgumentException("Choose a valid project.");var used=new HashSet<string>(project.Clips.Where(c=>c!=null).Select(c=>c.MediaId));return project.Media.Where(m=>m!=null&&!used.Contains(m.Id)).ToList();}
  public static int RemoveUnused(Project project){var unused=Unused(project);return project.Media.RemoveAll(m=>unused.Contains(m));}
 }
}
