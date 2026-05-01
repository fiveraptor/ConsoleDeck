from logic import load_config, ascolta_seriale, discord_connect, discord_start_watchdog

def main():
    config = load_config()
    discord_connect()
    discord_start_watchdog()
    ascolta_seriale(config)

if __name__ == "__main__":
    main()
