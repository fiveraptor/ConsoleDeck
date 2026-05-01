param(
    [switch]$Install
)

if ($Install) {
    pip install pyserial pypresence requests rich pyinstaller
}

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue dist, build, *.spec

Write-Host "Building consoledeck.exe ..."
pyinstaller --onefile --noconsole `
    --hidden-import serial.tools.list_ports `
    --name consoledeck `
    main.py

Write-Host "Building consoledeck-config.exe ..."
pyinstaller --onefile `
    --hidden-import serial.tools.list_ports `
    --name consoledeck-config `
    cli.py

Write-Host ""
Write-Host "Done. Executables are in .\dist\"
