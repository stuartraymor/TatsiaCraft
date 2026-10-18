using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
namespace TasiaCraft {
public partial class Editor {
 // Tool order, wording and window caption follow the observed reference layout; see docs/UI-WORDING-EVIDENCE.md.
 static readonly string[] NavigationTools={"Media","Library","Annotations","Visual Effects","Transitions","Animations","Audio Effects","Captions"};
 TextBlock frameRate;
 string WindowTitle(){return ProductInfo.DisplayName+" - "+(projectPath==""?"Untitled Project":Path.GetFileName(projectPath))+(dirty?"*":"");}
 void UpdateWindowTitle(){Title=WindowTitle();}
 void StepFrames(int frames){Pause();Seek(head+frames/(double)project.Fps);}
 void AddTrack(){if(busy)return;if(project.TrackCount>=64){Info("A project can have at most 64 tracks.");return;}Checkpoint();project.TrackCount++;Refresh();}
 static string ClockTime(double time){var span=TimeSpan.FromSeconds(Math.Max(0,Math.Floor(time)));return span.TotalHours>=1?((int)span.TotalHours)+":"+span.ToString(@"mm\:ss"):span.ToString(@"mm\:ss");}
 static ControlTemplate PillTemplate(){return (ControlTemplate)XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border Background='{TemplateBinding Background}' CornerRadius='16' Padding='{TemplateBinding Padding}'><ContentPresenter VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter Property='Opacity' Value='.82'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='.35'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");}
 void ShowShortcuts(){MessageBox.Show(
  "File\n  Ctrl+N  New Project\n  Ctrl+O  Open Project\n  Ctrl+S / Ctrl+Shift+S  Save / Save As\n  Ctrl+I  Import Media\n  Ctrl+R  New Recording\n  Ctrl+H  Home\n\n"+
  "Edit\n  Ctrl+Z / Ctrl+Y  Undo / Redo\n  Ctrl+X / C / V  Cut / Copy / Paste\n  Ctrl+D  Duplicate\n  Del  Delete\n  S  Split Selected\n  Ctrl+A  Select All\n  Ctrl+G / Ctrl+U  Group / Ungroup\n  Ctrl/Shift-click  Add to or remove from selection\n\n"+
  "Playback\n  Spacebar  Play / Pause\n  , / .  Previous / Next frame\n  Ctrl+Alt+, / Ctrl+Alt+.  Previous / Next media\n  Home / End  Jump to beginning / end\n\n"+
  "View\n  Ctrl+2  Show or hide Properties\n  Ctrl+= / Ctrl+-  Canvas zoom in / out\n  Ctrl+Shift+= / Ctrl+Shift+-  Timeline zoom in / out\n  Ctrl+Shift+7  Bring all media on timeline into view\n  Ctrl+;  Enable Canvas Snapping\n\n"+
  "Export\n  Ctrl+Shift+F  Export Frame As\n\n"+
  "Canvas: drag to move, corner handles resize, the side handle rotates. Crop mode: drag media inside a fixed crop; drag edges to trim.\nTimeline: drag clip edges to trim; hold Shift to disable snapping.",
  "Keyboard Shortcuts");}
}
}
