[Setup]
AppName=ConsoleDeck
AppVersion={#AppVersion}
AppPublisher=fiveraptor
AppPublisherURL=https://github.com/fiveraptor/ConsoleDeck
DefaultDirName={localappdata}\ConsoleDeck
DefaultGroupName=ConsoleDeck
OutputBaseFilename=ConsoleDeck-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
WizardStyle=modern
UninstallDisplayName=ConsoleDeck
SetupIconFile=ConsoleDeck.ico

[Files]
Source: "dist\ConsoleDeck.exe";      DestDir: "{app}"; Flags: ignoreversion
Source: "config.default.json";        DestDir: "{app}"; Flags: ignoreversion
Source: "console_deck_v2_arduino_code\*"; DestDir: "{app}\arduino"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\ConsoleDeck";         Filename: "{app}\ConsoleDeck.exe"
Name: "{group}\Uninstall ConsoleDeck"; Filename: "{uninstallexe}"

[Tasks]
Name: "startup"; Description: "ConsoleDeck automatisch mit Windows starten"; GroupDescription: "Zusätzliche Optionen:"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ConsoleDeck"; ValueData: """{app}\ConsoleDeck.exe"""; Flags: uninsdeletevalue; Tasks: startup
