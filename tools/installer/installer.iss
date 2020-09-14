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
Source: "..\Updater\out\win-x64\Nine Chronicles Updater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\windowsdesktop-runtime-3.1.3-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: ".\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#GameExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#GameExeName}"; IconFilename: "{app}\{#GameIconName}"; Tasks: CreateDesktopIcon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#GameExeName}"; Tasks: RegisterStartup

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  WinHttpReq: Variant;
begin
  if CurStep = ssInstall then
  begin
    Log('Install: Request Mixpanel.');
    WinHttpReq := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    WinHttpReq.Open('POST', 'https://api.mixpanel.com/track', false);
    WinHttpReq.SetRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
    WinHttpReq.Send('data={"event":"Installer/Start","properties":{"token":"80a1e14b57d050536185c7459d45195a","action":"start"}}');
    if WinHttpReq.ResponseText = 1 then begin
      Log('Mixpanel request success.');
    end else begin
      Log('Mixpanel request failed. ' + WinHttpReq.ResponseText);
    end;
  end;  

  if CurStep = ssPostInstall then
  begin                     
    Log('PostInstall: Request Mixpanel.');
    WinHttpReq := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    WinHttpReq.Open('POST', 'https://api.mixpanel.com/track', false);
    WinHttpReq.SetRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
    WinHttpReq.Send('data={"event":"Installer/End","properties":{"token":"80a1e14b57d050536185c7459d45195a","action":"end"}}');
    if WinHttpReq.ResponseText = 1 then begin
      Log('Mixpanel request success.');
    end else begin
      Log('Mixpanel request failed. ' + WinHttpReq.ResponseText);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  WinHttpReq: Variant;
begin
  if CurUninstallStep = usUninstall then
  begin      
    Log('UnInstall: Request Mixpanel.');
    WinHttpReq := CreateOleObject('WinHttp.WinHttpRequest.5.1');
    WinHttpReq.Open('POST', 'https://api.mixpanel.com/track', false);
    WinHttpReq.SetRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
    WinHttpReq.Send('data={"event":"Installer/Uninstall","properties":{"token":"80a1e14b57d050536185c7459d45195a","action":"uninstall"}}');
    if WinHttpReq.ResponseText = 1 then begin
      Log('Mixpanel request success.');
    end else begin
      Log('Mixpanel request failed. ' + WinHttpReq.ResponseText);
    end;
  end;
end;

[Run]
Filename: "{cmd}"; Parameters: "/C ""taskkill /im ""{#MyAppName}.exe"""" /f /t"

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

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C ""taskkill /im ""{#MyAppName}.exe"""" /f /t"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{%TEMP}\.net\Nine Chronicles"  
Type: filesandordirs; Name: "{%TEMP}\.net\Nine Chronicles Updater"
