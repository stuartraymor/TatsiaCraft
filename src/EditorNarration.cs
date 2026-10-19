using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;
namespace TasiaCraft {
public partial class Editor {
 // Narration tool: microphone, input meter and level, "Mute timeline during recording", Start Voice Recording and a
 // script box, following the observed reference panel. The take is placed on a new track at the playhead where
 // recording started. See docs/NARRATION-EVIDENCE.md.
 List<string> narrationDevices; string narrationDevice; double narrationGain=1; bool narrationMute=true;
 string narrationInputOverride=null; // test hook: FFmpeg input arguments used instead of the selected microphone
 NarrationCapture narrationMonitor,narrationRecording; double narrationStartHead; Stopwatch narrationClock; bool narrationStarting;
 Border narrationMeter; Button narrationButton;
 string NarrationFolder{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TasiaCraft","Narration");}}
 string NarrationInput(){return narrationInputOverride??(narrationDevice==null?null:NarrationDevices.InputArgs(narrationDevice));}
 void ShowLevel(double db){var meter=narrationMeter;if(meter==null)return;double level=double.IsNegativeInfinity(db)?0:Math.Max(0,Math.Min(1,(db+60)/60));Dispatcher.BeginInvoke(new Action(()=>{if(meter.Parent is FrameworkElement){meter.Width=Math.Max(0,((FrameworkElement)meter.Parent).ActualWidth*level);meter.Background=db>-6?B("#e53935"):db>-18?B("#f2c94c"):B("#5bd6a2");}}));}
 void StopNarrationMonitor(){var m=narrationMonitor;narrationMonitor=null;if(m!=null)Task.Run(()=>m.Dispose());}
 async void StartNarrationMonitor(){StopNarrationMonitor();if(narrationRecording!=null||activePanel!="Narration")return;string input=NarrationInput();if(input==null)return;var m=new NarrationCapture(input,null,narrationGain);m.Level+=ShowLevel;narrationMonitor=m;try{await m.StartAsync();}catch(Exception e){if(narrationMonitor==m){narrationMonitor=null;Info("Microphone: "+e.Message);}}if(narrationMonitor!=m)m.Dispose();}
 void NarrationPanel(){
  PaletteHeading("Narration",null);
  var deviceRow=new DockPanel{Margin=new Thickness(10,4,10,4)};var devices=new ComboBox{IsEnabled=false};AutomationProperties.SetAutomationId(devices,"Narration.Device");deviceRow.Children.Add(devices);library.Children.Add(deviceRow);
  var meterTrack=new Border{Height=6,Margin=new Thickness(10,2,10,6),Background=B("#11181c"),CornerRadius=new CornerRadius(2)};narrationMeter=new Border{Width=0,HorizontalAlignment=HorizontalAlignment.Left,Background=B("#5bd6a2"),CornerRadius=new CornerRadius(2)};AutomationProperties.SetAutomationId(narrationMeter,"Narration.Meter");meterTrack.Child=narrationMeter;meterTrack.ToolTip="Input level";library.Children.Add(meterTrack);
  var gain=new Slider{Minimum=0,Maximum=2,Value=narrationGain,Margin=new Thickness(10,0,10,6),ToolTip="Input volume"};AutomationProperties.SetAutomationId(gain,"Narration.Gain");gain.ValueChanged+=(s,e)=>{narrationGain=gain.Value;if(narrationRecording==null)StartNarrationMonitor();};library.Children.Add(gain);
  var mute=new CheckBox{Content="Mute timeline during recording",IsChecked=narrationMute,Foreground=Brushes.White,Margin=new Thickness(10,2,10,8)};AutomationProperties.SetAutomationId(mute,"Narration.Mute");mute.Click+=(s,e)=>narrationMute=mute.IsChecked==true;library.Children.Add(mute);
  narrationButton=Btn(narrationRecording!=null?"Stop Voice Recording":"Start Voice Recording",ToggleNarration);narrationButton.Background=B("#e8484d");narrationButton.FontWeight=FontWeights.SemiBold;narrationButton.Padding=new Thickness(10,11,10,11);narrationButton.Margin=new Thickness(10,4,10,10);AutomationProperties.SetAutomationId(narrationButton,"Narration.Record");library.Children.Add(narrationButton);
  var script=new TextBox{Text=project.NarrationScript??"",AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,MinHeight=180,Margin=new Thickness(10,0,10,10),Padding=new Thickness(6),Background=B("#11181c"),Foreground=Brushes.White,BorderBrush=B("#3b4850"),VerticalScrollBarVisibility=ScrollBarVisibility.Auto};AutomationProperties.SetAutomationId(script,"Narration.Script");
  var placeholder=new TextBlock{Text="Paste or type script text here",Foreground=muted,Margin=new Thickness(18,7,0,0),IsHitTestVisible=false,FontStyle=FontStyles.Italic,Visibility=string.IsNullOrEmpty(script.Text)?Visibility.Visible:Visibility.Collapsed};var scriptHost=new Grid();scriptHost.Children.Add(script);scriptHost.Children.Add(placeholder);library.Children.Add(scriptHost);
  script.TextChanged+=(s,e)=>{placeholder.Visibility=string.IsNullOrEmpty(script.Text)?Visibility.Visible:Visibility.Collapsed;if(script.Text.Length>100000){Info("The script is limited to 100,000 characters.");return;}if((project.NarrationScript??"")==script.Text)return;project.NarrationScript=script.Text;dirty=true;UpdateWindowTitle();};
  Action fill=()=>{devices.Items.Clear();foreach(string d in narrationDevices)devices.Items.Add(d);if(narrationDevices.Count==0){devices.Items.Add("No microphone found");devices.SelectedIndex=0;devices.IsEnabled=false;return;}if(narrationDevice==null||!narrationDevices.Contains(narrationDevice))narrationDevice=narrationDevices[0];devices.SelectedItem=narrationDevice;devices.IsEnabled=narrationRecording==null;StartNarrationMonitor();};
  devices.SelectionChanged+=(s,e)=>{var d=devices.SelectedItem as string;if(d==null||narrationDevices==null||!narrationDevices.Contains(d)||d==narrationDevice)return;narrationDevice=d;StartNarrationMonitor();};
  if(narrationDevices!=null)fill();else{devices.Items.Add("Finding microphones…");devices.SelectedIndex=0;Task.Run(()=>{try{return NarrationDevices.List();}catch{return new List<string>();}}).ContinueWith(t=>Dispatcher.BeginInvoke(new Action(()=>{narrationDevices=t.Result;if(activePanel=="Narration")fill();})));}
 }
 async void ToggleNarration(){if(narrationRecording!=null)await StopNarration();else await StartNarration();}
 async Task<bool> StartNarration(){
  if(busy||narrationStarting||narrationRecording!=null)return false;string input=NarrationInput();if(input==null){Info("Select a microphone to record narration.");return false;}
  narrationStarting=true;StopNarrationMonitor();Pause();narrationStartHead=head;Directory.CreateDirectory(NarrationFolder);string path=Path.Combine(NarrationFolder,"Narration-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".wav");
  var capture=new NarrationCapture(input,path,narrationGain);capture.Level+=ShowLevel;
  try{await capture.StartAsync();}catch(Exception e){narrationStarting=false;Info(e.Message);MessageBox.Show(e.Message,"Narration");StartNarrationMonitor();return false;}
  narrationRecording=capture;narrationStarting=false;narrationClock=Stopwatch.StartNew();if(live!=null){live.Muted=narrationMute;if(Length()>head+.01){live.SetProject(project);live.Play();playing=live.IsPlaying;UpdatePlayUi();}}
  if(narrationButton!=null)narrationButton.Content="Stop Voice Recording";Info("Recording narration… click Stop Voice Recording to finish");return true;
 }
 async Task<bool> StopNarration(){
  var capture=narrationRecording;if(capture==null)return false;narrationRecording=null;Pause();if(live!=null)live.Muted=false;if(narrationButton!=null){narrationButton.IsEnabled=false;narrationButton.Content="Finishing…";}
  try{
   string path=await capture.StopAsync();var media=await Task.Run(()=>Engine.Probe(path));capture.Dispose();
   if(project.TrackCount>=64)throw new InvalidOperationException("A project can have at most 64 tracks.");
   // The first level reading covers the first 100 ms of audio and playback started when it arrived, so skip that lead-in.
   double lead=Math.Min(NarrationCapture.Window,Math.Max(0,media.Duration-.05));
   Checkpoint();project.Media.Add(media);project.TrackCount++;var clip=new Clip{MediaId=media.Id,Start=narrationStartHead,In=lead,Duration=Math.Max(.05,media.Duration-lead),Track=project.TrackCount-1,NativeSize=true};project.Clips.Add(clip);SetSelected(new[]{clip});
   Refresh();Info("Narration added: "+media.Duration.ToString("0.0")+" s on Track "+(clip.Track+1));return true;
  }catch(Exception e){capture.Dispose();Info("Narration failed: "+e.Message);MessageBox.Show(e.Message,"Narration");return false;}
  finally{if(narrationButton!=null){narrationButton.IsEnabled=true;narrationButton.Content="Start Voice Recording";}StartNarrationMonitor();}
 }
}
}
