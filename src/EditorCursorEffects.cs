using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Automation;
namespace TasiaCraft {
public partial class Editor {
 // Cursor Effects tool with the reference Cursor / Left Click / Right Click tabs. Effects apply to clips whose
 // recording has cursor data (TatsiaCraft 0.2.2+ screen recordings). See docs/CURSOR-EFFECTS-EVIDENCE.md.
 string cursorTab="Cursor";
 static readonly string[] CursorColors={"Default","Red","Yellow","Blue","Green","White","Black"};
 static string CursorColorHex(string name){return name=="Red"?"#E53935":name=="Yellow"?"#FFE14D":name=="Blue"?"#2F6BFF":name=="Green"?"#3CE36B":name=="White"?"#FFFFFF":name=="Black"?"#000000":null;}
 void CursorEffectsPalette(UniformGrid tiles,Action<string,string,Action> tile){
  if(presetCollector==null){var tabs=new UniformGrid{Columns=3,Margin=new Thickness(6,2,6,6)};foreach(var name in new[]{"Cursor","Left Click","Right Click"}){string tab=name;var b=Btn(tab,()=>{cursorTab=tab;Panel("Cursor Effects");});b.Background=cursorTab==tab?B("#354048"):B("#252d33");b.BorderBrush=accent;b.BorderThickness=new Thickness(0,0,0,cursorTab==tab?2:0);AutomationProperties.SetAutomationId(b,"CursorTab."+tab.Replace(" ",""));tabs.Children.Add(b);}library.Children.Insert(Math.Max(0,library.Children.IndexOf(tiles)),tabs);}
  var names=presetCollector!=null?CursorEffects.Catalog:cursorTab=="Cursor"?CursorEffects.CursorCatalog:CursorEffects.ClickCatalog.Where(n=>n.StartsWith(cursorTab+" "));
  foreach(string effect in names){string chosen=effect;tile(chosen,"Cursor:"+chosen,()=>AddCursorEffect(chosen));}
 }
 bool AddCursorEffect(string name){
  if(busy||!CursorEffects.Catalog.Contains(name))return false;var clips=SelectedClips().Where(c=>CursorEffects.HasCursorData(project.Media.FirstOrDefault(m=>m.Id==c.MediaId))&&c.Stream!="audio").ToList();
  if(clips.Count==0){Info("Select a screen recording with cursor data. Recordings made with TatsiaCraft 0.2.2 or later record the cursor separately.");return false;}if(clips.Any(TrackLocked)){Info("Track is locked.");return false;}
  var targets=clips.Where(c=>!CursorEffects.Of(c).Any(e=>e.Name==name)).ToList();if(targets.Count==0){Info(name+" is already applied.");return false;}if(targets.Any(c=>CursorEffects.Of(c).Count>=16)){Info("A clip can have at most 16 cursor effects.");return false;}
  Checkpoint();foreach(var c in targets)CursorEffects.Of(c).Add(new CursorEffect{Name=name,Size=CursorEffects.DefaultSize(name)});Refresh();Info("Cursor effect added");return true;
 }
 bool EditCursor(Clip c,Action<Clip> change){
  if(busy||TrackLocked(c))return false;var candidate=json.Deserialize<Project>(json.Serialize(project));var target=candidate.Clips.First(x=>x.Id==c.Id);change(target);try{Engine.Validate(candidate,false);}catch(Exception e){Info(e.Message);return false;}
  if(json.Serialize(target)==json.Serialize(c))return true;Checkpoint();change(c);live.Refresh();DrawTimeline();return true;
 }
 void CursorProps(Clip c){
  var m=project.Media.FirstOrDefault(x=>x.Id==c.MediaId);if(!CursorEffects.HasCursorData(m)||c.Stream=="audio")return;properties.Children.Add(Text("CURSOR",10,muted));
  Action<string,double,string,Action<double>> number=(label,value,id,apply)=>{var row=new Grid{Margin=new Thickness(10,2,10,2)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(94)});row.Children.Add(new TextBlock{Text=label,Foreground=muted,VerticalAlignment=VerticalAlignment.Center});var box=new TextBox{Text=value.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture),Padding=new Thickness(5,3,5,3),Background=B("#1b2328"),Foreground=Brushes.White,BorderBrush=B("#46525b")};AutomationProperties.SetAutomationId(box,id);Grid.SetColumn(box,1);row.Children.Add(box);properties.Children.Add(row);Action commit=()=>{double v;if(!double.TryParse(box.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)){Info("Enter a number.");return;}apply(v);};box.LostKeyboardFocus+=(s,e)=>commit();box.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter){commit();e.Handled=true;}};};
  number("Cursor scale (%)",c.CursorScale*100,"Cursor.Scale",v=>EditCursor(c,x=>x.CursorScale=v/100));number("Cursor opacity (%)",c.CursorOpacity*100,"Cursor.Opacity",v=>EditCursor(c,x=>x.CursorOpacity=v/100));
  for(int i=0;i<CursorEffects.Of(c).Count;i++){
   int index=i;var e=CursorEffects.Of(c)[i];properties.Children.Add(new TextBlock{Text=e.Name,Foreground=Brushes.White,FontWeight=FontWeights.SemiBold,Margin=new Thickness(10,6,10,2)});
   if(e.Name!="Cursor Smoothing"&&e.Name!="Cursor Color"&&e.Name!="Cursor Shadow")number(e.Name=="Cursor Glow"?"Glow width (px)":"Size (px)",e.Size,"CursorEffect.Size."+i,v=>EditCursor(c,x=>CursorEffects.Of(x)[index].Size=v));
   if(e.Name=="Cursor Smoothing")number("Smoothing (s)",e.Size,"CursorEffect.Size."+i,v=>EditCursor(c,x=>CursorEffects.Of(x)[index].Size=v));
   if(e.Name!="Cursor Smoothing"){var row=new Grid{Margin=new Thickness(10,2,10,2)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(94)});row.Children.Add(new TextBlock{Text="Color",Foreground=muted,VerticalAlignment=VerticalAlignment.Center});var combo=new ComboBox{ItemsSource=CursorColors,SelectedItem=CursorColors.FirstOrDefault(n=>CursorColorHex(n)==e.Color)??"Default"};AutomationProperties.SetAutomationId(combo,"CursorEffect.Color."+i);Grid.SetColumn(combo,1);row.Children.Add(combo);properties.Children.Add(row);combo.SelectionChanged+=(s,a)=>EditCursor(c,x=>CursorEffects.Of(x)[index].Color=CursorColorHex((string)combo.SelectedItem));}
   properties.Children.Add(Btn("Remove "+e.Name,()=>EditCursor(c,x=>CursorEffects.Of(x).RemoveAt(index))));
  }
 }
}
}
