# DVDispatcherMod
Where are the cars for this job? Don't worry. If look at a job or hold one in your hand, we'll let you know.

If you are in VR, the popups will only show up when you hold the job overview in your right hand.

This mod reuses the tutorial popups to show you where the cars for a job are. As usual, this mod requires the Unity Mod Manager for installation.

[Nexus Mods Link](https://www.nexusmods.com/derailvalley/mods/743/)

# Example Directory.Build.Props
For compiling the mod yourself, adjust [path-to-steam] in this file and put it in your source folder (next to .sln or .csproj).
```xml
<Project>
  <PropertyGroup>
    <DvInstallDir>C:\[path-to-steam]\steamapps\common\Derail Valley</DvInstallDir>
    <ReferencePath>
      $(DvInstallDir)\DerailValley_Data\Managed\;
      $(DvInstallDir)\DerailValley_Data\Managed\UnityModManager\;
      $(DvInstallDir)\Mods\MessageBox\
    </ReferencePath>
    <AssemblySearchPaths>$(AssemblySearchPaths);$(ReferencePath)</AssemblySearchPaths>
  </PropertyGroup>
</Project>
```
