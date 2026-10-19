using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Input;
using TasiaCraft;
public static partial class MediaBinTest {
 static Button PreviewButton(MediaSourcePreview w,string id){return Tree(w).OfType<Button>().First(b=>AutomationProperties.GetAutomationId(b)=="MediaPreview."+id);}
 static void ClickPreview(MediaSourcePreview w,string id){PreviewButton(w,id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
 static void RejectPreview(Media m,string label){bool rejected=false;try{MediaSourcePreview.Snapshot(m);}catch(ArgumentException){rejected=true;}catch(FileNotFoundException){rejected=true;}catch(InvalidDataException){rejected=true;}Check(rejected,label);}
 static void DialogCheck(Editor editor,Action open,Media source,bool timed){
  Exception failure=null;MediaSourcePreview seen=null;
  editor.Dispatcher.BeginInvoke(new Action(async()=>{try{
   seen=Application.Current.Windows.OfType<MediaSourcePreview>().Single();Check(seen.Owner==editor,"preview window belongs to the editor");Check(!seen.Viewer.IsPlaying&&!seen.Viewer.IsHitTestVisible,"preview opens paused with canvas editing disabled");
   var seek=Tree(seen).OfType<Slider>().Single(s=>AutomationProperties.GetAutomationId(s)=="MediaPreview.Seek");Check(Math.Abs(seek.Maximum-source.Duration)<.001,"preview duration follows untrimmed source");
   if(source.Kind!="image"){
    var decoder=Tree(seen.Viewer).OfType<MediaElement>().Single();for(int attempt=0;attempt<20&&!decoder.NaturalDuration.HasTimeSpan;attempt++)await Task.Delay(100);
    var status=Tree(seen).OfType<TextBlock>().First(t=>AutomationProperties.GetAutomationId(t)=="MediaPreview.Status");
    Check(decoder.NaturalDuration.HasTimeSpan&&decoder.NaturalDuration.TimeSpan.TotalSeconds>0,"actual "+source.Kind+" decoder opens source: "+status.Text);
    if(source.Kind=="video")Check(decoder.NaturalVideoWidth==320&&decoder.NaturalVideoHeight==180,"actual video decoder reports fixture dimensions");
   }
   seen.Viewer.UpdateLayout();if(source.Kind=="image"){var bmp=new System.Windows.Media.Imaging.RenderTargetBitmap(320,180,96,96,System.Windows.Media.PixelFormats.Pbgra32);bmp.Render(seen.Viewer.Stage);byte[] pixels=new byte[320*180*4];bmp.CopyPixels(pixels,320*4,0);Check(pixels[(90*320+160)*4+2]>200&&pixels[(90*320+160)*4]<30,"source preview renders original red image pixels");}
   seek.Value=source.Duration*.5;Check(Math.Abs(seen.Viewer.Position-source.Duration*.5)<.001,"actual source seek slider moves only its decoder");
   ClickPreview(seen,"Stop");Check(seen.Viewer.Position==0&&!seen.Viewer.IsPlaying,"Stop pauses and returns source to zero");
   ClickPreview(seen,"Play");Check(seen.Viewer.IsPlaying&&(string)PreviewButton(seen,"Play").Content=="Pause","Play starts independent source playback");
   if(timed){await Task.Delay(180);Check(seen.Viewer.Position>0,"source playback advances under actual WPF dispatcher");}
   ClickPreview(seen,"Play");Check(!seen.Viewer.IsPlaying&&(string)PreviewButton(seen,"Play").Content=="Play","Pause stops source playback");
   seek.Value=-10;Check(seen.Viewer.Position==0,"source seek clamps below zero");seek.Value=source.Duration+10;Check(seen.Viewer.Position==source.Duration,"source seek clamps above duration");
   ClickPreview(seen,"Play");Check(seen.Viewer.IsPlaying&&seen.Viewer.Position<source.Duration,"Play at end restarts source");
  }catch(Exception ex){failure=ex;}finally{if(seen!=null)ClickPreview(seen,"Close");}}));
  open();if(failure!=null)throw failure;Check(seen!=null&&seen.Released&&!seen.Viewer.IsPlaying,"Close disposes independent playback resources");
 }
 static void PreviewTests(Editor editor,Project p,LivePreview main,byte[] before,byte[] exported){
  Reset(editor,p);Call(editor,"Panel","Media");var bin=(ListBox)Field(editor,"bin");var media=P(editor).Media[0];bin.SelectedItem=bin.Items.OfType<ListBoxItem>().First(i=>i.Tag==media);
  string baseline=json.Serialize(P(editor));double head=(double)Field(editor,"head");var selected=Field(editor,"selected");var redo=((System.Collections.Generic.Stack<string>)Field(editor,"redo")).Count;
  var snapshot=MediaSourcePreview.Snapshot(media);Check(snapshot.Media.Count==1&&snapshot.Clips.Count==1&&snapshot.Clips[0].In==0&&snapshot.Clips[0].Start==0,"source snapshot contains one complete original clip");Check(!object.ReferenceEquals(snapshot.Media[0],media)&&snapshot.Media[0].Id==media.Id,"source metadata is cloned instead of shared");snapshot.Clips[0].X=20;snapshot.Media[0].Name="Changed";Check(media.Name!="Changed"&&json.Serialize(P(editor))==baseline,"snapshot edits cannot affect editor project");
  RejectPreview(null,"null preview source rejected");RejectPreview(new Media{Path=Path.Combine(root,"absent-preview.png")},"missing preview source rejected");var bad=Clone(p).Media[0];bad.Duration=double.NaN;RejectPreview(bad,"nonfinite source duration rejected");bad.Duration=0;RejectPreview(bad,"zero source duration rejected");bad.Duration=86401;RejectPreview(bad,"source duration beyond project limit rejected");bad.Duration=1;bad.Width=0;RejectPreview(bad,"unknown visual dimensions rejected");bad.Width=320;bad.Kind="unknown";RejectPreview(bad,"unsupported media kind rejected");
  var previewMenu=bin.ContextMenu.Items.OfType<MenuItem>().First(i=>(string)i.Header=="Preview");DialogCheck(editor,()=>previewMenu.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)),media,true);
  bin=(ListBox)Field(editor,"bin");var item=(ListBoxItem)bin.SelectedItem;DialogCheck(editor,()=>bin.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left){RoutedEvent=Control.MouseDoubleClickEvent,Source=item}),media,false);
  Check(json.Serialize(P(editor))==baseline&&Undos(editor)==0&&((System.Collections.Generic.Stack<string>)Field(editor,"redo")).Count==redo&&!(bool)Field(editor,"dirty"),"context and double-click previews preserve project dirty undo and redo state");Check((double)Field(editor,"head")==head&&Field(editor,"selected")==selected,"source preview preserves timeline head and selection");
  bin=(ListBox)Field(editor,"bin");item=bin.Items.OfType<ListBoxItem>().First(i=>i.Tag==media);bin.SelectedItems.Clear();Call(editor,"PreviewBinMedia");Check(!Application.Current.Windows.OfType<MediaSourcePreview>().Any(),"empty preview selection is a no-op");foreach(var i in bin.Items.OfType<ListBoxItem>().Where(i=>i.Tag is Media))i.IsSelected=true;Call(editor,"PreviewBinMedia");Check(!Application.Current.Windows.OfType<MediaSourcePreview>().Any(),"multiple preview selection is a no-op");bin.SelectedItems.Clear();bin.SelectedItem=item;Set(editor,"busy",true);Call(editor,"PreviewBinMedia");Check(!Application.Current.Windows.OfType<MediaSourcePreview>().Any(),"busy preview is a no-op");Set(editor,"busy",false);var original=item.Tag;item.Tag=new Media{Id=media.Id,Path=media.Path};Call(editor,"PreviewBinMedia");Check(!Application.Current.Windows.OfType<MediaSourcePreview>().Any(),"stale media object cannot open a preview");item.Tag=original;
  var audio=P(editor).Media.Last();bin.SelectedItem=bin.Items.OfType<ListBoxItem>().First(i=>i.Tag==audio);DialogCheck(editor,()=>Call(editor,"PreviewBinMedia"),audio,true);
  string videoPath=Path.Combine(root,"source-preview.mp4");Engine.Run("ffmpeg","-y -v error -f lavfi -i color=blue:s=320x180:d=1 -f lavfi -i sine=frequency=660:duration=1 -c:v mpeg4 -pix_fmt yuv420p -c:a aac -shortest "+Engine.Quote(videoPath));var video=Engine.Probe(videoPath);var vp=MediaSourcePreview.Snapshot(video);Check(vp.Media[0].HasAudio&&vp.Media[0].Kind=="video"&&vp.Clips[0].Duration==video.Duration,"video source snapshot retains both streams and full duration");
  DialogCheck(editor,()=>{var w=new MediaSourcePreview(video){Owner=editor};w.ShowDialog();},video,true);
  string brokenPath=Path.Combine(root,"broken-preview.png");File.WriteAllText(brokenPath,"not an image");var broken=new Media{Path=brokenPath,Name="Broken",Kind="image",Width=320,Height=180,Duration=1};bool errorVisible=false;editor.Dispatcher.BeginInvoke(new Action(()=>{var w=Application.Current.Windows.OfType<MediaSourcePreview>().Single();errorVisible=Tree(w).OfType<TextBlock>().Any(t=>AutomationProperties.GetAutomationId(t)=="MediaPreview.Status"&&t.Text.StartsWith("Image preview failed:"));w.Close();}));new MediaSourcePreview(broken){Owner=editor}.ShowDialog();Check(errorVisible,"corrupt image reports visible decoder error and remains closable");Check(File.ReadAllText(brokenPath)=="not an image","decoder failure preserves corrupt source bytes");
  byte[] videoBytes=File.ReadAllBytes(videoPath);MediaSourcePreview.Snapshot(video);Check(File.ReadAllBytes(videoPath).SequenceEqual(videoBytes),"source snapshot preserves video bytes");
  Check(json.Serialize(P(editor))==baseline&&Preview(main,P(editor)).SequenceEqual(before)&&Export(P(editor),"source-preview-preserved").SequenceEqual(exported),"source playback leaves main preview and decoded export unchanged");string save=Path.Combine(root,"preview-preserved.tasiaproj");Engine.Save(P(editor),save);Check(json.Serialize(Engine.Load(save))==baseline,"source previews preserve save/reopen data");
  bin=(ListBox)Field(editor,"bin");item=bin.Items.OfType<ListBoxItem>().First(i=>i.Tag==media);bin.SelectedItem=item;var insert=bin.ContextMenu.Items.OfType<MenuItem>().First(i=>(string)i.Header=="Add to Timeline at Playhead");Set(editor,"busy",true);insert.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Check(json.Serialize(P(editor))==baseline&&Undos(editor)==0,"explicit insertion remains guarded while busy");Set(editor,"busy",false);P(editor).LockedTracks.Add(0);insert.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Check(P(editor).Clips.Count==p.Clips.Count&&Undos(editor)==0,"explicit insertion respects target track locking");P(editor).LockedTracks.Remove(0);insert.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Check(P(editor).Clips.Count==p.Clips.Count+1&&Undos(editor)==1,"explicit context insertion creates one timeline clip and checkpoint");Call(editor,"Undo");Check(json.Serialize(P(editor))==baseline,"one undo restores project after explicit source insertion");Reset(editor,p);
 }
}
