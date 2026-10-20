using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
[assembly: AssemblyTitle("TatsiaCraft Setup")]
namespace TatsiaCraftSetup {
 // Per-user installer and uninstaller for TatsiaCraft. One source builds both: Setup embeds the payload zip
 // (application, FFmpeg tools, licenses, uninstaller and a SHA-256 manifest); the uninstaller has no payload.
 // No administrator rights are needed. See docs/INSTALLATION.md.
 public class ManifestFile { public string path,sha256; public long bytes; }
 public class Manifest { public string application,version; public List<ManifestFile> files=new List<ManifestFile>(); }
 public sealed class Options {
  public bool Quiet,Uninstall,Desktop=true,Associate=true,Launch=true,RemoveLegacy,Register=true; public string Directory,RegistryRoot,WaitPid;
  public static Options Parse(string[] args){var o=new Options();foreach(var raw in args){string a=raw.Trim();string l=a.ToLowerInvariant();
   if(l=="/quiet"||l=="/silent"||l=="/s"){o.Quiet=true;o.Launch=false;}else if(l=="/uninstall")o.Uninstall=true;else if(l=="/nodesktop")o.Desktop=false;else if(l=="/noassoc")o.Associate=false;else if(l=="/nolaunch")o.Launch=false;else if(l=="/launch")o.Launch=true;else if(l=="/removelegacy")o.RemoveLegacy=true;else if(l=="/noregister"){o.Register=false;o.Desktop=false;o.Associate=false;}
   else if(l.StartsWith("/dir="))o.Directory=a.Substring(5).Trim('"');else if(l.StartsWith("/regroot="))o.RegistryRoot=a.Substring(9).Trim('"');else if(l.StartsWith("/waitpid="))o.WaitPid=a.Substring(9);else throw new ArgumentException("Unknown option: "+a);}return o;}
 }
 public static class Installer {
  public const string AppName="TatsiaCraft",ExeName="TasiaCraft.exe",UninstallerName="Uninstall TatsiaCraft.exe",ProgId="TatsiaCraft.Project";
  public static string DefaultDirectory{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","TatsiaCraft");}}
  static readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};
  public static Stream Payload(){return Assembly.GetExecutingAssembly().GetManifestResourceStream("TatsiaCraft.Payload.zip");}
  public static Manifest PayloadManifest(){using(var s=Payload()){if(s==null)return null;using(var zip=new ZipArchive(s,ZipArchiveMode.Read)){var e=zip.GetEntry("install-manifest.json");using(var r=new StreamReader(e.Open()))return json.Deserialize<Manifest>(r.ReadToEnd());}}}
  public static Manifest InstalledManifest(string dir){try{string p=Path.Combine(dir,"install-manifest.json");return File.Exists(p)?json.Deserialize<Manifest>(File.ReadAllText(p)):null;}catch{return null;}}
  static string Hash(string file){using(var sha=SHA256.Create())using(var f=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","");}
  static string Safe(string root,string relative){if(string.IsNullOrEmpty(relative)||relative.Contains("..")||Path.IsPathRooted(relative))throw new InvalidDataException("Unsafe payload path: "+relative);var full=Path.GetFullPath(Path.Combine(root,relative.Replace('/','\\')));if(!full.StartsWith(Path.GetFullPath(root).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unsafe payload path: "+relative);return full;}
  public static List<Process> RunningCopies(string dir){var list=new List<Process>();string target=Path.GetFullPath(Path.Combine(dir,ExeName));foreach(var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExeName))){try{if(string.Equals(p.MainModule.FileName,target,StringComparison.OrdinalIgnoreCase))list.Add(p);}catch{}}return list;}
  public static List<string> LegacyVersions(string dir){if(!Directory.Exists(dir))return new List<string>();return Directory.GetDirectories(dir).Select(Path.GetFileName).Where(n=>{Version v;return Version.TryParse(n,out v)&&File.Exists(Path.Combine(dir,n,ExeName));}).OrderBy(n=>Version.Parse(n)).ToList();}
  // Installs or updates in place. Files whose SHA-256 already matches are left untouched; everything replaced is
  // backed up first and restored if any step fails. Returns the number of files written.
  public static int Install(Options o,Action<double,string> progress){
   string dir=Path.GetFullPath(o.Directory??DefaultDirectory);var manifest=PayloadManifest();if(manifest==null)throw new InvalidOperationException("This program has no installation payload.");
   if(RunningCopies(dir).Count>0)throw new IOException("TatsiaCraft is running. Close it and try again.");
   Directory.CreateDirectory(dir);string token=Guid.NewGuid().ToString("N").Substring(0,8),staging=Path.Combine(dir,".setup-"+token),backup=Path.Combine(dir,".backup-"+token);
   var previous=InstalledManifest(dir);var changed=new List<ManifestFile>();
   foreach(var f in manifest.files){string target=Safe(dir,f.path);var old=previous==null?null:previous.files.FirstOrDefault(x=>string.Equals(x.path,f.path,StringComparison.OrdinalIgnoreCase));bool same=old!=null&&old.sha256==f.sha256&&File.Exists(target)&&new FileInfo(target).Length==f.bytes&&(f.bytes>4000000||Hash(target)==f.sha256);if(!same)changed.Add(f);}
   var moved=new List<Tuple<string,string>>();var added=new List<string>();
   try{
    using(var s=Payload())using(var zip=new ZipArchive(s,ZipArchiveMode.Read)){var entries=new Dictionary<string,ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);foreach(var e in zip.Entries)entries[e.FullName.Replace('\\','/')]=e;int i=0;foreach(var f in changed){ZipArchiveEntry entry;entries.TryGetValue(f.path,out entry);if(entry==null)throw new InvalidDataException("Payload is missing "+f.path);string staged=Safe(staging,f.path);Directory.CreateDirectory(Path.GetDirectoryName(staged));entry.ExtractToFile(staged,true);if(new FileInfo(staged).Length!=f.bytes||Hash(staged)!=f.sha256)throw new InvalidDataException("Payload verification failed: "+f.path);i++;progress(.05+.75*i/Math.Max(1,changed.Count),"Copying "+Path.GetFileName(f.path));}}
    progress(.82,"Finishing");
    foreach(var f in changed){string target=Safe(dir,f.path),staged=Safe(staging,f.path);Directory.CreateDirectory(Path.GetDirectoryName(target));if(File.Exists(target)){string b=Safe(backup,f.path);Directory.CreateDirectory(Path.GetDirectoryName(b));File.Move(target,b);moved.Add(Tuple.Create(target,b));}else added.Add(target);File.Move(staged,target);}
    if(previous!=null)foreach(var stale in previous.files.Where(x=>!manifest.files.Any(n=>string.Equals(n.path,x.path,StringComparison.OrdinalIgnoreCase)))){string target;try{target=Safe(dir,stale.path);}catch{continue;}if(!File.Exists(target))continue;string b=Safe(backup,stale.path);Directory.CreateDirectory(Path.GetDirectoryName(b));File.Move(target,b);moved.Add(Tuple.Create(target,b));}
    File.WriteAllText(Path.Combine(dir,"install-manifest.json"),json.Serialize(manifest),new UTF8Encoding(false));
    progress(.9,"Registering");if(o.Register)Register(o,dir,manifest);
    if(o.RemoveLegacy)foreach(var v in LegacyVersions(dir)){try{Directory.Delete(Path.Combine(dir,v),true);foreach(var folder in new[]{Environment.GetFolderPath(Environment.SpecialFolder.Programs),Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}){string link=Path.Combine(folder,"TatsiaCraft "+v+".lnk");if(File.Exists(link))File.Delete(link);}}catch{}}
   }catch{
    foreach(var a in added)try{if(File.Exists(a))File.Delete(a);}catch{}
    foreach(var m in moved.AsEnumerable().Reverse())try{if(File.Exists(m.Item1))File.Delete(m.Item1);File.Move(m.Item2,m.Item1);}catch{}
    throw;
   }finally{try{if(Directory.Exists(staging))Directory.Delete(staging,true);}catch{}try{if(Directory.Exists(backup))Directory.Delete(backup,true);}catch{}}
   progress(1,"Installed");return changed.Count;
  }
  static RegistryKey Root(Options o,string sub){return Registry.CurrentUser.CreateSubKey((o.RegistryRoot==null?"Software\\":o.RegistryRoot.TrimEnd('\\')+"\\")+sub);}
  static string UninstallKey{get{return "Microsoft\\Windows\\CurrentVersion\\Uninstall\\"+AppName;}}
  static void Register(Options o,string dir,Manifest m){
   string exe=Path.Combine(dir,ExeName),uninstaller=Path.Combine(dir,UninstallerName);long kb=m.files.Sum(f=>f.bytes)/1024;
   using(var k=Root(o,UninstallKey)){k.SetValue("DisplayName",AppName);k.SetValue("DisplayVersion",m.version);k.SetValue("Publisher","TatsiaCraft");k.SetValue("DisplayIcon",exe+",0");k.SetValue("InstallLocation",dir);k.SetValue("UninstallString","\""+uninstaller+"\" /uninstall");k.SetValue("QuietUninstallString","\""+uninstaller+"\" /uninstall /quiet");k.SetValue("URLInfoAbout","https://github.com/stuartraymor/TatsiaCraft");k.SetValue("URLUpdateInfo","https://github.com/stuartraymor/TatsiaCraft/releases");k.SetValue("EstimatedSize",(int)Math.Min(int.MaxValue,kb),RegistryValueKind.DWord);k.SetValue("NoModify",1,RegistryValueKind.DWord);k.SetValue("NoRepair",1,RegistryValueKind.DWord);k.SetValue("InstallDate",DateTime.Now.ToString("yyyyMMdd"));}
   using(var k=Root(o,"Microsoft\\Windows\\CurrentVersion\\App Paths\\TatsiaCraft.exe")){k.SetValue("",exe);k.SetValue("Path",dir);}
   using(var k=Root(o,"TatsiaCraft")){k.SetValue("InstallLocation",dir);k.SetValue("Version",m.version);k.SetValue("Desktop",o.Desktop?1:0,RegistryValueKind.DWord);k.SetValue("Associate",o.Associate?1:0,RegistryValueKind.DWord);}
   if(o.Associate){using(var k=Root(o,"Classes\\.tasiaproj")){k.SetValue("",ProgId);}using(var k=Root(o,"Classes\\"+ProgId)){k.SetValue("","TatsiaCraft Project");}using(var k=Root(o,"Classes\\"+ProgId+"\\DefaultIcon")){k.SetValue("",exe+",0");}using(var k=Root(o,"Classes\\"+ProgId+"\\shell\\open\\command")){k.SetValue("","\""+exe+"\" \"%1\"");}if(o.RegistryRoot==null)SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);}
   if(o.RegistryRoot==null){Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),AppName+".lnk"),exe,dir);string desk=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),AppName+".lnk");if(o.Desktop)Shortcut(desk,exe,dir);else if(File.Exists(desk))File.Delete(desk);}
  }
  [DllImport("shell32.dll")] static extern void SHChangeNotify(int eventId,int flags,IntPtr item1,IntPtr item2);
  static void Shortcut(string path,string target,string dir){Type t=Type.GetTypeFromProgID("WScript.Shell");dynamic shell=Activator.CreateInstance(t);dynamic s=shell.CreateShortcut(path);s.TargetPath=target;s.WorkingDirectory=dir;s.Description="TatsiaCraft video editor";s.IconLocation=target+",0";s.Save();Marshal.FinalReleaseComObject(shell);}
  // Removes the installed files, shortcuts and registration. Projects, recordings and settings are kept.
  public static void Uninstall(Options o,Action<double,string> progress){
   string dir=Path.GetFullPath(o.Directory??(ReadInstallLocation(o)??Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));var manifest=InstalledManifest(dir);if(manifest==null||manifest.application!=AppName)throw new InvalidOperationException("No TatsiaCraft installation was found in "+dir);
   if(RunningCopies(dir).Count>0)throw new IOException("TatsiaCraft is running. Close it and try again.");
   string self=Path.GetFullPath(Assembly.GetExecutingAssembly().Location);int i=0;
   foreach(var f in manifest.files){string target;try{target=Safe(dir,f.path);}catch{continue;}i++;progress(.8*i/manifest.files.Count,"Removing "+Path.GetFileName(f.path));if(string.Equals(target,self,StringComparison.OrdinalIgnoreCase))continue;try{if(File.Exists(target))File.Delete(target);}catch{}}
   try{File.Delete(Path.Combine(dir,"install-manifest.json"));}catch{}
   foreach(var d in Directory.GetDirectories(dir,"*",SearchOption.AllDirectories).OrderByDescending(x=>x.Length))try{if(!Directory.EnumerateFileSystemEntries(d).Any())Directory.Delete(d);}catch{}
   progress(.9,"Removing shortcuts");
   if(o.RegistryRoot==null){foreach(var link in new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),AppName+".lnk"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),AppName+".lnk")})try{if(File.Exists(link))File.Delete(link);}catch{}}
   string prefix=o.RegistryRoot==null?"Software\\":o.RegistryRoot.TrimEnd('\\')+"\\";foreach(var key in new[]{UninstallKey,"Microsoft\\Windows\\CurrentVersion\\App Paths\\TatsiaCraft.exe","TatsiaCraft"})try{Registry.CurrentUser.DeleteSubKeyTree(prefix+key,false);}catch{}
   try{using(var k=Registry.CurrentUser.OpenSubKey(prefix+"Classes\\.tasiaproj")){if(k!=null&&(k.GetValue("") as string)==ProgId){k.Close();Registry.CurrentUser.DeleteSubKeyTree(prefix+"Classes\\.tasiaproj",false);}}Registry.CurrentUser.DeleteSubKeyTree(prefix+"Classes\\"+ProgId,false);}catch{}
   if(string.Equals(Path.GetDirectoryName(self),dir,StringComparison.OrdinalIgnoreCase))try{Process.Start(new ProcessStartInfo("cmd.exe","/c ping 127.0.0.1 -n 3 >nul & del /f /q \""+self+"\" & rmdir \""+dir+"\""){CreateNoWindow=true,UseShellExecute=false,WindowStyle=ProcessWindowStyle.Hidden});}catch{}
   else try{if(!Directory.EnumerateFileSystemEntries(dir).Any())Directory.Delete(dir);}catch{}
   progress(1,"Removed");
  }
  static string ReadInstallLocation(Options o){try{using(var k=Registry.CurrentUser.OpenSubKey((o.RegistryRoot==null?"Software\\":o.RegistryRoot.TrimEnd('\\')+"\\")+"TatsiaCraft"))return k==null?null:k.GetValue("InstallLocation") as string;}catch{return null;}}
 }
 public static class Program {
  // Quiet runs append their outcome to %TEMP%\TatsiaCraft-Setup.log for support and automation.
  static void Log(string text){try{File.AppendAllText(Path.Combine(Path.GetTempPath(),"TatsiaCraft-Setup.log"),DateTime.Now.ToString("s")+" "+text+Environment.NewLine);}catch{}}
  [STAThread] public static int Main(string[] args){
   Options o;try{o=Options.Parse(args);}catch(Exception e){if(args.Any(a=>a.ToLowerInvariant()=="/quiet"))Console.Error.WriteLine(e.Message);else MessageBox.Show(e.Message,"TatsiaCraft Setup");return 2;}
   if(o.WaitPid!=null){int pid;if(int.TryParse(o.WaitPid,out pid))try{Process.GetProcessById(pid).WaitForExit(60000);}catch{}}
   bool uninstaller=Installer.Payload()==null;if(uninstaller)o.Uninstall=true;
   if(o.Quiet){try{if(o.Uninstall)Installer.Uninstall(o,(p,s)=>{});else Installer.Install(o,(p,s)=>{});Log("OK "+(o.Uninstall?"uninstall":"install"));return 0;}catch(IOException e){Log(e.ToString());return e.Message.Contains("is running")?3:1;}catch(Exception e){Log(e.ToString());return 1;}}
   var app=new Application();return app.Run(new SetupWindow(o));
  }
 }
 public sealed class SetupWindow : Window {
  static Brush B(string hex){return (Brush)new BrushConverter().ConvertFromString(hex);}
  readonly Options options;readonly StackPanel body=new StackPanel();readonly ProgressBar bar=new ProgressBar{Height=6,Minimum=0,Maximum=1,Visibility=Visibility.Collapsed,Foreground=B("#7ca814"),Background=B("#1c2327"),BorderThickness=new Thickness(0)};readonly TextBlock status=new TextBlock{Foreground=B("#a8abb2"),Margin=new Thickness(0,8,0,0),TextWrapping=TextWrapping.Wrap};readonly StackPanel buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,18,0,0)};
  Button Btn(string text,bool primary,Action a){var b=new Button{Content=text,MinWidth=110,Padding=new Thickness(16,8,16,8),Margin=new Thickness(8,0,0,0),Foreground=Brushes.White,Background=primary?B("#7ca814"):B("#3b3d42"),BorderThickness=new Thickness(0),Cursor=System.Windows.Input.Cursors.Hand};b.Click+=(s,e)=>a();return b;}
  CheckBox Check(string text,bool value){return new CheckBox{Content=text,IsChecked=value,Foreground=Brushes.White,Margin=new Thickness(0,6,0,0)};}
  [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h,int a,ref int v,int size);
  public SetupWindow(Options o){
   SourceInitialized+=(s,e)=>{int dark=1;try{DwmSetWindowAttribute(new System.Windows.Interop.WindowInteropHelper(this).Handle,20,ref dark,4);}catch{}};
   options=o;Title=o.Uninstall?"Uninstall TatsiaCraft":"TatsiaCraft Setup";Width=540;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=B("#1c2327");Foreground=Brushes.White;FontFamily=new FontFamily("Segoe UI");FontSize=13;
   try{Icon=System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location).Handle,Int32Rect.Empty,System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());}catch{}
   var root=new StackPanel{Margin=new Thickness(28,24,28,22)};Content=root;var header=new StackPanel{Orientation=Orientation.Horizontal};if(Icon!=null)header.Children.Add(new Image{Source=Icon,Width=44,Height=44,Margin=new Thickness(0,0,14,0)});var titles=new StackPanel{VerticalAlignment=VerticalAlignment.Center};header.Children.Add(titles);root.Children.Add(header);root.Children.Add(body);root.Children.Add(bar);root.Children.Add(status);root.Children.Add(buttons);
   if(o.Uninstall){var m=Installer.InstalledManifest(o.Directory??Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));titles.Children.Add(new TextBlock{Text="Uninstall TatsiaCraft"+(m!=null?" "+m.version:""),FontSize=22,FontWeight=FontWeights.SemiBold});titles.Children.Add(new TextBlock{Text="Your projects, recordings and settings are kept.",Foreground=B("#a8abb2")});buttons.Children.Add(Btn("Cancel",false,Close));buttons.Children.Add(Btn("Uninstall",true,()=>Run(true)));return;}
   var manifest=Installer.PayloadManifest();string dir=o.Directory??Installer.DefaultDirectory;var installed=Installer.InstalledManifest(dir);
   titles.Children.Add(new TextBlock{Text="TatsiaCraft "+manifest.version,FontSize=22,FontWeight=FontWeights.SemiBold});titles.Children.Add(new TextBlock{Text=installed==null?"Video editor for Windows. Installs for you only, no administrator needed.":installed.version==manifest.version?"Version "+manifest.version+" is already installed. Setup will repair it.":"Update from version "+installed.version+" to "+manifest.version+". Only changed files are replaced.",Foreground=B("#a8abb2"),TextWrapping=TextWrapping.Wrap,MaxWidth=420});
   body.Margin=new Thickness(0,18,0,0);body.Children.Add(new TextBlock{Text="Install location: "+dir,Foreground=B("#a8abb2"),TextWrapping=TextWrapping.Wrap,FontSize=12});
   var desktop=Check("Create a desktop shortcut",o.Desktop);var assoc=Check("Open .tasiaproj project files with TatsiaCraft",o.Associate);var launch=Check("Start TatsiaCraft when setup finishes",true);body.Children.Add(desktop);body.Children.Add(assoc);
   var legacy=Installer.LegacyVersions(dir);CheckBox legacyBox=null;if(legacy.Count>0){legacyBox=Check("Remove older side-by-side versions ("+string.Join(", ",legacy)+")",true);body.Children.Add(legacyBox);}body.Children.Add(launch);
   body.Children.Add(new TextBlock{Text="Includes FFmpeg media tools and their licenses. Requires Windows 10 or 11 (.NET Framework 4.8).",Foreground=B("#7f8a91"),FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)});
   buttons.Children.Add(Btn("Cancel",false,Close));buttons.Children.Add(Btn(installed==null?"Install":installed.version==manifest.version?"Repair":"Update",true,()=>{o.Desktop=desktop.IsChecked==true;o.Associate=assoc.IsChecked==true;o.Launch=launch.IsChecked==true;o.RemoveLegacy=legacyBox!=null&&legacyBox.IsChecked==true;Run(false);}));
  }
  async void Run(bool uninstall){
   body.IsEnabled=false;buttons.Children.Clear();bar.Visibility=Visibility.Visible;
   while(true){
    try{await Task.Run(()=>{if(uninstall)Installer.Uninstall(options,Report);else Installer.Install(options,Report);});break;}
    catch(Exception e){if(e is IOException&&e.Message.Contains("is running")){if(MessageBox.Show(this,"Close TatsiaCraft, then select OK to continue.","TatsiaCraft is running",MessageBoxButton.OKCancel,MessageBoxImage.Information)==MessageBoxResult.OK)continue;Close();return;}status.Text="Setup could not finish: "+e.Message+" Nothing was changed.";bar.Visibility=Visibility.Collapsed;buttons.Children.Add(Btn("Close",true,Close));return;}
   }
   string dir=Path.GetFullPath(options.Directory??Installer.DefaultDirectory);status.Text=uninstall?"TatsiaCraft was removed. Your projects and settings were kept.":"TatsiaCraft is ready.";
   if(!uninstall&&options.Launch){try{Process.Start(new ProcessStartInfo(Path.Combine(dir,Installer.ExeName)){WorkingDirectory=dir,UseShellExecute=true});}catch{}Close();return;}
   buttons.Children.Add(Btn(uninstall?"Close":"Finish",true,Close));
  }
  void Report(double value,string text){Dispatcher.BeginInvoke(new Action(()=>{bar.Value=value;status.Text=text;}));}
 }
}
