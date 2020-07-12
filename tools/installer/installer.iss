#define MyAppName "Nine Chronicles"
#define MyAppPublisher "Nine Corporation"
#define MyAppURL "https://nine-chronicles.com/"
#define GameExeName "Nine Chronicles.exe"
#define GameIconName "icon.ico"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{789AAC8F-6C36-4A84-ABB9-4FEA48EA924C}}
AppName={#MyAppName}
AppVersion=
AppVerName={#MyAppName}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}{\}Programs{\}{#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename="Nine Chronicles Installer"
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
CreateDesktopIcon=Create a &desktop icon
RegisterStartup=Register Nine Chronicles to the startup program

[Tasks]
Name: "CreateDesktopIcon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "RegisterStartup"; Description: "{cm:RegisterStartup}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\..\NineChronicles.Launcher\out\win-x64\Nine Chronicles Updater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\NineChronicles.Launcher\Launcher\icon.ico"; DestDir: "{app}"
Source: ".\windowsdesktop-runtime-3.1.3-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: ".\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#GameExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#GameExeName}"; IconFilename: "{app}\{#GameIconName}"; Tasks: CreateDesktopIcon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#GameExeName}"; Tasks: RegisterStartup

[Run]
Filename: {tmp}\windowsdesktop-runtime-3.1.3-win-x64.exe; \
    Parameters: "/q /norestart"; \
    StatusMsg: "Installing .NET Core Runtime..."

[Run]
Filename: {tmp}\vc_redist.x64.exe; \
    Parameters: "/q /norestart"; \
    StatusMsg: "Installing VC++ Redistributables..."

[Run]
Filename: {app}\Nine Chronicles Updater.exe; \
    StatusMsg: "Updating Nine Chonicles Executables..."

[Run]
Filename: "{app}\{#GameExeName}"; Flags: nowait postinstall skipifsilent


[InstallDelete] 
Type: filesandordirs; Name: "{%TEMP}\.net\Nine Chronicles"  
Type: filesandordirs; Name: "{%TEMP}\.net\Nine Chronicles Updater"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{%TEMP}\.net\Nine Chronicles"  
Type: filesandordirs; Name: "{%TEMP}\.net\Nine Chronicles Updater"
