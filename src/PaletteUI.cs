using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Automation;

namespace TasiaCraft {
public partial class Editor {
 void PaletteHeading(string title,string subtitle){
  library.Children.Add(new Border{Background=B("#171e22"),Padding=new Thickness(7,8,7,8),Child=new TextBlock{Text=title,Foreground=Brushes.White,FontSize=14,TextAlignment=TextAlignment.Center}});
  if(!string.IsNullOrEmpty(subtitle))library.Children.Add(Text(subtitle,11,muted));
 }
 Button PaletteTile(string name,string picture,Action action){
  var b=Btn(name,action);b.Padding=new Thickness(3);b.Margin=new Thickness(4);b.Background=B("#343e45");b.HorizontalContentAlignment=HorizontalAlignment.Stretch;b.VerticalContentAlignment=VerticalAlignment.Stretch;
  var stack=new StackPanel();stack.Children.Add(new Image{Source=PalettePicture(picture),Height=48,Margin=new Thickness(3,5,3,1),Stretch=Stretch.Uniform});stack.Children.Add(new TextBlock{Text=name,Foreground=Brushes.White,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,FontSize=10,Margin=new Thickness(1,3,1,5)});b.Content=stack;
  AutomationProperties.SetName(b,name);AutomationProperties.SetAutomationId(b,"Preset."+name.Replace(" ",""));b.ToolTip=name+" — click to apply";return b;
 }
 DrawingImage PalettePicture(string kind){
  var group=new DrawingGroup();using(var d=group.Open()){
   var mint=B("#62cfb3");var white=B("#e8edf0");var blue=B("#408ba9");var outline=new Pen(mint,2);d.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,90,48));
   if(kind=="Rectangle")d.DrawRoundedRectangle(mint,null,new Rect(10,8,70,32),3,3);
   else if(kind=="Ellipse")d.DrawEllipse(mint,null,new Point(45,24),33,17);
   else if(kind=="Arrow"||kind=="Pan right") {var geometry=Geometry.Parse("M8,18 L58,18 58,6 84,24 58,42 58,30 8,30 Z");d.DrawGeometry(mint,null,geometry);}
   else if(kind=="Text"||kind=="Title"||kind=="Caption"){
    if(kind=="Caption")d.DrawRoundedRectangle(blue,null,new Rect(4,12,82,28),3,3);
    var text=new FormattedText(kind=="Caption"?"Caption":"ABC",System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),kind=="Caption"?14:23,white,1.0);d.DrawText(text,new Point((90-text.Width)/2,9));
   }
   else if(kind.StartsWith("Fade")){
    var gradient=new LinearGradientBrush(Colors.Transparent,Color.FromRgb(98,207,179),new Point(0,0),new Point(1,0));d.DrawRectangle(gradient,null,new Rect(7,7,76,34));d.DrawLine(new Pen(white,2),new Point(7,40),new Point(83,7));
   }
   else if(kind=="Grayscale"||kind=="Blur"||kind=="Sharpen"){
    for(int i=0;i<6;i++)d.DrawRectangle(kind=="Grayscale"?new SolidColorBrush(Color.FromRgb((byte)(40+i*35),(byte)(40+i*35),(byte)(40+i*35))):i%2==0?blue:mint,null,new Rect(9+i*12,8,12,32));
    if(kind=="Blur")for(int i=0;i<4;i++)d.DrawEllipse(new SolidColorBrush(Color.FromArgb(50,255,255,255)),null,new Point(35+i*6,24),15,17);
    if(kind=="Sharpen")d.DrawRectangle(null,new Pen(white,2),new Rect(21,12,48,24));
   }
   else if(kind=="Zoom in") {d.DrawRectangle(null,outline,new Rect(8,7,55,31));d.DrawRectangle(mint,null,new Rect(35,17,46,25));}
   else if(kind=="Background") {d.DrawRectangle(new LinearGradientBrush(Color.FromRgb(28,50,76),Color.FromRgb(41,126,126),45),null,new Rect(5,5,80,38));d.DrawEllipse(new SolidColorBrush(Color.FromArgb(55,255,255,255)),null,new Point(65,13),21,21);}
   else if(kind=="Reset"||kind=="Remove") {d.DrawEllipse(null,new Pen(muted,2),new Point(45,24),17,17);d.DrawLine(new Pen(white,2),new Point(32,37),new Point(58,11));}
   else {
    for(int i=0;i<14;i++){double h=7+Math.Abs(Math.Sin(i*.8))*28;d.DrawLine(new Pen(kind=="Mute"?muted:mint,3),new Point(10+i*5,24-h/2),new Point(10+i*5,24+h/2));}
    if(kind=="Mute")d.DrawLine(new Pen(white,2),new Point(13,6),new Point(77,42));
   }
  }var image=new DrawingImage(group);image.Freeze();return image;
 }
 void EditPreset(Action<Clip> edit){if(selected==null){Info("Select a timeline clip to apply this preset.");return;}if(SelectedClips().Count>1||TrackLocked(selected)){Info("Open the group and select one unlocked member to apply a preset.");return;}Checkpoint();edit(selected);Refresh();}
 void BuildPalette(string name){
  PaletteHeading(name,name=="Annotations"?"Click a preset to insert it on the top track.":name=="Captions"?"Editable text and timed SubRip captions.":"Select a timeline clip, then click a preset.");
  var tiles=new UniformGrid{Columns=3,Margin=new Thickness(4,4,4,10)};library.Children.Add(tiles);Action<string,string,Action> tile=(label,picture,apply)=>tiles.Children.Add(PaletteTile(label,picture,apply));
  if(name=="Annotations"){
   tile("Text","Text",()=>AddText("Your text here"));tile("Rectangle","Rectangle",()=>AddShape("Rectangle"));tile("Ellipse","Ellipse",()=>AddShape("Ellipse"));tile("Arrow","Arrow",()=>AddShape("Arrow"));
  }else if(name=="Transitions"){
   foreach(double seconds in new[]{.25,.5,1.0,2.0}){double duration=seconds;tile("Fade "+seconds.ToString("0.##")+" s","Fade",()=>EditPreset(c=>c.FadeIn=c.FadeOut=Math.Min(duration,c.Duration/2)));}
   tile("Remove fades","Remove",()=>EditPreset(c=>c.FadeIn=c.FadeOut=0));
  }else if(name=="Visual effects"){
   foreach(string effect in new[]{"Grayscale","Blur","Sharpen"}){string fx=effect;tile(fx,fx,()=>EditPreset(c=>c.Effect=fx.ToLowerInvariant()));}
   tile("Remove effect","Remove",()=>EditPreset(c=>c.Effect=null));tile("Reset transform","Reset",()=>EditPreset(c=>{c.Scale=1;c.X=c.Y=c.Rotation=0;c.Opacity=1;c.Animate=false;}));
  }else if(name=="Animations"){
   tile("Pan right","Pan right",()=>AnimateClip(false));tile("Zoom in","Zoom in",()=>AnimateClip(true));tile("Remove animation","Remove",()=>EditPreset(c=>c.Animate=false));
  }else if(name=="Audio effects"){
   tile("Normalize","Audio",()=>EditPreset(c=>c.AudioEffect="normalize"));tile("Noise reduction","Audio",()=>EditPreset(c=>c.AudioEffect="noise reduction"));tile("Remove effect","Remove",()=>EditPreset(c=>c.AudioEffect=null));
   tile("Mute","Mute",()=>EditPreset(c=>c.Volume=0));tile("Restore gain","Audio",()=>EditPreset(c=>c.Volume=1));tile("Audio fade","Fade",()=>EditPreset(c=>c.FadeIn=c.FadeOut=Math.Min(.5,c.Duration/2)));
   library.Children.Add(Text("Fade presets affect both audio and video. Gain and fade durations can be adjusted in Properties.",11,muted));
  }else if(name=="Captions"){
   tile("Add caption","Caption",()=>{AddText("Your caption here");selected.Y=project.Height*.3;Refresh();});tile("Import SRT…","Caption",ImportCaptions);
  }
 }
 void BuildLibrary(){
  PaletteHeading("Library","TasiaCraft originals");
  var search=new TextBox{Margin=new Thickness(9,3,9,9),Padding=new Thickness(6),Background=B("#343e45"),Foreground=Brushes.White,BorderThickness=new Thickness(0),ToolTip="Search library"};AutomationProperties.SetAutomationId(search,"LibrarySearch");library.Children.Add(search);
  var groups=new StackPanel();library.Children.Add(groups);
  Action populate=()=>{groups.Children.Clear();string query=search.Text.Trim();
   Action<string,string[],Action<string>> folder=(title,items,insert)=>{
    var found=items.Where(n=>query.Length==0||n.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||title.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();if(found.Length==0)return;
    var body=new StackPanel();foreach(string item in found){string captured=item;var button=Btn(item,()=>insert(captured));button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Padding=new Thickness(12,7,6,7);button.Margin=new Thickness(2,1,2,1);body.Children.Add(button);}
    groups.Children.Add(new Expander{Header=title,IsExpanded=true,Foreground=Brushes.White,Background=B("#293239"),Margin=new Thickness(5,2,5,3),Content=body});
   };
   folder("Titles",new[]{"Opening title","Section heading","Lower caption"},name=>{AddText(name=="Opening title"?"Your title here":name=="Section heading"?"Section heading":"Name • Description");if(name=="Lower caption"){selected.Y=project.Height*.3;Refresh();}});
   folder("Backgrounds",new[]{"Deep teal gradient","Midnight blue","Warm slate"},InsertLibraryBackground);
   folder("Shapes",new[]{"Rectangle","Ellipse","Arrow"},AddShape);
   var media=project.Media.Where(m=>query.Length==0||m.Name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();if(media.Length>0){var body=new StackPanel();foreach(var item in media){var source=item;var b=Btn(source.Name,()=>Add(source,head,0));b.HorizontalContentAlignment=HorizontalAlignment.Left;body.Children.Add(b);}groups.Children.Add(new Expander{Header="Project media ("+media.Length+")",IsExpanded=false,Content=body,Foreground=Brushes.White,Margin=new Thickness(5)});}
  };search.TextChanged+=(s,e)=>populate();populate();library.Children.Add(Btn("+ Import media…",ImportDialog));
 }
 void InsertLibraryBackground(string name){
  try{
   string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TasiaCraft","Assets");Directory.CreateDirectory(dir);string path=Path.Combine(dir,"Original-"+name.Replace(" ","-")+"-"+project.Width+"x"+project.Height+".png");
   if(!File.Exists(path)){var visual=new DrawingVisual();using(var d=visual.RenderOpen()){
    Color a=name=="Midnight blue"?Color.FromRgb(17,26,52):name=="Warm slate"?Color.FromRgb(82,58,58):Color.FromRgb(16,48,60);Color z=name=="Midnight blue"?Color.FromRgb(49,74,119):name=="Warm slate"?Color.FromRgb(150,106,81):Color.FromRgb(40,137,134);
    d.DrawRectangle(new LinearGradientBrush(a,z,35),null,new Rect(0,0,project.Width,project.Height));d.DrawEllipse(new SolidColorBrush(Color.FromArgb(17,255,255,255)),null,new Point(project.Width*.82,project.Height*.18),project.Width*.35,project.Width*.35);
   }var bmp=new RenderTargetBitmap(project.Width,project.Height,96,96,PixelFormats.Pbgra32);bmp.Render(visual);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using(var output=File.Create(path))png.Save(output);}
   var media=project.Media.FirstOrDefault(m=>m.Path==path);if(media==null){media=Engine.Probe(path);Checkpoint();project.Media.Add(media);}Add(media,head,0);
  }catch(Exception ex){MessageBox.Show(ex.Message,"Library background");}
 }
}
}
