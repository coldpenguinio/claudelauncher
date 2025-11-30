; ClaudeLauncher Inno Setup Script
; Requires Inno Setup 6.x (https://jrsoftware.org/isinfo.php)

#define MyAppName "ClaudeLauncher"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Cold Penguin"
#define MyAppURL "https://github.com/yourusername/ClaudeLauncher"
#define MyAppExeName "ClaudeLauncher.exe"

[Setup]
AppId={{8A2B4F5E-3C1D-4E6F-9A8B-7C2D1E0F3A4B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE.txt
OutputDir=Output
OutputBaseFilename=ClaudeLauncher-Setup-{#MyAppVersion}
SetupIconFile=..\ClaudeLauncher\Resources\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start with Windows"; GroupDescription: "Startup:"

[Files]
; Main application files - publish output
Source: "..\ClaudeLauncher\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\ClaudeLauncher"

[Code]
// Close running instance before install/uninstall
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  // Try to close running instance gracefully
  if CheckForMutexes('ClaudeLauncherMutex') then
  begin
    MsgBox('ClaudeLauncher is currently running. Please close it before continuing.', mbInformation, MB_OK);
    Result := False;
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  if CheckForMutexes('ClaudeLauncherMutex') then
  begin
    MsgBox('ClaudeLauncher is currently running. Please close it before uninstalling.', mbInformation, MB_OK);
    Result := False;
  end;
end;
