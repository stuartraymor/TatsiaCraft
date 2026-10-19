using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Automation;
namespace TasiaCraft {
public partial class Editor {
 const double LaneHeight=64; const double TrackHeaderWidth=110;
 FrameworkElement playheadFlag;TextBlock playheadTime;
 bool TrackLocked(Clip c){return c!=null&&project.LockedTracks.Contains(c.Track);}
 void ToggleTrack(List<int> list,int track){Checkpoint();if(list.Contains(track))list.Remove(track);else list.Add(track);Refresh();}
 void DrawTimeline(){
  timeline.Children.Clear();timeline.Width=Math.Max(Math.Max(1000,ActualWidth-20),TrackHeaderWidth+(Length()+10)*zoom);timeline.Height=TracksTop+project.TrackCount*LaneHeight;
  for(int t=0;t<project.TrackCount;t++){
   int track=t;double y=TracksTop+(project.TrackCount-1-t)*LaneHeight;
   var row=new Border{Width=timeline.Width,Height=LaneHeight,Background=B("#171e22"),BorderBrush=B("#2e3940"),BorderThickness=new Thickness(0,1,0,0)};Canvas.SetTop(row,y);timeline.Children.Add(row);row.MouseLeftButtonDown+=(s,e)=>{ClearRange();Seek((e.GetPosition(timeline).X-TrackHeaderWidth)/zoom);};
   var header=new Canvas{Width=TrackHeaderWidth,Height=LaneHeight,Background=B("#29323a")};Canvas.SetTop(header,y);timeline.Children.Add(header);
   var name=new TextBlock{Text="Track "+(track+1),Foreground=Brushes.White,FontSize=11,FontWeight=FontWeights.SemiBold};Canvas.SetLeft(name,8);Canvas.SetTop(name,24);header.Children.Add(name);
   var eye=IconButton("Toggle Track "+(track+1)+" visibility","Eye",()=>ToggleTrack(project.HiddenTracks,track));eye.Width=19;eye.Height=18;eye.Padding=new Thickness(2);eye.Opacity=project.HiddenTracks.Contains(track)?.3:1;AutomationProperties.SetAutomationId(eye,"Track.Visible."+track);Canvas.SetLeft(eye,86);Canvas.SetTop(eye,1);header.Children.Add(eye);
   var snap=IconButton("Toggle Track "+(track+1)+" snapping","Snap",()=>ToggleTrack(project.SnapDisabledTracks,track));snap.Width=19;snap.Height=18;snap.Padding=new Thickness(2);snap.Opacity=project.SnapDisabledTracks.Contains(track)?.3:1;AutomationProperties.SetAutomationId(snap,"Track.Snap."+track);Canvas.SetLeft(snap,86);Canvas.SetTop(snap,22);header.Children.Add(snap);
   var locked=IconButton("Toggle Track "+(track+1)+" lock","Lock",()=>ToggleTrack(project.LockedTracks,track));locked.Width=19;locked.Height=18;locked.Padding=new Thickness(2);locked.Opacity=project.LockedTracks.Contains(track)?1:.3;AutomationProperties.SetAutomationId(locked,"Track.Lock."+track);Canvas.SetLeft(locked,86);Canvas.SetTop(locked,43);header.Children.Add(locked);
  }
  var trackAdd=IconButton("Add a track","Plus",AddTrack);trackAdd.Width=28;trackAdd.Height=24;AutomationProperties.SetAutomationId(trackAdd,"Timeline.AddTrack");Canvas.SetLeft(trackAdd,TrackHeaderWidth-36);Canvas.SetTop(trackAdd,5);timeline.Children.Add(trackAdd);
  var trayToggle=IconButton("Show or hide the Marker (Ctrl+M) tray above the timeline.",markerTrackVisible?"MarkerTrayOpen":"MarkerTrayClosed",ToggleMarkerTrack);trayToggle.Width=28;trayToggle.Height=24;AutomationProperties.SetAutomationId(trayToggle,"Timeline.MarkerTrayToggle");Canvas.SetLeft(trayToggle,TrackHeaderWidth-68);Canvas.SetTop(trayToggle,5);timeline.Children.Add(trayToggle);
  if(markerTrackVisible)DrawMarkerRow();
  int step=zoom<12?20:zoom<30?10:zoom<80?5:1;
  for(double t=0;t<(timeline.Width-TrackHeaderWidth)/zoom;t+=step){var label=new TextBlock{Text=FrameTime(t),Foreground=muted,FontSize=10};Canvas.SetLeft(label,TrackHeaderWidth+t*zoom+2);Canvas.SetTop(label,14);timeline.Children.Add(label);var tick=new Line{X1=TrackHeaderWidth+t*zoom,X2=TrackHeaderWidth+t*zoom,Y1=0,Y2=11,Stroke=B("#617079"),StrokeThickness=1};timeline.Children.Add(tick);for(int n=1;n<5;n++){double x=TrackHeaderWidth+(t+n*step/5)*zoom;timeline.Children.Add(new Line{X1=x,X2=x,Y1=0,Y2=6,Stroke=B("#3b4850"),StrokeThickness=1});}}
  if(openedGroupId!=null){var back=Btn("Main Timeline  /  "+project.Groups.First(g=>g.Id==openedGroupId).Name+"  ×",CloseGroup);back.Padding=new Thickness(4,0,4,0);Canvas.SetLeft(back,230);Canvas.SetTop(back,0);timeline.Children.Add(back);}
  var selectedIds=new HashSet<string>(SelectedClips().Select(x=>x.Id));
  var visible=project.Clips.Where(c=>openedGroupId==null||project.Groups.First(g=>g.Id==openedGroupId).ClipIds.Contains(c.Id));
  var renderedGroups=new HashSet<string>();
  foreach(var c in visible){
   var group=openedGroupId==null?ClipGroups.ForClip(project,c):null;
   if(group!=null){if(!renderedGroups.Add(group.Id))continue;var members=project.Clips.Where(x=>group.ClipIds.Contains(x.Id)).ToList();double first=members.Min(x=>x.Start),last=members.Max(x=>x.Start+x.Duration);var groupBox=new Border{Width=Math.Max(8,(last-first)*zoom),Height=LaneHeight-8,Background=B("#40505c"),BorderBrush=members.Any(x=>selectedIds.Contains(x.Id))?B("#d1bc28"):B("#53616a"),BorderThickness=new Thickness(2),ClipToBounds=true,Child=Text(group.Name+"  ["+members.Count+"]",11),ToolTip=group.Name+"\nGroup • Ctrl+U to ungroup • right-click to open"};Canvas.SetLeft(groupBox,110+first*zoom);Canvas.SetTop(groupBox,TracksTop+4+(project.TrackCount-1-members.Min(x=>x.Track))*LaneHeight);timeline.Children.Add(groupBox);WireSelectionClip(groupBox,c);continue;}

   var m=project.Media.FirstOrDefault(x=>x.Id==c.MediaId);var box=new Border{Width=Math.Max(8,c.Duration*zoom),Height=LaneHeight-8,Background=B("#354149"),Opacity=project.HiddenTracks.Contains(c.Track)?.35:1,BorderBrush=selectedIds.Contains(c.Id)?B("#d1bc28"):B("#53616a"),BorderThickness=new Thickness(selectedIds.Contains(c.Id)?1.5:1),CornerRadius=new CornerRadius(4),ToolTip=(m==null?c.Text:m.Path)+(c.Silenced?"\nAudio silenced (Shift+R to restore)":"")+(c.Stream!=null?"\n"+(c.Stream=="audio"?"Audio only":"Video only"):"")+"\n"+(TrackLocked(c)?"Track locked":"Drag to move • Shift-drag to ripple move • edges trim")};
   var content=new Grid();BitmapImageSource(content,m);var text=new TextBlock{Text=(c.Text??(m==null?"Missing media":m.Name))+(c.Silenced?"  (silenced)":""),Foreground=Brushes.White,FontSize=11,Margin=new Thickness(5,4,5,4),TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Bottom};content.Children.Add(text);box.Child=content;Canvas.SetLeft(box,TrackHeaderWidth+c.Start*zoom);Canvas.SetTop(box,TracksTop+4+(project.TrackCount-1-c.Track)*LaneHeight);timeline.Children.Add(box);WireSelectionClip(box,c);DrawEnvelope(c,TrackHeaderWidth+c.Start*zoom,TracksTop+4+(project.TrackCount-1-c.Track)*LaneHeight,box.Width,LaneHeight-8);
  }
  DrawRange();
  var marker=new Line{X1=TrackHeaderWidth+head*zoom,X2=TrackHeaderWidth+head*zoom,Y1=0,Y2=timeline.Height,Stroke=B("#d1d8dc"),StrokeThickness=1.5,IsHitTestVisible=false};timeline.Children.Add(marker);
  var flag=new StackPanel{Orientation=Orientation.Horizontal,Cursor=Cursors.SizeWE};flag.Children.Add(new Polygon{Points=new PointCollection(new[]{new Point(0,0),new Point(20,0),new Point(20,12),new Point(10,20),new Point(0,12)}),Fill=B("#549294"),Stroke=B("#99b7bb"),StrokeThickness=1});playheadTime=new TextBlock{Text=FrameTime(head),Foreground=Brushes.White,Background=B("#252d33"),FontSize=10,Margin=new Thickness(3,0,0,0)};flag.Children.Add(playheadTime);playheadFlag=flag;Canvas.SetLeft(flag,TrackHeaderWidth+head*zoom-10);Canvas.SetTop(flag,-1);timeline.Children.Add(flag);bool drag=false,inserting=false;double insertAt=0,insertX=0;flag.MouseLeftButtonDown+=(s,e)=>{Pause();drag=true;flag.CaptureMouse();e.Handled=true;if((Keyboard.Modifiers&ModifierKeys.Shift)!=0){inserting=true;insertAt=TimelineMarkers.Frame(project,head);insertX=e.GetPosition(timeline).X;Info("Ripple Insert: drag right to insert time at "+FrameTime(insertAt));return;}Seek((e.GetPosition(timeline).X-TrackHeaderWidth)/zoom);};flag.MouseMove+=(s,e)=>{if(drag&&inserting){double amount=TimelineMarkers.Frame(project,Math.Max(0,(e.GetPosition(timeline).X-insertX)/zoom));Info("Ripple Insert "+FrameTime(amount)+" at "+FrameTime(insertAt));Canvas.SetLeft(flag,TrackHeaderWidth+(insertAt+amount)*zoom-10);return;}if(drag)Seek(SnapPlayheadTime((e.GetPosition(timeline).X-TrackHeaderWidth)/zoom));};flag.MouseLeftButtonUp+=(s,e)=>{drag=false;flag.ReleaseMouseCapture();e.Handled=true;if(inserting){inserting=false;double amount=TimelineMarkers.Frame(project,Math.Max(0,(e.GetPosition(timeline).X-insertX)/zoom));if(amount>0&&RippleInsertAt(insertAt,amount))Seek(insertAt+amount);else DrawTimeline();}};
 }
 void BitmapImageSource(Grid content,Media media){System.Windows.Media.Imaging.BitmapImage thumbnail;if(media!=null&&thumbnails.TryGetValue(media.Id,out thumbnail))content.Children.Add(new Image{Source=thumbnail,Width=media.Kind=="audio"?double.NaN:68,HorizontalAlignment=media.Kind=="audio"?HorizontalAlignment.Stretch:HorizontalAlignment.Left,Stretch=Stretch.Fill,Opacity=media.Kind=="audio"?.5:.65,Margin=new Thickness(4)});}
}
}
