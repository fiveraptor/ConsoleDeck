# ConsoleDeck

A physical macro deck powered by an Arduino — control media, apps, hotkeys, Discord, and Home Assistant from 9 configurable buttons and a rotary encoder.

---

## Features

- **9 configurable buttons** — assign any action to each button
- **Rotary encoder** — turn for volume, press to mute
- **Profiles** — multiple button layouts, switch instantly from the sidebar
- **Discord integration** — mute, deafen, leave/join voice channels via RPC
- **Home Assistant integration** — toggle/control any entity
- **Audio device switching** — switch to a specific output or toggle between two
- **Hotkeys** — send any keyboard shortcut (e.g. `ctrl+shift+s`)
- **System tray** — runs in the background, optional autostart with Windows
- **Light/dark mode** — follows the Windows system setting

---

## Installation

**1. Flash the Arduino**

Open `arduino/console_deck_v2/console_deck_v2.ino` in the [Arduino IDE](https://www.arduino.cc/en/software), select your board and COM port, then click Upload.

**2. Install ConsoleDeck**

Download the latest release from the [Releases](../../releases) page:

- `ConsoleDeck-Setup-vX.X.X.exe` — installer (recommended)
- `ConsoleDeck-vX.X.X-portable.zip` — portable, just extract and run

**3. Configure**

Launch `ConsoleDeck.exe`. Click any button card on the Dashboard to assign an action. Open Settings to connect Discord or Home Assistant.

The app minimizes to the system tray when you close the window. Right-click the tray icon to reopen or quit.

---

## Button Actions

| Type | Description |
|---|---|
| Link | Open a URL in the browser |
| File / App | Launch an application (with optional window-focus hint) |
| Hotkey | Send a keyboard shortcut, e.g. `ctrl+shift+s` |
| Play/Pause, Next, Prev, Stop | Media keys |
| System Mute | Toggle system mute |
| Discord Mute/Deafen | Toggle microphone or deafen |
| Discord Leave/Join | Leave or join a voice channel |
| Home Assistant | Control any HA entity (toggle / turn_on / turn_off) |
| Set Audio Device | Switch to a specific audio output |
| Toggle Audio Devices | Alternate between two outputs |

---

## Discord Setup

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create an application
2. Under **OAuth2**, add `http://localhost` as a redirect URI — copy the **Client ID** and **Client Secret**
3. In ConsoleDeck: **Settings → Discord**, enter both values and click **Save**
4. Confirm the Discord authorization popup that appears

For **Join Channel**: enable Developer Mode in Discord (Settings → Appearance), then right-click any voice channel and copy its ID.

---

## Home Assistant Setup

1. In Home Assistant: **Profile → Security → Create Long-Lived Access Token**
2. In ConsoleDeck: **Settings → Home Assistant**, enter your HA URL and token, click **Save**
3. When configuring a button, select **Home Assistant**, enter the entity ID (e.g. `light.living_room`) and choose an action

---

## For Developers

**Requirements:** [.NET 9 SDK](https://dotnet.microsoft.com/download)

```powershell
# Run from source
dotnet run --project ConsoleDeckApp\ConsoleDeck\ConsoleDeck.csproj

# Publish self-contained executable
dotnet publish ConsoleDeckApp\ConsoleDeck\ConsoleDeck.csproj `
  -c Release -r win-x64 --self-contained -o dist
```

Pushing a `v*` tag triggers the GitHub Actions workflow which builds the installer and portable zip and publishes a release automatically.

**Stack:** .NET 9 · WPF · WPF-UI 3.1.1 (Fluent) · CommunityToolkit.Mvvm · H.NotifyIcon · System.IO.Ports

**Arduino pins:**

| Component | Pin |
|---|---|
| Encoder CLK / DT / SW | 5 / 4 / 3 |
| Buttons 1–7 | 6–12 |
| Buttons 8–9 | A0, A1 |
| Media button | 2 |

All buttons use `INPUT_PULLUP` (connect between pin and GND).
