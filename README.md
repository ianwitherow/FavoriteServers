# FavoriteServers

A BepInEx mod for Valheim that lets you save favorite servers and connect to them with a single click from the main menu.

## Features

- **Favorites Panel** — accessible from the main menu via a button or the F6 hotkey (configurable)
- **Quick Connect** — one-click connection to saved servers without manually entering addresses
- **Server Management** — add, edit, and delete favorite server entries
- **Password Storage** — optionally save server passwords for automatic authentication
- **Character Auto-Selection** — assign a character to each server and have it auto-selected on connect
- **DNS Resolution** — supports both IP addresses and hostnames

## Requirements

- [BepInEx 5.4.x](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
- [Jotunn 2.27.x](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)

## Installation

1. Install BepInEx and Jotunn if you haven't already.
2. Place `FavoriteServers.dll` into your `BepInEx/plugins` folder.

## Building from Source

Open `FavoriteServers.sln` in Visual Studio or Rider. The project targets .NET Framework 4.6.2. Build output is automatically copied to the BepInEx plugins folder via a post-build target.

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `ToggleHotkey` | `F6` | Keyboard shortcut to open/close the favorites panel |
| `WindowPosX` | `-1` | Saved window X position (-1 = auto-center) |
| `WindowPosY` | `-1` | Saved window Y position (-1 = auto-center) |

Server data is stored in `FavoriteServers.json` in the BepInEx config directory.
