; Inno Setup 鈥?Command Palette extension (WinGet EXE)
#define AppVersion "0.1.2.0"
#define ExtensionName "HardwareMonitorExtension"
#define DisplayName "Hardware Monitor (PawnIO)"
#define DeveloperName "zhang-astronaut"

[Setup]
AppId={{3F8A2C91-6B4E-4D2A-9C17-8E5F0A7B4D63}}
AppName={#DisplayName}
AppVersion={#AppVersion}
AppPublisher={#DeveloperName}
DefaultDirName={autopf}\{#ExtensionName}
OutputDir=bin\Release\installer
OutputBaseFilename={#ExtensionName}-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
MinVersion=10.0.19041
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "bin\Release\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#DisplayName}"; Filename: "{app}\{#ExtensionName}.exe"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{3F8A2C91-6B4E-4D2A-9C17-8E5F0A7B4D63}}"; ValueData: "{#DisplayName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "SOFTWARE\Classes\CLSID\{{3F8A2C91-6B4E-4D2A-9C17-8E5F0A7B4D63}}\LocalServer32"; ValueData: """{app}\{#ExtensionName}.exe"" -RegisterProcessAsComServer"; Flags: uninsdeletekey


