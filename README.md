# Wallchanger v2

> **Note:** This project is no longer actively maintained. It was developed in 2022–2023 as a personal project and is archived here as a showcase of my C# and WPF development work.

A Windows desktop wallpaper manager built with C# and WPF. Browse, organize, and automatically cycle through your wallpaper collection from a tabbed gallery interface.

## Features

- **Gallery View** — Browse wallpapers in a thumbnail grid organized by tabs
- **Album Management** — Create, rename, and delete albums to organize images
- **Set Wallpaper** — Click any image to instantly set it as your desktop wallpaper
- **Randomize** — Randomly pick a wallpaper from the current album or entire collection
- **Next / Previous** — Step through wallpapers sequentially
- **Multi-Select** — Shift+click to batch-add images to albums, favorite, or delete
- **Drag & Drop** — Drop image files onto album tabs to import them
- **Scheduled Changes** — Automatically change wallpaper on a timer or at a specific time of day
- **System Tray** — Minimize to tray with quick controls for next, previous, randomize, and album switching
- **Favorites** — Built-in album for bookmarking wallpapers

## Screenshots

<!-- Add screenshots here if available -->

## Technologies

| Technology | Purpose |
|---|---|
| C# / .NET 6.0 | Application runtime |
| WPF | Desktop UI framework |
| MahApps.Metro | Modern Metro-style WPF controls and theming |
| Extended.Wpf.Toolkit | Extended controls (e.g. `DateTimeUpDown` for scheduling) |
| Newtonsoft.Json | JSON serialization for albums and settings |
| WinForms (`NotifyIcon`) | System tray integration |
| P/Invoke (`user32.dll`) | `SystemParametersInfo` to set the desktop wallpaper |

## Project Structure

```
Wallchanger-v2/
├── App.xaml / App.xaml.cs       — Application entry point, system tray lifecycle
├── MainWindow.xaml / .xaml.cs   — Main window UI and all application logic
├── WPButton.cs                  — Custom wallpaper thumbnail button
├── WPContainer.cs               — TabItem wrapper with scroll support
├── WPPanel.cs                   — WrapPanel with child name tracking
├── AlbumButton.cs               — Custom album selection button
├── AssemblyInfo.cs               — Assembly metadata
└── Wallchanger v2.csproj        — .NET 6.0 project file
```

## Building

Requires the [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) on Windows.

```bash
dotnet build
```

Or open `Wallchanger v2.sln` in Visual Studio 2022 and build from there.

## Runtime Data

The application creates the following files at runtime (not included in the repo):

- `albums.json` — Album definitions and image mappings
- `prefs.json` — User settings (close to tray, randomize on start, etc.)
- `wallpapers/` — Stored wallpaper images
- `opened_albums.txt` — Tracks which album tabs are open

## Supported Formats

PNG, JPG, BMP, GIF
