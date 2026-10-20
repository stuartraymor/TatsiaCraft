using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
namespace TasiaCraft {
 // A behavior animates a visual clip with an In (entrance), During (while on screen) and Out (exit) phase.
 // Names follow the reference Behaviors catalog; the motion curves are independent definitions shared by the
 // live preview (Evaluate) and export (expressions), so both render the same frames. See docs/BEHAVIORS-EVIDENCE.md.
 public class Behavior { public string Name; public double In=1,Out=1; public bool Enabled=true; }
 public struct BehaviorState { public double Alpha,Scale,X,Y,Rotation; }
 public static class Behaviors {
  public static readonly string[] Catalog={"Drifting","Explode","Fade","Fly In","Jump And Fall","Pop Up","Pulsating","Reveal","Scale","Shifting","Sliding","Tremble","Wiggle"};
  public const int MaxPerClip=8; public const double MaxPhase=60,ExplodeScale=1.8;
  static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  static string N(double v){return v.ToString("0.######",CultureInfo.InvariantCulture);}
  public static List<Behavior> Of(Clip c){if(c.Behaviors==null)c.Behaviors=new List<Behavior>();return c.Behaviors;}
  public static bool Applies(Clip c,Media m){return !string.IsNullOrEmpty(c.Text)||m!=null&&m.Kind!="audio"&&c.Stream!="audio";}
  public static void Validate(Project p){foreach(var c in p.Clips){if(c.Behaviors==null){c.Behaviors=new List<Behavior>();continue;}if(c.Behaviors.Count==0)continue;if(c.Behaviors.Count>MaxPerClip)throw new InvalidDataException("Too many behaviors on a clip");if(!Applies(c,p.Media.FirstOrDefault(m=>m.Id==c.MediaId)))throw new InvalidDataException("Behaviors require a visual clip");foreach(var b in c.Behaviors)if(b==null||!Catalog.Contains(b.Name)||!Finite(b.In)||!Finite(b.Out)||b.In<0||b.Out<0||b.In>MaxPhase||b.Out>MaxPhase)throw new InvalidDataException("Invalid behavior");}}
  // In/Out lengths, shrunk proportionally when they would overlap on a short clip.
  static void Phases(Behavior b,double duration,out double inD,out double outD){inD=b.In;outD=b.Out;if(inD+outD>duration&&inD+outD>0){double k=duration/(inD+outD);inD*=k;outD*=k;}}
  // Which phases fade linearly (exported as fade filters, multiplied in preview).
  static bool FadesIn(string name){return name=="Fade"||name=="Sliding"||name=="Drifting"||name=="Explode"||name=="Reveal";}
  static bool FadesOut(string name){return FadesIn(name)||name=="Jump And Fall";}
  static double Ease(double x){return x*x*(3-2*x);}
  static double Back(double x){double y=x-1;return 1+2.70158*y*y*y+1.70158*y*y;}
  static double Bounce(double p){return (1-p)*(1-p)*Math.Abs(Math.Cos(2.5*Math.PI*p));}
  public static BehaviorState Evaluate(Clip c,double local,double width,double height){
   var s=new BehaviorState{Alpha=1,Scale=1};
   foreach(var b in Of(c)){if(!b.Enabled)continue;double inD,outD;Phases(b,c.Duration,out inD,out outD);
    double p=inD>0?Math.Min(1,Math.Max(0,local/inD)):1,q=outD>0?Math.Min(1,Math.Max(0,(local-(c.Duration-outD))/outD)):0,pe=Ease(p),qe=Ease(q);
    if(FadesIn(b.Name))s.Alpha*=p;if(FadesOut(b.Name))s.Alpha*=1-q;
    switch(b.Name){
     case "Fly In":s.X+=-width*(1-pe)+width*qe;break;
     case "Pop Up":s.Scale*=Back(p)*(1-qe);break;
     case "Scale":s.Scale*=pe*(1-qe);break;
     case "Sliding":s.Y+=height*.25*(1-pe)-height*.25*qe;break;
     case "Pulsating":s.Scale*=1+.06*Math.Sin(2*Math.PI*local/.8);break;
     case "Tremble":s.X+=3*Math.Sin(2*Math.PI*11*local);s.Y+=2*Math.Sin(2*Math.PI*13*local);break;
     case "Drifting":s.X+=width*.04*(c.Duration>0?local/c.Duration-.5:0);break;
     case "Explode":s.Scale*=1+(ExplodeScale-1)*(1-pe)+(ExplodeScale-1)*qe;break;
     case "Jump And Fall":s.Y+=-height*.3*Bounce(p)+height*.6*q*q;break;
     case "Reveal":s.Scale*=.6+.4*pe;break;
     case "Shifting":s.X+=width*.02*Math.Sin(2*Math.PI*local/2);s.Y+=height*.02*Math.Cos(2*Math.PI*local/2);break;
     case "Wiggle":s.Rotation+=6*Math.Sin(2*Math.PI*1.5*local);break;
    }}
   s.Scale=Math.Max(0,s.Scale);s.Alpha=Math.Max(0,Math.Min(1,s.Alpha));return s;
  }
  // FFmpeg expressions for the same curves, in terms of a local-time expression lt.
  static string Clamp(string e){return "min(1,max(0,"+e+"))";}
  static string EaseE(string x){return "(("+x+")*("+x+")*(3-2*("+x+")))";}
  static void Parts(Behavior b,double duration,string lt,out string p,out string q){double inD,outD;Phases(b,duration,out inD,out outD);p=inD>0?Clamp("("+lt+")/"+N(inD)):"1";q=outD>0?Clamp("(("+lt+")-"+N(duration-outD)+")/"+N(outD)):"0";}
  static IEnumerable<Behavior> On(Clip c,params string[] names){return Of(c).Where(b=>b.Enabled&&names.Contains(b.Name));}
  public static bool HasScale(Clip c){return On(c,"Pop Up","Scale","Pulsating","Explode","Reveal").Any();}
  public static bool HasAlpha(Clip c){return Of(c).Any(b=>b.Enabled&&FadesOut(b.Name));}
  public static bool HasOffset(Clip c){return On(c,"Fly In","Sliding","Tremble","Drifting","Jump And Fall","Shifting").Any();}
  public static bool HasRotation(Clip c){return On(c,"Wiggle").Any();}
  public static double MaxScale(Clip c){double k=1;foreach(var b in Of(c))if(b.Enabled){if(b.Name=="Pop Up")k*=1.11;else if(b.Name=="Pulsating")k*=1.06;else if(b.Name=="Explode")k*=ExplodeScale;}return k;}
  public static string ScaleExpression(Clip c,string lt){var parts=new List<string>();foreach(var b in Of(c)){if(!b.Enabled)continue;string p,q;Parts(b,c.Duration,lt,out p,out q);
   if(b.Name=="Pop Up")parts.Add("(1+2.70158*pow("+p+"-1,3)+1.70158*pow("+p+"-1,2))*(1-"+EaseE(q)+")");else if(b.Name=="Scale")parts.Add(EaseE(p)+"*(1-"+EaseE(q)+")");else if(b.Name=="Pulsating")parts.Add("(1+0.06*sin(2*PI*("+lt+")/0.8))");
   else if(b.Name=="Explode")parts.Add("(1+"+N(ExplodeScale-1)+"*(1-"+EaseE(p)+")+"+N(ExplodeScale-1)+"*"+EaseE(q)+")");else if(b.Name=="Reveal")parts.Add("(0.6+0.4*"+EaseE(p)+")");}
   return parts.Count==0?"1":"max(0,"+string.Join("*",parts)+")";}
  // Linear alpha in/out as fade filters in clip-local time.
  public static string FadeFilters(Clip c){var s=new System.Text.StringBuilder();foreach(var b in Of(c)){if(!b.Enabled)continue;double inD,outD;Phases(b,c.Duration,out inD,out outD);if(FadesIn(b.Name)&&inD>0)s.Append(",fade=t=in:st=0:d="+N(inD)+":alpha=1");if(FadesOut(b.Name)&&outD>0)s.Append(",fade=t=out:st="+N(c.Duration-outD)+":d="+N(outD)+":alpha=1");}return s.ToString();}
  public static string AlphaExpression(Clip c,string lt){var parts=new List<string>();foreach(var b in Of(c)){if(!b.Enabled)continue;string p,q;Parts(b,c.Duration,lt,out p,out q);if(FadesIn(b.Name))parts.Add(p);if(FadesOut(b.Name))parts.Add("(1-"+q+")");}return parts.Count==0?"1":string.Join("*",parts);}
  public static string XExpression(Clip c,string lt,double width){var parts=new List<string>();foreach(var b in Of(c)){if(!b.Enabled)continue;string p,q;Parts(b,c.Duration,lt,out p,out q);
   if(b.Name=="Fly In")parts.Add("(-"+N(width)+"*(1-"+EaseE(p)+")+"+N(width)+"*"+EaseE(q)+")");else if(b.Name=="Tremble")parts.Add("3*sin(2*PI*11*("+lt+"))");else if(b.Name=="Drifting")parts.Add(N(width*.04)+"*(("+lt+")/"+N(Math.Max(1e-9,c.Duration))+"-0.5)");else if(b.Name=="Shifting")parts.Add(N(width*.02)+"*sin(2*PI*("+lt+")/2)");}
   return parts.Count==0?"0":string.Join("+",parts);}
  public static string YExpression(Clip c,string lt,double height){var parts=new List<string>();foreach(var b in Of(c)){if(!b.Enabled)continue;string p,q;Parts(b,c.Duration,lt,out p,out q);
   if(b.Name=="Sliding")parts.Add("("+N(height*.25)+"*(1-"+EaseE(p)+")-"+N(height*.25)+"*"+EaseE(q)+")");else if(b.Name=="Tremble")parts.Add("2*sin(2*PI*13*("+lt+"))");
   else if(b.Name=="Jump And Fall")parts.Add("(-"+N(height*.3)+"*pow(1-"+p+",2)*abs(cos(2.5*PI*"+p+"))+"+N(height*.6)+"*"+q+"*"+q+")");else if(b.Name=="Shifting")parts.Add(N(height*.02)+"*cos(2*PI*("+lt+")/2)");}
   return parts.Count==0?"0":string.Join("+",parts);}
  // Degrees, clockwise, added to the clip rotation.
  public static string RotationExpression(Clip c,string lt){var parts=new List<string>();foreach(var b in On(c,"Wiggle"))parts.Add("6*sin(2*PI*1.5*("+lt+"))");return parts.Count==0?"0":string.Join("+",parts);}
 }
}
