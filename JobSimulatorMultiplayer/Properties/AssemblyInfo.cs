using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;

[assembly: AssemblyTitle(JobSimulatorMultiplayer.BuildInfo.Name)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(JobSimulatorMultiplayer.BuildInfo.Company)]
[assembly: AssemblyProduct(JobSimulatorMultiplayer.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + JobSimulatorMultiplayer.BuildInfo.Author)]
[assembly: AssemblyTrademark(JobSimulatorMultiplayer.BuildInfo.Company)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
//[assembly: Guid("")]
[assembly: AssemblyVersion(JobSimulatorMultiplayer.BuildInfo.Version)]
[assembly: AssemblyFileVersion(JobSimulatorMultiplayer.BuildInfo.Version)]
[assembly: NeutralResourcesLanguage("en")]
[assembly: MelonModInfo(typeof(JobSimulatorMultiplayer.JobSimulatorMultiplayer), JobSimulatorMultiplayer.BuildInfo.Name, JobSimulatorMultiplayer.BuildInfo.Version, JobSimulatorMultiplayer.BuildInfo.Author, JobSimulatorMultiplayer.BuildInfo.DownloadLink)]


// Create and Setup a MelonModGame to mark a Mod as Universal or Compatible with specific Games.
// If no MelonModGameAttribute is found or any of the Values for any MelonModGame on the Mod is null or empty it will be assumed the Mod is Universal.
// Values for MelonModGame can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonModGame(null, null)]