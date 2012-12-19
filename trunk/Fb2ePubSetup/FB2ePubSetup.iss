; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "FB2ePub"
#define MyAppVersion "1.5"
#define MyAppPublisher "Lord KiRon"
#define MyAppURL "http://www.fb2epub.net"
#define Contact "lordkiron@fb2epub.net"
#define BaseFolder "C:\Project\GoogleCode\fb2epub\"
#define BuildFolder64 BaseFolder + "Output\x64\Release\"
#define BuildFolder86 BaseFolder + "Output\x86\Release\"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{86973C45-84A3-458B-A98E-CF360FD87909}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputBaseFilename=Fb2ePubSetup
SetupIconFile={#BaseFolder}Fb2ePub\epub-logo-color-book.ICO
Compression=lzma
SolidCompression=yes
AppCopyright=CopyFree�
ShowLanguageDialog=auto
AppContact={#Contact}
UninstallDisplayIcon={#BaseFolder}Fb2ePub\epub-logo-color-book.ICO
ArchitecturesInstallIn64BitMode=x64
MinVersion=0,5.01sp3

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
; common
Source: "{#BaseFolder}FB2EPubConverter\CSS\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BaseFolder}FB2EPubConverter\Fonts\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BaseFolder}FB2EPubConverter\Translit\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BaseFolder}Fb2ePub\prompt.cmd"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BaseFolder}FB2EPubConverter\fb2.dtd"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BaseFolder}FB2EPubConverter\genrestransfer.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BaseFolder}Fb2ePub\readme_en.htm"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BaseFolder}Fb2ePub\epub-logo-color-book.ICO"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BaseFolder}Fb2ePub\license.docx"; DestDir: "{app}"; Flags: ignoreversion

; x64
Source: "{#BuildFolder64}Fb2ePub.exe"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}Fb2ePub.exe.config"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}Fb2ePubGui.exe"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}Fb2ePubGui.exe.config"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}RegisterFB2EPub.exe"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}RegisterFB2EPub.exe.config"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}ru\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs
Source: "{#BuildFolder64}ChilkatDotNet2.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}EPubLibrary.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}FB2EPubConverter.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}FB2EPubConverter.dll.config"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}Fb2EpubExt_x64.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}Fb2epubSettings.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}Fb2FixLib.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}FB2Library.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}FBE2EpubPlugin.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}FolderSettingsHelper.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}FontSettings.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}ICSharpCode.SharpZipLib.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}ISOLanguages.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}log4net.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}TranslitRu.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}XHTMLClassLibrary.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder64}XMLFixerLibrary.dll"; DestDir: "{app}"; Flags: 

;x86
Source: "{#BuildFolder86}Fb2ePub.exe"; DestDir: "{app}"
Source: "{#BuildFolder86}Fb2ePub.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder86}Fb2ePubGui.exe"; DestDir: "{app}"
Source: "{#BuildFolder86}Fb2ePubGui.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder86}RegisterFB2EPub.exe"; DestDir: "{app}"
Source: "{#BuildFolder86}RegisterFB2EPub.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder86}ru\*"; DestDir: "{app}"; Flags: createallsubdirs recursesubdirs
Source: "{#BuildFolder86}ChilkatDotNet2.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}EPubLibrary.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}FB2EPubConverter.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}FB2EPubConverter.dll.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder86}Fb2EpubExt.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}Fb2epubSettings.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}Fb2FixLib.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}FB2Library.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}FBE2EpubPlugin.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}FolderSettingsHelper.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}FontSettings.dll"; DestDir: "{app}"
Source: "{#BuildFolder86}ICSharpCode.SharpZipLib.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder86}ISOLanguages.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder86}log4net.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder86}TranslitRu.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder86}XHTMLClassLibrary.dll"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder86}XMLFixerLibrary.dll"; DestDir: "{app}"; Flags: 
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{group}\FB2ePub Command Prompt"; Filename: "{app}\prompt.cmd"; 
Name: "{group}\FB2ePub GUI"; Filename: "{app}\Fb2ePubGui.exe"; 
Name: "{group}\Register FB2ePub"; Filename: "{app}\RegisterFB2EPub.exe"; 


[Types]
Name: "common"; Description: "Stuff common to both CPU configurations"
Name: "x86"; Description: "x86 CPU install"
Name: "x64"; Description: "x64 CPU install"