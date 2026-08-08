# SEPS — Space Engineers Planet Studio

Prototype/MVP foundation for a WYSIWYG Space Engineers planet editor.

## Current target

- Face workspace first
- Basic Globe workspace groundwork later
- Shared layer-based rendering architecture
- Static WaterMod water visualization in the MVP
- No tide/wave simulation in the MVP

## Prerequisites

- .NET 10 SDK (x64 recommended on Windows)
- Visual Studio 2026, Rider, or VS Code with C# tooling

The project targets .NET 10. Avalonia 12 is used for the desktop UI. OpenGL rendering will be hosted through Avalonia's OpenGL control infrastructure; Silk.NET is included as the intended low-level OpenGL binding for the renderer as it becomes active.

## First run

```powershell
dotnet restore
dotnet build
dotnet run --project src/SEPS.UI
```

The current prototype provides the application shell and planet-folder discovery. The next implementation slice is the actual OpenGL face renderer and exact 16-bit PNG codec.

## Important

Do not point SEPS at the only copy of a production planet once editing is implemented. Work on a copy until the import/export pipeline has been validated against Space Engineers.
