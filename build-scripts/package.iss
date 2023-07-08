


#define MyAppName "DataLayer Storage Upload Plugin"
#define MyAppVersion "1.0"
#define MyAppPublisher "Taylor Digital Services"
#define MyAppURL "https://datalayer.storage"
#define MyAppExeName "DataLayerStorageUploadService.exe"
#define MyAppSourcePath "C:\Users\14434\source\repos\ChiaNetwork\datalayer-storage-upload-service\dist"

[Setup]
AppId={{Some Unique Identifier, like GUID}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DisableDirPage=no
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#MyAppSourcePath}
OutputBaseFilename=DatalayerStorageInstaller
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "runasservice"; Description: "Run {#MyAppName} as a Windows service"; GroupDescription: "Additional Tasks"; Flags: unchecked

[Files]
Source: "{#MyAppSourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "update_config.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "InstallService.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "UnInstallService.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Code]
var
  ClientAccessKeyPage: TInputQueryWizardPage;
  InstallDirPage: TInputDirWizardPage;
  ExePath: string;
  UpdateConfigExe: string;
  RunAsService: Boolean;

procedure InitializeWizard;
begin
  ClientAccessKeyPage := CreateInputQueryPage(wpWelcome,
    'Client Access Key', 'Please enter your Client Access Key:',
    'Please enter the Client Access Key, then click Next. You must create your access keys at https://datalayer.storage and place them here to use this software.');
  ClientAccessKeyPage.Add('Access Key:', False);
  ClientAccessKeyPage.Add('Secret Key:', False);
end;

procedure SaveConfig(AccessKey: string; SecretKey: string);
var
  ConfigFile: string;
  ConfigContents: string;
begin
  ConfigFile := ExpandConstant('{%USERPROFILE}\.dlaas\config.yaml');
  ConfigContents :=
    'CLIENT_ACCESS_KEY: ' + AccessKey + #13#10 +
    'CLIENT_SECRET_ACCESS_KEY: ' + SecretKey + #13#10 +
    'RPC_HOST: localhost' + #13#10 +
    'RPC_WALLET_PORT: 9256' + #13#10 +
    'RPC_DATALAYER_PORT: 8562' + #13#10 +
    'PORT: 41410';

  ForceDirectories(ExtractFilePath(ConfigFile));
  SaveStringToFile(ConfigFile, ConfigContents, False);
end;

function RegisterService(): Boolean;
var
  ResultCode: Integer;
  PowerShellPath: string;
  ScriptPath: string;
  ExePath: string;
  ServiceName: string;
begin
  PowerShellPath := ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe');
  ScriptPath := ExpandConstant('{app}\InstallService.ps1');  // replace with actual path if different
  ExePath := ExpandConstant('{app}\DataLayerStorageUploadService.exe');
  ServiceName := 'DataLayerStorage';

  if not Exec(PowerShellPath, '-ExecutionPolicy Bypass -File "' + ScriptPath + '" -PathToExe "' + ExePath + '"', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('An error occurred while executing the service installation. Error code: ' + IntToStr(ResultCode), mbError, MB_OK);
    Result := False;
    Exit;
  end;

  Result := True;
end;

function DeregisterService(): Boolean;
var
  ResultCode: Integer;
  PowerShellPath: string;
  ScriptPath: string;
  ExePath: string;
begin
  PowerShellPath := ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe');
  ScriptPath := ExpandConstant('{app}\UnInstallService.ps1');  // replace with actual path if different
  ExePath := ExpandConstant('{app}\DataLayerStorageUploadService.exe');

  if not Exec(PowerShellPath, '-ExecutionPolicy Bypass -File "' + ScriptPath + '" -PathToExe "' + ExePath + '"', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('An error occurred while executing the service installation. Error code: ' + IntToStr(ResultCode), mbError, MB_OK);
    Result := False;
    Exit;
  end;

  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsTaskSelected('runasservice') then
      RegisterService(); // Call the RegisterService function if the checkbox is checked
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
  begin
    WizardForm.TasksList.Checked[1] := True; // Index 1 corresponds to the "runasservice" task
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  AccessKey: string;
  SecretKey: string;
  ResultCode: Integer;
begin
  Result := True;

  if CurPageID = ClientAccessKeyPage.ID then
  begin
    AccessKey := ClientAccessKeyPage.Values[0];
    SecretKey := ClientAccessKeyPage.Values[1];

    if (AccessKey = '') or (SecretKey = '') then
    begin
      MsgBox('You must enter both the Client Access Key and the Client Secret Key.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    SaveConfig(AccessKey, SecretKey);
  end
  else if CurPageID = wpReady then
  begin
    UpdateConfigExe := ExpandConstant('{app}\update_config.exe');
  end
  else if CurPageID = wpFinished then
  begin
    if not Exec(UpdateConfigExe, '', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('An error occurred while executing the update_config.exe.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    // Display the final message box
    MsgBox('A modification was made to ~/.chia/mainnet/config/config.yaml to register this software with Chia. Please restart Chia to begin using this software.', mbInformation, MB_OK);
    if MsgBox('Installation completed successfully. Do you want to restart Windows now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      // Restart Windows
      RestartComputer;
    end;
  end;
end;
