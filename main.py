import os
import sys
import threading
import subprocess


def _create_icon_image():
    from PIL import Image, ImageDraw
    size = 64
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, size - 1, size - 1], radius=10, fill=(30, 30, 30))
    cell, gap = 16, 4
    for row in range(3):
        for col in range(3):
            x = gap + col * (cell + gap)
            y = gap + row * (cell + gap)
            d.rounded_rectangle([x, y, x + cell - 1, y + cell - 1], radius=3, fill=(0, 120, 215))
    return img


def _open_config():
    base = os.path.dirname(sys.executable) if getattr(sys, "frozen", False) else os.path.dirname(os.path.abspath(__file__))
    exe = os.path.join(base, "consoledeck-config.exe")
    if os.path.exists(exe):
        subprocess.Popen([exe])
    else:
        subprocess.Popen([sys.executable, os.path.join(base, "cli.py")])


def _quit(icon):
    icon.stop()
    os._exit(0)


def main():
    from logic import load_config, ascolta_seriale, discord_connect, discord_start_watchdog

    config = load_config()
    discord_connect()
    discord_start_watchdog()

    threading.Thread(target=ascolta_seriale, args=(config,), daemon=True).start()

    try:
        import pystray
        icon = pystray.Icon(
            "consoledeck",
            _create_icon_image(),
            "ConsoleDeck",
            menu=pystray.Menu(
                pystray.MenuItem("Open Config", lambda icon, item: _open_config()),
                pystray.MenuItem("Quit", lambda icon, item: _quit(icon)),
            ),
        )
        icon.run()
    except ImportError:
        threading.Event().wait()


if __name__ == "__main__":
    main()
