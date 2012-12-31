; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "FB2ePub"
#define MyAppPublisher "Lord KiRon"
#define MyAppURL "http://www.fb2epub.net"
#define Contact "lordkiron@fb2epub.net"
#define BaseFolder "C:\Project\GoogleCode\fb2epub\"
#define BuildFolder BaseFolder + "Output\Release\"
#define BuildFolder64 BaseFolder + "Output\x64\Release\"
#define BuildFolder86 BaseFolder + "Output\x86\Release\"
#define File4Version BuildFolder + "Fb2ePub.exe"
#define AppVersionNo GetFileVersion(File4Version)





[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{86973C45-84A3-458B-A98E-CF360FD87909}
AppName={#MyAppName}
AppVersion={#AppVersionNo}
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
WizardSmallImageFile={#BaseFolder}Fb2ePubSetup\banner_small.bmp
WizardImageFile={#BaseFolder}Fb2ePubSetup\banner.bmp
WizardImageStretch=False
LicenseFile={#BaseFolder}license.txt

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
; common
Source: "{#BuildFolder}CSS\*"; DestDir: "{app}\CSS"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildFolder}Fonts\*"; DestDir: "{app}\Fonts"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildFolder}Translit\*"; DestDir: "{app}\Translit"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildFolder}ru\*"; DestDir: "{app}\ru";   Flags: recursesubdirs createallsubdirs
Source: "{#BuildFolder}fb2.dtd"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder}genrestransfer.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder}changes.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder}Fb2ePub.exe"; DestDir: "{app}"; Flags: 
Source: "{#BuildFolder}Fb2ePub.exe.config"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}Fb2ePubGui.exe"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}Fb2ePubGui.exe.config"; DestDir: "{app}";  Flags: 
;Source: "{#BuildFolder}RegisterFB2EPub.exe"; DestDir: "{app}";  Flags: 
;Source: "{#BuildFolder}RegisterFB2EPub.exe.config"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}EPubLibrary.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}FB2EPubConverter.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}FB2EPubConverter.dll.config"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}Fb2epubSettings.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}Fb2FixLib.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}FB2Library.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}FolderSettingsHelper.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}FontSettings.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}ICSharpCode.SharpZipLib.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}ISOLanguages.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}log4net.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}NUnrar.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}TranslitRu.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}XHTMLClassLibrary.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}XMLFixerLibrary.dll"; DestDir: "{app}";  Flags: 
Source: "{#BuildFolder}prompt.cmd"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder}readme_en.htm"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder}epub-logo-color-book.ICO"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildFolder}license.docx"; DestDir: "{app}"; Flags: ignoreversion


Source: "{#BuildFolder86}FBE2EpubPlugin.dll"; DestDir: "{app}"; Flags: regserver 32bit

; x64
Source: "{#BuildFolder64}Fb2EpubExt_x64.dll"; DestDir: "{app}";  Check: Is64BitInstallMode; Flags: regserver 

;x86
Source: "{#BuildFolder86}Fb2EpubExt.dll"; DestDir: "{app}"; Check: not Is64BitInstallMode; 

[Icons]
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; Flags: excludefromshowinnewinstall
Name: "{group}\FB2ePub Command Prompt"; Filename: "{app}\prompt.cmd"; Flags: excludefromshowinnewinstall
Name: "{group}\FB2ePub GUI"; Filename: "{app}\Fb2ePubGui.exe"; 



#include "scripts\products.iss"
#include "scripts\products\winversion.iss"
#include "scripts\products\stringversion.iss"
#include "scripts\products\fileversion.iss"
#include "scripts\products\dotnetfxversion.iss"
#include "scripts\products\msi45.iss"
#include "scripts\products\dotnetfx40client.iss"
#include "scripts\products\vcredist2012.iss"
#include "scripts\products\fb2epub_ext.iss"

