# ConsoleDeck V2

A custom macro deck powered by an Arduino — control media, apps, hotkeys and Discord directly from physical buttons and an encoder knob.

> Based on [LucaDiLorenzo98/cd_v2_script](https://github.com/LucaDiLorenzo98/cd_v2_script) with significant improvements.

---

## Features

- 9 configurable buttons
- Rotary encoder for volume control (turn = volume, press = mute)
- 1 dedicated media button (play/pause)
- Automatic Arduino detection — no manual COM port configuration
- Discord integration (mute, deafen, leave channel)
- CLI configuration tool

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

Go to the [Releases](../../releases) page and download the latest `consoledeck-vX.X.X.zip`.

Extract the ZIP — you should have:

```
consoledeck.exe          ← main app (runs in background)
consoledeck-config.exe   ← configuration tool
config.json              ← button config (auto-created on first run)
arduino/                 ← Arduino firmware
```

### 3. Configure your buttons

Run `consoledeck-config.exe` and follow the prompts to assign actions to each button.

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

### 4. Run ConsoleDeck

Double-click `consoledeck.exe`. It will automatically find your Arduino and start listening.

To run it at startup, create a shortcut to `consoledeck.exe` and place it in:
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

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
pip install pyserial pypresence requests rich pyinstaller

# Build both executables
.\build.ps1
# Output: dist\consoledeck.exe and dist\consoledeck-config.exe
```
