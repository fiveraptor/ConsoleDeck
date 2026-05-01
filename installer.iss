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
Source: "dist\consoledeck.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "dist\consoledeck-config.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "config.default.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "ConsoleDeck.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "console_deck_v2_arduino_code\*"; DestDir: "{app}\arduino"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\ConsoleDeck"; Filename: "{app}\consoledeck.exe"; IconFilename: "{app}\ConsoleDeck.ico"
Name: "{group}\Uninstall ConsoleDeck"; Filename: "{uninstallexe}"

[Tasks]
Name: "startup"; Description: "Start ConsoleDeck automatically with Windows"; GroupDescription: "Additional options:"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ConsoleDeck"; ValueData: """{app}\consoledeck.exe"""; Flags: uninsdeletevalue; Tasks: startup