[Run]
Filename: "{dotnet4064}\RegAsm.exe"; Parameters: "/codebase ""{app}\FB2EPubConverter.dll"" /n"; Flags: runascurrentuser waituntilterminated runhidden; WorkingDir: {app};    Check: Is64BitInstallMode;  
; for 64bit we need both
Filename: "{dotnet4032}\RegAsm.exe"; Parameters: "/codebase ""{app}\FB2EPubConverter.dll"" /n"; Flags: runascurrentuser waituntilterminated runhidden; WorkingDir: {app};


[UninstallRun]
Filename: "{dotnet4064}\RegAsm.exe"; Parameters: "/u ""{app}\FB2EPubConverter.dll"" /n"; Flags: runascurrentuser runhidden; WorkingDir: {app};     Check: Is64BitInstallMode;  
; for 64bit we need both
Filename: "{dotnet4032}\RegAsm.exe"; Parameters: "/u ""{app}\FB2EPubConverter.dll"" /n"; Flags: runascurrentuser runhidden; WorkingDir: {app};

[Registry]
;FBE related plugins
Root: HKLM32; Subkey: "Software\Haali\FBE\Plugins"; Flags: uninsdeletekeyifempty ;
Root: HKLM32; Subkey: "Software\Haali\FBE\Plugins\{{469E5867-292A-4A8D-B094-5F3597C4B353}"; Flags: uninsdeletekey; ValueName:""; ValueData: "Export FB2 to ePub"; ValueType: string ;
Root: HKLM32; Subkey: "Software\Haali\FBE\Plugins\{{469E5867-292A-4A8D-B094-5F3597C4B353}"; Flags: uninsdeletekey; ValueName: "Icon"; ValueData: "{app}\FBE2EpubPlugin.dll"; ValueType: string;
Root: HKLM32; Subkey: "Software\Haali\FBE\Plugins\{{469E5867-292A-4A8D-B094-5F3597C4B353}"; Flags: uninsdeletekey; ValueName: "Menu"; ValueData: "To ePub"; ValueType: string;
Root: HKLM32; Subkey: "Software\Haali\FBE\Plugins\{{469E5867-292A-4A8D-B094-5F3597C4B353}"; Flags: uninsdeletekey; ValueName: "Type"; ValueData: "Export"; ValueType: string;

Root: HKCU; Subkey: "Software\FBETeam\FictionBook Editor\Plugins"; Flags: uninsdeletekeyifempty ;
Root: HKCU; Subkey: "Software\FBETeam\FictionBook Editor\Plugins\{{469E5867-292A-4A8D-B094-5F3597C4B353}"; Flags: uninsdeletekey; ValueName:""; ValueData: "Export FB2 to ePub"; ValueType: string ;
Root: HKCU; Subkey: "Software\FBETeam\FictionBook Editor\Plugins\{{469E5867-292A-4A8D-B094-5F3597C4B353}"; Flags: uninsdeletekey; ValueName: "Icon"; ValueData: "{app}\FBE2EpubPlugin.dll"; ValueType: string;
Root: HKCU; Subkey: "Software\FBETeam\FictionBook Editor\Plugins\{{469E5867-292A-4A8D-B094-5F3597C4B353}"; Flags: uninsdeletekey; ValueName: "Menu"; ValueData: "To ePub"; ValueType: string;
Root: HKCU; Subkey: "Software\FBETeam\FictionBook Editor\Plugins\{{469E5867-292A-4A8D-B094-5F3597C4B353}"; Flags: uninsdeletekey; ValueName: "Type"; ValueData: "Export"; ValueType: string;

