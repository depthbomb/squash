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
WizardStyle=modern dark
ShowTasksTreeLines=yes
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName={#NameLong}
ChangesEnvironment=true

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
    if (CurStep = ssPostInstall) and WizardIsTaskSelected('envPath')
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
Source: "..\..\build\squash.dist\_bz2.pyd"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\build\squash.dist\_lzma.pyd"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\build\squash.dist\python313.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\build\squash.dist\squash.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\build\squash.dist\vcruntime140.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[UninstallDelete]
Type: dirifempty; Name: "{app}"
