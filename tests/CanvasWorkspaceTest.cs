using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using TasiaCraft;
public static class CanvasWorkspaceTest {
 static int passed; static string log;
 static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static object Field(Editor editor,string name){return typeof(Editor).GetField(name,flags).GetValue(editor);}
 static void Invoke(Editor editor,string name){typeof(Editor).GetMethod(name,flags).Invoke(editor,null);}
 static void Require(bool value,string label){if(!value)throw new Exception(label);File.AppendAllText(log,"PASS "+label+Environment.NewLine);passed++;}
 [STAThread] public static void Main(string[] args){
  log=args[0];File.WriteAllText(log,"");var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var editor=new Editor();
  editor.Loaded+=async(s,e)=>{try{
   Invoke(editor,"HideHome");var project=(Project)Field(editor,"project");project.Width=320;project.Height=180;
   var clip=new Clip{Text="Canvas regression",Duration=10,Scale=.5,X=10,Y=20};project.Clips.Add(clip);Invoke(editor,"Refresh");
   var live=(LivePreview)Field(editor,"live");var zoom=(ComboBox)Field(editor,"canvasZoom");
   Require(editor.Title.StartsWith(ProductInfo.DisplayName+" — "),"top left title includes shared product version");
   Invoke(editor,"Invalidate");Require(editor.Title.StartsWith(ProductInfo.DisplayName+" — ")&&editor.Title.EndsWith(" *"),"unsaved project title retains product version");typeof(Editor).GetField("dirty",flags).SetValue(editor,false);
   string original=new JavaScriptSerializer().Serialize(project);
   Require(zoom.Items.Count==13,"all observed zoom levels and canvas commands exposed");
   foreach(int percent in new[]{25,50,75,100,200,300,800,1200,1600,2000}){
    zoom.SelectedItem=percent+"%";editor.UpdateLayout();
    double dpi=PresentationSource.FromVisual(live).CompositionTarget.TransformToDevice.M11;
    double width=(live.Stage.TransformToAncestor(live).Transform(new Point(320,0))-live.Stage.TransformToAncestor(live).Transform(new Point(0,0))).Length;
    Require(Math.Abs(width*dpi-320*percent/100.0)<2,"physical canvas width at "+percent+" percent");
   }
   zoom.SelectedItem="Fit";Require(live.ViewZoom==0,"Fit restores automatic viewport sizing");
   live.Seek(1.25);zoom.SelectedItem="Detach Canvas";await Task.Delay(100);
   var detached=(Window)Field(editor,"detachedCanvas");Require(detached!=null&&detached.IsVisible,"zoom menu opens detached canvas");
   Require(zoom.SelectedItem.ToString()=="Fit","canvas command preserves selected zoom");
   Require(Window.GetWindow(live)==detached,"existing compositor moved to detached window");
   Require(zoom.Items[11].ToString()=="Attach Canvas"&&detached.Resources==editor.Resources,"detached canvas retains theme and exposes Attach Canvas");
   Require(Window.GetWindow((Grid)Field(editor,"transportPanel"))==detached,"same transport moved with compositor");
   Require(Math.Abs(live.Position-1.25)<.001,"detaching retains playhead");
   live.Play();await Task.Delay(180);Require(live.IsPlaying&&live.Position>1.3,"live playback advances while detached");live.Pause();double paused=live.Position;
   zoom.SelectedItem="2000%";Require(live.ViewZoom==20,"detached canvas supports 2000 percent");
   zoom.SelectedItem="Attach Canvas";await Task.Delay(80);Require(Field(editor,"detachedCanvas")==null&&Window.GetWindow(live)==editor,"Attach Canvas menu reattaches compositor");
   Require(Window.GetWindow((Grid)Field(editor,"transportPanel"))==editor&&Math.Abs(live.Position-paused)<.001,"reattach preserves transport and playhead");
   Require(live.ViewZoom==20,"reattach preserves viewport magnification");
   Require(new JavaScriptSerializer().Serialize(project)==original,"zoom detach playback and reattach preserve project and export geometry");
   for(int i=0;i<3;i++){Invoke(editor,"ToggleCanvasDock");Invoke(editor,"ToggleCanvasDock");}
   Require(Field(editor,"detachedCanvas")==null&&Window.GetWindow(live)==editor,"repeated detach attach cycles restore parents");
   Invoke(editor,"ToggleCanvasDock");editor.Close();await Task.Delay(80);
   Require(Field(editor,"detachedCanvas")==null&&!live.IsPlaying,"owner close cleans up detached window and playback");
   File.AppendAllText(log,"PASS total "+passed+Environment.NewLine);app.Shutdown(0);
  }catch(Exception ex){File.AppendAllText(log,"FAIL "+ex+Environment.NewLine);app.Shutdown(1);}};
  app.Run(editor);
 }
}
