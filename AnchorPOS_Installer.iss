#define MyAppName "Anchor POS"
#define MyAppVersion "3.5.0"
#define MyAppPublisher "Anchor POS Team"
#define MyAppExeName "AnchorPOS.Desktop.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".apos"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
AppId={{A8B9C0D1-E2F3-4A5B-6C7D-8E9F0A1B2C3D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE.txt
InfoBeforeFile=INSTALLATION_NOTES.txt
OutputDir=installer_output
OutputBaseFilename=AnchorPOS_Setup_v3.5.0
; Only enable icon if it exists
SetupIconFile=icon.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main application files
Source: "src\AnchorPOS.Desktop\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Documentation
Source: "DEVELOPER_MANUAL.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "BLUETOOTH_PRINTER_SETUP.md"; DestDir: "{app}"; Flags: ignoreversion
; SQL Server Express Installer (must be in the project root)
Source: "sql_express_installer.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsSqlInstance

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
; Silent install SQL Server Express
Filename: "{tmp}\sql_express_installer.exe"; \
    Parameters: "/QS /ACTION=Install /FEATURES=SQL /INSTANCENAME=SQLEXPRESS /SQLSVCACCOUNT=""NT AUTHORITY\NetworkService"" /SQLSYSADMINACCOUNTS=""Builtin\Administrators"" /TCPENABLED=1 /NPENABLED=1 /IACCEPTSQLSERVERLICENSETERMS"; \
    Check: NeedsSqlInstance; \
    StatusMsg: "Installing SQL Server Express (this may take a few minutes)..."; \
    Flags: waituntilterminated

[Code]
function NeedsSqlInstance(): Boolean;
var
  Instances: String;
begin
  // Check if SQLEXPRESS instance name exists in registry
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', 'SQLEXPRESS', Instances) then
  begin
    // Instance found
    Result := False;
  end
  else
  begin
    // Instance not found, we need to install it
    Result := True;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Create database directory if it doesn't exist
    ForceDirectories(ExpandConstant('{userappdata}\AnchorPOS\Database'));
    
    // Optional: Run database initialization
    // Exec(ExpandConstant('{app}\{#MyAppExeName}'), '--init-db', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\AnchorPOS"
