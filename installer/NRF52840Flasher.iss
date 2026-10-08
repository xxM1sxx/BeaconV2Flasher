; Versi aplikasi TIDAK ditulis di sini: build-installer.ps1 membacanya dari
; NRF52840Flasher\Directory.Build.props lalu meneruskannya dengan
; /DMyAppVersion=<versi>. Nilai di bawah hanya cadangan bila .iss dikompilasi
; manual tanpa parameter tersebut.
#ifndef MyAppVersion
  #define MyAppVersion "1.1.0"
#endif

#define MyAppName "NRF52840 Flasher"
#define MyAppPublisher "Qimtronics"
#define MyAppCopyright "© Qimtronics"
#define MyAppExeName "NRF52840Flasher.exe"
; Bentuk 4 angka dari versi aplikasi, mis. 1.1.0 -> 1.1.0.0.
#define MyAppVersionInfo MyAppVersion + ".0"

[Setup]
AppId={{7B799A18-E576-4BB3-B289-CC43A78E301B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
; Ini yang tampil sebagai "Penerbit" dan "Pembuat" di daftar aplikasi Windows.
AppPublisher={#MyAppPublisher}
AppCopyright={#MyAppCopyright}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup - dibuat oleh {#MyAppPublisher}
VersionInfoCopyright={#MyAppCopyright}
DefaultDirName={autopf}\Qimtronics\NRF52840 Flasher
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=Qimtronics-NRF52840-Flasher-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\NRF52840Flasher\Assets\NRF52840 Flasher Pixel Icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
LicenseFile=..\licenses\THIRD_PARTY_NOTICES.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Buat shortcut di Desktop"; GroupDescription: "Shortcut tambahan:"; Flags: unchecked

[Files]
; Gagalkan build bila executable hasil publish bukan versi yang diminta, supaya
; installer tidak pernah memaketkan binary versi lama.
#if GetFileVersion(SourcePath + "..\artifacts\publish\" + MyAppExeName) != MyAppVersionInfo
  #error Executable hasil publish bukan versi MyAppVersion. Jalankan dotnet publish untuk versi ini lebih dulu.
#endif
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Jalankan {#MyAppName}"; Flags: nowait postinstall skipifsilent
