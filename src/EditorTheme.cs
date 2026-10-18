using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Automation;
using Microsoft.Win32;
namespace TasiaCraft {
public partial class Editor {
 void InitTheme(){
  Resources[SystemColors.WindowBrushKey]=panel;Resources[SystemColors.ControlBrushKey]=panel;Resources[SystemColors.ControlTextBrushKey]=Brushes.White;Resources[SystemColors.WindowTextBrushKey]=Brushes.White;
  string combo=@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ComboBox'><Setter Property='Foreground' Value='#d5dce0'/><Setter Property='Background' Value='#354048'/><Setter Property='Padding' Value='7,4'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ComboBox'><Grid><ToggleButton Focusable='False' IsChecked='{Binding IsDropDownOpen,Mode=TwoWay,RelativeSource={RelativeSource TemplatedParent}}'><ToggleButton.Template><ControlTemplate TargetType='ToggleButton'><Border Background='#354048' BorderBrush='#435059' BorderThickness='1' CornerRadius='1'/></ControlTemplate></ToggleButton.Template></ToggleButton><Grid IsHitTestVisible='False' Margin='7,3,7,3'><Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width='12'/></Grid.ColumnDefinitions><ContentPresenter Content='{TemplateBinding SelectionBoxItem}' VerticalAlignment='Center'/><Path Grid.Column='1' Data='M0,0 L8,0 4,5 Z' Fill='#c0cbd1' VerticalAlignment='Center'/></Grid><Popup x:Name='PART_Popup' Placement='Bottom' IsOpen='{TemplateBinding IsDropDownOpen}' AllowsTransparency='True' Focusable='False'><Border Background='#303b43' BorderBrush='#435059' BorderThickness='1' MinWidth='{TemplateBinding ActualWidth}'><ScrollViewer MaxHeight='300'><ItemsPresenter/></ScrollViewer></Border></Popup></Grid></ControlTemplate></Setter.Value></Setter></Style>";
  Resources[typeof(ComboBox)]=(Style)XamlReader.Parse(combo);
  var button=new Style(typeof(Button));button.Setters.Add(new Setter(Button.TemplateProperty,(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border Background='{TemplateBinding Background}' CornerRadius='2' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter Property='Opacity' Value='.82'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='.35'/></Trigger></ControlTemplate.Triggers></ControlTemplate>")));Resources[typeof(Button)]=button;
 }
 DrawingImage ToolPicture(string kind){var group=new DrawingGroup();var light=B("#c6ced3");var pen=new Pen(light,2);using(var d=group.Open()){
  d.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,24,24));
  if(kind=="Undo"||kind=="Redo"){var g=Geometry.Parse("M18,20 C23,8 15,4 7,7 M7,7 L7,1 M7,7 L1,7");if(kind=="Redo")d.PushTransform(new ScaleTransform(-1,1,12,12));d.DrawGeometry(null,pen,g);if(kind=="Redo")d.Pop();}
  else if(kind=="Cut"){d.DrawEllipse(null,pen,new Point(6,18),4,4);d.DrawEllipse(null,pen,new Point(18,18),4,4);d.DrawLine(pen,new Point(8,15),new Point(20,2));d.DrawLine(pen,new Point(16,15),new Point(4,2));}
  else if(kind=="Copy"){d.DrawRoundedRectangle(null,pen,new Rect(3,2,14,17),1,1);d.DrawRoundedRectangle(light,null,new Rect(7,6,14,17),1,1);d.DrawLine(new Pen(panel,2),new Point(10,11),new Point(18,11));d.DrawLine(new Pen(panel,2),new Point(10,16),new Point(18,16));}
  else if(kind=="Paste"){d.DrawRoundedRectangle(null,pen,new Rect(5,5,14,17),1,1);d.DrawRoundedRectangle(light,null,new Rect(8,2,8,5),1,1);d.DrawLine(pen,new Point(8,11),new Point(16,11));d.DrawLine(pen,new Point(8,16),new Point(16,16));}
  else if(kind=="Split"){d.DrawRectangle(null,pen,new Rect(2,5,7,15));d.DrawRectangle(null,pen,new Rect(15,5,7,15));d.DrawLine(pen,new Point(12,2),new Point(12,23));}
  else if(kind=="Frame"){d.DrawRoundedRectangle(light,null,new Rect(2,6,20,15),2,2);d.DrawRectangle(light,null,new Rect(7,3,8,5));d.DrawEllipse(panel,null,new Point(12,13),5,5);}
  else if(kind=="Zoom"){d.DrawEllipse(null,pen,new Point(10,10),6,6);d.DrawLine(new Pen(light,3),new Point(14,14),new Point(22,22));}
  else if(kind=="Eye"){d.DrawEllipse(light,null,new Point(12,12),11,6);d.DrawEllipse(panel,null,new Point(12,12),4,4);}
  else if(kind=="Lock"){d.DrawRoundedRectangle(light,null,new Rect(6,10,12,12),1,1);d.DrawRoundedRectangle(null,pen,new Rect(8,2,8,13),4,4);d.DrawEllipse(panel,null,new Point(12,16),2,2);}
  else if(kind=="Snap"){d.DrawGeometry(null,new Pen(light,4),Geometry.Parse("M5,4 L5,15 C5,23 19,23 19,15 L19,4"));d.DrawLine(new Pen(panel,2),new Point(2,9),new Point(8,9));d.DrawLine(new Pen(panel,2),new Point(16,9),new Point(22,9));}
  else if(kind=="Select"){d.DrawGeometry(light,null,Geometry.Parse("M4,2 L4,20 9,15 13,23 17,21 13,13 21,13 Z"));}
  else if(kind=="Crop"){d.DrawGeometry(null,pen,Geometry.Parse("M6,1 L6,18 L23,18 M1,6 L18,6 L18,23 M10,10 L14,10 L14,14"));}
  else if(kind=="Pan"){d.DrawGeometry(null,pen,Geometry.Parse("M5,12 L5,18 C5,25 19,25 19,18 L19,9 M5,15 L2,11 M8,15 L8,3 M12,15 L12,1 M16,15 L16,4"));}
 }var image=new DrawingImage(group);image.Freeze();return image;}
 Button IconButton(string label,string kind,Action action){var b=Btn(label,action);b.Content=new Image{Source=ToolPicture(kind),Width=18,Height=18};b.Width=30;b.Height=26;b.Padding=new Thickness(5);b.Margin=new Thickness(1);b.Background=B("#252d33");b.ToolTip=label;AutomationProperties.SetName(b,label);AutomationProperties.SetAutomationId(b,"Tool."+kind);return b;}
 void UpdatePlayUi(){if(play==null)return;play.Content=playing?"Ⅱ":"▶";AutomationProperties.SetName(play,playing?"Ⅱ  Pause":"▶  Play");}
 void ExportFrame(){if(live==null)return;Pause();var d=new SaveFileDialog{Filter="PNG image|*.png",FileName="TasiaCraft-frame.png"};if(d.ShowDialog()!=true)return;try{if(project.Media.Exists(m=>string.Equals(Path.GetFullPath(m.Path),Path.GetFullPath(d.FileName),StringComparison.OrdinalIgnoreCase)))throw new Exception("Cannot overwrite source media.");live.SetSelection(null);live.Stage.UpdateLayout();var bitmap=new RenderTargetBitmap(project.Width,project.Height,96,96,PixelFormats.Pbgra32);bitmap.Render(live.Stage);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var f=File.Create(d.FileName))encoder.Save(f);Info("Frame saved: "+d.FileName);}catch(Exception e){MessageBox.Show(e.Message,"Frame export");}finally{live.SetSelection(selected);}}
}
}
