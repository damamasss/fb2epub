﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using EPubLibrary;
using EPubLibrary.Content.Guide;
using EPubLibrary.CSS_Items;
using EPubLibrary.XHTML_Items;
using FB2EPubConverter;
using FB2EPubConverter.ElementConverters;
using FB2Library;
using FB2Library.Elements;
using FB2Library.Elements.Poem;
using FB2Library.Elements.Table;
using FB2Library.HeaderItems;
using FontsSettings;
using ICSharpCode.SharpZipLib.Zip;
using TranslitRu;
using XHTMLClassLibrary.BaseElements;
using XHTMLClassLibrary.BaseElements.BlockElements;
using XHTMLClassLibrary.BaseElements.InlineElements;
using XHTMLClassLibrary.BaseElements.TableElements;
using XMLFixerLibrary;
using EPubLibrary.ReferenceUtils;
using ZipEntry=ICSharpCode.SharpZipLib.Zip.ZipEntry;
using FB2Fix;
using NUnrar.Archive;
using Fb2epubSettings;
using EPubLibrary.AppleEPubV2Extensions;


namespace Fb2ePubConverter
{


    internal static class Logger
    {
        // Create a logger for use in this class
        public static readonly log4net.ILog Log = log4net.LogManager.GetLogger(Assembly.GetExecutingAssembly().GetType());
        
    }



    public class Fb2EPubConverterEngine
    {
        /// <summary>
        /// Represent acceptable input file types
        /// </summary>
        private enum FileTypesEnum
        {
            FileTypeZIP,
            FileTypeFB2,
            FileTypeRAR,
            FileTypeUnknown,
        }

        private readonly ImageManager images = new ImageManager();
        private readonly HRefManager referencesManager = new HRefManager();
        private readonly List<FB2File> fb2Files = new List<FB2File>();


        private long _maxSize = 245 * 1024;
        private int _sectionCounter = 0;


        /// <summary>
        /// Get/Set max document size in bytes
        /// </summary>
        public long MaxSize
        {
            get { return _maxSize; }
            set 
            { 
                if ( value <= 0)
                {
                    throw new ArgumentException("value");
                }
                _maxSize = value;
            }
        }




