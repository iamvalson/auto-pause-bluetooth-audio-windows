# BT Audio Guard

## Why?

If you've ever had your **Bluetooth headphones disconnect in public** and your music
kept blasting through your laptop speakers — this fixes that.

BT Audio Guard runs in the background and **automatically pauses Spotify, browsers,
VLC, and any other media player** the instant your Bluetooth audio device disconnects
on Windows 10 or Windows 11. No more embarrassing moments in the library or office.

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
git clone [https://github.com/iamvalson/bt-audio-guard](https://github.com/iamvalson/auto-pause-bluetooth-audio-windows)
cd auto-pause-bluetooth-audio-windows
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
```

The output is `publish\BtAudioGuard.exe`.

## Tech Stack

- C# / .NET 10
- NAudio — Windows Core Audio API (`IMMNotificationClient`) for device monitoring
- P/Invoke `SendInput` — media key simulation
- WinForms `NotifyIcon` — system tray
