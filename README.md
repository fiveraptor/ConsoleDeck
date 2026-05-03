# ConsoleDeck V2

A custom macro deck powered by an Arduino — control media, apps, hotkeys, Discord and Home Assistant directly from physical buttons and a rotary encoder.

> Based on [LucaDiLorenzo98/cd_v2_script](https://github.com/LucaDiLorenzo98/cd_v2_script) with significant improvements.

---

## Features

- **9 configurable buttons** — each button can be assigned any action
- **Rotary encoder** — turn for volume, press to mute
- **1 dedicated media button** — always play/pause
- **Profiles** — create multiple button layouts and switch between them instantly
- **Automatic Arduino detection** — no manual COM port configuration
- **Windows-style UI** — light/dark mode follows Windows system setting
- **Discord integration** — mute, deafen, leave voice channel via RPC
- **Home Assistant integration** — toggle/control any HA entity
- **Audio device switching** — assign a button to switch to a specific output device or toggle between two
- **System tray** — runs silently in the background, optional autostart

**Available button actions:**

| Type | Description |
|---|---|
| `link` | Open a URL in the default browser |
| `exe` | Launch an application |
| `hotkey` | Send a keyboard shortcut (e.g. `ctrl+shift+s`) |
| `play_pause` | Media play/pause |
| `next_track` | Next track |
| `prev_track` | Previous track |
| `stop` | Stop media |
| `mute` | Toggle system mute |
| `discord_mute` | Toggle Discord microphone |
| `discord_deafen` | Toggle Discord deafen |
| `discord_leave` | Leave Discord voice channel |
| `homeassistant` | Control a Home Assistant entity (toggle / turn_on / turn_off) |
| `audio_device` | Switch to a specific audio output device |
| `audio_toggle` | Toggle between two audio output devices |

---

## Requirements

- Windows 10/11
- Arduino Uno (or compatible clone with CH340/FTDI/CP210x chip)
- Arduino IDE (to flash the firmware)

---

## Installation

### 1. Flash the Arduino

1. Open `arduino/console_deck_v2/console_deck_v2.ino` in the [Arduino IDE](https://www.arduino.cc/en/software)
2. Select your board (`Arduino Uno`) and the correct COM port
3. Click **Upload**

### 2. Download ConsoleDeck

Go to the [Releases](../../releases) page and download the latest version.

Two options are available:
- **`ConsoleDeck-Setup-vX.X.X.exe`** — recommended, installs with Start Menu shortcuts and optional autostart
- **`ConsoleDeck-vX.X.X-portable.zip`** — no installation needed, just extract and run

### 3. Run & Configure

Launch `ConsoleDeck.exe`. The main window opens with a **Dashboard** and a **Settings** page.

- **Dashboard** — click any of the 9 button cards to configure that button
- **Settings** — configure Discord and Home Assistant credentials, toggle autostart
- **Profiles** — use the dropdown in the sidebar to create, rename, switch, or delete profiles; each profile stores its own set of 9 button assignments

The app runs in the system tray after closing the window. Right-click the tray icon to reopen it or quit.

---

## Home Assistant Integration

1. In Home Assistant: **Profile** (bottom left) → **Security** → **Create Long-Lived Access Token**
2. In ConsoleDeck: open **Settings → Home Assistant**, enter your HA URL and token, click **Save**
3. When configuring a button, select **HA**, enter the entity ID (e.g. `light.living_room`) and choose an action (`toggle`, `turn_on`, or `turn_off`)

---

## Discord Integration

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create a new application
2. Under **OAuth2**, add `http://localhost` as a Redirect URI, then copy the **Client ID** and **Client Secret**
3. In ConsoleDeck: open **Settings → Discord**, enter both values, click **Save**
4. A Discord authorization popup will appear — confirm it to connect

---

## Building from Source

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download) and [Inno Setup](https://jrsoftware.org/isinfo.php) (for the installer).

```powershell
# Publish self-contained Windows executable
dotnet publish ConsoleDeckApp\ConsoleDeck\ConsoleDeck.csproj `
  -c Release -r win-x64 --self-contained -o dist

# Build installer (optional)
iscc /DAppVersion=2.3.0 installer.iss
```

Or use the GitHub Actions workflow — any push to a `v*` tag automatically builds and publishes a release.

---

## Hardware

Wiring for the Arduino Uno:

| Component | Pin |
|---|---|
| Encoder CLK | 5 |
| Encoder DT | 4 |
| Encoder SW (mute) | 3 |
| Button 1–7 | 6, 7, 8, 9, 10, 11, 12 |
| Button 8–9 | A0, A1 |
| Media button | 2 |

All buttons use `INPUT_PULLUP` (connect between pin and GND).
