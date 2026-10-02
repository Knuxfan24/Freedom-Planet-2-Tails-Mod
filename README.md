# Freedom Planet 2 Tails Mod

A mod for Freedom Planet 2 (using [FP2Lib](https://github.com/Kuborros/FP2Lib)) that adds Miles "Tails" Prower as a playable character.

## Features

### Moveset

knux please add details

## Building

First off, ensure that your system has a modern version of [Visual Studio](https://visualstudio.microsoft.com/) installed alongside the `.NET Framework 3.5 development tools`, as well as [Unity 5.6.3](https://unity.com/releases/editor/whats-new/5.6.3#installs) and [FP2Lib](https://github.com/Kuborros/FP2Lib) (at least Version 0.6.1. The [Freedom Manager](https://github.com/Kuborros/FreedomManager) program should install this automatically if used.).

Open the solution file in Visual Studio then go to `Tools > Options` and select `Package Sources` under the `NuGet Package Manager` category. Then add a package source called `BepInEx` with the source url set to `https://nuget.bepinex.dev/v3/index.json`.

Next, go to the `Assemblies` category in the `Dependencies` for the project, then delete the `Assembly-CSharp` and `FP2Lib` references. Right click on the Assemblies category and click `Add Assembly Reference...`, then click `Browse...` and navigate to Freedom Planet 2's install directory. Open the `FP2_Data` directory, then the `Managed` directory and select the `Assembly-CSharp.dll` file. Click Add, then Browse again and navigate to the location that FP2Lib's DLL is installed to (likely `BepInEx\Plugins\lib`) and select the `FP2Lib.dll` file. Click Add, then click OK.

You should now be able to right click the solution and choose `Rebuild` to build the mod.

## Installing

Navigate to `BepInEx/plugins` and create a new folder with whatever name you want. Then copy the `Freedom_Planet_2_Tails_Mod.dll` file from the build (`bin/Debug/net35` or `bin/Release/net35`) into it. If using Freedom Manager, you may also want to copy the included `modinfo.json` file to give the mod a proper entry in the manager, although this is not strictly required.

## Asset Credits

knux please add details