using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace TasiaCraft {
 // Microphone capture for Narration. One FFmpeg process records 48 kHz mono PCM (when an output path is given)
 // and prints a short-window RMS level for the input meter. The first level reading means audio is flowing; the
 // editor starts timeline playback at that moment. See docs/NARRATION-EVIDENCE.md.
 public static class NarrationDevices {
  public static List<string> Parse(string listing){var names=new List<string>();foreach(Match m in Regex.Matches(listing??"","\"([^\"]+)\"\\s+\\(audio\\)"))if(!names.Contains(m.Groups[1].Value))names.Add(m.Groups[1].Value);return names;}
  public static List<string> List(){var p=new Process{StartInfo=new ProcessStartInfo(Engine.Tool("ffmpeg"),"-hide_banner -list_devices true -f dshow -i dummy"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,StandardErrorEncoding=new UTF8Encoding(false)}};p.Start();string text=p.StandardError.ReadToEnd();p.WaitForExit(10000);return Parse(text);}
  public static string InputArgs(string device){return "-f dshow -audio_buffer_size 50 -i "+Engine.Quote("audio="+device);}
 }
 public sealed class NarrationCapture : IDisposable {
  public const double Window=.1;
  readonly Process process; readonly TaskCompletionSource<bool> ready=new TaskCompletionSource<bool>(); readonly StringBuilder errors=new StringBuilder(); bool stopped;
  public string OutputPath{get;private set;}
  public event Action<double> Level; // dBFS of the last 100 ms window
  public bool IsReady{get{return ready.Task.IsCompleted;}}
  public NarrationCapture(string inputArgs,string outputPath,double gain){
   if(double.IsNaN(gain)||double.IsInfinity(gain)||gain<0||gain>4)throw new ArgumentException("Invalid input level");OutputPath=outputPath;
   string g=gain.ToString("0.###",CultureInfo.InvariantCulture),meter="asetnsamples=n=4800:p=0,astats=metadata=1:reset=1,ametadata=print:file=-";
   string args="-hide_banner -nostats -y "+inputArgs+(outputPath==null?" -af aformat=sample_rates=48000:channel_layouts=mono,volume="+g+","+meter+" -f null -":" -filter_complex \"[0:a]aformat=sample_rates=48000:channel_layouts=mono,volume="+g+",asplit=2[rec][mon];[mon]"+meter+"[m]\" -map \"[rec]\" -c:a pcm_s16le "+Engine.Quote(outputPath)+" -map \"[m]\" -f null -");
   process=new Process{StartInfo=new ProcessStartInfo(Engine.Tool("ffmpeg"),args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=new UTF8Encoding(false),StandardErrorEncoding=new UTF8Encoding(false)},EnableRaisingEvents=true};
   process.OutputDataReceived+=(s,e)=>{if(e.Data==null)return;const string key="lavfi.astats.1.RMS_level=";if(!e.Data.StartsWith(key,StringComparison.Ordinal))return;double db;string v=e.Data.Substring(key.Length);if(!double.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out db))db=v.Contains("inf")?double.NegativeInfinity:-120;ready.TrySetResult(true);var handler=Level;if(handler!=null)handler(db);};
   process.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)lock(errors){errors.AppendLine(e.Data);if(errors.Length>20000)errors.Remove(0,errors.Length-20000);}};
   process.Exited+=(s,e)=>ready.TrySetResult(false);
  }
  public string Errors{get{lock(errors)return errors.ToString();}}
  public async Task StartAsync(){process.Start();process.BeginOutputReadLine();process.BeginErrorReadLine();var first=await Task.WhenAny(ready.Task,Task.Delay(10000));if(first!=ready.Task||!ready.Task.Result){Dispose();throw new InvalidOperationException("The microphone did not start. "+LastError());}}
  string LastError(){var lines=Errors.Split(new[]{'\n'},StringSplitOptions.RemoveEmptyEntries);return lines.Length==0?"":lines[lines.Length-1].Trim();}
  public async Task<string> StopAsync(){if(stopped)return OutputPath;stopped=true;try{process.StandardInput.WriteLine("q");}catch{}bool exited=await Task.Run(()=>process.WaitForExit(15000));if(!exited){try{process.Kill();}catch{}throw new InvalidOperationException("Recording did not finalize in time.");}process.WaitForExit();if(OutputPath!=null&&(!File.Exists(OutputPath)||new FileInfo(OutputPath).Length<=44))throw new InvalidOperationException("No audio was recorded. "+LastError());return OutputPath;}
  public void Dispose(){stopped=true;try{if(!process.HasExited){try{process.StandardInput.WriteLine("q");}catch{}if(!process.WaitForExit(3000))process.Kill();}}catch{}try{process.Dispose();}catch{}}
 }
}
