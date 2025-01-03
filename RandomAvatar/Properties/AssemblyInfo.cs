using System.Reflection;
using System.Runtime.InteropServices;

using MelonLoader;

using RandomAvatar;

#region MelonLoader

[assembly: MelonInfo(typeof(Core), "RandomAvatar", "1.0.0", "HAHOOS", "https://thunderstore.io/c/bonelab/p/HAHOOS/RandomAvatar")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonColor(0, 255, 0, 255)]
[assembly: MelonAuthorColor(0, 255, 165, 0)]
[assembly: MelonOptionalDependencies("LabFusion")]

#endregion MelonLoader

#region Info

[assembly: AssemblyTitle("Lets you switch to a random avatar by pressing button, as well in other ways")]
[assembly: AssemblyDescription("Lets you switch to a random avatar by pressing button, as well in other ways")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("HAHOOS")]
[assembly: AssemblyProduct("RandomAvatar")]
[assembly: AssemblyCulture("")]

#region Version

[assembly: AssemblyVersion(Core.Version)]
[assembly: AssemblyFileVersion(Core.Version)]
[assembly: AssemblyInformationalVersion(Core.Version)]

#endregion Version

#endregion Info

#region Other

[assembly: ComVisible(false)]
[assembly: Guid("6c044178-2021-4782-90bf-1dc7b0a93b08")]

#endregion Other