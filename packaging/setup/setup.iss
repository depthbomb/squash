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
OutputBaseFilename={#ExeBaseName}-setup
SetupIconFile=..\icons\icon.ico
Compression=lzma2/ultra64
LZMAUseSeparateProcess=yes
SolidCompression=yes
ArchitecturesAllowed=x64compatible
MinVersion=10.0
WizardStyle=modern dynamic
ShowTasksTreeLines=yes
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName={#NameLong}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Icons]
Name: "{autoprograms}\{#Company}\{#NameLong}"; Filename: "{app}\{#ExeBaseName}.exe"; Comment: "Squash Start Menu shortcut"
Name: "{autodesktop}\{#NameLong}"; Filename: "{app}\{#ExeBaseName}.exe"; Tasks: desktopicon; Comment: "Squash Desktop shortcut"

[Run]
Filename: "{app}\{#ExeBaseName}.exe"; Description: "{cm:LaunchProgram,{#StringChange(NameLong, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Files]
Source: "..\..\build\jpackage\output\squash\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\vendor\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[UninstallDelete]
Type: dirifempty; Name: "{app}"
