import os
import sys
import json
import subprocess
import webbrowser
import serial
import time
import ctypes
import threading

try:
    import requests
except ImportError:
    requests = None

BAUDRATE = 9600

def find_arduino_port():
    from serial.tools import list_ports
    candidates = [p.device for p in list_ports.comports()]
    if not candidates:
        print("[ERROR] Kein COM-Port gefunden.")
        return None
    print(f"[INFO] Suche ConsoleDeck auf: {', '.join(candidates)}")
    for port in candidates:
        try:
            with serial.Serial(port, BAUDRATE, timeout=2) as ser:
                time.sleep(2)          # Arduino reset nach Port-Öffnung abwarten
                ser.reset_input_buffer()
                ser.write(b"WHO\n")
                response = ser.readline().decode("utf-8").strip()
                if response == "CONSOLEDECK":
                    print(f"[INFO] ConsoleDeck gefunden auf {port}")
                    return port
        except Exception:
            pass
    print("[ERROR] ConsoleDeck nicht gefunden. Arduino geflasht und verbunden?")
    return None

SCRIPT_DIR = os.path.dirname(sys.executable) if getattr(sys, "frozen", False) else os.path.dirname(os.path.abspath(__file__))
CONFIG_FILE = os.path.join(SCRIPT_DIR, "config.json")
CONFIG_DEFAULT_FILE = os.path.join(SCRIPT_DIR, "config.default.json")
DISCORD_AUTH_FILE = os.path.join(SCRIPT_DIR, "discord_auth.json")

_discord_client = None
_discord_reconnecting = False

# State of the selected button
pulsante_selezionato = None

# Internal state for VOLUME
last_volume_value = 0
is_muted = False

def load_config():
    if not os.path.exists(CONFIG_FILE):
        if os.path.exists(CONFIG_DEFAULT_FILE):
            import shutil
            shutil.copy(CONFIG_DEFAULT_FILE, CONFIG_FILE)
            print("[INFO] config.json aus config.default.json erstellt.")
        else:
            config = {f"BUTTON_{i}": {"type": "none", "value": ""} for i in range(1, 10)}
            save_config(config)
            print("[INFO] Leere config.json erstellt.")
    with open(CONFIG_FILE, "r") as f:
        return json.load(f)

def save_config(config):
    with open(CONFIG_FILE, "w") as f:
        json.dump(config, f, indent=2)

def _focus_window(title_hint):
    """Bring the first visible window whose title contains title_hint to the foreground.
    Returns True if found, False otherwise."""
    found = []
    WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_int, ctypes.c_int)

    def callback(hwnd, _):
        if ctypes.windll.user32.IsWindowVisible(hwnd):
            length = ctypes.windll.user32.GetWindowTextLengthW(hwnd)
            if length > 0:
                buf = ctypes.create_unicode_buffer(length + 1)
                ctypes.windll.user32.GetWindowTextW(hwnd, buf, length + 1)
                if title_hint.lower() in buf.value.lower():
                    found.append(hwnd)
        return True

    ctypes.windll.user32.EnumWindows(WNDENUMPROC(callback), 0)
    if not found:
        return False
    hwnd = found[0]
    ctypes.windll.user32.ShowWindow(hwnd, 9)  # SW_RESTORE
    ctypes.windll.user32.SetForegroundWindow(hwnd)
    return True


def simulate_hotkey(combo):
    VK = {
        'ctrl': 0x11, 'control': 0x11,
        'shift': 0x10,
        'alt': 0x12,
        'win': 0x5B,
        'space': 0x20, 'enter': 0x0D, 'tab': 0x09,
        'esc': 0x1B, 'escape': 0x1B,
        'backspace': 0x08,
        'del': 0x2E, 'delete': 0x2E,
        'up': 0x26, 'down': 0x28, 'left': 0x25, 'right': 0x27,
        'home': 0x24, 'end': 0x23, 'pageup': 0x21, 'pagedown': 0x22,
        **{f'f{i}': 0x6F + i for i in range(1, 13)},
    }
    vk_codes = []
    for key in [k.strip().lower() for k in combo.split('+')]:
        if key in VK:
            vk_codes.append(VK[key])
        elif len(key) == 1:
            vk_codes.append(ord(key.upper()))
        else:
            print(f"[ERROR] Unknown key in hotkey: '{key}'")
            return

    print(f"[DEBUG] Sending hotkey: {combo} → VK codes: {[hex(v) for v in vk_codes]}")

    KEYEVENTF_EXTENDEDKEY = 0x0001
    KEYEVENTF_KEYUP = 0x0002

    class KEYBDINPUT(ctypes.Structure):
        _fields_ = [
            ("wVk",         ctypes.c_ushort),
            ("wScan",       ctypes.c_ushort),
            ("dwFlags",     ctypes.c_ulong),
            ("time",        ctypes.c_ulong),
            ("dwExtraInfo", ctypes.c_size_t),  # ULONG_PTR — pointer-sized
        ]

    class _INPUTunion(ctypes.Union):
        _fields_ = [
            ("ki",   KEYBDINPUT),
            ("_pad", ctypes.c_byte * 32),  # MOUSEINPUT is 32 bytes on 64-bit → union must match
        ]

    class INPUT(ctypes.Structure):
        _fields_ = [("type", ctypes.c_ulong), ("union", _INPUTunion)]

    def make_input(vk, flags):
        inp = INPUT()
        inp.type = 1  # INPUT_KEYBOARD
        inp.union.ki.wVk = vk
        inp.union.ki.wScan = 0
        inp.union.ki.dwFlags = flags
        inp.union.ki.time = 0
        inp.union.ki.dwExtraInfo = 0
        return inp

    inputs = (
        [make_input(vk, KEYEVENTF_EXTENDEDKEY) for vk in vk_codes] +
        [make_input(vk, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP) for vk in reversed(vk_codes)]
    )
    arr = (INPUT * len(inputs))(*inputs)
    ctypes.windll.kernel32.SetLastError(0)
    sent = ctypes.windll.user32.SendInput(len(inputs), arr, ctypes.sizeof(INPUT))
    err = ctypes.windll.kernel32.GetLastError()
    print(f"[DEBUG] SendInput sent {sent}/{len(inputs)} events, GetLastError={err}")


