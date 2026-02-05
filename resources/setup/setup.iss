#include "environment.iss"

[Setup]
AppId={#AppId}
AppName={#NameLong}
AppVersion={#Version}
AppVerName={#NameLong}
AppPublisher={#Company}
AppPublisherURL={#RepoUrl}
AppSupportURL={#IssuesUrl}
AppUpdatesURL={#ReleasesUrl}
AppCopyright={#Copyright}
VersionInfoVersion={#Version}
DefaultGroupName={#NameLong}
DefaultDirName={autopf}\{#Company}\{#NameLong}
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
AllowNoIcons=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\build
OutputBaseFilename=squash-setup
SetupIconFile=..\icon.ico
Compression=lzma2/ultra64
LZMAUseSeparateProcess=yes
SolidCompression=yes
ArchitecturesAllowed=x64compatible
MinVersion=10.0
WizardStyle=modern dynamic
ShowTasksTreeLines=yes
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName={#NameLong}
ChangesEnvironment=true

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
    if (CurStep = ssPostInstall) and IsTaskSelected('envPath')
    then EnvAddPath(ExpandConstant('{app}'));
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
    if CurUninstallStep = usPostUninstall
    then EnvRemovePath(ExpandConstant('{app}'));
end;

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: envPath; Description: "Add to system PATH"

[Files]
Source: "..\..\build\squash.dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\vendor\*"; DestDir: "{app}\vendor"; Flags: ignoreversion recursesubdirs
Source: "..\..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[UninstallDelete]
Type: dirifempty; Name: "{app}"
