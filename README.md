# L2 .NET
L2 .NET is an automated assistant tool (commonly referred to as 'bot') for Lineage 2 that assists a player's experience during their play. It is similar to [L2Divine](http://www.l2divine.com/) and [L2Walker](http://www.towalker.com/).

WARNING: This software is not designed to circumvent anti-bot software nor is it supported for retail versions of Lineage 2. Using this on servers where botting is not allowed is not supported or condoned in any way.

## Features
- Out of Game Mode, allowing you to fully control your characters without being inside the normal Lineage 2 Client.
- In Game Mode, allowing you to attach to a game client and enhance the gameplay experience (best of both worlds).
- Top-Down Overview map allowing you to interact with the world.
- Combat / Buff/Heal Options to automate skill usage.
- Custom Scripting Support, including packet detection and logic chains

## Agreement
This program is developed on and for L2J servers ONLY.
Use of this program on any other type of server is against the EULA.
This program comes with no warranty expressed or implied.
Any concerns about copyright or other challenges can be messaged to the open source contributor for review and removal.

## Requirements
- .NET Framework 4.8
- Visual Studio 2017 or later

## Compiling
1. Open `L2 Login.sln` in Visual Studio
2. Restore NuGet packages if needed
3. Build the solution

## Recent Changes

### 2025 - SlimDX Removal
Migrated from SlimDX to built-in .NET libraries:
- Replaced SlimDX.Direct3D9 with GDI+ (System.Drawing) for map rendering
- Replaced SlimDX.DirectInput with Windows.GetAsyncKeyState API for keyboard input
- Added custom Vector4 struct to replace SlimDX.Vector4
- Updated DDS loader for texture support
- Fixed AES encryption for .NET 4.8 compatibility

### 2018 Changelog
Compiled program is ready to be used in L2Net_June3_2018.7z