; Wadevo Installer Script (Inno Setup)
; ------------------------------------
; This script builds a real Windows installer (.exe) from the app's published output.
; Inno Setup itself is free: https://jrsoftware.org/isinfo.php
;
; HOW TO USE:
; 1. Publish the app first (from a terminal, in the Wadevo project folder):
;
;      dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
;
;    This produces a folder at:
;      bin\Release\net8.0-windows\win-x64\publish\
;
; 2. Install Inno Setup (free): https://jrsoftware.org/isdl.php
;
; 3. Open this file (Wadevo.iss) in Inno Setup and click Build > Compile.
;    (Or run from a command line: iscc Wadevo.iss)
;
; 4. The finished installer appears in the "installer\output" folder as
;    WadevoSetup.exe - that's the file you'd hand to another streamer.
;
; NOTE: Update MyAppVersion below to match WadevoBrand.Version before each build.

#define MyAppName "Wadevo"
#define MyAppVersion "0.3.0"
#define MyAppPublisher "Wadevo"
#define MyAppExeName "Wadevo.exe"
#define MyPublishDir "..\bin\Release\net8.0-windows\win-x64\publish"

[Setup]
AppId={{B4C1E7B0-6A3E-4C6D-9F1E-2D5F6A7B8C9D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Installs per-user by default - no admin rights required, matching how a free
; independent tool should behave. Change to "admin" if you'd rather install
; system-wide for all users on the machine.
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=WadevoSetup
SetupIconFile=..\Assets\Logos\WadevoLogo.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
; Pulls in everything from the published output folder (the exe, dlls, and
; any content files like fonts/logos that were marked CopyToOutputDirectory).
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName} now"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Removes Wadevo's saved data (commands, alerts, overlays, custom fonts, etc.)
; on uninstall. Comment this section out if you'd rather leave user data behind
; in case they reinstall later.
; Type: filesandordirs; Name: "{userappdata}\Wadevo"
