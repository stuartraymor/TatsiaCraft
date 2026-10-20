using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows;
namespace TasiaCraft {
 // Help > Check for Updates: reads the GitHub releases list, and when a newer TatsiaCraft-Setup is published,
 // downloads it, verifies its size and SHA-256 digest, and runs it after this editor closes.
 public sealed class UpdateInfo { public string Version,Url,Digest,Notes,Page; public long Size; }
 public static class Updates {
  public const string ReleasesApi="https://api.github.com/repos/stuartraymor/TatsiaCraft/releases?per_page=20";
  public static bool IsNewer(string candidate,string current){Version a,b;return Version.TryParse((candidate??"").TrimStart('v'),out a)&&Version.TryParse((current??"").TrimStart('v'),out b)&&a>b;}
  // Picks the highest published, non-draft release that has a setup asset.
  public static UpdateInfo Latest(string releasesJson){
   var list=new JavaScriptSerializer{MaxJsonLength=int.MaxValue}.Deserialize<List<Dictionary<string,object>>>(releasesJson);UpdateInfo best=null;Version bestVersion=null;
   foreach(var r in list){if(r.ContainsKey("draft")&&Convert.ToBoolean(r["draft"]))continue;string tag=Convert.ToString(r["tag_name"]).TrimStart('v');Version v;if(!Version.TryParse(tag,out v))continue;
    var assets=r.ContainsKey("assets")?(System.Collections.ArrayList)r["assets"]:new System.Collections.ArrayList();foreach(Dictionary<string,object> a in assets){string name=Convert.ToString(a["name"]);if(!string.Equals(name,"TatsiaCraft-Setup-"+tag+".exe",StringComparison.OrdinalIgnoreCase))continue;if(bestVersion==null||v>bestVersion){bestVersion=v;best=new UpdateInfo{Version=tag,Url=Convert.ToString(a["browser_download_url"]),Size=Convert.ToInt64(a["size"]),Digest=a.ContainsKey("digest")?Convert.ToString(a["digest"]):null,Notes=r.ContainsKey("body")?Convert.ToString(r["body"]):"",Page=r.ContainsKey("html_url")?Convert.ToString(r["html_url"]):""};}}}
   return best;
  }
  static WebClient Client(){ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var c=new WebClient();c.Headers[HttpRequestHeader.UserAgent]="TatsiaCraft/"+ProductInfo.Version;c.Headers[HttpRequestHeader.Accept]="application/vnd.github+json";return c;}
  public static Task<UpdateInfo> CheckAsync(){return Task.Run(()=>{using(var c=Client())return Latest(c.DownloadString(ReleasesApi));});}
  public static bool Verify(string file,UpdateInfo info){var f=new FileInfo(file);if(!f.Exists||f.Length!=info.Size)return false;if(string.IsNullOrEmpty(info.Digest))return true;using(var sha=SHA256.Create())using(var s=f.OpenRead())return "sha256:"+BitConverter.ToString(sha.ComputeHash(s)).Replace("-","").ToLowerInvariant()==info.Digest.ToLowerInvariant();}
  public static async Task<string> DownloadAsync(UpdateInfo info,Action<int> progress){
   string dir=Path.Combine(Path.GetTempPath(),"TatsiaCraft-Update");Directory.CreateDirectory(dir);string file=Path.Combine(dir,"TatsiaCraft-Setup-"+info.Version+".exe");if(Verify(file,info))return file;
   using(var c=Client()){c.DownloadProgressChanged+=(s,e)=>progress(e.ProgressPercentage);await c.DownloadFileTaskAsync(new Uri(info.Url),file+".part");}
   if(File.Exists(file))File.Delete(file);File.Move(file+".part",file);if(!Verify(file,info)){File.Delete(file);throw new InvalidDataException("The downloaded update did not match its published size and SHA-256 digest.");}return file;
  }
 }
 public partial class Editor {
  public string StartupProject;
  async void CheckForUpdates(){
   Info("Checking for updates…");UpdateInfo info;
   try{info=await Updates.CheckAsync();}catch(Exception e){Info("Update check failed");MessageBox.Show(this,"TatsiaCraft could not reach GitHub to check for updates.\n\n"+e.Message,"Check for Updates");return;}
   if(info==null||!Updates.IsNewer(info.Version,ProductInfo.Version)){Info("TatsiaCraft is up to date");MessageBox.Show(this,ProductInfo.DisplayName+" is the latest version.","Check for Updates");return;}
   if(MessageBox.Show(this,"TatsiaCraft "+info.Version+" is available (you have "+ProductInfo.Version+").\n\nDownload and install it now? TatsiaCraft will close when the installer starts.","Update available",MessageBoxButton.YesNo,MessageBoxImage.Information)!=MessageBoxResult.Yes){Info("Update postponed");return;}
   if(busy){Info("Finish the current operation before updating.");return;}
   string setup;try{setup=await Updates.DownloadAsync(info,p=>Dispatcher.BeginInvoke(new Action(()=>Info("Downloading TatsiaCraft "+info.Version+"… "+p+"%"))));}catch(Exception e){Info("Update download failed");MessageBox.Show(this,e.Message,"Check for Updates");return;}
   if(!Discard())return;dirty=false;
   try{Process.Start(new ProcessStartInfo(setup,"/waitpid="+Process.GetCurrentProcess().Id){UseShellExecute=true});}catch(Exception e){MessageBox.Show(this,e.Message,"Check for Updates");return;}
   Close();
  }
 }
}
