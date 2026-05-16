# BT Audio Guard

Automatically pauses media playback when your Bluetooth audio device disconnects — so your music never blasts through your PC speakers in public.

## The Problem

You're in the library. Bluetooth headphones disconnect. Spotify keeps playing. Everyone stares.

## How It Works

BT Audio Guard runs silently in your system tray and listens for audio device disconnect events. The moment your Bluetooth device drops, it sends a media pause command before Windows reroutes audio to your speakers.

## Features

- Runs silently in the system tray — no window, no clutter
- Pause / Resume monitoring from the tray menu
- Optional **Run at startup** toggle (no admin required)
- Balloon tip notification when media is paused
- Works with Spotify, browsers, Windows Media Player, VLC — anything that responds to the media pause key

## Download

Grab the latest `BtAudioGuard.exe` from [Releases](../../releases) — single file, no install needed.

## Requirements

- Windows 10 / 11 (x64)
- Bluetooth audio device

## Build From Source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
git clone https://github.com/YOUR_USERNAME/bt-audio-guard
cd bt-audio-guard
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
```

The output is `publish\BtAudioGuard.exe`.

## Tech Stack

- C# / .NET 10
- NAudio — Windows Core Audio API (`IMMNotificationClient`) for device monitoring
- P/Invoke `SendInput` — media key simulation
- WinForms `NotifyIcon` — system tray
