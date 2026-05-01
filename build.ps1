param(
    [switch]$Install,
    [string]$Version = "dev"
)

if ($Install) {
    pip install pyserial pypresence requests rich pyinstaller pystray pillow
}

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue dist, build, *.spec

Write-Host "Building consoledeck.exe ..."
pyinstaller --onefile --noconsole `
    --hidden-import serial.tools.list_ports `
    --hidden-import pystray._win32 `
    --collect-all PIL `
    --icon=ConsoleDeck.ico `
    --name consoledeck `
    main.py

Write-Host "Building consoledeck-config.exe ..."
pyinstaller --onefile `
    --hidden-import serial.tools.list_ports `
    --icon=ConsoleDeck.ico `
    --name consoledeck-config `
    cli.py

if (Get-Command iscc -ErrorAction SilentlyContinue) {
    Write-Host "Building installer ..."
    iscc /DAppVersion=$Version installer.iss
} else {
    Write-Host "Inno Setup not found — installer skipped. Install from https://jrsoftware.org/isinfo.php"
}

Write-Host ""
Write-Host "Done. Output in .\dist\"