def esegui_azione(azione):
    t = azione.get("type", "none")
    v = azione.get("value", "")

    if t == "link" and v:
        webbrowser.open(v)
    elif t == "exe" and v:
        focus = azione.get("focus", "")
        if focus and _focus_window(focus):
            return
        try:
            os.startfile(v)
        except Exception as e:
            print("Error opening file:", e)
    elif t == "play_pause":
        simulate_keypress(0xB3)
    elif t == "next_track":
        simulate_keypress(0xB0)
    elif t == "prev_track":
        simulate_keypress(0xB1)
    elif t == "stop":
        simulate_keypress(0xB2)
    elif t == "mute":
        simulate_keypress(0xAD)
    elif t == "hotkey" and v:
        simulate_hotkey(v)
    elif t == "discord_mute":
        discord_toggle_mute()
    elif t == "discord_deafen":
        discord_toggle_deafen()
    elif t == "discord_leave":
        discord_leave_channel()
    else:
        print("No action defined")

def ascolta_seriale(config):
    last_mtime = os.path.getmtime(CONFIG_FILE) if os.path.exists(CONFIG_FILE) else 0
    port = find_arduino_port()
    if not port:
        time.sleep(5)
        ascolta_seriale(config)
        return
    try:
        with serial.Serial(port, BAUDRATE, timeout=1) as ser:
            print(f"Connected to {port}")
            while True:
                if os.path.exists(CONFIG_FILE):
                    mtime = os.path.getmtime(CONFIG_FILE)
                    if mtime != last_mtime:
                        config = load_config()
                        last_mtime = mtime
                        print("[DEBUG] Config reloaded.")

                linea = ser.readline().decode('utf-8').strip()
                if linea:
                    print("Received:", linea)
                    if linea.startswith("VOLUME_"):
                        valore = linea.replace("VOLUME_", "")
                        gestisci_volume(valore)
                    elif linea == "MUTE":
                        gestisci_mute()
                    elif linea == "MEDIA":
                        gestisci_media()
                    elif linea in config:
                        esegui_azione(config[linea])
    except Exception as e:
        print(f"[ERROR] Serial port ({port}): {e}")
        time.sleep(5)
        ascolta_seriale(config)

def simulate_keypress(vk_code):
    KEYEVENTF_EXTENDEDKEY = 0x0001
    KEYEVENTF_KEYUP = 0x0002
    ctypes.windll.user32.keybd_event(vk_code, 0, KEYEVENTF_EXTENDEDKEY, 0)
    ctypes.windll.user32.keybd_event(vk_code, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0)

def gestisci_volume(value):
    global last_volume_value
    try:
        valore = int(value)
        delta = valore - last_volume_value
        if delta != 0:
            vk = 0xAF if delta > 0 else 0xAE  # VK_VOLUME_UP / VK_VOLUME_DOWN
            for _ in range(abs(delta)):
                simulate_keypress(vk)
            print(f"[DEBUG] Volume adjusted by {delta}")
        last_volume_value = valore
    except ValueError:
        print("[ERROR] Invalid volume value:", value)

def gestisci_mute():
    global is_muted
    simulate_keypress(0xAD)  # VK_VOLUME_MUTE
    is_muted = not is_muted
    print(f"[DEBUG] Mute toggled -> {'ON' if is_muted else 'OFF'}")

def gestisci_media():
    simulate_keypress(0xB3)  # VK_MEDIA_PLAY_PAUSE
    print("[DEBUG] Media play/pause triggered")

