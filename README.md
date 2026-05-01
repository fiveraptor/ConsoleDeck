# ConsoleDeck V2

A custom macro deck powered by an Arduino — control media, apps, hotkeys and Discord directly from physical buttons and an encoder knob.

> Based on [LucaDiLorenzo98/cd_v2_script](https://github.com/LucaDiLorenzo98/cd_v2_script) with significant improvements.

---

## Features

- 9 configurable buttons
- Rotary encoder for volume control (turn = volume, press = mute)
- 1 dedicated media button (play/pause)
- Automatic Arduino detection — no manual COM port configuration
- System tray icon with quick access to config and quit
- Discord integration (mute, deafen, leave channel)
- CLI configuration tool
- Windows installer with optional autostart

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
- **`ConsoleDeck-Setup-vX.X.X.exe`** — recommended, installs like any Windows app with Start Menu shortcuts and optional autostart
- **`ConsoleDeck-vX.X.X-portable.zip`** — no installation needed, just extract and run

### 3. Configure your buttons

Run `consoledeck-config.exe` (or open it from the tray icon menu) and follow the prompts to assign actions to each button.

**Available action types:**

| Type | Description |
|---|---|
| `link` | Open a URL in your browser |
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
| `homeassistant` | Control a Home Assistant device (toggle / turn_on / turn_off) |

### 4. Run ConsoleDeck

Double-click `consoledeck.exe`. It will automatically detect your Arduino and start running in the background.

A tray icon will appear in the system tray (bottom right). Right-click it to:
- **Open Config** — launch the configuration tool
- **Quit** — stop ConsoleDeck

If you used the installer, you can optionally enable autostart during setup so ConsoleDeck launches automatically with Windows.

---

## Home Assistant Integration

1. In Home Assistant: **Profil** (bottom left) → **Sicherheit** → **Long-Lived Access Token erstellen**
2. Run `consoledeck-config.exe` → **Settings → Home Assistant** and enter your HA URL and the token
3. When configuring a button, choose **HA** and enter the entity ID (e.g. `light.wohnzimmer`) and the action (`toggle`, `turn_on`, or `turn_off`)

---

## Discord Integration

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create a new application
2. Under **OAuth2**, copy the **Client ID** and **Client Secret**
3. Run `consoledeck-config.exe` → go to **Settings → Discord** and enter both values
4. Restart `consoledeck.exe` — a Discord authorization popup will appear on first connect

---

## Building from Source

```powershell
# Install dependencies
pip install pyserial pypresence requests rich pyinstaller pystray pillow

# Build both executables (add -Install to install dependencies automatically)
.\build.ps1

# Also build the installer (requires Inno Setup: https://jrsoftware.org/isinfo.php)
.\build.ps1 -Version 1.0.0
```

Output: `dist\consoledeck.exe`, `dist\consoledeck-config.exe`, `Output\ConsoleDeck-Setup-1.0.0.exe`

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

All buttons are wired with `INPUT_PULLUP` (connect between pin and GND).
