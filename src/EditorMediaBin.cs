using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Input;
namespace TasiaCraft { public partial class Editor {
 bool MediaBinKey(Key key,ModifierKeys modifiers,bool focused){
  if(!focused||bin==null)return false;bool ctrl=(modifiers&ModifierKeys.Control)!=0,shift=(modifiers&ModifierKeys.Shift)!=0,alt=(modifiers&ModifierKeys.Alt)!=0;
  if(ctrl&&!alt&&(key==Key.A&&!shift||key==Key.D&&shift)){bin.SelectedItems.Clear();if(key==Key.A)foreach(var item in bin.Items.OfType<ListBoxItem>())if(item.Tag is Media)item.IsSelected=true;return true;}
  bool editing=key==Key.Delete||!ctrl&&!alt&&key==Key.S||!ctrl&&!alt&&shift&&key==Key.R||ctrl&&(key==Key.X||key==Key.C||key==Key.V||key==Key.D||key==Key.G||key==Key.U||key==Key.Back||alt&&key==Key.I);
  if(editing){Info("Use the Media Bin context menu for media commands.");return true;}return false;
 }
 void SelectUnusedMedia(){if(busy)return;var ids=MediaBinOperations.Unused(project).Select(m=>m.Id).ToList();mediaFilter="All media";Panel("Media");bin.SelectedItems.Clear();foreach(var item in bin.Items.OfType<ListBoxItem>())if(item.Tag is Media&&ids.Contains(((Media)item.Tag).Id))item.IsSelected=true;if(bin.SelectedItem!=null)bin.ScrollIntoView(bin.SelectedItem);Info(ids.Count+" unused media item(s) selected");}
 void DeleteUnusedMedia(){if(busy)return;var unused=MediaBinOperations.Unused(project);if(unused.Count==0){Info("No unused media");return;}Checkpoint();int count=MediaBinOperations.RemoveUnused(project);Refresh();Info("Removed "+count+" unused media reference(s). Source files were kept.");}
 string MediaDetailsText(Media media){return media.Name+"\nType: "+media.Kind+"\nDimensions: "+(media.Width>0&&media.Height>0?media.Width+" × "+media.Height:"Not applicable")+"\nDuration: "+media.Duration.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+" s\nAudio: "+(media.HasAudio?"Yes":"No")+"\nFile: "+(File.Exists(media.Path)?"Available":"Missing")+"\nTimeline references: "+project.Clips.Count(c=>c.MediaId==media.Id)+"\n\n"+media.Path;}
 void ShowMediaDetails(){if(bin==null||bin.SelectedItems.Count!=1)return;var item=bin.SelectedItem as ListBoxItem;var media=item==null?null:item.Tag as Media;if(media==null)return;var dialog=new Window{Title="Media Details",Owner=this,Width=490,Height=340,MinWidth=330,MinHeight=230,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=panel,Foreground=Foreground};var dock=new DockPanel{Margin=new Thickness(14)};dialog.Content=dock;var close=Btn("Close",()=>dialog.Close());close.IsCancel=true;close.IsDefault=true;DockPanel.SetDock(close,Dock.Bottom);dock.Children.Add(close);var details=new TextBox{Text=MediaDetailsText(media),IsReadOnly=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Background=panel,Foreground=Foreground,BorderThickness=new Thickness(0),Padding=new Thickness(8)};AutomationProperties.SetAutomationId(details,"Media.Details");dock.Children.Add(details);dialog.ShowDialog();}
 void WireMediaBinMenu(){var menu=new ContextMenu();var details=new MenuItem{Header="Details"};details.Click+=(s,e)=>ShowMediaDetails();menu.Items.Add(details);menu.Items.Add(new Separator());var select=new MenuItem{Header="Select Unused Media"};select.Click+=(s,e)=>SelectUnusedMedia();menu.Items.Add(select);var remove=new MenuItem{Header="Delete Unused Media"};remove.Click+=(s,e)=>DeleteUnusedMedia();menu.Items.Add(remove);bin.ContextMenu=menu;bin.ContextMenuOpening+=(s,e)=>{details.IsEnabled=bin.SelectedItems.Count==1&&bin.SelectedItem is ListBoxItem&&((ListBoxItem)bin.SelectedItem).Tag is Media;bool any=MediaBinOperations.Unused(project).Count>0;select.IsEnabled=remove.IsEnabled=!busy&&any;};}
}}