def load_discord_auth():
    if os.path.exists(DISCORD_AUTH_FILE):
        with open(DISCORD_AUTH_FILE, "r") as f:
            return json.load(f)
    return {}

def save_discord_auth(data):
    with open(DISCORD_AUTH_FILE, "w") as f:
        json.dump(data, f, indent=2)

def _discord_refresh_token(auth):
    if not requests:
        return None
    try:
        r = requests.post("https://discord.com/api/oauth2/token", data={
            "client_id":     auth["client_id"],
            "client_secret": auth["client_secret"],
            "grant_type":    "refresh_token",
            "refresh_token": auth["refresh_token"],
        })
        data = r.json()
        if "access_token" in data:
            auth["access_token"]  = data["access_token"]
            auth["refresh_token"] = data.get("refresh_token", auth["refresh_token"])
            save_discord_auth(auth)
            return data["access_token"]
    except Exception as e:
        print(f"[Discord] Token-Refresh fehlgeschlagen: {e}")
    return None

def discord_connect():
    global _discord_client
    auth = load_discord_auth()
    client_id = auth.get("client_id")
    if not client_id:
        return False
    try:
        from pypresence import Client
    except ImportError:
        print("[Discord] pypresence nicht installiert — pip install pypresence")
        return False

    try:
        _discord_client = Client(client_id)
        _discord_client.start()

        token = auth.get("access_token")
        if token:
            try:
                _discord_client.authenticate(token)
                print("[Discord] Verbunden.")
                return True
            except Exception:
                pass

        if auth.get("refresh_token") and auth.get("client_secret"):
            token = _discord_refresh_token(auth)
            if token:
                try:
                    _discord_client.authenticate(token)
                    print("[Discord] Verbunden (Token erneuert).")
                    return True
                except Exception:
                    pass

        client_secret = auth.get("client_secret")
        if not client_secret:
            print("[Discord] Client Secret fehlt — bitte in cli.py unter Einstellungen eintragen.")
            return False

        print("[Discord] Autorisierung nötig — Discord-Popup bestätigen ...")
        auth_resp = _discord_client.authorize(client_id, ["rpc", "rpc.voice.read", "rpc.voice.write"])
        code = auth_resp["data"]["code"]

        if not requests:
            print("[Discord] requests-Bibliothek fehlt — pip install requests")
            return False

        r = requests.post("https://discord.com/api/oauth2/token", data={
            "client_id":     client_id,
            "client_secret": client_secret,
            "grant_type":    "authorization_code",
            "code":          code,
            "redirect_uri":  "http://localhost",
        })
        data = r.json()
        if "access_token" not in data:
            print(f"[Discord] Token-Austausch fehlgeschlagen: {data}")
            return False

        auth["access_token"]  = data["access_token"]
        auth["refresh_token"] = data.get("refresh_token")
        save_discord_auth(auth)

        _discord_client.authenticate(data["access_token"])
        print("[Discord] Autorisiert und verbunden.")
        return True

    except Exception as e:
        print(f"[Discord] Verbindungsfehler: {e}")
        _discord_client = None
        return False

def _discord_watchdog():
    global _discord_reconnecting
    while True:
        time.sleep(10)
        if _discord_client is None and not _discord_reconnecting:
            _discord_reconnecting = True
            try:
                discord_connect()
            finally:
                _discord_reconnecting = False

def discord_start_watchdog():
    t = threading.Thread(target=_discord_watchdog, daemon=True)
    t.start()

def _discord_action(fn):
    global _discord_client
    if not _discord_client:
        print("[Discord] Nicht verbunden — warte auf Reconnect ...")
        return
    try:
        fn()
    except Exception as e:
        print(f"[Discord] Fehler: {e} — Reconnect wird versucht")
        _discord_client = None

def discord_toggle_mute():
    def action():
        current  = _discord_client.get_voice_settings()
        is_muted = current["data"]["mute"]
        _discord_client.set_voice_settings(mute=not is_muted)
        print(f"[Discord] Mute → {'AN' if not is_muted else 'AUS'}")
    _discord_action(action)

def discord_toggle_deafen():
    def action():
        current = _discord_client.get_voice_settings()
        is_deaf = current["data"]["deaf"]
        _discord_client.set_voice_settings(deaf=not is_deaf)
        print(f"[Discord] Deafen → {'AN' if not is_deaf else 'AUS'}")
    _discord_action(action)

def discord_leave_channel():
    def action():
        _discord_client.select_voice_channel(None)
        print("[Discord] Voice Channel verlassen.")
    _discord_action(action)


def seleziona_pulsante(btn):
    global pulsante_selezionato
    pulsante_selezionato = btn
    print(f"[DEBUG] Button selected: BUTTON_{btn}")

def deseleziona_pulsante():
    global pulsante_selezionato
    pulsante_selezionato = None    

def get_pulsante_selezionato():
    return pulsante_selezionato