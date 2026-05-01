#!/usr/bin/env python3
"""ConsoleDeck V2 — CLI configuration tool"""

import json
import os
import sys

try:
    from rich.console import Console
    from rich.table import Table
    from rich.text import Text
    from rich.prompt import Prompt
    from rich.align import Align
    from rich.rule import Rule
    from rich import box
except ImportError:
    print("Error: 'rich' library not found. Install it with:  pip install rich")
    sys.exit(1)

SCRIPT_DIR = os.path.dirname(sys.executable) if getattr(sys, "frozen", False) else os.path.dirname(os.path.abspath(__file__))
CONFIG_FILE = os.path.join(SCRIPT_DIR, "config.json")
DISCORD_AUTH_FILE = os.path.join(SCRIPT_DIR, "discord_auth.json")

console = Console()

TYPE_COLOR = {
    "link":         "cyan",
    "exe":          "green",
    "play_pause":   "blue",
    "next_track":   "blue",
    "prev_track":   "blue",
    "stop":         "blue",
    "mute":         "magenta",
    "hotkey":       "yellow",
    "discord_mute":   "bright_magenta",
    "discord_deafen": "bright_magenta",
    "discord_leave":  "red",
    "none":           "bright_black",
}
TYPE_LABEL = {
    "link":           "LINK",
    "exe":            "FILE",
    "play_pause":     "PLAY",
    "next_track":     "NEXT",
    "prev_track":     "PREV",
    "stop":           "STOP",
    "mute":           "MUTE",
    "hotkey":         "KEY ",
    "discord_mute":   "MUTE",
    "discord_deafen": "DEAF",
    "discord_leave":  "LEAV",
    "none":           "NONE",
}


def load_config():
    if os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, "r") as f:
            return json.load(f)
    config = {f"BUTTON_{i}": {"type": "none", "value": ""} for i in range(1, 10)}
    config["BUTTON_1"] = {"type": "link", "value": "https://www.youtube.com"}
    return config


def save_config(config):
    with open(CONFIG_FILE, "w") as f:
        json.dump(config, f, indent=2)


def load_discord_auth():
    if os.path.exists(DISCORD_AUTH_FILE):
        with open(DISCORD_AUTH_FILE, "r") as f:
            return json.load(f)
    return {}


def save_discord_auth(data):
    with open(DISCORD_AUTH_FILE, "w") as f:
        json.dump(data, f, indent=2)


def shorten(value, max_len=18):
    if not value:
        return ""
    v = value.replace("https://", "").replace("http://", "")
    return v if len(v) <= max_len else v[: max_len - 1] + "…"


def cell_text(i, config):
    key = f"BUTTON_{i}"
    data = config.get(key, {"type": "none", "value": ""})
    t = data.get("type", "none")
    v = data.get("value", "")

    color = TYPE_COLOR.get(t, "bright_black")
    label = TYPE_LABEL.get(t, "NONE")

    # For exe, prefer the focus hint as the display name if set
    if t == "exe":
        preview = data.get("focus", "") or shorten(v)
    else:
        preview = shorten(v)

    text = Text(justify="center")
    text.append(f"{i}\n", style="bold white")
    text.append(f"[{label}]", style=f"bold {color}")
    text.append(f"\n{preview}" if preview else "\n ", style=f"dim {color}")
    return text


def draw_grid(config, message=None):
    console.clear()
    console.print()
    console.print(Align.center(
        "[bold white]ConsoleDeck V2[/bold white]  [dim]—  Button Configuration[/dim]"
    ))
    console.print()

    table = Table(box=box.ROUNDED, show_header=False, padding=(1, 4), show_edge=True)
    for _ in range(3):
        table.add_column(justify="center", min_width=20)

    for row in range(3):
        table.add_row(*(cell_text(row * 3 + col + 1, config) for col in range(3)))

    console.print(Align.center(table))
    console.print()
    console.print(Align.center(
        "[dim][ MEDIA button — always play/pause, not configurable ][/dim]"
    ))
    console.print()

    if message:
        console.print(Align.center(f"[bold green]✓  {message}[/bold green]"))
        console.print()

    console.print(Align.center(
        "[dim]Enter [bold white]1–9[/bold white] to configure a button"
        ",  [bold white]s[/bold white] for settings"
        ",  [bold white]q[/bold white] to quit[/dim]"
    ))
    console.print()