;File assosiations
; Generic enable it to be an extension
Root: HKLM; Subkey: "Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved"; ValueType: string; ValueName: "{#CLSID_Fb2EpubShlExt}"; ValueData: "Fb2EpubShlExt extension"; Flags: uninsdeletevalue; Tasks: registreFB2Extension; 
; FB2
Root: HKCR; Subkey: {#FB2_Extension_Path}\ShellEx\ContextMenuHandlers\{#Fb2EpubShlExtName}; ValueType: string; ValueName: ""; ValueData: {#CLSID_Fb2EpubShlExt}; Flags: uninsdeletekey; Tasks: registreFB2Extension; 
;ZIP
Root: HKCR; Subkey: {#ZIP_Extension_Path}\ShellEx\ContextMenuHandlers\{#Fb2EpubShlExtName}; ValueType: string; ValueName: ""; ValueData: {#CLSID_Fb2EpubShlExt}; Flags: uninsdeletekey; Tasks: registreZIPExtension; 
;RAR
Root: HKCR; Subkey: {#RAR_Extension_Path}\ShellEx\ContextMenuHandlers\{#Fb2EpubShlExtName}; ValueType: string; ValueName: ""; ValueData: {#CLSID_Fb2EpubShlExt}; Flags: uninsdeletekey; Tasks: registreRARExtension; 
; Any (.*)
Root: HKCR; Subkey: {#Any_Extension_Path}\ShellEx\ContextMenuHandlers\{#Fb2EpubShlExtName}; ValueType: string; ValueName: ""; ValueData: {#CLSID_Fb2EpubShlExt}; Flags: uninsdeletekey; Tasks: registreAnyExtension; 



[Tasks]
Name: registreFB2Extension; Description: {cm:assosiateFB2}; GroupDescription: "File extensions";
Name: registreZIPExtension; Description: {cm:assosiateZIP}; GroupDescription: "File extensions";
Name: registreRARExtension; Description: {cm:assosiateRAR}; GroupDescription: "File extensions";
Name: registreAnyExtension; Description: {cm:assosiateAny}; GroupDescription: "File extensions"; Flags: unchecked;

[Code]
function InitializeSetup(): boolean;
begin
	//init windows version
	initwinversion();

	msi45('4.5');
  dotnetfx40client();
  vcredist2012();

	Result := true;
end;

//const DB_PAGE_CAPTION='Select Application Database Folder';
//  DB_PAGE_DESCRIPTION='Where should application database files be installed or where     your database files already are?';
//  DB_PAGE_SUBCAPTION='In case of new installation select the folder in which Setup should install application database files, then click Next. Or select folder where previous version of application stored database files, then click Next';
//
//var databasePage : TInputDirWizardPage;//this is predefined form declaration
//    CheckListBox : TNewCheckListBox;  //this is new element i'm about to add to page
//
//procedure createDatabaseWizardPage; //creating page
//begin
//databasePage :=CreateInputDirPage(wpSelectDir,
//DB_PAGE_CAPTION,
//DB_PAGE_DESCRIPTION,
//DB_PAGE_SUBCAPTION,
//False, '');
//databasePage.Add('');
//
//databasePage.buttons[0].Top:=databasePage.buttons[0].Top+ScaleY(70);//moving predefined 
//databasePage.edits[0].Top:=databasePage.edits[0].Top+ScaleY(70);    //elements down.
//databasePage.edits[0].Text:=ExpandConstant('{commonappdata}\my app');//default value
//
//CheckListBox := TNewCheckListBox.Create(databasePage);//creating and modifying new checklistbox
//CheckListBox.Top := 40 + ScaleY(8);
//CheckListBox.Width := databasePage.SurfaceWidth;
//CheckListBox.Height := ScaleY(50);
//CheckListBox.BorderStyle := bsNone;
//CheckListBox.ParentColor := True;
//CheckListBox.MinItemHeight := WizardForm.TasksList.MinItemHeight;
//CheckListBox.ShowLines := False;
//CheckListBox.WantTabs := True;
//CheckListBox.Parent := databasePage.Surface;//setting control's parent element
//CheckListBox.AddRadioButton('New Installation', '', 0, True, True, nil);
//CheckListBox.AddRadioButton('Update existing copy', '', 0, False, True, nil);
//end;


procedure InitializeWizard;
begin
//createDatabaseWizardPage(); 
end;