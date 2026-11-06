; Vocaluxe Windows installer - Inno Setup 6.3 or newer (https://jrsoftware.org/isinfo.php)
;
; The installer packs a finished "InstallerWin" build from the Output folder.
; Usually it is created by build-installer.ps1, which builds the solution first.
; Manual compile (after building InstallerWin|x64 in Visual Studio):
;   ISCC.exe /DArch=x64 Installer\Vocaluxe.iss
;
; Optional defines:
;   /DArch=x64|x86         platform of the build in Output (default: x64)
;   /DSourceDir=<path>     folder with the build (default: ..\Output)
;   /DAppVersion=<string>  version shown to the user (default: ProductVersion of Vocaluxe.exe)

#if VER < EncodeVer(6, 3, 0)
  #error Inno Setup 6.3 or newer is required
#endif

#ifndef Arch
  #define Arch "x64"
#endif
#if Arch != "x64" && Arch != "x86"
  #error Arch must be x64 or x86
#endif

#ifndef SourceDir
  #define SourceDir AddBackslash(SourcePath) + "..\Output"
#endif
#define ExeFile SourceDir + "\Vocaluxe.exe"
#if !FileExists(ExeFile)
  #error Vocaluxe.exe not found in SourceDir - build the solution with configuration InstallerWin first
#endif

#define AppNumVersion GetVersionNumbersString(ExeFile)
#ifndef AppVersion
  #define AppVersion GetStringFileInfo(ExeFile, "ProductVersion")
#endif
#if AppVersion == ""
  #undef AppVersion
  #define AppVersion AppNumVersion
#endif

; Version for the file name: the part before the first "-" of the git describe version
; (e.g. "0.5.0" from "0.5.0-12-gabcdef"). Unversioned and nightly builds are named "Nightly".
#define DashPos Pos("-", AppVersion)
#if DashPos > 0
  #define ShortVersion Copy(AppVersion, 1, DashPos - 1)
#else
  #define ShortVersion AppVersion
#endif
#if ShortVersion == "" || Pos("0.0.0", ShortVersion) == 1 || Pos("Nightly", ShortVersion) == 1
  #define FileVersionName "Nightly"
#else
  #define FileVersionName ShortVersion
#endif

; .NET runtimes needed by Vocaluxe (framework dependent build)
#define DotNetChannel "10.0"
#if Arch == "x64"
  #define DotNetRoot "{commonpf64}\dotnet"
  #define OtherArchExcludes "\libs\unmanaged\gstreamer86,\libs\unmanaged\ffmpeg86,\libs\managed\x86"
#else
  #define DotNetRoot "{commonpf32}\dotnet"
  #define OtherArchExcludes "\libs\unmanaged\gstreamer64,\libs\unmanaged\ffmpeg64,\libs\managed\x64"
#endif

; Files that are created at runtime or belong to other platforms - never pack them
#define UserDataExcludes "\Linux,\Songs,\Logs,\Screenshots,\Photos,\Playlists,\WebCache,\CoverCache,\Config.xml,\HighscoreDB.sqlite,\CoverDB.sqlite,\Ultrastar.db,\WebServer.pfx,\WebServer.json,*.pdb,*.log"

[Setup]
AppId={{9873707B-271F-41D1-BCF9-5FA71A5AEBC9}
AppName=Vocaluxe
AppVersion={#AppVersion}
AppVerName=Vocaluxe {#AppVersion} ({#Arch})
AppPublisher=Vocaluxe Team
AppPublisherURL=https://vocaluxe.org
AppSupportURL=https://github.com/Vocaluxe/Vocaluxe/issues
AppUpdatesURL=https://github.com/Vocaluxe/Vocaluxe/releases
AppComments=Vocaluxe is an open source singing game inspired by SingStar(TM) and Ultrastar Deluxe.
VersionInfoVersion={#AppNumVersion}
VersionInfoProductName=Vocaluxe
VersionInfoDescription=Vocaluxe Setup
DefaultDirName={autopf}\Vocaluxe
UsePreviousAppDir=yes
DisableProgramGroupPage=yes
PrivilegesRequired=admin
MinVersion=10.0
#if Arch == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
LicenseFile=License.rtf
SetupIconFile={#SourceDir}\Vocaluxe.ico
UninstallDisplayIcon={app}\Vocaluxe.exe
UninstallDisplayName=Vocaluxe ({#Arch})
WizardStyle=modern
WizardImageFile=Graphics\WizardImage.bmp
WizardSmallImageFile=Graphics\WizardSmallImage.bmp
ShowLanguageDialog=auto
Compression=lzma2/max
SolidCompression=yes
OutputDir=Setup Files
OutputBaseFilename=Vocaluxe_{#FileVersionName}_Windows_{#Arch}_Installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
#if FileExists(CompilerPath + "Languages\German.isl")
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
#endif
#if FileExists(CompilerPath + "Languages\Czech.isl")
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
#endif
#if FileExists(CompilerPath + "Languages\Spanish.isl")
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
#endif
#if FileExists(CompilerPath + "Languages\Finnish.isl")
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
#endif
#if FileExists(CompilerPath + "Languages\French.isl")
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
#endif
#if FileExists(CompilerPath + "Languages\Hungarian.isl")
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"
#endif
#if FileExists(CompilerPath + "Languages\Italian.isl")
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
#endif
#if FileExists(CompilerPath + "Languages\Dutch.isl")
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
#endif
#if FileExists(CompilerPath + "Languages\Portuguese.isl")
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
#endif
#if FileExists(CompilerPath + "Languages\Russian.isl")
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
#endif
#if FileExists(CompilerPath + "Languages\Swedish.isl")
Name: "swedish"; MessagesFile: "compiler:Languages\Swedish.isl"
#endif
#if FileExists(CompilerPath + "Languages\Turkish.isl")
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
#endif

[CustomMessages]
RuntimeDownloadFailed=The .NET runtime required by Vocaluxe could not be downloaded:%n%n%1%n%nPlease check your internet connection or install the ".NET Runtime 10" and the "ASP.NET Core Runtime 10" ({#Arch}) manually from https://dotnet.microsoft.com/download/dotnet/10.0
RuntimeInstallFailed=Installing %1 failed (error %2).%n%nPlease install it manually from https://dotnet.microsoft.com/download/dotnet/10.0 and run this setup again.
german.RuntimeDownloadFailed=Die von Vocaluxe benötigte .NET-Laufzeitumgebung konnte nicht heruntergeladen werden:%n%n%1%n%nBitte prüfe die Internetverbindung oder installiere die ".NET Runtime 10" und die "ASP.NET Core Runtime 10" ({#Arch}) manuell von https://dotnet.microsoft.com/download/dotnet/10.0
german.RuntimeInstallFailed=Die Installation von %1 ist fehlgeschlagen (Fehler %2).%n%nBitte installiere sie manuell von https://dotnet.microsoft.com/download/dotnet/10.0 und starte das Setup erneut.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[InstallDelete]
; Native libraries change with every release - remove the old ones before copying the new set
Type: filesandordirs; Name: "{app}\libs"
Type: filesandordirs; Name: "{app}\Website"
; Content of Vocaluxe 0.4 that no longer exists
Type: filesandordirs; Name: "{app}\Themes\Ambient"
Type: files; Name: "{app}\Themes\Ambient.xml"
Type: filesandordirs; Name: "{app}\Cover\Ambient *"
Type: files; Name: "{app}\Cover\Ambient *.xml"
Type: filesandordirs; Name: "{app}\PartyModes\ChallengeMedley"
Type: files; Name: "{app}\PartyModes\ChallengeMedley.xml"
Type: filesandordirs; Name: "{app}\CoverCache"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "{#UserDataExcludes},{#OtherArchExcludes}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
; Songs can also be put next to the program (besides Documents\Vocaluxe\Songs)
Name: "{app}\Songs"; Permissions: users-modify

[Icons]
Name: "{autoprograms}\Vocaluxe"; Filename: "{app}\Vocaluxe.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Vocaluxe"; Filename: "{app}\Vocaluxe.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Vocaluxe.exe"; WorkingDir: "{app}"; Description: "{cm:LaunchProgram,Vocaluxe}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallDelete]
Type: filesandordirs; Name: "{app}\CoverCache"

[Code]
const
  DotNetChannel = '{#DotNetChannel}';
  NetCoreName = 'Microsoft.NETCore.App';
  AspNetCoreName = 'Microsoft.AspNetCore.App';
  NetCoreFile = 'dotnet-runtime-{#Arch}.exe';
  AspNetCoreFile = 'aspnetcore-runtime-{#Arch}.exe';

var
  DownloadPage: TDownloadWizardPage;
  NeedNetCore: Boolean;
  NeedAspNetCore: Boolean;

{ True if a value named like the wanted channel (e.g. "10.0.14") exists in the given registry key }
function RegHasChannel(const RootKey: Integer; const SubKey: String): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetValueNames(RootKey, SubKey, Names) then
    for I := 0 to GetArrayLength(Names) - 1 do
      if Pos(DotNetChannel + '.', Names[I]) = 1 then
      begin
        Result := True;
        Exit;
      end;
end;

{ Checks whether a shared framework (e.g. Microsoft.NETCore.App) of the wanted channel is installed }
function SharedFrameworkInstalled(const Name: String): Boolean;
var
  SubKey: String;
  FindRec: TFindRec;
begin
  SubKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\{#Arch}\sharedfx\' + Name;
  Result := RegHasChannel(HKLM32, SubKey);
  if not Result and IsWin64 then
    Result := RegHasChannel(HKLM64, SubKey);
  if not Result then
  begin
    { fallback: look into the dotnet folder directly }
    if FindFirst(ExpandConstant('{#DotNetRoot}\shared\') + Name + '\' + DotNetChannel + '.*', FindRec) then
    begin
      Result := True;
      FindClose(FindRec);
    end;
  end;
  Log(Format('%s %s.x installed: %d', [Name, DotNetChannel, Ord(Result)]));
end;

function RuntimeUrl(const Product: String): String;
begin
  { aka.ms always redirects to the latest patch of the channel }
  Result := 'https://aka.ms/dotnet/' + DotNetChannel + '/' + Product + '-win-{#Arch}.exe';
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpReady then
  begin
    NeedNetCore := not SharedFrameworkInstalled(NetCoreName);
    NeedAspNetCore := not SharedFrameworkInstalled(AspNetCoreName);
    if NeedNetCore or NeedAspNetCore then
    begin
      DownloadPage.Clear;
      if NeedNetCore then
        DownloadPage.Add(RuntimeUrl('dotnet-runtime'), NetCoreFile, '');
      if NeedAspNetCore then
        DownloadPage.Add(RuntimeUrl('aspnetcore-runtime'), AspNetCoreFile, '');
      DownloadPage.Show;
      try
        try
          DownloadPage.Download;
        except
          if DownloadPage.AbortedByUser then
            Log('Runtime download aborted by user.')
          else
            SuppressibleMsgBox(FmtMessage(CustomMessage('RuntimeDownloadFailed'), [GetExceptionMessage]), mbCriticalError, MB_OK, IDOK);
          Result := False;
        end;
      finally
        DownloadPage.Hide;
      end;
    end;
  end;
end;

{ Runs a downloaded runtime installer. Returns an error message or '' on success. }
function InstallRuntime(const FileName, Title: String; var NeedsRestart: Boolean): String;
var
  Params: String;
  ResultCode: Integer;
begin
  Result := '';
  if WizardSilent then
    Params := '/install /quiet /norestart'
  else
    Params := '/install /passive /norestart';
  Log('Installing ' + Title);
  if not Exec(ExpandConstant('{tmp}\') + FileName, Params, '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
    Result := FmtMessage(CustomMessage('RuntimeInstallFailed'), [Title, SysErrorMessage(ResultCode)])
  else if (ResultCode = 3010) or (ResultCode = 1641) then
    NeedsRestart := True
  else if ResultCode <> 0 then
    Result := FmtMessage(CustomMessage('RuntimeInstallFailed'), [Title, IntToStr(ResultCode)]);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if NeedNetCore then
    Result := InstallRuntime(NetCoreFile, '.NET Runtime ' + DotNetChannel + ' ({#Arch})', NeedsRestart);
  if (Result = '') and NeedAspNetCore then
    Result := InstallRuntime(AspNetCoreFile, 'ASP.NET Core Runtime ' + DotNetChannel + ' ({#Arch})', NeedsRestart);
end;
