using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
namespace TasiaCraft {
public partial class Editor {
 // Tool order, wording and window caption follow the observed reference layout; see docs/UI-WORDING-EVIDENCE.md.
 static readonly string[] NavigationTools={"Favorites","Media","Library","Annotations","Visual Effects","Transitions","Animations","Behaviors","Audio Effects","Narration","Captions"};
 TextBlock frameRate;
 string WindowTitle(){return ProductInfo.DisplayName+" - "+(projectPath==""?"Untitled Project":Path.GetFileName(projectPath))+(dirty?"*":"");}
 void UpdateWindowTitle(){Title=WindowTitle();}
 void RippleDeleteSelection(){if(busy)return;var clips=SelectedClips();if(clips.Count==0)return;if(clips.Any(TrackLocked)){Info("Track is locked.");return;}var candidate=json.Deserialize<Project>(json.Serialize(project));try{RippleEditing.RippleDelete(candidate,candidate.Clips.Where(x=>clips.Any(y=>y.Id==x.Id)));Engine.Validate(candidate,false);}catch(Exception e){Info(e.Message);return;}Checkpoint();RippleEditing.RippleDelete(project,clips);SetSelected(new Clip[0]);Refresh();Info("Ripple Delete closed the gap");}
 void SeparateSelection(){if(busy)return;var clips=SelectedClips();if(!clips.Any(c=>RippleEditing.CanSeparate(project,c))){Info("Select a video clip that has audio to separate.");return;}var candidate=json.Deserialize<Project>(json.Serialize(project));try{RippleEditing.SeparateAudioVideo(candidate,candidate.Clips.Where(x=>clips.Any(y=>y.Id==x.Id)));Engine.Validate(candidate,false);}catch(Exception e){Info(e.Message);return;}Checkpoint();var created=RippleEditing.SeparateAudioVideo(project,clips);SetSelected(clips.Where(c=>c.Stream=="video").Concat(created));SyncSelection();Refresh();Info("Audio separated to a new track");}
 void SilenceSelection(bool silenced){if(busy)return;if(HasRange){ApplyRange(silenced);return;}var clips=SelectedClips();if(!clips.Any(c=>RippleEditing.HasPlayableAudio(project,c)&&(silenced?!c.Silenced:c.Silenced||AudioEnvelope.Points(c).Count>0))){Info(silenced?"Select a clip with audio to silence.":"Select a silenced clip to restore.");return;}if(clips.Any(TrackLocked)){Info("Track is locked.");return;}Checkpoint();int n=RippleEditing.SetSilenced(project,clips,silenced);Refresh();Info(silenced?"Audio silenced on "+n+" clip(s)":"Audio restored on "+n+" clip(s)");}
 void StitchSelection(){if(busy)return;var clips=SelectedClips();if(clips.Count<2){Info("Select adjacent clips from the same media to stitch.");return;}if(clips.Any(TrackLocked)){Info("Track is locked.");return;}var candidate=json.Deserialize<Project>(json.Serialize(project));int joined;try{joined=RippleEditing.Stitch(candidate,candidate.Clips.Where(x=>clips.Any(y=>y.Id==x.Id)));Engine.Validate(candidate,false);}catch(Exception e){Info(e.Message);return;}if(joined==0){Info("Only touching clips that continue the same media can be stitched.");return;}Checkpoint();RippleEditing.Stitch(project,clips);SetSelected(clips.Where(project.Clips.Contains));Refresh();Info("Stitched "+(joined+1)+" clips");}
 bool RippleInsertAt(double at,double seconds){if(busy)return false;var candidate=json.Deserialize<Project>(json.Serialize(project));try{RippleEditing.RippleInsert(candidate,at,seconds);Engine.Validate(candidate,false);}catch(Exception e){Info(e.Message);return false;}if(json.Serialize(candidate)==json.Serialize(project)){Info("Nothing after the playhead to move.");return false;}Checkpoint();RippleEditing.RippleInsert(project,at,seconds);Refresh();Info("Inserted "+seconds.ToString("0.##")+" s at "+FrameTime(at));return true;}
 void DeselectAll(){SetSelected(new Clip[0]);selectedMarkerId=null;rangeStart=rangeEnd=null;SyncSelection();Props();DrawTimeline();}
 void StepFrames(int frames){Pause();Seek(head+frames/(double)project.Fps);}
 void AddTrack(){if(busy)return;if(project.TrackCount>=64){Info("A project can have at most 64 tracks.");return;}Checkpoint();project.TrackCount++;Refresh();}
 static string ClockTime(double time){var span=TimeSpan.FromSeconds(Math.Max(0,Math.Floor(time)));return span.TotalHours>=1?((int)span.TotalHours)+":"+span.ToString(@"mm\:ss"):span.ToString(@"mm\:ss");}
 static ControlTemplate PillTemplate(){return (ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border Background='{TemplateBinding Background}' CornerRadius='16' Padding='{TemplateBinding Padding}'><ContentPresenter VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter Property='Opacity' Value='.82'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='.35'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");}
 void ShowShortcuts(){MessageBox.Show(
  "File\n  Ctrl+N  New Project\n  Ctrl+O  Open Project\n  Ctrl+S / Ctrl+Shift+S  Save / Save As\n  Ctrl+I  Import Media\n  Ctrl+R  New Recording\n  Ctrl+H  Home\n\n"+
  "Edit\n  Ctrl+Z / Ctrl+Y  Undo / Redo\n  Ctrl+X / C / V  Cut / Copy / Paste\n  Ctrl+D  Duplicate\n  Del  Delete\n  Ctrl+Backspace  Ripple Delete (closes the gap on the track)\n  Ctrl+Alt+I  Stitch Selected Media\n  Shift+S / Shift+R  Silence Audio / Restore Audio (the green/red selection range, or whole clips)\n  Double-click the green audio line to add an audio point; drag points; right-click to delete\n  Shift-drag the playhead  Ripple Insert time on all unlocked tracks\n  Shift-drag  Ripple move (moves later media on the track too)\n  S  Split Selected\n  Ctrl+A  Select All\n  Ctrl+G / Ctrl+U  Group / Ungroup\n  Ctrl/Shift-click  Add to or remove from selection\n  Ctrl+Shift+D  Deselect All\n\n"+
  "Playback\n  Spacebar  Play / Pause\n  , / .  Previous / Next frame\n  Ctrl+Alt+, / Ctrl+Alt+.  Previous / Next media\n  Ctrl+[ / Ctrl+]  Previous / Next marker\n  Ctrl+Home / Ctrl+End  Jump to beginning / end\n\n"+
  "Markers\n  Ctrl+M  Show or hide the Marker tray\n  Shift+M  Add Timeline Marker at the playhead\n  Click the tray to add; drag to move; double-click or right-click to rename or delete\n\n"+
  "View\n  Ctrl+2  Show or hide Properties\n  Ctrl+= / Ctrl+-  Canvas zoom in / out\n  Ctrl+Shift+= / Ctrl+Shift+-  Timeline zoom in / out\n  Ctrl+Shift+7  Bring all media on timeline into view\n  Ctrl+;  Enable Canvas Snapping\n\n"+
  "Tools\n  F  Favorites (click a tile's star to add it)\n  Behaviors: select a clip and click a behavior; adjust In/Out in Properties\n\n"+
  "Narration\n  Pick a microphone, then Start Voice Recording; the take is placed on a new track at the playhead\n\n"+
  "Export\n  Ctrl+Shift+F  Export Frame As\n  Markers are saved in exported MP4 files as chapters\n\n"+
  "Canvas: drag to move, corner handles resize, the side handle rotates. Crop mode: drag media inside a fixed crop; drag edges to trim.\nTimeline: drag clip edges to trim. Shift-drag ripple moves without snapping; Shift also disables snapping for markers and the playhead.",
  "Keyboard Shortcuts");}
}
}
