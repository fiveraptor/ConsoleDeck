param(
    [string]$Version = "dev"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$proj = "ConsoleDeckApp\ConsoleDeck\ConsoleDeck.csproj"

Write-Host "==> Building ConsoleDeck $Version ..."
dotnet publish $proj `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:Version=$Version `
    -o dist

Write-Host ""
Write-Host "==> Built: dist\ConsoleDeck.exe  ($([Math]::Round((Get-Item dist\ConsoleDeck.exe).Length / 1MB, 1)) MB)"
Write-Host ""

if (Get-Command iscc -ErrorAction SilentlyContinue) {
    Write-Host "==> Building installer ..."
    iscc /DAppVersion=$Version installer.iss
    Write-Host "==> Installer: Output\ConsoleDeck-Setup-$Version.exe"
} else {
    Write-Host "[skip] Inno Setup not found — https://jrsoftware.org/isinfo.php"
}

Write-Host ""
Write-Host "Done."
