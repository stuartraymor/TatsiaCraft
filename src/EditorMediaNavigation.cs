using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
namespace TasiaCraft { public partial class Editor {
 List<Media> BinMediaSelection(){return bin==null?new List<Media>():bin.SelectedItems.OfType<ListBoxItem>().Select(i=>i.Tag as Media).Where(m=>m!=null&&project.Media.Contains(m)).Distinct().ToList();}
 void SelectBinOnTimeline(){
  if(busy)return;var matches=MediaBinOperations.TimelineReferences(project,BinMediaSelection());if(matches.Count==0){Info("No timeline references for the selected media.");return;}
  var units=ClipGroups.Expand(project,matches);openedGroupId=null;SetSelected(units);SyncSelection();Props();DrawTimeline();
  var first=units.OrderBy(c=>c.Start).ThenBy(c=>c.Track).First();var group=ClipGroups.ForClip(project,first);var members=group==null?new List<Clip>{first}:project.Clips.Where(c=>group.ClipIds.Contains(c.Id)).ToList();
  timeline.UpdateLayout();timeline.BringIntoView(new Rect(TrackHeaderWidth+members.Min(c=>c.Start)*zoom,TracksTop+4+(project.TrackCount-1-members.Min(c=>c.Track))*LaneHeight,Math.Max(8,Math.Min(80,first.Duration*zoom)),LaneHeight-8));
  Info(matches.Count+" timeline reference(s); "+units.Count+" clip(s) selected including group members.");
 }
 void SelectTimelineInBin(){
  if(busy)return;var media=MediaBinOperations.SourceReferences(project,SelectedClips());if(media.Count==0){Info("Selected clips have no Media Bin source.");return;}
  var ids=new HashSet<string>(media.Select(m=>m.Id));mediaFilter="All media";Panel("Media");HighlightNavigation();bin.SelectedItems.Clear();foreach(var item in bin.Items.OfType<ListBoxItem>())if(item.Tag is Media&&ids.Contains(((Media)item.Tag).Id))item.IsSelected=true;
  if(bin.SelectedItem!=null)bin.ScrollIntoView(bin.SelectedItem);Info(media.Count+" source media item(s) selected in Media Bin.");
 }
}}
