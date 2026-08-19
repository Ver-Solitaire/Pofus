; Installeur Pofus — compile avec Inno Setup 6 (ISCC.exe Pofus.iss)
; La sortie de `dotnet publish` doit exister dans ..\publish avant de compiler.

#define AppName "Pofus"
#define AppVersion "1.0.0"
#define AppPublisher "Ver-Solitaire"
#define AppExeName "Pofus.App.exe"
#define AppUrl "https://github.com/Ver-Solitaire/Pofus"

[Setup]
AppId={{7C3F1B94-2A5E-4E1D-9E33-0F6B2D5A9C41}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppSupportURL={#AppUrl}
AppUpdatesURL={#AppUrl}/releases

; Installation par utilisateur : ni UAC ni droits administrateur requis,
; ce qui convient à un outil personnel et évite une élévation inutile.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=Pofus-{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; GroupDescription: "Raccourcis :"
Name: "startupicon"; Description: "Lancer Pofus au démarrage de Windows"; GroupDescription: "Démarrage :"; Flags: unchecked

[Files]
; Tout le dossier publié, runtime .NET inclus : aucun prérequis à installer.
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Désinstaller {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Lancer {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Les fichiers créés à l'exécution (configuration, journaux, profil du
; navigateur embarqué) vivent hors du dossier d'installation : on les retire
; explicitement pour ne rien laisser derrière.
Type: filesandordirs; Name: "{userappdata}\Pofus"

[Code]
// Pofus lit les ateliers DofusBook via WebView2, présent par défaut sur
// Windows 11 mais pas forcément sur Windows 10 : on prévient plutôt que de
// laisser l'import échouer sans explication une fois installé.
function IsWebView2Installed(): Boolean;
var
  Version: String;
begin
  Result :=
    RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) or
    RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) or
    RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsWebView2Installed() then
    MsgBox('Le composant « Microsoft Edge WebView2 Runtime » est introuvable.' + #13#10 +
           'Pofus s''installera et fonctionnera, mais l''import d''un atelier DofusBook restera indisponible' + #13#10 +
           'tant que ce composant n''est pas installé (gratuit, fourni par Microsoft).',
           mbInformation, MB_OK);
end;
