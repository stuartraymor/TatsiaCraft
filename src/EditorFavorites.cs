using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Automation;
namespace TasiaCraft {
public partial class Editor {
 // Favorites: star any preset tile to collect it in the Favorites tool, filtered by Type. Favorites belong to the
 // user, not the project, and are stored in %LOCALAPPDATA%\TasiaCraft\favorites.json. See docs/BEHAVIORS-EVIDENCE.md.
 class PresetEntry { public string Panel,Label,Picture; public Action Apply; public string Key{get{return Panel+"/"+Label;}} }
 static readonly string[] PresetPanels={"Annotations","Visual Effects","Transitions","Animations","Behaviors","Audio Effects","Captions"};
 static readonly string[] ClipPresetPanels={"Visual Effects","Transitions","Animations","Behaviors","Audio Effects"};
 List<PresetEntry> presetCollector; string favoritesPath; List<string> favorites; string favoritesType="All";
 string FavoritesPath{get{return favoritesPath??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TasiaCraft","favorites.json");}}
 List<string> Favorites(){if(favorites!=null)return favorites;try{favorites=File.Exists(FavoritesPath)?json.Deserialize<List<string>>(File.ReadAllText(FavoritesPath)).Where(k=>!string.IsNullOrWhiteSpace(k)).Distinct().ToList():new List<string>();}catch{favorites=new List<string>();}return favorites;}
 void SaveFavorites(){try{Directory.CreateDirectory(Path.GetDirectoryName(FavoritesPath));string temp=FavoritesPath+".tmp";File.WriteAllText(temp,json.Serialize(Favorites()));if(File.Exists(FavoritesPath))File.Replace(temp,FavoritesPath,null);else File.Move(temp,FavoritesPath);}catch(Exception e){Info("Favorites could not be saved: "+e.Message);}}
 bool IsFavorite(string key){return Favorites().Contains(key);}
 void ToggleFavorite(string key){if(IsFavorite(key))Favorites().Remove(key);else Favorites().Add(key);SaveFavorites();Info(IsFavorite(key)?"Added to Favorites":"Removed from Favorites");Panel(activePanel);}
 List<PresetEntry> CollectPresets(string panel){var saved=library;library=new StackPanel();presetCollector=new List<PresetEntry>();try{BuildPalette(panel);return presetCollector;}finally{presetCollector=null;library=saved;}}
 void AddPresetTile(UniformGrid tiles,string panel,string label,string picture,Action apply){
  var entry=new PresetEntry{Panel=panel,Label=label,Picture=picture,Apply=apply};if(presetCollector!=null){presetCollector.Add(entry);return;}tiles.Children.Add(FavoriteTile(entry));
 }
 FrameworkElement FavoriteTile(PresetEntry entry){
  var grid=new Grid();var tile=PaletteTile(entry.Label,entry.Picture,entry.Apply);grid.Children.Add(tile);bool starred=IsFavorite(entry.Key);
  var star=new Button{Content=new TextBlock{Text=starred?"★":"☆",FontSize=14,Foreground=starred?B("#f2c94c"):Brushes.White},Width=22,Height=22,Padding=new Thickness(0),Margin=new Thickness(0,6,6,0),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Cursor=System.Windows.Input.Cursors.Hand,ToolTip=starred?"Remove from Favorites":"Click to save to Favorites."};
  star.Template=PillTemplate();AutomationProperties.SetAutomationId(star,"Favorite."+entry.Key.Replace(" ",""));AutomationProperties.SetName(star,starred?"Remove "+entry.Label+" from Favorites":"Add "+entry.Label+" to Favorites");star.Click+=(s,e)=>ToggleFavorite(entry.Key);grid.Children.Add(star);
  var menu=new ContextMenu();if(ClipPresetPanels.Contains(entry.Panel)){var add=new MenuItem{Header="Add to Selected Media"};add.Click+=(s,e)=>entry.Apply();menu.Items.Add(add);}var fav=new MenuItem{Header=starred?"Remove from Favorites":"Add to Favorites"};fav.Click+=(s,e)=>ToggleFavorite(entry.Key);menu.Items.Add(fav);tile.ContextMenu=menu;
  return grid;
 }
 void FavoritesPanel(){
  PaletteHeading("Favorites",null);
  var all=PresetPanels.SelectMany(CollectPresets).Where(p=>IsFavorite(p.Key)).ToList();var types=new[]{"All"}.Concat(PresetPanels.Where(t=>all.Any(p=>p.Panel==t))).ToList();if(!types.Contains(favoritesType))favoritesType="All";
  var row=new Grid{Margin=new Thickness(10,2,10,8)};row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});row.ColumnDefinitions.Add(new ColumnDefinition());row.Children.Add(new TextBlock{Text="Type",Foreground=muted,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,10,0)});
  var type=new ComboBox{ItemsSource=types,SelectedItem=favoritesType,IsEnabled=all.Count>0};AutomationProperties.SetAutomationId(type,"Favorites.Type");Grid.SetColumn(type,1);row.Children.Add(type);library.Children.Add(row);type.SelectionChanged+=(s,e)=>{favoritesType=(string)type.SelectedItem??"All";Panel("Favorites");};
  var shown=all.Where(p=>favoritesType=="All"||p.Panel==favoritesType).ToList();
  if(shown.Count==0){var empty=new StackPanel{Margin=new Thickness(20,40,20,20)};empty.Children.Add(new Border{Width=78,Height=78,CornerRadius=new CornerRadius(8),Background=B("#c9d1d6"),Child=new TextBlock{Text="★",FontSize=40,Foreground=B("#20282d"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}});var hint=Text("Click the star icon to save callouts, effects, or other video enhancements to Favorites.",12,muted);hint.TextAlignment=TextAlignment.Center;AutomationProperties.SetAutomationId(hint,"Favorites.Empty");empty.Children.Add(hint);library.Children.Add(empty);return;}
  var tiles=new UniformGrid{Columns=3,Margin=new Thickness(4,4,4,10)};foreach(var p in shown)tiles.Children.Add(FavoriteTile(p));library.Children.Add(tiles);
 }
 // Behaviors applied to the selected visual clips; the reference confirms with a "Behavior Added" notice.
 bool AddBehavior(string name){
  if(busy||!Behaviors.Catalog.Contains(name))return false;var clips=SelectedClips().Where(c=>Behaviors.Applies(c,project.Media.FirstOrDefault(m=>m.Id==c.MediaId))).ToList();
  if(clips.Count==0){Info("Select a visual clip on the timeline to add a behavior.");return false;}if(clips.Any(TrackLocked)){Info("Track is locked.");return false;}if(clips.Any(c=>Behaviors.Of(c).Count>=Behaviors.MaxPerClip)){Info("A clip can have at most "+Behaviors.MaxPerClip+" behaviors.");return false;}
  Checkpoint();foreach(var c in clips){double phase=Math.Min(1,c.Duration/3);Behaviors.Of(c).Add(new Behavior{Name=name,In=phase,Out=phase});}Refresh();Info("Behavior Added");return true;
 }
 bool EditBehavior(Clip c,int index,Action<Behavior> change){
  if(busy||TrackLocked(c)||index<0||index>=Behaviors.Of(c).Count)return false;var candidate=json.Deserialize<Project>(json.Serialize(project));var target=candidate.Clips.First(x=>x.Id==c.Id);change(Behaviors.Of(target)[index]);try{Engine.Validate(candidate,false);}catch(Exception e){Info(e.Message);return false;}
  if(json.Serialize(Behaviors.Of(target)[index])==json.Serialize(Behaviors.Of(c)[index]))return true;Checkpoint();change(Behaviors.Of(c)[index]);live.Refresh();DrawTimeline();return true;
 }
 void RemoveBehavior(Clip c,int index){if(busy||TrackLocked(c)||index<0||index>=Behaviors.Of(c).Count)return;Checkpoint();Behaviors.Of(c).RemoveAt(index);Refresh();}
 void BehaviorProps(Clip c){
  if(Behaviors.Of(c).Count==0)return;properties.Children.Add(Text("BEHAVIORS",10,muted));
  for(int i=0;i<Behaviors.Of(c).Count;i++){
   int index=i;var b=Behaviors.Of(c)[i];var header=new DockPanel{Margin=new Thickness(10,4,10,2)};var enabled=new CheckBox{IsChecked=b.Enabled,VerticalAlignment=VerticalAlignment.Center,ToolTip="Turn this behavior on or off"};AutomationProperties.SetAutomationId(enabled,"Behavior.Enabled."+i);DockPanel.SetDock(enabled,Dock.Right);header.Children.Add(enabled);header.Children.Add(new TextBlock{Text=b.Name,Foreground=Brushes.White,FontWeight=FontWeights.SemiBold});properties.Children.Add(header);
   enabled.Click+=(s,e)=>EditBehavior(c,index,x=>x.Enabled=enabled.IsChecked==true);
   foreach(var phase in new[]{"In","Out"}){string which=phase;var row=new Grid{Margin=new Thickness(10,2,10,2)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(94)});row.Children.Add(new TextBlock{Text=which+" duration (s)",Foreground=muted,VerticalAlignment=VerticalAlignment.Center});var box=new TextBox{Text=(which=="In"?b.In:b.Out).ToString("0.###",System.Globalization.CultureInfo.InvariantCulture),Padding=new Thickness(5,3,5,3),Background=B("#1b2328"),Foreground=Brushes.White,BorderBrush=B("#46525b")};AutomationProperties.SetAutomationId(box,"Behavior."+which+"."+i);Grid.SetColumn(box,1);row.Children.Add(box);properties.Children.Add(row);
    Action commit=()=>{double v;if(!double.TryParse(box.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)){Info("Enter a number of seconds.");return;}EditBehavior(c,index,x=>{if(which=="In")x.In=v;else x.Out=v;});};box.LostKeyboardFocus+=(s,e)=>commit();box.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter){commit();e.Handled=true;}};}
   properties.Children.Add(Btn("Remove "+b.Name,()=>RemoveBehavior(c,index)));
  }
 }
}
}
