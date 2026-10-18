using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
namespace TasiaCraft {
 public class Marker { public string Id=Guid.NewGuid().ToString("N"),Name; public double Time; }
 // Timeline markers: named project-time points shown in the Marker tray, used for
 // navigation and snapping, and written to exported MP4 files as chapters.
 public static class TimelineMarkers {
  public const int MaxName=200; public const double MaxTime=86400;
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  public static string NormalizeName(string name){if(name==null)throw new ArgumentException("Marker name is required");string trimmed=name.Trim();if(trimmed.Length==0)throw new ArgumentException("Marker name cannot be blank");if(trimmed.Length>MaxName)throw new ArgumentException("Marker name is limited to "+MaxName+" characters");if(trimmed.Any(ch=>char.IsControl(ch)))throw new ArgumentException("Marker name cannot contain line breaks or control characters");return trimmed;}
  public static void Validate(Project p){
   if(p.Markers==null)p.Markers=new List<Marker>();var ids=new HashSet<string>(p.Clips.Where(c=>c!=null).Select(c=>c.Id));
   foreach(var m in p.Markers){if(m==null||string.IsNullOrWhiteSpace(m.Id)||!ids.Add(m.Id))throw new InvalidDataException("Invalid or duplicate marker identity");if(!Finite(m.Time)||m.Time<0||m.Time>MaxTime)throw new InvalidDataException("Invalid marker time");try{if(NormalizeName(m.Name)!=m.Name)throw new InvalidDataException("Marker name has surrounding whitespace");}catch(ArgumentException e){throw new InvalidDataException(e.Message);}}
  }
  public static double Frame(Project p,double time){return Math.Round(Math.Max(0,time)*p.Fps)/p.Fps;}
  public static Marker At(Project p,double time){double t=Frame(p,time);return p.Markers.FirstOrDefault(m=>Math.Abs(m.Time-t)<.5/p.Fps);}
  public static string NextName(Project p){int n=1;while(p.Markers.Any(m=>m.Name=="Marker "+n))n++;return "Marker "+n;}
  // Adds a frame-aligned marker; returns the existing marker when one already occupies that frame.
  public static Marker Add(Project p,double time,string name=null){if(!Finite(time)||time<0||time>MaxTime)throw new ArgumentException("Marker time is outside the timeline");var existing=At(p,time);if(existing!=null)return existing;var marker=new Marker{Time=Frame(p,time),Name=NormalizeName(name??NextName(p))};p.Markers.Add(marker);Sort(p);return marker;}
  public static Marker Find(Project p,string id){var m=p.Markers.FirstOrDefault(x=>x.Id==id);if(m==null)throw new ArgumentException("Unknown marker");return m;}
  public static bool Rename(Project p,string id,string name){var m=Find(p,id);string normalized=NormalizeName(name);if(m.Name==normalized)return false;m.Name=normalized;return true;}
  public static bool Move(Project p,string id,double time){var m=Find(p,id);if(!Finite(time))throw new ArgumentException("Invalid marker time");double t=Frame(p,Math.Min(MaxTime,time));var other=At(p,t);if(other!=null&&other!=m)throw new ArgumentException("Another marker already occupies that frame");if(m.Time==t)return false;m.Time=t;Sort(p);return true;}
  public static bool Remove(Project p,string id){return p.Markers.RemoveAll(m=>m.Id==id)>0;}
  public static void Sort(Project p){var ordered=p.Markers.OrderBy(m=>m.Time).ToList();p.Markers.Clear();p.Markers.AddRange(ordered);}
  public static Marker Next(Project p,double time,int direction){return direction>0?p.Markers.Where(m=>m.Time>time+1e-6).OrderBy(m=>m.Time).FirstOrDefault():p.Markers.Where(m=>m.Time<time-1e-6).OrderByDescending(m=>m.Time).FirstOrDefault();}
  static string Escape(string s){var b=new StringBuilder();foreach(char ch in s){if(ch=='='||ch==';'||ch=='#'||ch=='\\')b.Append('\\');b.Append(ch);}return b.ToString();}
  // FFmpeg metadata with one chapter per marker before the end of the export, each lasting until the next marker.
  public static string ChapterMetadata(Project p,double end){
   var markers=p.Markers.Where(m=>m.Time<end-1e-6).OrderBy(m=>m.Time).ToList();if(markers.Count==0)return null;
   var s=new StringBuilder(";FFMETADATA1\n");for(int i=0;i<markers.Count;i++){long a=(long)Math.Round(markers[i].Time*1000),z=(long)Math.Round((i+1<markers.Count?markers[i+1].Time:end)*1000);s.Append("[CHAPTER]\nTIMEBASE=1/1000\nSTART=").Append(a.ToString(CultureInfo.InvariantCulture)).Append("\nEND=").Append(z.ToString(CultureInfo.InvariantCulture)).Append("\ntitle=").Append(Escape(markers[i].Name)).Append('\n');}
   return s.ToString();
  }
 }
}