def configure_button(key, config):
    num = key.split("_")[1]
    data = config.get(key, {"type": "none", "value": ""})
    t = data.get("type", "none")
    v = data.get("value", "")
    focus = data.get("focus", "")

    color = TYPE_COLOR.get(t, "bright_black")
    label = TYPE_LABEL.get(t, "NONE")

    console.print()
    console.print(Rule(f"[bold yellow]Button {num}[/bold yellow]"))
    console.print()

    current_display = focus or v or "—"
    console.print(
        f"  Current  [bold {color}][{label}][/bold {color}]"
        f"  [dim {color}]{current_display}[/dim {color}]"
    )
    console.print()

    console.print("  [dim]── Launch ──────────────────────────────────[/dim]")
    console.print("  [cyan bold]1[/cyan bold]  LINK    Open a URL in the browser")
    console.print("  [green bold]2[/green bold]  FILE    Open a file or shortcut (.exe, .lnk, ...)")
    console.print("  [dim]── Media ───────────────────────────────────[/dim]")
    console.print("  [blue bold]3[/blue bold]  PLAY    Play / Pause")
    console.print("  [blue bold]4[/blue bold]  NEXT    Next track")
    console.print("  [blue bold]5[/blue bold]  PREV    Previous track")
    console.print("  [blue bold]6[/blue bold]  STOP    Stop")
    console.print("  [dim]── System ──────────────────────────────────[/dim]")
    console.print("  [magenta bold]7[/magenta bold]  MUTE    Toggle system mute")
    console.print("  [yellow bold]8[/yellow bold]  KEY     Send a keyboard shortcut  [dim](e.g. ctrl+shift+m)[/dim]")
    console.print("  [dim]── Discord ─────────────────────────────────[/dim]")
    console.print("  [bright_magenta bold]d[/bright_magenta bold]  MUTE    Discord Mute/Unmute")
    console.print("  [bright_magenta bold]e[/bright_magenta bold]  DEAF    Discord Deafen/Undeafen")
    console.print("  [red bold]f[/red bold]  LEAV    Discord Voice Channel verlassen")
    console.print("  [dim]───────────────────────────────────────────[/dim]")
    console.print("  [bright_black]9  NONE    No action[/bright_black]")
    console.print("  [bright_black]0  Cancel[/bright_black]")
    console.print()

    choice = Prompt.ask(
        "  Type",
        choices=["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "d", "e", "f"],
        default="0",
        show_choices=False,
    )

    if choice == "0":
        return config, None

    new_focus = ""

    if choice == "1":
        new_type = "link"
        default = v if t == "link" else "https://"
        console.print()
        new_value = Prompt.ask("  URL", default=default).strip()

    elif choice == "2":
        new_type = "exe"
        default = v if t == "exe" else ""
        console.print()
        new_value = Prompt.ask(
            "  Path to file or shortcut  [dim](paste with Ctrl+V)[/dim]", default=default
        ).strip()
        console.print()
        console.print(
            "  [dim]Window focus hint — enter a word from the app's window title.[/dim]"
        )
        console.print(
            "  [dim]When the button is pressed, that window gets focused instead of opening a new instance.[/dim]"
        )
        console.print(
            "  [dim]Leave blank to always launch the file.[/dim]"
        )
        new_focus = Prompt.ask(
            "  Focus hint", default=focus if t == "exe" else ""
        ).strip()

    elif choice == "3":
        new_type, new_value = "play_pause", ""
    elif choice == "4":
        new_type, new_value = "next_track", ""
    elif choice == "5":
        new_type, new_value = "prev_track", ""
    elif choice == "6":
        new_type, new_value = "stop", ""
    elif choice == "7":
        new_type, new_value = "mute", ""
    elif choice == "8":
        new_type = "hotkey"
        default = v if t == "hotkey" else ""
        console.print()
        console.print(
            "  [dim]Enter a key combination, e.g.  ctrl+shift+m  or  ctrl+alt+d[/dim]"
        )
        console.print(
            "  [dim]Supported modifiers: ctrl, shift, alt, win[/dim]"
        )
        new_value = Prompt.ask("  Shortcut", default=default).strip().lower()
    elif choice == "d":
        new_type, new_value = "discord_mute", ""
    elif choice == "e":
        new_type, new_value = "discord_deafen", ""
    elif choice == "f":
        new_type, new_value = "discord_leave", ""
    else:
        new_type, new_value = "none", ""

    entry = {"type": new_type, "value": new_value}
    if new_focus:
        entry["focus"] = new_focus

    config[key] = entry
    save_config(config)
    return config, f"Button {num} saved."


def configure_discord():
    auth = load_discord_auth()
    console.clear()
    console.print()
    console.print(Rule("[bold yellow]Discord Integration[/bold yellow]"))
    console.print()

    client_id     = auth.get("client_id", "")
    client_secret = auth.get("client_secret", "")

    console.print(f"  Client ID:     [cyan]{client_id or '[dim]nicht gesetzt[/dim]'}[/cyan]")
    masked = ("*" * len(client_secret)) if client_secret else "[dim]nicht gesetzt[/dim]"
    console.print(f"  Client Secret: [green]{masked}[/green]")
    status = "[green]gespeichert[/green]" if auth.get("access_token") else "[dim]noch nicht autorisiert[/dim]"
    console.print(f"  Token:         {status}")
    console.print()
    console.print("  [dim]Client ID und Secret findest du unter:[/dim]")
    console.print("  [dim]discord.com/developers/applications → deine App → OAuth2[/dim]")
    console.print("  [dim]Redirect URI in der App eintragen:  http://localhost[/dim]")
    console.print()

    new_id     = Prompt.ask("  Client ID    ", default=client_id).strip()
    new_secret = Prompt.ask("  Client Secret", default=client_secret, password=True).strip()

    changed = new_id != client_id or new_secret != client_secret
    if changed:
        auth["client_id"]     = new_id
        auth["client_secret"] = new_secret
        auth.pop("access_token",  None)
        auth.pop("refresh_token", None)
        save_discord_auth(auth)
        return "Discord-Einstellungen gespeichert. main.py neu starten zum Autorisieren."
    return None


def main():
    config = load_config()
    message = None

    while True:
        draw_grid(config, message)
        message = None

        try:
            raw = console.input("  [dim]>[/dim] ").strip().lower()
        except (KeyboardInterrupt, EOFError):
            break

        if raw == "q":
            break
        elif raw == "s":
            message = configure_discord()
        elif raw.isdigit() and 1 <= int(raw) <= 9:
            config, message = configure_button(f"BUTTON_{raw}", config)
        else:
            message = "Ungültig — 1 bis 9, s für Einstellungen, oder q zum Beenden."

    console.print()
    console.print("  [dim]Goodbye.[/dim]")
    console.print()


if __name__ == "__main__":
    main()
