using System.Reflection;
namespace TasiaCraft {
 // Release identity comes from VERSION via scripts/build.ps1. Never hard-code a UI version.
 // Every completed revision must be validated, committed, pushed and released on GitHub.
 // Use scripts/release.ps1; see AGENTS.md and CONTRIBUTING.md for the release contract.
 public static class ProductInfo {
  public const string Name="TatsiaCraft";
  public static string Version {get{return ((AssemblyInformationalVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute),false)[0]).InformationalVersion;}}
  public static string DisplayName {get{return Name+" "+Version;}}
 }
}
