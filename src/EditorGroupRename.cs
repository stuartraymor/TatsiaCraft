using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
namespace TasiaCraft {public partial class Editor {
 ClipGroup RenameTarget(){var clips=SelectedClips();if(clips.Count==0)return null;var group=ClipGroups.ForClip(project,clips[0]);return group!=null&&clips.Count==group.ClipIds.Count&&clips.All(c=>group.ClipIds.Contains(c.Id))?group:null;}
 bool RenameGroupTo(string name){if(busy){Info("Wait for the current operation to finish.");return false;}var group=RenameTarget();if(group==null){Info("Select exactly one complete group to rename.");return false;}try{var candidate=json.Deserialize<Project>(json.Serialize(project));if(!ClipGroups.Rename(candidate,group.Id,name))return true;Checkpoint();group.Name=candidate.Groups.First(g=>g.Id==group.Id).Name;Refresh();Info("Group renamed");return true;}catch(Exception error){Info(error.Message);return false;}}
 void RenameSelectedGroup(){if(busy)return;var group=RenameTarget();if(group==null){Info("Select exactly one complete group to rename.");return;}if(project.Clips.Any(c=>group.ClipIds.Contains(c.Id)&&TrackLocked(c))){Info("Unlock every member track before renaming the group.");return;}
  var dialog=new Window{Title="Rename group",Owner=this,Width=390,Height=230,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=panel,Foreground=Foreground};var content=new StackPanel{Margin=new Thickness(16)};dialog.Content=content;content.Children.Add(Text("Group name"));var input=new TextBox{Text=group.Name,Margin=new Thickness(8),Padding=new Thickness(5)};AutomationProperties.SetAutomationId(input,"Group.Name");content.Children.Add(input);var error=Text("",11,muted);content.Children.Add(error);var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};content.Children.Add(actions);var cancel=Btn("Cancel",()=>dialog.Close());cancel.IsCancel=true;actions.Children.Add(cancel);var save=Btn("Rename",()=>{if(RenameGroupTo(input.Text))dialog.Close();else error.Text=status.Text;},true);save.IsDefault=true;actions.Children.Add(save);dialog.Loaded+=(s,e)=>{input.Focus();input.SelectAll();};dialog.ShowDialog();
 }
}}
