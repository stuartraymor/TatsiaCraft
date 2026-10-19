using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
namespace TasiaCraft {
 // An audio point: a volume level (0-100%) at a source-media time. Points are stored in source time, like Clip.In,
 // so they stay attached to the audio through trims, splits, stitches, ripple inserts and separation.
 public class AudioPoint { public double Time,Level=1; }
 public static class AudioEnvelope {
  public const int MaxPoints=1000; public const double Ramp=1/30.0;
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  static string N(double v){return v.ToString("0.######",CultureInfo.InvariantCulture);}
  public static double Source(Clip c,double local){return c.In+local*c.Speed;}
  public static double Local(Clip c,double source){return (source-c.In)/c.Speed;}
  public static List<AudioPoint> Points(Clip c){if(c.AudioPoints==null)c.AudioPoints=new List<AudioPoint>();return c.AudioPoints;}
  // Piecewise-linear level at a source time; holds the first/last level outside the points; 1 with no points.
  public static double LevelAtSource(Clip c,double source){var p=Points(c);if(p.Count==0)return 1;if(source<=p[0].Time)return p[0].Level;for(int i=1;i<p.Count;i++)if(source<=p[i].Time){var a=p[i-1];var b=p[i];return b.Time==a.Time?b.Level:a.Level+(b.Level-a.Level)*(source-a.Time)/(b.Time-a.Time);}return p[p.Count-1].Level;}
  public static double Level(Clip c,double local){return LevelAtSource(c,Source(c,local));}
  public static void Normalize(Clip c){var ordered=Points(c).OrderBy(x=>x.Time).ToList();var result=new List<AudioPoint>();foreach(var x in ordered){if(result.Count>0&&Math.Abs(result[result.Count-1].Time-x.Time)<1e-9)result[result.Count-1].Level=x.Level;else result.Add(new AudioPoint{Time=x.Time,Level=x.Level});}c.AudioPoints=result;}
  public static void Validate(Project p){
   foreach(var c in p.Clips){if(c.AudioPoints==null){c.AudioPoints=new List<AudioPoint>();continue;}if(c.AudioPoints.Count>MaxPoints)throw new InvalidDataException("Too many audio points");if(c.AudioPoints.Count>0&&!string.IsNullOrEmpty(c.Text))throw new InvalidDataException("Text clips cannot have audio points");double last=double.NegativeInfinity;foreach(var a in c.AudioPoints){if(a==null||!Finite(a.Time)||!Finite(a.Level)||a.Time<0||a.Time>345600||a.Level<0||a.Level>1)throw new InvalidDataException("Invalid audio point");if(a.Time<=last)throw new InvalidDataException("Audio points must be in increasing time order");last=a.Time;}}
  }
  // Adds or replaces a point at a clip-local time. Returns the point.
  public static AudioPoint Add(Clip c,double local,double level){if(!Finite(local)||!Finite(level))throw new ArgumentException("Invalid audio point");if(Points(c).Count>=MaxPoints)throw new InvalidOperationException("Too many audio points");local=Math.Max(0,Math.Min(c.Duration,local));var point=new AudioPoint{Time=Source(c,local),Level=Math.Max(0,Math.Min(1,level))};Points(c).RemoveAll(x=>Math.Abs(x.Time-point.Time)<1e-9);Points(c).Add(point);Normalize(c);return Points(c).First(x=>Math.Abs(x.Time-point.Time)<1e-9);}
  // Moves a point to a clip-local time and level, kept strictly between its neighbours.
  public static void Move(Clip c,int index,double local,double level){var p=Points(c);if(index<0||index>=p.Count)throw new ArgumentException("Unknown audio point");if(!Finite(local)||!Finite(level))throw new ArgumentException("Invalid audio point");double source=Source(c,Math.Max(0,Math.Min(c.Duration,local)));double min=index>0?p[index-1].Time+1e-6:double.NegativeInfinity,max=index+1<p.Count?p[index+1].Time-1e-6:double.PositiveInfinity;p[index].Time=Math.Max(0,Math.Max(min,Math.Min(max,source)));p[index].Level=Math.Max(0,Math.Min(1,level));}
  public static bool Remove(Clip c,int index){var p=Points(c);if(index<0||index>=p.Count)return false;p.RemoveAt(index);return true;}
  public static bool Clear(Clip c){if(Points(c).Count==0)return false;c.AudioPoints.Clear();return true;}
  // Silence a clip-local range: hold the current level just outside it and drop to 0 inside, with one-frame ramps.
  public static bool SilenceRange(Clip c,double from,double to){
   from=Math.Max(0,from);to=Math.Min(c.Duration,to);if(to-from<1e-6)return false;double sa=Source(c,from),sz=Source(c,to),ramp=Ramp*c.Speed;
   double before=LevelAtSource(c,sa-ramp),after=LevelAtSource(c,sz+ramp);var p=Points(c);p.RemoveAll(x=>x.Time>sa-ramp-1e-9&&x.Time<sz+ramp+1e-9);
   if(from>1e-9)p.Add(new AudioPoint{Time=Math.Max(0,sa-ramp),Level=before});p.Add(new AudioPoint{Time=sa,Level=0});p.Add(new AudioPoint{Time=sz,Level=0});if(c.Duration-to>1e-9)p.Add(new AudioPoint{Time=sz+ramp,Level=after});
   Normalize(c);return true;
  }
  // Restore a clip-local range: remove the points inside it so the level follows the points around it.
  public static bool RestoreRange(Clip c,double from,double to){double sa=Source(c,Math.Max(0,from)),sz=Source(c,Math.Min(c.Duration,to)),ramp=Ramp*c.Speed;int removed=Points(c).RemoveAll(x=>x.Time>sa-ramp-1e-9&&x.Time<sz+ramp+1e-9);if(Points(c).Count>0&&Points(c).All(x=>x.Level==0))return Clear(c)||removed>0;return removed>0;}
  // FFmpeg volume expression in playback time t from the clip start (after trim/tempo), or null for a flat envelope.
  public static string Expression(Clip c){
   var p=Points(c);if(p.Count==0||p.All(x=>Math.Abs(x.Level-1)<1e-9))return null;string s="("+N(c.In)+"+t*"+N(c.Speed)+")";
   var expr=new StringBuilder();var tail=new StringBuilder();expr.Append("if(lte("+s+","+N(p[0].Time)+"),"+N(p[0].Level)+",");tail.Append(")");
   for(int i=1;i<p.Count;i++){var a=p[i-1];var b=p[i];double slope=b.Time==a.Time?0:(b.Level-a.Level)/(b.Time-a.Time);expr.Append("if(lte("+s+","+N(b.Time)+"),"+N(a.Level)+"+("+N(slope)+")*("+s+"-"+N(a.Time)+"),");tail.Append(")");}
   expr.Append(N(p[p.Count-1].Level));expr.Append(tail);return expr.ToString();
  }
 }
}