        /// <summary>
        /// Settings for the converter
        /// </summary>
        public ConverterSettings Settings;






        
        /// <summary>
        /// Convert input file or archive to ePub(s)
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool ConvertFile(string fileName)
        {
            referencesManager.FlatStructure = Settings.Flat;
            Logger.Log.InfoFormat("Starting to convert {0}", fileName);
            fb2Files.Clear();
            if (!File.Exists(fileName))
            {
                Logger.Log.ErrorFormat("Unable to locate file {0} on disk.", fileName);
                return false;
            }
            switch (DetectFileType(fileName))
            {
                case FileTypesEnum.FileTypeZIP:
                    Logger.Log.InfoFormat("Loading ZIP : {0}",fileName);
                    if (!LoadFb2ZipFile(fileName))
                    {
                        Logger.Log.ErrorFormat("Error loading ZIP {0} :",fileName);
                        return false;
                    }
                    break;
                case FileTypesEnum.FileTypeFB2:
                    Logger.Log.InfoFormat("Processing {0} ...", fileName);
                    if (!LoadFb2File(fileName))
                    {
                        Logger.Log.ErrorFormat("Error loading FB2 {0} :", fileName);
                        return false;
                    }
                    break;
                case FileTypesEnum.FileTypeRAR:
                    Logger.Log.InfoFormat("Loading RAR : {0}", fileName);
                    if (!LoadFb2RarFile(fileName))
                    {
                        Logger.Log.ErrorFormat("Error loading RAR {0} :", fileName);
                        return false;
                    }
                    break;
                default:
                    Logger.Log.InfoFormat("File {0} is of unsupported type",fileName);
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Loads FB2 from RAR files
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool LoadFb2RarFile(string fileName)
        {
            bool fb2FileFound = false;
            bool Fb2FileLoaded = false;
            try
            {
                RarArchive rarFile = RarArchive.Open(fileName);

                int n = rarFile.Entries.Count;
                Logger.Log.DebugFormat("Detected {0} entries in RAR file", n);
                foreach(var entry in rarFile.Entries)
                {
                    if (entry.IsDirectory) 
                    {
                        Logger.Log.DebugFormat("{0} is not file but folder", fileName);
                        continue;
                    }
                    if (Path.GetExtension(entry.FilePath).ToUpper() == ".FB2")
                    {
                        fb2FileFound = true;
                        try
                        {
                            string tempPath = Path.GetTempPath();
                            entry.WriteToDirectory(tempPath);
                            string fileNameOnly = Path.GetFileName(entry.FilePath);
                            Logger.Log.InfoFormat("Processing {0} ...", fileNameOnly);
                            if (!LoadFb2File(string.Format(@"{0}\{1}", tempPath, fileNameOnly)))
                            {
                                Logger.Log.ErrorFormat("Unable to load {0}", fileNameOnly);
                                continue;
                            }
                            else
                            {
                                Fb2FileLoaded = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log.ErrorFormat("Unable to unrar file entry {0} : {1}", entry.FilePath, ex.ToString());
                            continue;
                        }
                    }
                    else
                    {
                        Logger.Log.InfoFormat("{0} is not FB2 file", entry.FilePath);
                        continue;                        
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Error loading RAR file : {0}",ex.ToString());
                return false;
            }
            return fb2FileFound && Fb2FileLoaded;
        }

        /// <summary>
        /// Loads FB2 file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool LoadFb2File(string fileName)
        {
            try
            {
                Stream s = File.OpenRead(fileName);

                if (Settings.FixMode == FixOptions.Fb2FixAlways)
                {
                    LoadFB2StreamWithFix(s, ReadFb2FileStream);
                }
                else
                {
                    try
                    {
                        try
                        {
                            ReadFb2FileStream(s);
                        }
                        catch (XmlException)
                        {
                            if (Settings.FixMode == FixOptions.DoNotFix)
                            {
                                Logger.Log.ErrorFormat("Error in file, not fixing ");
                                return false;
                            }
                            Logger.Log.Info("Error loading file - invalid XML content - attempting to repair...");
                            // try to read broken XML
                            s.Seek(0, SeekOrigin.Begin);
                            ReadBrokenXmlFb2FileStream(s);
                        }
                        s.Close();
                    }
                    catch (XmlException)
                    {
                        if (Settings.FixMode == FixOptions.MinimalFix)
                        {
                            Logger.Log.ErrorFormat("Error in file, not fixing ");
                            return false;
                        }
                        Logger.Log.Info("Repair attempt failed - attempting to repair using Fb2Fix...");
                        // try to read broken XML
                        s.Seek(0, SeekOrigin.Begin);
                        LoadFB2StreamWithFix(s, ReadFb2FileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Error loading FB2 file : {0}", ex.ToString());
                return false;
            }

            return true;
        }

        private void LoadFB2StreamWithFix(Stream s, Action<Stream> streamLoader)
        {
            Fb2FixArguments options = new Fb2FixArguments();
            options.incversion = true;
            options.regenerateId = false;
            options.indentBody = false;
            options.indentHeader = true;
            options.mapGenres = true;
            options.encoding = Encoding.UTF8;

            using (Stream output = Fb2Fix.Process(s, options))
            {
                streamLoader(output);
            }
            
        }

        private void ReadFb2FileStream(Stream s)
        {
            Logger.Log.Debug("Starting to load FB2 stream");
            XmlReaderSettings settings = new XmlReaderSettings
                                             {
                                                 ValidationType = ValidationType.None,
                                                 DtdProcessing = DtdProcessing.Prohibit,
                                                 CheckCharacters = false
                                                 
            };
            XDocument fb2Document;
            try
            {
                    using (XmlReader reader = XmlReader.Create(s, settings))
                    {
                        fb2Document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
                        reader.Close();
                    }

            }
            catch(XmlException) // we handle this on top
            {
               throw;
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Error loading file : {0}", ex.ToString());
                throw;
            }
            FB2File file = new FB2File();
            try
            {
                file.Load(fb2Document,false);
                fb2Files.Add(file);
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Error loading file : {0}",ex.ToString());
            }
            Logger.Log.Debug("FB2 stream loaded");
        }

        private bool LoadFb2ZipFile(string fileName)
        {
            Logger.Log.DebugFormat("Starting to load ZIP file {0}",fileName);
            try
            {
                Exception returnEx = null;
                bool fb2FileFound = false;
                bool fb2FileLoaded = false;
                using (var s = new ZipInputStream(File.OpenRead(fileName)))
                {
                    ZipEntry theEntry;
                    try
                    {
                        while ((theEntry = s.GetNextEntry()) != null)
                        {
                            if (!theEntry.IsFile || !theEntry.CanDecompress)
                            {
                                Logger.Log.InfoFormat("{0} is not file or not decompresable",fileName);
                                continue;
                            }
                            Logger.Log.InfoFormat("Processing {0} ...", theEntry.Name);
                            if (Path.GetExtension(theEntry.Name).ToUpper() == ".FB2")
                            {
                                fb2FileFound = true;

                                if (Settings.FixMode == FixOptions.Fb2FixAlways)
                                {
                                    using (var s1 = new ZipInputStream(File.OpenRead(fileName)))
                                    {
                                        // reach the same position in ZIP
                                        while (theEntry.ToString() != s1.GetNextEntry().ToString());
                                        LoadFB2StreamWithFix(s1, ReadFb2FileStream);
                                        fb2FileLoaded = true;
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        ReadFb2FileStream(s);
                                        fb2FileLoaded = true;
                                    }
                                    catch (XmlException) // broken/mailformed Xml detected
                                    {
                                        if (Settings.FixMode == FixOptions.DoNotFix)
                                        {
                                            Logger.Log.ErrorFormat("Error in file, not fixing ");
                                            continue;
                                        }
                                        // try to run work around case 
                                        Logger.Log.Info(
                                            "Error loading file - invalid XML content - attempting to repair...");
                                        using (var s1 = new ZipInputStream(File.OpenRead(fileName)))
                                        {
                                            // reach the same position in ZIP
                                            while (theEntry.ToString() != s1.GetNextEntry().ToString())
                                            {
                                            }
                                            // try to read broken XML
                                            try
                                            {
                                                ReadBrokenXmlFb2FileStream(s1);
                                                fb2FileLoaded = true;
                                            }
                                            catch (XmlException)
                                            {
                                                if (Settings.FixMode == FixOptions.MinimalFix)
                                                {
                                                    Logger.Log.ErrorFormat("Error in file, not fixing ");
                                                    continue;
                                                }
                                                s1.Close();
                                                using (var s2 = new ZipInputStream(File.OpenRead(fileName)))
                                                {
                                                    // reach the same position in ZIP
                                                    while (theEntry.ToString() != s2.GetNextEntry().ToString())
                                                    {
                                                    }

                                                    Logger.Log.Info(
                                                        "Repair attempt failed - attempting to repair using Fb2Fix...");
                                                    // try to read broken XML
                                                    try
                                                    {
                                                        try
                                                        {
                                                            LoadFB2StreamWithFix(s2, ReadFb2FileStream);
                                                            fb2FileLoaded = true;
                                                        }
                                                        catch (XmlException)
                                                        {
                                                            Logger.Log.ErrorFormat("Error in file - unable to fix");
                                                            continue;
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Logger.Log.ErrorFormat("Error in file - Fb2Fix crashes - unable to fix. \nError {0}",ex.Message);
                                                        continue;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch(ZipException ze)
                    {
                        Logger.Log.ErrorFormat("{0} - problem decompressing the file, UnZip error: {1}", fileName,ze.Message);
                        s.Close();
                        returnEx = ze;
                    }
                    catch (Exception ex)
                    {
                       Logger.Log.ErrorFormat("{0} - problem decompressing the file, error: {1}", fileName, ex.ToString());
                        s.Close();
                        returnEx = ex;
                    }
                    s.Close();
                }
                Logger.Log.DebugFormat("ZIP file {0} loaded successfully", fileName);
                if (returnEx != null)
                {
                    throw returnEx;
                }
                return fb2FileFound && fb2FileLoaded;
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Error loading ZIP file {0} : {1}",fileName,ex.ToString());
            }
            return false;
            
        }

        private void ReadBrokenXmlFb2FileStream(Stream stream)
        {
            Logger.Log.Debug("Starting to load FB2 stream");
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    XmlRepair fixer = new XmlRepair();
                    fixer.Repair(stream,ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    ReadFb2FileStream(ms);
                }

            }
            catch(XmlException xex)
            {
                Logger.Log.WarnFormat("Error loading file - invalid XML content : {0} \nRepair attempt failed", xex.ToString());
                throw;

            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Error loading file : {0}", ex.ToString());
                throw;
            }
        }


        /// <summary>
        /// Detects file type of the input file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static FileTypesEnum DetectFileType(string fileName)
        {
            Logger.Log.DebugFormat("Detecting file type for {0}",fileName);
            switch (Path.GetExtension(fileName).ToUpper())
            {
                case ".FB2":
                    Logger.Log.Debug("The file is FB2 file");
                    return FileTypesEnum.FileTypeFB2;
                case ".ZIP":
                    Logger.Log.Debug("The file is ZIP file");
                    return FileTypesEnum.FileTypeZIP;
                case ".RAR":
                    Logger.Log.Debug("The file is RAR file");
                    return FileTypesEnum.FileTypeRAR;
                default:
                    Logger.Log.Debug("Can't use extension - attempting to detect");
                    if (IsZipFile(fileName))
                    {
                        Logger.Log.Debug("The file is ZIP file");
                        return FileTypesEnum.FileTypeZIP;                        
                    }
                    if (IsRarFile(fileName))
                    {
                        Logger.Log.Debug("The file is RAR file");
                        return FileTypesEnum.FileTypeRAR;
                    }
                    break;
            }
            Logger.Log.Debug("The file is unknown file type");
            return FileTypesEnum.FileTypeUnknown;
        }

        private static bool IsRarFile(string fileName)
        {
            try
            {
                return RarArchive.IsRarFile(fileName);
            }
            catch (Exception ex)
            {
                Logger.Log.Debug(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Checks if input file is a ZIP archive
        /// </summary>
        /// <param name="fileName">file to check</param>
        /// <returns>true if file is ZIP archive, false otherwise</returns>
        private static bool IsZipFile(string fileName)
        {
            try
            {
                ZipFile file = new ZipFile(fileName);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Saves the loaded FB2 file(s) to the destination as ePub
        /// </summary>
        /// <param name="outFileName"></param>
        public void Save(string outFileName)
        {
            Logger.Log.DebugFormat("Saving {0}",outFileName);
            try
            {
                EPubFile epubFile;
                int count = 0;
                Logger.Log.DebugFormat("Saving totaly {0} file(s)",fb2Files.Count);
                foreach (var fb2File in fb2Files)
                {
                    epubFile = new EPubFile { FlatStructure = Settings.Flat, EmbedStyles = Settings.EmbedStyles };
                    Reset();
                    if (string.IsNullOrEmpty(Settings.ResourcesPath))
                    {
                        epubFile.Transliterator.RuleFile = @".\Translit\translit.xml";
                    }
                    else
                    {
                        epubFile.Transliterator.RuleFile = string.Format(@"{0}\Translit\translit.xml", Settings.ResourcesPath);
                    }
                    Logger.Log.DebugFormat("Using transliteration rule file : {0}", epubFile.Transliterator.RuleFile);
                    if (Settings.Transliterate)
                    {
                        epubFile.TranslitMode = TranslitModeEnum.ExternalRuleFile;
                        if (!File.Exists(epubFile.Transliterator.RuleFile))
                        {
                            Console.WriteLine(string.Format("Unable to locate translation file {0}",epubFile.Transliterator.RuleFile));
                        }
                    }
                    else
                    {
                        epubFile.TranslitMode = TranslitModeEnum.None;
                    }
                    if (string.IsNullOrEmpty(Settings.AdobeTemplatePath))
                    {
                        epubFile.AdobeTemplatePath = @".\Template\template.xpgt";
                    }
                    else
                    {
                        epubFile.AdobeTemplatePath = Settings.AdobeTemplatePath;
                    }
                    epubFile.UseAdobeTemplate = Settings.EnableAdobeTemplate;
                    epubFile.TranliterateToc = Settings.TransliterateToc;
                    Logger.Log.DebugFormat("Transliteration mode : {0}", epubFile.TranslitMode);
                    images.RemoveAlpha = Settings.ConvertAlphaPng;
                    images.LoadFromBinarySection(fb2File.Images);
                    PassHeaderDataFromFb2ToEpub(epubFile, fb2File);
                    SetupCSS(epubFile);
                    SetupFonts(epubFile);
                    SetupAppleSettings(epubFile);
                    PassTextFromFb2ToEpub(epubFile, fb2File);
                    if (Settings.Fb2Info)
                    {
                        PassFb2InfoToEpub(epubFile, fb2File);
                    }
                    UpdateInternalLinks(epubFile, fb2File);
                    PassImagesDataFromFb2ToEpub(epubFile, fb2File);
                    Logger.Log.DebugFormat("Transliteration of sile names set to : {0}",Settings.TransliterateFileName);
                    if (Settings.TransliterateFileName)
                    {
                        outFileName = epubFile.Transliterator.Translate(outFileName, epubFile.TranslitMode);
                        Logger.Log.DebugFormat("New transliterated file name : {0}",outFileName);
                    }
                    string outFile = outFileName;
                    string outFolder = Path.GetDirectoryName(outFileName);
                    if (string.IsNullOrEmpty(outFolder))
                    {
                        Logger.Log.DebugFormat("Using output folder : {0}", Settings.OutPutPath);
                        outFolder = Settings.OutPutPath;
                        if (!string.IsNullOrEmpty(outFolder))
                        {
                            outFile = string.Format(@"{0}\{1}.epub", outFolder, Path.GetFileNameWithoutExtension(outFileName));    
                        }
                        
                    }
                    while (File.Exists(outFile) && (fb2File != fb2Files[0]))
                    {
                        Logger.Log.DebugFormat("{0} file exists , incrementing",outFile);
                        outFile = string.Format(@"{0}\{1}_{2}.epub", outFolder, Path.GetFileNameWithoutExtension(outFileName), count++);
                    }
                    //outFile.Replace('?','1' );
                    Logger.Log.DebugFormat("Final output file name : {0}",outFile);
                    Assembly asm = Assembly.GetAssembly(GetType());
                    string version = "???";
                    if (asm != null)
                    {
                        version = asm.GetName().Version.ToString();
                    }
                    if (!Settings.SkipAboutPage)
                    {
                        epubFile.AboutTexts.Add(
                            string.Format("This file was generated by Lord KiRon's FB2EPUB converter version {0}.",
                                          version));
                        epubFile.AboutTexts.Add("(This book might contain copyrighted material, author of the converter bears no responsibility for it's usage)");
                        epubFile.AboutTexts.Add(
                            string.Format("Этот файл создан при помощи конвертера FB2EPUB версии {0} написанного Lord KiRon.",
                                version));
                        epubFile.AboutTexts.Add("(Эта книга может содержать материал который защищен авторским правом, автор конвертера не несет ответственности за его использование)");
                        epubFile.AboutLinks.Add(@"http://www.fb2epub.net");
                        epubFile.AboutLinks.Add(@"https://code.google.com/p/fb2epub/");
                    }
                    epubFile.InjectLKRLicense = true;
                    epubFile.Generate(outFile);                        
                }
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Error saving file {0} : {1} - {2}", outFileName,ex.StackTrace, ex);
                throw;
            }
        }

        private void SetupAppleSettings(EPubFile epubFile)
        {
            if (epubFile == null)
            {
                throw new ArgumentNullException("epubFile");
            }
            // setup epub2 options
            epubFile.AppleOptions.Reset();
            foreach (var platform in Settings.AppleConverterEPubSettings.V2Settings.Platforms)
            {
                AppleTargetPlatform targetPlatform = new AppleTargetPlatform();
                switch (platform.Name)
                {
                    case Fb2epubSettings.AppleSettings.ePub_v2.AppleTargetPlatform.All:
                        targetPlatform.Type = PlatformType.All;
                        break;
                    case Fb2epubSettings.AppleSettings.ePub_v2.AppleTargetPlatform.iPad:
                        targetPlatform.Type = PlatformType.iPad;
                        break;
                    case Fb2epubSettings.AppleSettings.ePub_v2.AppleTargetPlatform.iPhone:
                        targetPlatform.Type = PlatformType.iPhone;
                        break;
                    case Fb2epubSettings.AppleSettings.ePub_v2.AppleTargetPlatform.NotSet: // we not going to add if type not set
                        Logger.Log.Error("SetupAppleSettings() - passed apple platform of type NotSet");
                        continue;
                }
                targetPlatform.FixedLayout = platform.FixedLayout;
                targetPlatform.OpenToSpread = platform.OpenToSpread;
                targetPlatform.CustomFontsAllowed = platform.UseCustomFonts;
                switch(platform.OrientationLock)
                {
                    case Fb2epubSettings.AppleSettings.ePub_v2.AppleOrientationLock.None:
                        targetPlatform.OrientationLockType = OrientationLock.Off;               
                        break;
                    case Fb2epubSettings.AppleSettings.ePub_v2.AppleOrientationLock.LandscapeOnly:
                        targetPlatform.OrientationLockType = OrientationLock.LandscapeOnly;
                        break;
                    case Fb2epubSettings.AppleSettings.ePub_v2.AppleOrientationLock.PortraitOnly:
                        targetPlatform.OrientationLockType = OrientationLock.PortraitOnly;
                        break;
                }
                epubFile.AppleOptions.AddPlatform(targetPlatform);
            }
            
        }

        private void UpdateInternalLinks(EPubFile epubFile, FB2File fb2File)
        {
            referencesManager.RemoveInvalidAnchors();
            referencesManager.RemoveInvalidImages(fb2File.Images);
            referencesManager.RemapAnchors(epubFile);
        }

        /// <summary>
        /// Passes FB2 info to the EPub file to be added at the end of the book
        /// </summary>
        /// <param name="epubFile">destination epub object</param>
        /// <param name="fb2File">source fb2 object</param>
        private void PassFb2InfoToEpub(EPubFile epubFile, FB2File fb2File)
        {
            BookDocument infoDocument = epubFile.AddDocument("FB2 Info");
            ConverterOptions converterSettings = new ConverterOptions
            {
                CapitalDrop = false,
                Images = images,
                MaxSize = MaxSize,
                ReferencesManager = referencesManager
            };
            Fb2EpubInfoConverter infoConverter = new Fb2EpubInfoConverter { Settings = converterSettings };
            infoDocument.Content = infoConverter.ConvertInfo(fb2File);
            infoDocument.FileName = "fb2info.xhtml";
            infoDocument.DocumentType = GuideTypeEnum.Notes;
            infoDocument.Type = SectionTypeEnum.Text;
            infoDocument.NotPartOfNavigation = true;
        }


        private void SetupCSS(EPubFile epubFile)
        {
            Assembly asm = Assembly.GetAssembly(GetType());
            string pathPreffix = Path.GetDirectoryName(asm.Location);
            if (!string.IsNullOrEmpty(Settings.ResourcesPath))
            {
                pathPreffix = Settings.ResourcesPath;
            }
            epubFile.CSSFiles.Add(new CSSFile { FileExtPath = string.Format(@"{0}\{1}", pathPreffix, @"CSS\default.css"), FileName = "default.css" });
        }

        private void SetupFonts(EPubFile epubFile)
        {
            if (Settings.Fonts == null)
            {
                Logger.Log.Warn("No fonts defined in configuration file.");
                return;
            }
            epubFile.SetEPubFonts(Settings.Fonts, Settings.ResourcesPath, Settings.DecorateFontNames);
        }


        private void PassTextFromFb2ToEpub(EPubFile epubFile, FB2File fb2File)
        {
            // create second title page
            if ((fb2File.MainBody.Title!=null) &&(!string.IsNullOrEmpty(fb2File.MainBody.Title.ToString())))
            {
                string docTitle = fb2File.MainBody.Title.ToString();
                Logger.Log.DebugFormat("Adding section : {0}", docTitle);
                BookDocument addTitlePage = epubFile.AddDocument(docTitle);
                addTitlePage.DocumentType = GuideTypeEnum.TitlePage;
                addTitlePage.Content = new Div();
                ConverterOptions converterSettings = new ConverterOptions
                                                          {
                                                              CapitalDrop = Settings.CapitalDrop,
                                                              Images = images,
                                                              MaxSize = MaxSize,
                                                              ReferencesManager = referencesManager
                                                          };
                TitleConverter titleConverter = new TitleConverter { Settings = converterSettings };
                addTitlePage.Content.Add(titleConverter.Convert(fb2File.MainBody.Title, 2));
                addTitlePage.NavigationParent = null;
                addTitlePage.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
                addTitlePage.NotPartOfNavigation = true;
            }

            BookDocument mainDocument = null;
            if (!string.IsNullOrEmpty(fb2File.MainBody.Name))
            {
                string docTitle = string.Empty;
                docTitle = fb2File.MainBody.Name;
                Logger.Log.DebugFormat("Adding section : {0}", docTitle);
                mainDocument = epubFile.AddDocument(docTitle);
                mainDocument.DocumentType = GuideTypeEnum.Text;
                mainDocument.Content = new Div();
                mainDocument.NavigationParent = null;
                mainDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
            }

            if ((fb2File.MainBody.ImageName!= null) && !string.IsNullOrEmpty(fb2File.MainBody.ImageName.HRef))
            {
                if (mainDocument == null)
                {
                    string newDocTitle = ((fb2File.MainBody.Title!=null)&&(!string.IsNullOrEmpty(fb2File.MainBody.Title.ToString())))?fb2File.MainBody.Title.ToString():"main";
                    mainDocument = epubFile.AddDocument(newDocTitle);
                    mainDocument.DocumentType = GuideTypeEnum.Text;
                    mainDocument.Content = new Div();
                    mainDocument.NavigationParent = null;
                    mainDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
                }
                if (images.IsImageIdReal(fb2File.MainBody.ImageName.HRef))
                {
                    Div enclosing = new Div(); // we use the enclosing so the user can style center it
                    ConverterOptions converterSettings = new ConverterOptions
                    {
                        CapitalDrop = Settings.CapitalDrop,
                        Images = images,
                        MaxSize = MaxSize,
                        ReferencesManager = referencesManager
                    };

                    ImageConverter imageConverter = new ImageConverter { Settings = converterSettings };
                    enclosing.Add(imageConverter.Convert(fb2File.MainBody.ImageName));
                    enclosing.Class.Value = "body_image";
                    mainDocument.Content.Add(enclosing);
                }

                
            }
            foreach (var ep in fb2File.MainBody.Epigraphs)
            {
                if (mainDocument == null)
                {
                    string newDocTitle = ((fb2File.MainBody.Title != null) && (!string.IsNullOrEmpty(fb2File.MainBody.Title.ToString()))) ? fb2File.MainBody.Title.ToString() : "main";
                    mainDocument = epubFile.AddDocument(newDocTitle);
                    mainDocument.DocumentType = GuideTypeEnum.Text;
                    mainDocument.Content = new Div();
                    mainDocument.NavigationParent = null;
                    mainDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
                }
                ConverterOptions converterSettings = new ConverterOptions
                {
                    CapitalDrop = Settings.CapitalDrop,
                    Images = images,
                    MaxSize = MaxSize,
                    ReferencesManager = referencesManager
                };

                MainEpigraphConverter epigraphConverter = new MainEpigraphConverter { Settings = converterSettings };
                mainDocument.Content.Add(epigraphConverter.Convert(ep,1));
            }

            Logger.Log.Debug("Adding main sections");
            foreach (var section in fb2File.MainBody.Sections)
            {
                AddSection(epubFile, section, mainDocument,false);
            }

            Logger.Log.Debug("Adding secondary bodies");
            foreach (var bodyItem in fb2File.Bodies)
            {
                if (bodyItem == fb2File.MainBody)
                {
                    continue;
                }
                bool fbeNotesSection = FBENotesSection(bodyItem.Name);
                if (fbeNotesSection)
                {
                    AddFbeNotesBody(epubFile, bodyItem);
                }
                else
                {
                    AddSecondaryBody(epubFile, bodyItem);                    
                }
            }
        }

        /// <summary>
        /// Add and convert FBE style generated notes sections
        /// </summary>
        /// <param name="epubFile"></param>
        /// <param name="bodyItem"></param>
        private void AddFbeNotesBody(EPubFile epubFile, BodyItem bodyItem)
        {
            string docTitle = bodyItem.Name;
            Logger.Log.DebugFormat("Adding section : {0}", docTitle);
            BookDocument sectionDocument = null;
            sectionDocument = epubFile.AddDocument(docTitle);
            sectionDocument.DocumentType = GuideTypeEnum.Glossary;
            sectionDocument.Type = SectionTypeEnum.Links;
            sectionDocument.Content = new Div();
            if (bodyItem.Title != null)
            {
                ConverterOptions converterSettings = new ConverterOptions
                {
                    CapitalDrop = false,
                    Images = images,
                    MaxSize = MaxSize,
                    ReferencesManager = referencesManager
                };
                TitleConverter titleConverter = new TitleConverter { Settings = converterSettings };
                sectionDocument.Content.Add(titleConverter.Convert(bodyItem.Title, 1));
            }
            sectionDocument.NavigationParent = null;
            sectionDocument.NotPartOfNavigation = true;
            sectionDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
            Logger.Log.Debug("Adding sub-sections");
            foreach (var section in bodyItem.Sections)
            {
                AddSection(epubFile, section, sectionDocument, true);
            }           
        }

        /// <summary>
        /// Add and convert generic secondary body section
        /// </summary>
        /// <param name="epubFile"></param>
        /// <param name="bodyItem"></param>
        private void AddSecondaryBody(EPubFile epubFile, BodyItem bodyItem)
        {
            string docTitle = string.Empty;
            if (string.IsNullOrEmpty(bodyItem.Name))
            {
                if (bodyItem.Title != null)
                {
                    docTitle = bodyItem.Title.ToString();
                }
            }
            else
            {
                docTitle = bodyItem.Name;
            }
            Logger.Log.DebugFormat("Adding section : {0}", docTitle);
            BookDocument sectionDocument = null;
            sectionDocument = epubFile.AddDocument(docTitle);
            sectionDocument.DocumentType = GuideTypeEnum.Text;
            sectionDocument.Type = SectionTypeEnum.Text;
            sectionDocument.Content = new Div();
            if (bodyItem.Title != null)
            {
                ConverterOptions converterSettings = new ConverterOptions
                {
                    CapitalDrop = Settings.CapitalDrop,
                    Images = images,
                    MaxSize = MaxSize,
                    ReferencesManager = referencesManager
                };
                TitleConverter titleConverter = new TitleConverter {Settings = converterSettings};
                sectionDocument.Content.Add(titleConverter.Convert(bodyItem.Title,1));
            }
            sectionDocument.NavigationParent = null;
            sectionDocument.NotPartOfNavigation = false;
            sectionDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
            Logger.Log.Debug("Adding sub-sections");
            foreach (var section in bodyItem.Sections)
            {
                AddSection(epubFile, section, sectionDocument, false);
            }
        }

        /// <summary>
        /// Check if the body is FBE generated notes section
        /// </summary>
        /// <param name="name">body name</param>
        /// <returns>true if it is FBE generated notes/comments body</returns>
        private static bool FBENotesSection(string name)
        {
            switch (name)
            {
                case "comments":
                case "footnotes":
                case "notes":
                    return true;
            }
            return false;
        }


        private void AddSection(EPubFile epubFile, SectionItem section,BookDocument navParent,bool fbeNotesSection)
        {
            string docTitle = string.Empty;
            if (section.Title != null)
            {
                docTitle = section.Title.ToString();
            }
            Logger.Log.DebugFormat("Adding section : {0}", docTitle);
            BookDocument sectionDocument = null;
            bool firstDocumentOfSplit = true;
            ConverterOptions converterSettings = new ConverterOptions
            {
                CapitalDrop = fbeNotesSection ? false : Settings.CapitalDrop,
                Images = images,
                MaxSize = MaxSize,
                ReferencesManager = referencesManager
            };
            SectionConverter sectionConverter = new SectionConverter
                                                    {
                                                        LinkSection = fbeNotesSection,
                                                        RecursionLevel = GetRecursionLevel(navParent),
                                                        Settings = converterSettings
                                                    };
            foreach (var subitem in sectionConverter.Convert(section))
            {
                sectionDocument = epubFile.AddDocument(docTitle);
                sectionDocument.DocumentType = (navParent==null)?GuideTypeEnum.Text:navParent.DocumentType;
                sectionDocument.Type = (navParent == null) ? SectionTypeEnum.Text: navParent.Type;
                sectionDocument.Content= subitem;
                sectionDocument.NavigationParent = navParent;
                sectionDocument.FileName = string.Format("section{0}.xhtml",++_sectionCounter);
                if (!firstDocumentOfSplit || (sectionDocument.Type == SectionTypeEnum.Links))
                {
                    sectionDocument.NotPartOfNavigation = true;
                }
                firstDocumentOfSplit = false;
            }
            Logger.Log.Debug("Adding sub-sections");
            foreach (var subSection in section.SubSections)
            {
                AddSection(epubFile,subSection,sectionDocument,fbeNotesSection);
            }
        }

        private static int GetRecursionLevel(BookDocument navParent)
        {
            if (navParent == null)
            {
                return 1;
            }
            return navParent.NavigationLevel+1;
        }


        private void PassImagesDataFromFb2ToEpub(EPubFile epubFile, FB2File fb2File)
        {
            images.ConvertFb2ToEpubImages(fb2File.Images,epubFile.Images);
        }

        private void PassHeaderDataFromFb2ToEpub(EPubFile epubFile, FB2File fb2File)
        {
            epubFile.Title.Languages.Clear();
            epubFile.Title.Creators.Clear();
            epubFile.Title.Contributors.Clear();
            epubFile.Title.Subjects.Clear();
            epubFile.Title.Identifiers.Clear();

            if (fb2File.MainBody == null)
            {
                throw new NullReferenceException("MainBody section of the file passed is null");
            }

            Logger.Log.Debug("Passing header data from FB2 to EPUB");
            // cReate new Title page
            epubFile.TitlePage = new TitlePageFile();

            // in case main body title is not defined (empty) 
            if ((fb2File.TitleInfo != null) && (fb2File.TitleInfo.BookTitle != null))
            {
                epubFile.TitlePage.BookTitle = fb2File.TitleInfo.BookTitle.Text;
            }


            // Pass all sequences 
            epubFile.AllSequences.Clear();
            Title bookTitle = new Title();

            if (fb2File.TitleInfo != null)
            {

                foreach (var seq in fb2File.TitleInfo.Sequences)
                {
                    List<string> allSequences = GetSequencesAsStrings(seq);
                    if (allSequences.Count != 0)
                    {
                        foreach (var sequence in allSequences)
                        {
                            epubFile.TitlePage.Series.Add(sequence);
                            epubFile.AllSequences.Add(sequence);
                        }
                    }
                }

                // Getting information from FB2 Title section
                if (fb2File.TitleInfo.BookTitle != null)
                {
                    bookTitle.TitleName =
                    epubFile.Transliterator.Translate(FormatBookTitle( fb2File.TitleInfo),
                                                          epubFile.TranslitMode);
                    bookTitle.Language = string.IsNullOrEmpty(fb2File.TitleInfo.BookTitle.Language)
                                             ? fb2File.TitleInfo.Language
                                             : fb2File.TitleInfo.BookTitle.Language;
                }
                if ((Settings.IgnoreTitle != IgnoreTitleOptions.IgnoreMainTitle) && (Settings.IgnoreTitle != IgnoreTitleOptions.IgnoreMainAndPublish) && 
                    Settings.IgnoreTitle != IgnoreTitleOptions.IgnoreMainAndSource)
                {
                    epubFile.Title.BookTitles.Add(bookTitle);
                }

                epubFile.Title.Languages.Add(fb2File.TitleInfo.Language);

                if (fb2File.TitleInfo.Annotation != null)
                {
                    epubFile.Title.Description = fb2File.TitleInfo.Annotation.ToString();
                    epubFile.AnnotationPage = new AnnotationPageFile();
                    ConverterOptions converterSettings = new ConverterOptions
                    {
                        CapitalDrop = Settings.CapitalDrop,
                        Images = images,
                        MaxSize = MaxSize,
                        ReferencesManager = referencesManager
                    };
                    AnnotationConverter annotationConverter = new AnnotationConverter {Settings = converterSettings};
                    epubFile.AnnotationPage.BookAnnotation = annotationConverter.Convert(fb2File.TitleInfo.Annotation,1);
                }


                // add authors
                foreach (var author in fb2File.TitleInfo.BookAuthors)
                {
                    PersoneWithRole person = new PersoneWithRole();
                    string authorString = GenerateAuthorString(author);
                    person.PersonName = epubFile.Transliterator.Translate(authorString, epubFile.TranslitMode);
                    person.FileAs = GenerateFileAsString(author);
                    person.Role = RolesEnum.Author;
                    person.Language = fb2File.TitleInfo.Language;
                    epubFile.Title.Creators.Add(person);

                    // add authors to Title page
                    epubFile.TitlePage.Authors.Add(authorString);
                }


                foreach (var translator in fb2File.TitleInfo.Translators)
                {
                    PersoneWithRole person = new PersoneWithRole();
                    person.PersonName = epubFile.Transliterator.Translate(GenerateAuthorString(translator),
                                                                          epubFile.TranslitMode);
                    person.FileAs = GenerateFileAsString(translator);
                    person.Role = RolesEnum.Translator;
                    person.Language = fb2File.TitleInfo.Language;
                    epubFile.Title.Contributors.Add(person);
                }

                foreach (var genre in fb2File.TitleInfo.Genres)
                {
                    Subject item = new Subject();
                    item.SubjectInfo = epubFile.Transliterator.Translate(Fb2GenreToDescription(genre.Genre),
                                                                         epubFile.TranslitMode);
                    epubFile.Title.Subjects.Add(item);
                }
            }

            // Getting information from FB2 document section
            Identifier bookId = new Identifier();
            if (!string.IsNullOrEmpty(fb2File.DocumentInfo.ID))
            {
                bookId.ID = fb2File.DocumentInfo.ID;
            }
            else
            {
                bookId.ID = Guid.NewGuid().ToString();
            }
            bookId.IdentifierName = "FB2BookID";
            bookId.Scheme = "URI";
            epubFile.Title.Identifiers.Add(bookId);

            if ((fb2File.DocumentInfo.SourceOCR != null) && !string.IsNullOrEmpty(fb2File.DocumentInfo.SourceOCR.Text))
            {
                epubFile.Title.Source = new Source() {SourceData = fb2File.DocumentInfo.SourceOCR.Text};
            }

            foreach (var docAuthor in fb2File.DocumentInfo.DocumentAuthors)
            {
                PersoneWithRole person = new PersoneWithRole();
                person.PersonName = epubFile.Transliterator.Translate(GenerateAuthorString(docAuthor), epubFile.TranslitMode);
                person.FileAs = GenerateFileAsString(docAuthor);
                person.Role = RolesEnum.Adapter;
                if (fb2File.TitleInfo != null)
                {
                    person.Language = fb2File.TitleInfo.Language;                    
                }
                epubFile.Title.Contributors.Add(person);                
            }

            // Getting information from FB2 Source Title Info section
            if ((fb2File.SourceTitleInfo.BookTitle != null) && !string.IsNullOrEmpty(fb2File.SourceTitleInfo.BookTitle.Text))
            {
                bookTitle = new Title();
                bookTitle.TitleName= epubFile.Transliterator.Translate(fb2File.SourceTitleInfo.BookTitle.Text,epubFile.TranslitMode);
                bookTitle.Language = string.IsNullOrEmpty(fb2File.SourceTitleInfo.BookTitle.Language)&&(fb2File.TitleInfo != null) ? fb2File.TitleInfo.Language : fb2File.SourceTitleInfo.BookTitle.Language;
                if ((Settings.IgnoreTitle != IgnoreTitleOptions.IgnoreSourceTitle) && (Settings.IgnoreTitle != IgnoreTitleOptions.IgnoreMainAndSource) 
                    && Settings.IgnoreTitle != IgnoreTitleOptions.IgnoreSourceAndPublish)
                {
                    epubFile.Title.BookTitles.Add(bookTitle);
                }

                epubFile.Title.Languages.Add(fb2File.SourceTitleInfo.Language);
            }

            // add authors
            foreach (var author in fb2File.SourceTitleInfo.BookAuthors)
            {
                PersoneWithRole person = new PersoneWithRole();
                person.PersonName = epubFile.Transliterator.Translate(string.Format("{0} {1} {2}", author.FirstName, author.MiddleName, author.LastName),epubFile.TranslitMode);
                person.FileAs = GenerateFileAsString(author);
                person.Role = RolesEnum.Author;
                person.Language = fb2File.SourceTitleInfo.Language;
                epubFile.Title.Creators.Add(person);
            }

            foreach (var translator in fb2File.SourceTitleInfo.Translators)
            {
                PersoneWithRole person = new PersoneWithRole();
                person.PersonName = epubFile.Transliterator.Translate(string.Format("{0} {1} {2}", translator.FirstName, translator.MiddleName, translator.LastName),epubFile.TranslitMode);
                person.FileAs = GenerateFileAsString(translator);
                person.Role = RolesEnum.Translator;
                person.Language = fb2File.SourceTitleInfo.Language;
                epubFile.Title.Contributors.Add(person);
            }


            foreach (var genre in fb2File.SourceTitleInfo.Genres)
            {
                Subject item = new Subject();
                item.SubjectInfo = epubFile.Transliterator.Translate(Fb2GenreToDescription(genre.Genre),epubFile.TranslitMode);
                if (epubFile.Title.Subjects.Contains(item))
                {
                    epubFile.Title.Subjects.Add(item);                    
                }

            }


            if (fb2File.PublishInfo.BookName != null)
            {
                bookTitle = new Title();
                bookTitle.TitleName = epubFile.Transliterator.Translate(fb2File.PublishInfo.BookName.Text,epubFile.TranslitMode);
                bookTitle.Language = !string.IsNullOrEmpty(fb2File.PublishInfo.BookName.Language) ? fb2File.PublishInfo.BookName.Language : fb2File.TitleInfo.Language;
                if ((Settings.IgnoreTitle != IgnoreTitleOptions.IgnorePublishTitle) && (Settings.IgnoreTitle != IgnoreTitleOptions.IgnoreMainAndPublish) && 
                    Settings.IgnoreTitle != IgnoreTitleOptions.IgnoreSourceAndPublish)
                {
                    epubFile.Title.BookTitles.Add(bookTitle);
                }
            }


            if (fb2File.PublishInfo.ISBN != null)
            {
                bookId = new Identifier
                             {
                                 IdentifierName = "BookISBN",
                                 ID = fb2File.PublishInfo.ISBN.Text,
                                 Scheme = "ISBN"
                             };
                epubFile.Title.Identifiers.Add(bookId);
            }


            if (fb2File.PublishInfo.Publisher != null )
            {
                epubFile.Title.Publisher.PublisherName = epubFile.Transliterator.Translate(fb2File.PublishInfo.Publisher.Text,epubFile.TranslitMode);
            }


            try
            {
                if (fb2File.PublishInfo.Year.HasValue)
                {
                    DateTime date = new DateTime(fb2File.PublishInfo.Year.Value, 1, 1);
                    epubFile.Title.DatePublished = date;
                }
            }
            catch (FormatException ex)
            {
                Logger.Log.DebugFormat("Date reading format exeception: {0}",ex.ToString());
            }
            catch (Exception exAll)
            {
                Logger.Log.ErrorFormat("Date reading exeception: {0}", exAll.ToString());
            }

            // if we have at least one coverpage image
            if ((fb2File.TitleInfo.Cover != null) && (fb2File.TitleInfo.Cover.HasImages()) && (fb2File.TitleInfo.Cover.CoverpageImages[0].HRef != null))
            {
                // we add just first one 
                epubFile.AddCoverImage(fb2File.TitleInfo.Cover.CoverpageImages[0].HRef);
                images.ImageIdUsed(fb2File.TitleInfo.Cover.CoverpageImages[0].HRef);
            }
                        
            epubFile.Title.DateFileCreation = DateTime.Now;


        }

        private string GenerateFileAsString(AuthorType author)
        {
            ProcessAuthorFormat processor = new ProcessAuthorFormat();
            processor.Format = Settings.FileAsFormat;
            return processor.GenerateAuthorString(author);

        }

        private string GenerateAuthorString(AuthorType author)
        {
            ProcessAuthorFormat processor = new ProcessAuthorFormat();
            processor.Format = Settings.AuthorFormat;
            return processor.GenerateAuthorString(author);
        }



        private string FormatBookTitle(ItemTitleInfo titleInfo)
        {
            ProcessSeqFormatString formatTitle = new ProcessSeqFormatString();
            formatTitle.BookTitleFormatSeqNum = Settings.SequenceFormat;
            formatTitle.BookTitleFormatNoSeqNum = Settings.NoSequenceFormat;
            formatTitle.BookTitleFormatNoSeries = Settings.NoSeriesFormat;

            String rc;
            if ((titleInfo.Sequences.Count > 0) && Settings.AddSeqToTitle)
            {
                rc = formatTitle.GenerateBookTitle(titleInfo.BookTitle.Text, titleInfo.Sequences[0].Name,
                                                   titleInfo.Sequences[0].Number);
            }
            else
            {
                rc = formatTitle.GenerateBookTitle(titleInfo.BookTitle.Text, "", 0);
            }
            return rc;
        }


        private static List<string> GetSequencesAsStrings(SequenceType seq)
        {
            List<string> allSequences =  new List<string>();
            if (seq != null)
            {
                if (!string.IsNullOrEmpty(seq.Name))
                {
                    string sequence = seq.Name;
                    if (seq.Number.HasValue && seq.Number != 0)
                    {
                        sequence = string.Format("{0} - {1}", seq.Name, seq.Number);
                    }
                    allSequences.Add(sequence);
                }
                if (seq.SubSections != null)
                {
                    foreach (var subSection in seq.SubSections)
                    {
                        foreach (var asString in GetSequencesAsStrings(subSection))
                        {
                            allSequences.Add(asString);
                        }
                    }
                }
            }
            return allSequences;
        }


        private static string Fb2GenreToDescription(string genre)
        {
            // TODO: implement real description conversion
            return genre;
        }



        /// <summary>
        /// Reset the object to default (nothing converted) state
        /// </summary>
        private void Reset()
        {
            _sectionCounter = 0;
            referencesManager.Reset();
        }


        public void ConvertXml(XDocument fb2Document)
        {
            fb2Files.Clear();
            FB2File file = new FB2File();
            try
            {
                file.Load(fb2Document, false);
                fb2Files.Add(file);
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Error loading XML document : {0}", ex);
            }
        }
    }
}