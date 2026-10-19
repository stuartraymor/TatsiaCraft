using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
namespace TasiaCraft {
 // Cursor data recorded beside a screen recording (<video>.cursor.json). Times are seconds of recording (source
 // time), positions are physical desktop pixels relative to Left/Top of the captured Width x Height desktop.
 public class CursorSample { public double T,X,Y; public string Kind="arrow"; }
 public class CursorClick { public double T,X,Y; public string Button="left"; }
 public class CursorTrack {
  public int Version=1; public int Left,Top,Width,Height; public List<CursorSample> Samples=new List<CursorSample>(); public List<CursorClick> Clicks=new List<CursorClick>();
  static readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};
  static readonly Dictionary<string,Tuple<DateTime,CursorTrack>> cache=new Dictionary<string,Tuple<DateTime,CursorTrack>>(StringComparer.OrdinalIgnoreCase);
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  public void Validate(){
   if(Width<1||Height<1||Width>65536||Height>65536||Samples==null||Clicks==null)throw new InvalidDataException("Invalid cursor data");double last=double.NegativeInfinity;
   foreach(var s in Samples){if(s==null||!Finite(s.T)||!Finite(s.X)||!Finite(s.Y)||s.T<last)throw new InvalidDataException("Invalid cursor sample");last=s.T;}
   foreach(var c in Clicks)if(c==null||!Finite(c.T)||!Finite(c.X)||!Finite(c.Y)||(c.Button!="left"&&c.Button!="right"))throw new InvalidDataException("Invalid cursor click");
  }
  public void Save(string path){Validate();string temp=path+".tmp";File.WriteAllText(temp,json.Serialize(this));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
  public static CursorTrack Load(string path){
   if(string.IsNullOrEmpty(path)||!File.Exists(path))return null;var stamp=File.GetLastWriteTimeUtc(path);lock(cache){Tuple<DateTime,CursorTrack> hit;if(cache.TryGetValue(path,out hit)&&hit.Item1==stamp)return hit.Item2;}
   try{var track=json.Deserialize<CursorTrack>(File.ReadAllText(path));track.Validate();lock(cache)cache[path]=Tuple.Create(stamp,track);return track;}catch{return null;}
  }
  public static string SidecarFor(string video){return video+".cursor.json";}
  // Interpolated position at a recording time, optionally averaged over +/-window seconds (Cursor Smoothing).
  public bool At(double t,double window,out double x,out double y,out string kind){
   x=y=0;kind="arrow";if(Samples.Count==0)return false;if(window<=0)return Raw(t,out x,out y,out kind);
   double sx=0,sy=0,weight=0;const int steps=12;for(int i=-steps;i<=steps;i++){double px,py;string k;Raw(t+window*i/steps,out px,out py,out k);double w=1-Math.Abs(i)/(double)(steps+1);sx+=px*w;sy+=py*w;weight+=w;}
   Raw(t,out x,out y,out kind);x=sx/weight;y=sy/weight;return kind!="hidden";
  }
  bool Raw(double t,out double x,out double y,out string kind){
   int lo=0,hi=Samples.Count-1;if(t<=Samples[0].T){x=Samples[0].X;y=Samples[0].Y;kind=Samples[0].Kind;return kind!="hidden";}if(t>=Samples[hi].T){x=Samples[hi].X;y=Samples[hi].Y;kind=Samples[hi].Kind;return kind!="hidden";}
   while(hi-lo>1){int mid=(lo+hi)/2;if(Samples[mid].T<=t)lo=mid;else hi=mid;}var a=Samples[lo];var b=Samples[hi];double f=b.T>a.T?(t-a.T)/(b.T-a.T):0;x=a.X+(b.X-a.X)*f;y=a.Y+(b.Y-a.Y)*f;kind=a.Kind;return kind!="hidden";
  }
 }
 // Polls the cursor at about 60 Hz on a per-monitor DPI-aware thread so positions match physical capture pixels.
 public sealed class CursorRecorder : IDisposable {
  [StructLayout(LayoutKind.Sequential)] struct POINT{public int X,Y;}
  [StructLayout(LayoutKind.Sequential)] struct CURSORINFO{public int cbSize,flags;public IntPtr hCursor;public POINT pt;}
  [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
  [DllImport("user32.dll")] static extern bool GetCursorInfo(ref CURSORINFO info);
  [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
  [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
  [DllImport("user32.dll")] static extern IntPtr LoadCursor(IntPtr instance,int id);
  [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
  readonly CursorTrack track=new CursorTrack(); readonly Stopwatch clock=new Stopwatch(); Thread thread; volatile bool running; double offset;
  public CursorTrack Track{get{return track;}}
  public void Start(){running=true;thread=new Thread(Loop){IsBackground=true,Name="Cursor recorder"};thread.Start();}
  // Call when the screen capture's first frame time is known: shifts samples so 0 is the first video frame.
  public void MarkVideoStart(){offset=clock.Elapsed.TotalSeconds;}
  void Loop(){
   try{SetThreadDpiAwarenessContext(new IntPtr(-4));}catch{} // per-monitor aware v2 where available
   track.Left=GetSystemMetrics(76);track.Top=GetSystemMetrics(77);track.Width=Math.Max(1,GetSystemMetrics(78));track.Height=Math.Max(1,GetSystemMetrics(79));
   var arrow=LoadCursor(IntPtr.Zero,32512);var ibeam=LoadCursor(IntPtr.Zero,32513);var hand=LoadCursor(IntPtr.Zero,32649);var wait=LoadCursor(IntPtr.Zero,32514);
   bool left=false,right=false;CursorSample last=null;clock.Start();
   while(running){
    double t=clock.Elapsed.TotalSeconds;var info=new CURSORINFO{cbSize=Marshal.SizeOf(typeof(CURSORINFO))};string kind="arrow";POINT p;
    if(GetCursorInfo(ref info)){p=info.pt;kind=(info.flags&1)==0?"hidden":info.hCursor==ibeam?"ibeam":info.hCursor==hand?"hand":info.hCursor==wait?"wait":"arrow";}else GetCursorPos(out p);
    double x=p.X-track.Left,y=p.Y-track.Top;
    lock(track){if(last==null||last.X!=x||last.Y!=y||last.Kind!=kind||t-last.T>.5){last=new CursorSample{T=t,X=x,Y=y,Kind=kind};track.Samples.Add(last);}
     bool l=(GetAsyncKeyState(1)&0x8000)!=0,r=(GetAsyncKeyState(2)&0x8000)!=0;if(l&&!left)track.Clicks.Add(new CursorClick{T=t,X=x,Y=y,Button="left"});if(r&&!right)track.Clicks.Add(new CursorClick{T=t,X=x,Y=y,Button="right"});left=l;right=r;}
    Thread.Sleep(15);
   }
  }
  public CursorTrack Stop(){running=false;if(thread!=null)thread.Join(2000);lock(track){foreach(var s in track.Samples)s.T-=offset;foreach(var c in track.Clicks)c.T-=offset;track.Samples.RemoveAll(s=>s.T< -1);track.Clicks.RemoveAll(c=>c.T<0);}return track;}
  public void Dispose(){running=false;}
 }
}
