using System;
using System.Linq;
using System.Collections.Generic;
namespace TasiaCraft {
 public static class MediaBinOperations {
  public static List<Media> Unused(Project project){if(project==null||project.Media==null||project.Clips==null)throw new ArgumentException("Choose a valid project.");var used=new HashSet<string>(project.Clips.Where(c=>c!=null).Select(c=>c.MediaId));return project.Media.Where(m=>m!=null&&!used.Contains(m.Id)).ToList();}
  public static int RemoveUnused(Project project){var unused=Unused(project);return project.Media.RemoveAll(m=>unused.Contains(m));}
 }
}
