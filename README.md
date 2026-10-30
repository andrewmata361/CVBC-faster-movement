# CVBC faster movement

A BepInEx plugin for **Castlevania: Belmont's Curse** that makes Rose run at 1.5× her normal ground speed. The run animation and its footstep markers stay on the normal run clip and play at the same faster rate, so the steps keep looping. Jumps and air control stay at the base game speed, which keeps platforming distances the same.

This is an unofficial fan plugin. It does not contain game files. `GameAssembly.dll` is not modified and is not part of this repo.

## Install

You need the Windows game and [BepInEx 6 bleeding edge, build be.788, Unity IL2CPP x64](https://builds.bepinex.dev/projects/bepinex_be). BepInEx 5 will not load this plugin. If you already use the Belmont's Curse ultrawide plugin, that loader is the right one.

1. Close the game.
2. Copy [`release/RoseSpeed.dll`](release/RoseSpeed.dll) to `BepInEx\plugins\` inside the game folder, next to the other plugins.
3. Launch the game from Steam as usual. There is no separate executable.

The first BepInEx launch can sit on a black screen while it generates interop assemblies. Later launches are normal.

`BepInEx\LogOutput.log` should contain `Rose Speed 1.3.0 loaded`.

## Playing

**F4** turns the speed change on and off. A short message appears at the top left. The choice is saved.

Settings are created on first launch at `BepInEx\config\local.rose.speed.cfg`. Close the game before editing that file.

| Setting | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Ground run uses the multiplier. |
| `Multiplier` | `1.5` | How much faster Rose runs. Clamped from 0.5 to 3. |
| `AffectAir` | `false` | Also multiply airborne control. Leave this off to keep jumps as designed. |
| `ToggleKey` | `F4` | Keyboard key that toggles the boost. Use a Unity Input System key name such as `F4` or `Home`. |

## Remove

Delete `BepInEx\plugins\RoseSpeed.dll`. Saves and the original game files are not changed by this plugin.

## Build

Building needs the [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) and a local game install where BepInEx has been launched once, so `BepInEx\interop\Assembly-CSharp.dll` exists.

From this folder:

```powershell
.\build.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Castlevania Belmont's Curse"
```

That compiles `RoseSpeed.dll`, copies it into the game's `BepInEx\plugins\`, and refreshes `release\RoseSpeed.dll`.

The same command without the script:

```powershell
dotnet build RoseSpeed.csproj -c Release -p:GameDir="C:\Path\To\Castlevania Belmont's Curse"
```

The project only references the BepInEx and interop assemblies on your machine. Do not commit those DLLs, `GameAssembly.dll`, or anything else from the game folder.

## What it changes

- Rose's ground movement speed is multiplied. The default is 1.5×.
- The animation is told her unboosted run speed, so it keeps the normal run clip instead of switching to a faster clip that only has one step sound.
- That normal clip, including its footstep markers, is played at the same multiplier. Steps stay in the loop and line up with her feet.
- Air speed is left alone unless `AffectAir` is turned on.

Tested on the Steam Windows build, game version 1.1.3, Unity 2022.3.62f2, at 3440×1440. A game update that renames the movement methods will make the matching patch log an error and leave that part unchanged.
