using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Automation;
namespace TasiaCraft {
public partial class Editor {
 TextBlock previewInfo; Dictionary<string,Button> navButtons=new Dictionary<string,Button>();
 Grid editorRoot,canvasBody,transportPanel; DockPanel editorToolbar; StackPanel canvasToolbar;
 Window detachedCanvas; Border detachedNotice; ComboBox canvasZoom; int lastCanvasZoom;
 Button selectCanvasTool,cropCanvasTool,panCanvasTool;
 void UpdateCanvasTools(){selectCanvasTool.Background=!live.CropMode&&!live.PanMode?accent:B("#252d33");cropCanvasTool.Background=live.CropMode?accent:B("#252d33");panCanvasTool.Background=live.PanMode?accent:B("#252d33");}
 void SetCanvasCrop(bool enabled){if(live==null)return;live.SetCropMode(enabled);live.SetSelection(selected);UpdateCanvasTools();}
 void SetCanvasPan(){if(live==null)return;live.SetPanMode(true);UpdateCanvasTools();}
 System.Collections.ObjectModel.ObservableCollection<string> canvasZoomOptions=new System.Collections.ObjectModel.ObservableCollection<string>(new[]{"Fit","25%","50%","75%","100%","200%","300%","800%","1200%","1600%","2000%","Detach Canvas","Project Settings…"});
 Button NavButton(string label,string glyph){var b=Btn(label,()=>{Panel(label);HighlightNavigation();});var content=new StackPanel{Orientation=Orientation.Horizontal};content.Children.Add(new TextBlock{Text=glyph,Width=28,FontSize=18,Foreground=muted});content.Children.Add(new TextBlock{Text=label,VerticalAlignment=VerticalAlignment.Center});b.Content=content;AutomationProperties.SetName(b,label);b.Background=panel;b.Margin=new Thickness(0);b.Padding=new Thickness(12,12,8,12);b.HorizontalContentAlignment=HorizontalAlignment.Left;navButtons[label]=b;return b;}
 void HighlightNavigation(){foreach(var pair in navButtons)pair.Value.Background=pair.Key==activePanel?B("#354048"):panel;}
 void Build(){
  var root=new Grid();editorRoot=root;Content=root;
  root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
  root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(54)});
  root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(245)});
  root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
  var menu=new Menu{Background=bg,Foreground=Brushes.White};root.Children.Add(menu);
  AddMenu(menu,"File",new[]{"New project","Home","Open project…","Save","Save as…","Import media…","Project settings…","Exit"},new Action[]{New,ShowHome,Open,()=>Save(false),()=>Save(true),ImportDialog,Settings,()=>Close()});
  AddMenu(menu,"Edit",new[]{"Undo","Redo","Split at playhead","Delete selected","Duplicate selected","Copy clip","Cut clip","Paste clip"},new Action[]{Undo,Redo,Split,Delete,Duplicate,Copy,Cut,Paste});
  AddMenu(menu,"Modify",new[]{"Clip properties","Reset transform"},new Action[]{()=>{propertyColumn.Width=new GridLength(260);},()=>{if(selected==null)return;Checkpoint();selected.Scale=1;selected.Rotation=0;selected.X=selected.Y=0;selected.Animate=false;Refresh();}});
  AddMenu(menu,"View",new[]{"Zoom in timeline","Zoom out timeline","Toggle Properties"},new Action[]{()=>{zoom=Math.Min(220,zoom*1.3);DrawTimeline();},()=>{zoom=Math.Max(8,zoom/1.3);DrawTimeline();},()=>{propertyColumn.Width=new GridLength(propertyColumn.Width.Value==0?260:0);}});
  AddMenu(menu,"Export",new[]{"Local MP4 video…","Cancel render"},new Action[]{Export,()=>Engine.CancelRender()});
  AddMenu(menu,"Help",new[]{"Keyboard shortcuts","About "+ProductInfo.Name},new Action[]{()=>MessageBox.Show("Space  Play / pause\nS  Split\nCtrl+C / X / V  Copy / cut / paste clip\nCtrl+Z / Y  Undo / redo\nCtrl+S  Save\nCtrl+O / I  Open / import\nCanvas: drag to move, corner handles resize, upper handle rotates.\nTimeline: drag clip edges to trim; Shift disables snapping.","Shortcuts"),()=>MessageBox.Show(ProductInfo.DisplayName+"\nIndependent Windows video editor with live preview.\nSee the included report and dependency notices.","About")});
  var toolbar=new DockPanel{Background=bg,LastChildFill=true,Margin=new Thickness(0,0,0,2)};var toolbarFrame=new Border{BorderBrush=accent,BorderThickness=new Thickness(0,0,0,1),Child=toolbar};Grid.SetRow(toolbarFrame,1);root.Children.Add(toolbarFrame);
  var export=Btn("Export  ↗",Export,true);DockPanel.SetDock(export,Dock.Right);toolbar.Children.Add(export);
  var settings=Btn("Project settings",Settings);DockPanel.SetDock(settings,Dock.Right);toolbar.Children.Add(settings);
  var home=Btn("⌂",ShowHome);AutomationProperties.SetAutomationId(home,"Editor.Home");DockPanel.SetDock(home,Dock.Left);toolbar.Children.Add(home);
  record=Btn("●  Record",Record);record.Background=B("#354048");DockPanel.SetDock(record,Dock.Left);toolbar.Children.Add(record);
  var canvasTools=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
  selectCanvasTool=IconButton("Select canvas","Select",()=>SetCanvasCrop(false));selectCanvasTool.Background=accent;canvasTools.Children.Add(selectCanvasTool);
  panCanvasTool=IconButton("Pan canvas view","Pan",SetCanvasPan);canvasTools.Children.Add(panCanvasTool);
  cropCanvasTool=IconButton("Crop canvas media","Crop",()=>SetCanvasCrop(true));canvasTools.Children.Add(cropCanvasTool);
  canvasZoom=new ComboBox{Width=134,Margin=new Thickness(8,3,8,3),VerticalContentAlignment=VerticalAlignment.Center,ItemsSource=canvasZoomOptions,SelectedIndex=0};AutomationProperties.SetAutomationId(canvasZoom,"CanvasZoom");canvasZoom.SelectionChanged+=(s,e)=>{int index=canvasZoom.SelectedIndex;if(index<0)return;if(index>=11){canvasZoom.SelectedIndex=lastCanvasZoom;if(index==11)ToggleCanvasDock();else Settings();return;}lastCanvasZoom=index;if(live!=null&&index==0)live.ResetViewOffset();if(live!=null)live.SetViewZoom(index==0?0:double.Parse(((string)canvasZoom.SelectedItem).TrimEnd('%'),System.Globalization.CultureInfo.InvariantCulture)/100);};canvasZoom.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,new MouseButtonEventHandler((s,e)=>{var item=ItemsControl.ContainerFromElement(canvasZoom,e.OriginalSource as DependencyObject) as ComboBoxItem;if(item!=null&&string.Equals(item.Content as string,"Fit",StringComparison.Ordinal)&&live!=null)live.ResetViewOffset();}),true);canvasZoom.PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Enter&&canvasZoom.IsDropDownOpen&&canvasZoom.SelectedIndex==0&&live!=null)live.ResetViewOffset();};canvasTools.Children.Add(canvasZoom);
  title=Text("Untitled project",12,muted);canvasTools.Children.Add(title);toolbar.Children.Add(canvasTools);
  editorToolbar=toolbar;canvasToolbar=canvasTools;
  var body=new Grid();canvasBody=body;Grid.SetRow(body,2);root.Children.Add(body);
  body.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(132)});
  body.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(282)});
  body.ColumnDefinitions.Add(new ColumnDefinition());propertyColumn=new ColumnDefinition{Width=new GridLength(0)};body.ColumnDefinitions.Add(propertyColumn);
  var nav=new StackPanel{Background=panel};body.Children.Add(nav);
  string[] labels={"Media","Library","Annotations","Visual effects","Transitions","Animations","Audio effects","Captions"};string[] glyphs={"▤","▥","▣","✦","◩","➜","♫","≡"};for(int i=0;i<labels.Length;i++)nav.Children.Add(NavButton(labels[i],glyphs[i]));
  library=new StackPanel{Background=B("#20282d")};var libscroll=new ScrollViewer{Content=library,Background=B("#20282d"),VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(libscroll,1);body.Children.Add(libscroll);
  var separator=new Border{Width=1,Background=B("#101719"),HorizontalAlignment=HorizontalAlignment.Right};Grid.SetColumn(separator,1);body.Children.Add(separator);
  live=new LivePreview();Grid.SetColumn(live,2);body.Children.Add(live);
  live.Status+=Info;
  live.PositionChanged+=t=>{head=t;playing=live.IsPlaying;if(play!=null)UpdatePlayUi();if(clock!=null&&seeker!=null)UpdateClock();};
  live.Selecting+=c=>{selected=c;Props();DrawTimeline();};live.BeforeEdit+=Checkpoint;
  live.AfterEdit+=()=>{Props();DrawTimeline();UpdateClock();};
  properties=new StackPanel{Background=panel};var ps=new ScrollViewer{Content=properties,Background=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(ps,3);body.Children.Add(ps);
  var transport=new Grid{Background=B("#252d33")};transportPanel=transport;Grid.SetRow(transport,3);root.Children.Add(transport);
  transport.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(414)});transport.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});transport.ColumnDefinitions.Add(new ColumnDefinition());transport.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
  previewInfo=Text("Live preview",12,muted);transport.Children.Add(previewInfo);
  var controls=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(controls,1);transport.Children.Add(controls);
  controls.Children.Add(Btn("|◀",()=>Seek(0)));controls.Children.Add(Btn("−1f",()=>Seek(head-1.0/project.Fps)));play=Btn("▶  Play",TogglePlay);play.Content="▶";play.Width=42;play.Height=42;play.FontSize=20;play.Padding=new Thickness(2);AutomationProperties.SetName(play,"▶  Play");controls.Children.Add(play);controls.Children.Add(Btn("+1f",()=>Seek(head+1.0/project.Fps)));
  seeker=new Slider{IsMoveToPointEnabled=true,Minimum=0,Maximum=1,Margin=new Thickness(15,0,15,0),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(seeker,2);transport.Children.Add(seeker);AutomationProperties.SetAutomationId(seeker,"TimelineSeek");seeker.ValueChanged+=(s,e)=>{if(!updatingSeek)Seek(seeker.Value);};
  var tright=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(tright,3);transport.Children.Add(tright);clock=Text("00:00;00 / 00:00;00",12,muted);AutomationProperties.SetAutomationId(clock,"TransportTime");tright.Children.Add(clock);tright.Children.Add(Btn("Properties",()=>{propertyColumn.Width=new GridLength(propertyColumn.Width.Value==0?260:0);}));
  var lower=new Grid{Background=B("#171e22")};Grid.SetRow(lower,4);root.Children.Add(lower);lower.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});lower.RowDefinitions.Add(new RowDefinition());
  var tools=new StackPanel{Orientation=Orientation.Horizontal,Background=B("#252d33")};lower.Children.Add(tools);
  tools.Children.Add(IconButton("↶ Undo","Undo",Undo));tools.Children.Add(IconButton("↷ Redo","Redo",Redo));tools.Children.Add(IconButton("Cut clip","Cut",Cut));tools.Children.Add(IconButton("Copy clip","Copy",Copy));tools.Children.Add(IconButton("Paste clip","Paste",Paste));tools.Children.Add(IconButton("Split  [S]","Split",Split));tools.Children.Add(IconButton("Export frame","Frame",ExportFrame));tools.Children.Add(IconButton("Timeline zoom","Zoom",()=>{zoom=18;DrawTimeline();}));
  tools.Children.Add(Btn("−",()=>{zoom=Math.Max(8,zoom/1.3);DrawTimeline();}));var timelineZoom=new Slider{Minimum=8,Maximum=220,Value=zoom,Width=126,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,8,0)};AutomationProperties.SetAutomationId(timelineZoom,"TimelineZoom");timelineZoom.ValueChanged+=(s,e)=>{zoom=timelineZoom.Value;DrawTimeline();};tools.Children.Add(timelineZoom);tools.Children.Add(Btn("+",()=>{zoom=Math.Min(220,zoom*1.3);DrawTimeline();}));var trackAdd=Btn("+ Track",()=>{Checkpoint();project.TrackCount++;Refresh();});trackAdd.Padding=new Thickness(7,3,7,3);tools.Children.Add(trackAdd);
  timeline=new ProjectCanvas{Background=B("#171e22"),Height=190,AllowDrop=true};AutomationProperties.SetAutomationId(timeline,"TimelineCanvas");timeline.Drop+=TimelineDrop;var scroll=new ScrollViewer{HorizontalContentAlignment=HorizontalAlignment.Left,VerticalContentAlignment=VerticalAlignment.Top,Content=timeline,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetRow(scroll,1);lower.Children.Add(scroll);timeline.MouseLeftButtonDown+=(s,e)=>{if(e.OriginalSource==timeline)Seek((e.GetPosition(timeline).X-110)/zoom);};
  status=Text("Ready • Import media to begin",11,muted);Grid.SetRow(status,5);root.Children.Add(status);Panel("Media");HighlightNavigation();
 }

 void ToggleCanvasDock(){
  if(detachedCanvas!=null){detachedCanvas.Close();return;}
  var detached=new Window{Title=Title+" — Canvas",Owner=this,Width=1100,Height=680,MinWidth=1000,MinHeight=400,Background=bg,Foreground=Brushes.White,FontFamily=FontFamily,FontSize=FontSize,Resources=Resources,WindowStartupLocation=WindowStartupLocation.CenterOwner};
  var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=new GridLength(54)});detached.Content=layout;
  var bar=new DockPanel{Background=bg,LastChildFill=true};Grid.SetRow(bar,0);layout.Children.Add(bar);
  var attach=Btn("Attach Canvas",()=>detached.Close());DockPanel.SetDock(attach,Dock.Left);bar.Children.Add(attach);
  var export=Btn("Export  ↗",Export,true);DockPanel.SetDock(export,Dock.Right);bar.Children.Add(export);
  var noticePanel=new StackPanel{VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};noticePanel.Children.Add(Text("Canvas is detached",16,muted));noticePanel.Children.Add(Btn("Attach Canvas",()=>detached.Close()));
  detachedNotice=new Border{Background=bg,Child=noticePanel};Grid.SetColumn(detachedNotice,2);
  editorToolbar.Children.Remove(canvasToolbar);canvasBody.Children.Remove(live);editorRoot.Children.Remove(transportPanel);
  bar.Children.Add(canvasToolbar);Grid.SetColumn(live,0);Grid.SetRow(live,1);layout.Children.Add(live);Grid.SetRow(transportPanel,2);layout.Children.Add(transportPanel);
  canvasBody.Children.Add(detachedNotice);editorRoot.RowDefinitions[3].Height=new GridLength(0);detachedCanvas=detached;canvasZoomOptions[11]="Attach Canvas";
  detached.PreviewKeyDown+=Keys;
  detached.Closed+=(s,e)=>{bar.Children.Remove(canvasToolbar);layout.Children.Remove(live);layout.Children.Remove(transportPanel);canvasBody.Children.Remove(detachedNotice);detachedNotice=null;editorToolbar.Children.Add(canvasToolbar);Grid.SetColumn(live,2);Grid.SetRow(live,0);canvasBody.Children.Add(live);Grid.SetRow(transportPanel,3);editorRoot.Children.Add(transportPanel);editorRoot.RowDefinitions[3].Height=new GridLength(54);detachedCanvas=null;canvasZoomOptions[11]="Detach Canvas";live.SetViewZoom(live.ViewZoom);};
  try{detached.Show();}catch{detached.Close();throw;}
 }
 string mediaFilter="All media",mediaSort="Type"; bool mediaTiles=true;
 void MediaPanel(){
  library.Children.Add(Text("▤     Media Bin",16));
  var filters=new Grid{Margin=new Thickness(8,2,8,6)};filters.ColumnDefinitions.Add(new ColumnDefinition());filters.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(93)});
  var filter=new ComboBox{ItemsSource=new[]{"All media","Video","Audio","Images"},SelectedItem=mediaFilter,Padding=new Thickness(4),Margin=new Thickness(0,0,5,0)};AutomationProperties.SetAutomationId(filter,"MediaFilter");filters.Children.Add(filter);
  var sort=new ComboBox{ItemsSource=new[]{"Type","Name"},SelectedItem=mediaSort,Padding=new Thickness(4)};AutomationProperties.SetAutomationId(sort,"MediaSort");Grid.SetColumn(sort,1);filters.Children.Add(sort);library.Children.Add(filters);
  var actions=new StackPanel{Orientation=Orientation.Horizontal};var import=Btn("+  Import media…",ImportDialog);import.Padding=new Thickness(9,5,9,5);actions.Children.Add(import);var toggle=Btn(mediaTiles?"☷ List":"▦ Tiles",()=>{mediaTiles=!mediaTiles;Panel("Media");});toggle.Padding=new Thickness(8,5,8,5);actions.Children.Add(toggle);library.Children.Add(actions);
  bin=new ListBox{Background=B("#20282d"),Foreground=Brushes.White,BorderThickness=new Thickness(0),MinHeight=170,Margin=new Thickness(3)};AutomationProperties.SetAutomationId(bin,"MediaBin");library.Children.Add(bin);
  Action populate=()=>{bin.Items.Clear();var items=project.Media.Where(m=>mediaFilter=="All media"||mediaFilter=="Video"&&m.Kind=="video"||mediaFilter=="Audio"&&m.Kind=="audio"||mediaFilter=="Images"&&m.Kind=="image");items=mediaSort=="Name"?items.OrderBy(m=>m.Name):items.OrderBy(m=>m.Kind).ThenBy(m=>m.Name);foreach(var m in items){var card=new StackPanel{MaxWidth=230};BitmapImage thumb;if(mediaTiles&&thumbnails.TryGetValue(m.Id,out thumb))card.Children.Add(new Image{Source=thumb,Width=172,Height=m.Kind=="audio"?38:97,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left});card.Children.Add(Text(m.Name,12));card.Children.Add(Text((m.Kind=="audio"?"♫ Audio":m.Kind=="image"?"Image":"Video")+"  •  "+m.Duration.ToString("0.0")+" s",10,muted));var item=new ListBoxItem{Content=card,Tag=m,Padding=new Thickness(4),Margin=new Thickness(3),BorderThickness=new Thickness(1)};AutomationProperties.SetName(item,m.Name);bin.Items.Add(item);}if(bin.Items.Count==0)bin.Items.Add(new ListBoxItem{Content=Text("Import video, audio or images to start editing.",12,muted),IsEnabled=false});};
  filter.SelectionChanged+=(s,e)=>{mediaFilter=(string)filter.SelectedItem;populate();};sort.SelectionChanged+=(s,e)=>{mediaSort=(string)sort.SelectedItem;populate();};populate();
  bin.MouseDoubleClick+=(s,e)=>{var item=bin.SelectedItem as ListBoxItem;if(item!=null&&item.Tag is Media)Add((Media)item.Tag,head,0);};bin.PreviewMouseMove+=(s,e)=>{if(e.LeftButton==MouseButtonState.Pressed){var item=bin.SelectedItem as ListBoxItem;if(item!=null&&item.Tag is Media)DragDrop.DoDragDrop(bin,new DataObject("TasiaMedia",((Media)item.Tag).Id),DragDropEffects.Copy);}};
  var relink=Btn("Relink selected media…",Relink);relink.Padding=new Thickness(8,5,8,5);library.Children.Add(relink);
 }
 void Props(){
  if(propertyColumn!=null)propertyColumn.Width=new GridLength(selected==null?0:260);
  properties.Children.Clear();properties.Children.Add(Text("Properties",16));if(selected==null)return;
  var c=selected;var source=project.Media.FirstOrDefault(m=>m.Id==c.MediaId);properties.Children.Add(Text(c.Text!=null?"Text annotation":source==null?"Missing media":source.Name,12,muted));
  var fields=new Dictionary<string,TextBox>();bool ready=false,checkpointed=false;Action<bool> commit=null;
  Action<string,double,string> row=(key,value,id)=>{var grid=new Grid{Margin=new Thickness(10,3,10,3)};grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(94)});grid.Children.Add(new TextBlock{Text=key,Foreground=muted,VerticalAlignment=VerticalAlignment.Center});var box=new TextBox{Text=value.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture),Padding=new Thickness(5,3,5,3),Background=B("#1b2328"),Foreground=Brushes.White,BorderBrush=B("#46525b"),BorderThickness=new Thickness(1),VerticalContentAlignment=VerticalAlignment.Center};Grid.SetColumn(box,1);grid.Children.Add(box);AutomationProperties.SetAutomationId(box,id);fields[key]=box;box.GotKeyboardFocus+=(s,e)=>checkpointed=false;box.TextChanged+=(s,e)=>{if(ready)commit(false);};properties.Children.Add(grid);};
  properties.Children.Add(Text("TRANSFORM",10,muted));row("Scale (%)",c.Scale*100,"Prop.Scale");row("Opacity (%)",c.Opacity*100,"Prop.Opacity");row("Rotation (°)",-c.Rotation,"Prop.Rotation");if(c.Text!=null)fields["Rotation (°)"].IsEnabled=false;
  row("Position X",c.X,"Prop.X");row("Position Y",-c.Y,"Prop.Y");
  bool cropAvailable=c.Text==null&&source!=null&&source.Kind!="audio"&&source.Width>0&&source.Height>0;
  if(cropAvailable){properties.Children.Add(Text("CROP (%)",10,muted));row("Crop left",c.CropLeft*100,"Prop.CropLeft");row("Crop top",c.CropTop*100,"Prop.CropTop");row("Crop right",c.CropRight*100,"Prop.CropRight");row("Crop bottom",c.CropBottom*100,"Prop.CropBottom");properties.Children.Add(Btn("Reset crop",()=>{if(TrackLocked(c)){Info("Track is locked.");return;}if(c.CropLeft+c.CropTop+c.CropRight+c.CropBottom==0)return;Checkpoint();c.CropLeft=c.CropTop=c.CropRight=c.CropBottom=0;live.Refresh();Props();}));}
  var native=new CheckBox{Content="Use native media size",IsChecked=c.NativeSize,Margin=new Thickness(10,6,10,6),Foreground=muted,IsEnabled=c.Text==null};AutomationProperties.SetAutomationId(native,"Prop.NativeSize");properties.Children.Add(native);
  if(source!=null&&source.Width>0)properties.Children.Add(Text("Source  "+source.Width+" × "+source.Height,11,muted));
  properties.Children.Add(Text("TIMING",10,muted));row("Start (s)",c.Start,"Prop.Start");row("Source in (s)",c.In,"Prop.In");row("Duration (s)",c.Duration,"Prop.Duration");row("Track",c.Track+1,"Prop.Track");row("Playback speed",c.Speed,"Prop.Speed");
  properties.Children.Add(Text("AUDIO & FADES",10,muted));row("Gain",c.Volume,"Prop.Volume");row("Fade in (s)",c.FadeIn,"Prop.FadeIn");row("Fade out (s)",c.FadeOut,"Prop.FadeOut");
  properties.Children.Add(Text("Video: "+(c.Effect??"none")+" • Audio: "+(c.AudioEffect??"none"),10,muted));
  TextBox text=null;if(c.Text!=null){text=new TextBox{Text=c.Text,AcceptsReturn=true,MinHeight=65,Margin=new Thickness(10),Background=B("#1b2328"),Foreground=Brushes.White,Padding=new Thickness(5)};AutomationProperties.SetAutomationId(text,"Prop.Text");properties.Children.Add(text);text.GotKeyboardFocus+=(s,e)=>checkpointed=false;text.TextChanged+=(s,e)=>{if(ready)commit(false);};}
  commit=showError=>{try{if(TrackLocked(c))throw new Exception("Track is locked.");var v=new Dictionary<string,double>();foreach(var pair in fields){double value;if(!double.TryParse(pair.Value.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out value))throw new Exception("Enter a numeric value.");v[pair.Key]=value;}var candidate=json.Deserialize<Clip>(json.Serialize(c));candidate.Scale=v["Scale (%)"]/100;candidate.Opacity=v["Opacity (%)"]/100;candidate.Rotation=-v["Rotation (°)"];candidate.X=v["Position X"];candidate.Y=-v["Position Y"];candidate.NativeSize=native.IsChecked==true;if(cropAvailable){candidate.CropLeft=v["Crop left"]/100;candidate.CropTop=v["Crop top"]/100;candidate.CropRight=v["Crop right"]/100;candidate.CropBottom=v["Crop bottom"]/100;}candidate.Start=v["Start (s)"];candidate.In=v["Source in (s)"];candidate.Duration=v["Duration (s)"];candidate.Track=(int)v["Track"]-1;candidate.Speed=v["Playback speed"];candidate.Volume=v["Gain"];candidate.FadeIn=v["Fade in (s)"];candidate.FadeOut=v["Fade out (s)"];if(text!=null)candidate.Text=text.Text;
   int index=project.Clips.IndexOf(c);if(index<0)return;project.Clips[index]=candidate;try{Engine.Validate(project,false);}finally{project.Clips[index]=c;}
   if(json.Serialize(candidate)==json.Serialize(c))return;if(!checkpointed){Checkpoint();checkpointed=true;}c.Scale=candidate.Scale;c.Opacity=candidate.Opacity;c.Rotation=candidate.Rotation;c.X=candidate.X;c.Y=candidate.Y;c.NativeSize=candidate.NativeSize;c.CropLeft=candidate.CropLeft;c.CropTop=candidate.CropTop;c.CropRight=candidate.CropRight;c.CropBottom=candidate.CropBottom;c.Start=candidate.Start;c.In=candidate.In;c.Duration=candidate.Duration;c.Track=candidate.Track;c.Speed=candidate.Speed;c.Volume=candidate.Volume;c.FadeIn=candidate.FadeIn;c.FadeOut=candidate.FadeOut;c.Text=candidate.Text;
   live.Refresh();DrawTimeline();UpdateClock();Info("Properties updated in live preview");
  }catch(Exception e){if(showError)MessageBox.Show(e.Message,"Properties");else Info(e.Message);}};
  native.Checked+=(s,e)=>{if(ready){checkpointed=false;commit(false);}};native.Unchecked+=(s,e)=>{if(ready){checkpointed=false;commit(false);}};
  properties.Children.Add(Btn("Apply changes",()=>commit(true),true));ready=true;
 }
}
}
