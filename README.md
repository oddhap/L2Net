# L2Net

L2Net is an automation client for Lineage 2 focused on L2J-based servers. It supports both out-of-game control and in-game tooling, including scripting, combat helpers, and a top-down world map.

## Status

This codebase targets `.NET Framework 4.8` and currently builds successfully from the included solution file.

Recent rendering work removed the old SlimDX dependency and moved the map renderer to `System.Drawing`/GDI+.

## Features

- Out-of-game mode
- In-game mode
- Top-down interactive map
- Combat, buff, and heal automation
- Custom scripting and packet-based logic

## Requirements

- Windows
- .NET Framework 4.8
- Visual Studio 2017 or later

## Build

1. Open `L2 login.sln` in Visual Studio.
2. Build the `Debug|x86` or `Release|x86` configuration.

You can also build from the command line with:

```powershell
dotnet build "L2 login.sln" -c Debug -p:Platform=x86
```

## Notes

- The project is intended for L2J servers.
- The legacy datapack and map assets are expected to be available at runtime.
