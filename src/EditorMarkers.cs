using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation;
namespace TasiaCraft {
public partial class Editor {
 // Marker tray behavior follows docs/MARKER-EVIDENCE.md: Ctrl+M shows the tray, Shift+M or a tray click adds
 // "Marker N", selection opens Marker properties, and the context menu offers Rename and Delete.
 const double MarkerRowHeight=26,SnapPixels=8;
 bool markerTrackVisible; string selectedMarkerId;
 double TracksTop{get{return 34+(markerTrackVisible?MarkerRowHeight:0);}}
 Marker SelectedMarker(){return selectedMarkerId==null?null:project.Markers.FirstOrDefault(m=>m.Id==selectedMarkerId);}
 void ToggleMarkerTrack(){markerTrackVisible=!markerTrackVisible;DrawTimeline();}
 void SelectMarker(string id){var marker=project.Markers.FirstOrDefault(m=>m.Id==id);if(marker==null)return;SetSelected(new Clip[0]);selectedMarkerId=id;SyncSelection();Pause();Seek(marker.Time);Props();DrawTimeline();}
 bool AddMarkerAt(double time){
  if(busy)return false;time=Math.Max(0,time);var existing=TimelineMarkers.At(project,time);if(existing!=null){markerTrackVisible=true;SelectMarker(existing.Id);Info("A marker already exists at this frame.");return false;}
  try{var candidate=json.Deserialize<Project>(json.Serialize(project));TimelineMarkers.Add(candidate,time);Engine.Validate(candidate,false);}catch(Exception e){Info(e.Message);return false;}
  Checkpoint();var marker=TimelineMarkers.Add(project,time);markerTrackVisible=true;selectedMarkerId=marker.Id;Refresh();SelectMarker(marker.Id);Info(marker.Name+" added");return true;
 }
 void AddMarkerAtPlayhead(){AddMarkerAt(head);}
 bool RenameMarkerTo(string id,string name){
  if(busy)return false;try{var candidate=json.Deserialize<Project>(json.Serialize(project));if(!TimelineMarkers.Rename(candidate,id,name))return true;Engine.Validate(candidate,false);Checkpoint();TimelineMarkers.Rename(project,id,name);Refresh();return true;}catch(Exception e){Info(e.Message);return false;}
 }
 void DeleteMarker(string id){if(busy||!project.Markers.Any(m=>m.Id==id))return;Checkpoint();TimelineMarkers.Remove(project,id);if(selectedMarkerId==id)selectedMarkerId=null;Refresh();}
 void RemoveAllMarkers(){if(busy||project.Markers.Count==0)return;Checkpoint();project.Markers.Clear();selectedMarkerId=null;Refresh();}
 void JumpToMarker(int direction){var marker=TimelineMarkers.Next(project,head,direction);if(marker==null){Info(direction>0?"No later marker":"No earlier marker");return;}Pause();Seek(marker.Time);}
 void RenameMarkerDialog(string id){
  var marker=project.Markers.FirstOrDefault(m=>m.Id==id);if(marker==null||busy)return;
  var dialog=new Window{Title="Rename Marker",Owner=this,Width=390,Height=230,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=panel,Foreground=Foreground};var content=new StackPanel{Margin=new Thickness(16)};dialog.Content=content;
  content.Children.Add(Text("Marker name"));var input=new TextBox{Text=marker.Name,Margin=new Thickness(8),Padding=new Thickness(5),Background=B("#1b2328"),Foreground=Brushes.White};AutomationProperties.SetAutomationId(input,"Marker.RenameText");content.Children.Add(input);var error=Text("",11,B("#ff8a80"));content.Children.Add(error);
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};content.Children.Add(buttons);
  Action accept=()=>{try{TimelineMarkers.NormalizeName(input.Text);}catch(ArgumentException e){error.Text=e.Message;return;}if(RenameMarkerTo(id,input.Text))dialog.Close();else error.Text="The marker could not be renamed.";};
  var ok=Btn("Rename",accept,true);ok.IsDefault=true;buttons.Children.Add(ok);var cancel=Btn("Cancel",()=>dialog.Close());cancel.IsCancel=true;buttons.Children.Add(cancel);
  dialog.Loaded+=(s,e)=>{input.Focus();input.SelectAll();};dialog.ShowDialog();
 }
 bool MarkerProps(){
  var marker=SelectedMarker();if(marker==null)return false;propertyColumn.Width=new GridLength(260);
  properties.Children.Clear();properties.Children.Add(Text("Marker",16));properties.Children.Add(Text(marker.Name,13,muted));
  properties.Children.Add(Text("Marker name",11,muted));var name=new TextBox{Text=marker.Name,Margin=new Thickness(10,0,10,6),Padding=new Thickness(5,3,5,3),Background=B("#1b2328"),Foreground=Brushes.White,BorderBrush=B("#46525b")};AutomationProperties.SetAutomationId(name,"Marker.Name");properties.Children.Add(name);
  var error=Text("",11,B("#ff8a80"));properties.Children.Add(error);
  Action commit=()=>{if(name.Text==marker.Name)return;try{TimelineMarkers.NormalizeName(name.Text);}catch(ArgumentException e){error.Text=e.Message;return;}string id=marker.Id;if(RenameMarkerTo(id,name.Text)){error.Text="";}};
  name.KeyDown+=(s,e)=>{if(e.Key==Key.Enter){commit();e.Handled=true;}else if(e.Key==Key.Escape){name.Text=marker.Name;error.Text="";e.Handled=true;}};name.LostKeyboardFocus+=(s,e)=>commit();
  properties.Children.Add(Text("Time  "+FrameTime(marker.Time),11,muted));
  properties.Children.Add(Btn("Delete Marker",()=>DeleteMarker(marker.Id)));
  return true;
 }
 void DrawMarkerRow(){
  var row=new Border{Width=timeline.Width-TrackHeaderWidth,Height=MarkerRowHeight,Background=B("#1d262b"),BorderBrush=B("#2e3940"),BorderThickness=new Thickness(0,1,0,0),ToolTip="Click to add a marker"};Canvas.SetLeft(row,TrackHeaderWidth);Canvas.SetTop(row,34);AutomationProperties.SetAutomationId(row,"Timeline.MarkerTray");timeline.Children.Add(row);
  row.MouseLeftButtonDown+=(s,e)=>{AddMarkerAt(SnapMarkerCreation(Math.Max(0,(e.GetPosition(timeline).X-TrackHeaderWidth)/zoom)));e.Handled=true;};
  var header=new Border{Width=TrackHeaderWidth,Height=MarkerRowHeight,Background=B("#29323a"),BorderBrush=B("#2e3940"),BorderThickness=new Thickness(0,1,0,0),Child=new TextBlock{Text="Marker",Foreground=Brushes.White,FontSize=11,Margin=new Thickness(8,5,0,0)}};Canvas.SetTop(header,34);timeline.Children.Add(header);
  foreach(var marker in project.Markers){
   var m=marker;bool active=m.Id==selectedMarkerId;var chip=new Border{Background=B("#3a95d6"),BorderBrush=active?Brushes.White:B("#2a6fa3"),BorderThickness=new Thickness(active?1.5:1),CornerRadius=new CornerRadius(2),Padding=new Thickness(5,1,5,1),Cursor=Cursors.SizeWE,ToolTip=m.Name+"  •  "+FrameTime(m.Time)+"\nDrag to move • double-click to rename",Child=new TextBlock{Text=m.Name,Foreground=Brushes.White,FontSize=11,MaxWidth=160,TextTrimming=TextTrimming.CharacterEllipsis}};
   AutomationProperties.SetName(chip,m.Name);AutomationProperties.SetAutomationId(chip,"Marker."+m.Id);Canvas.SetLeft(chip,TrackHeaderWidth+m.Time*zoom);Canvas.SetTop(chip,38);timeline.Children.Add(chip);
   double origin=0,start=0;bool moving=false,changed=false;
   chip.MouseLeftButtonDown+=(s,e)=>{if(busy)return;e.Handled=true;if(e.ClickCount==2){RenameMarkerDialog(m.Id);return;}origin=e.GetPosition(timeline).X;start=m.Time;moving=true;changed=false;chip.CaptureMouse();};
   chip.MouseMove+=(s,e)=>{if(!moving||!chip.IsMouseCaptured)return;double x=e.GetPosition(timeline).X;if(!changed&&Math.Abs(x-origin)<2)return;double t=SnapMarkerTime(Math.Max(0,start+(x-origin)/zoom),m.Id);var other=TimelineMarkers.At(project,t);if(other!=null&&other!=m)return;if(!changed){Checkpoint();changed=true;}TimelineMarkers.Move(project,m.Id,t);Canvas.SetLeft(chip,TrackHeaderWidth+m.Time*zoom);};
   chip.MouseLeftButtonUp+=(s,e)=>{if(!moving)return;moving=false;chip.ReleaseMouseCapture();e.Handled=true;if(changed){selectedMarkerId=m.Id;Refresh();Info(m.Name+" moved to "+FrameTime(m.Time));}else SelectMarker(m.Id);};
   var menu=new ContextMenu();var rename=new MenuItem{Header="Rename"};rename.Click+=(s,e)=>RenameMarkerDialog(m.Id);menu.Items.Add(rename);var delete=new MenuItem{Header="Delete"};delete.Click+=(s,e)=>DeleteMarker(m.Id);menu.Items.Add(delete);chip.ContextMenu=menu;
  }
 }
}
}
