﻿using System;
using System.IO;
using System.Reflection;
using EPubLibrary;
using EPubLibrary.CSS_Items;
using Fb2ePubConverter;
using FB2Library;
using FB2Library.HeaderItems;
using XHTMLClassLibrary.BaseElements;
using EPubLibrary.AppleEPubV2Extensions;
using EPubLibrary.Content.Guide;
using EPubLibrary.XHTML_Items;
using FB2EPubConverter.ElementConvertersV2;
using XHTMLClassLibrary.BaseElements.BlockElements;


namespace FB2EPubConverter
{
    internal class Fb2EPubConverterEngineV2 : Fb2EPubConverterEngineBase
    {
        private readonly HRefManagerV2 _referencesManager = new HRefManagerV2();

        private const string DefaultCSSFileName = "default_v2.css";

        protected override void ConvertContent(FB2File fb2File, IEpubFile epubFile)
        {
            var epubFileV2 = epubFile as EPubFileV2;
            if (epubFileV2 == null)
            {
                throw new ArrayTypeMismatchException(string.Format("Invalid ePub object type passed, expected EPubFileV2, got {0}", epubFile.GetType()));
            }

            PassHeaderDataFromFb2ToEpub(epubFileV2, fb2File);
            ConvertAnnotation(fb2File.TitleInfo, epubFileV2);
            PassCoverImageFromFB2(fb2File.TitleInfo.Cover, epubFileV2);
            SetupCSS(epubFileV2);
            SetupFonts(epubFileV2);
            PassTextFromFb2ToEpub(epubFileV2, fb2File);
            PassFb2InfoToEpub(epubFileV2, fb2File);
            UpdateInternalLinks(epubFileV2, fb2File);
            PassImagesDataFromFb2ToEpub(epubFileV2, fb2File);
            AddAboutInformation(epubFileV2);
        }

        private void SetupCSS(EPubFileV2 epubFile)
        {
            Assembly asm = Assembly.GetAssembly(GetType());
            string pathPreffix = Path.GetDirectoryName(asm.Location);
            if (!string.IsNullOrEmpty(Settings.ResourcesPath))
            {
                pathPreffix = Settings.ResourcesPath;
            }
            epubFile.CSSFiles.Add(new CSSFile { FilePathOnDisk = string.Format(@"{0}\CSS\{1}", pathPreffix, DefaultCSSFileName), FileName = DefaultCSSFileName });
        }


        private void SetupFonts(EPubFileV2 epubFile)
        {
            if (Settings.ConversionSettings.Fonts == null)
            {
                EPubLibrary.Logger.Log.Warn("No fonts defined in configuration file.");
                return;
            }
            epubFile.SetEPubFonts(Settings.ConversionSettings.Fonts, Settings.ResourcesPath, Settings.ConversionSettings.DecorateFontNames);
        }

        private void AddAboutInformation(EPubFileV2 epubFile)
        {
            Assembly asm = Assembly.GetAssembly(GetType());
            string version = "???";
            if (asm != null)
            {
                version = asm.GetName().Version.ToString();
            }
            epubFile.InjectLKRLicense = true;
            epubFile.CreatorSoftwareString = string.Format(@"Fb2epub v{0} [http://www.fb2epub.net]", version);

            if (!Settings.ConversionSettings.SkipAboutPage)
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
        }




        private void UpdateInternalLinks(EPubFileV2 epubFile, FB2File fb2File)
        {
            _referencesManager.RemoveInvalidAnchors();
            _referencesManager.RemoveInvalidImages(fb2File.Images);
            _referencesManager.RemapAnchors(epubFile);
        }


        /// <summary>
        /// Passes FB2 info to the EPub file to be added at the end of the book
        /// </summary>
        /// <param name="epubFile">destination epub object</param>
        /// <param name="fb2File">source fb2 object</param>
        private void PassFb2InfoToEpub(EPubFileV2 epubFile, FB2File fb2File)
        {
            if (!Settings.ConversionSettings.Fb2Info)
            {
                return;
            }
            BookDocument infoDocument = epubFile.AddDocument("FB2 Info");
            var converterSettings = new ConverterOptionsV2
            {
                CapitalDrop = false,
                Images = Images,
                MaxSize = Settings.V2Settings.HTMLFileMaxSize,
                ReferencesManager = _referencesManager,
            };
            var infoConverter = new Fb2EpubInfoConverterV2();
            infoDocument.Content = infoConverter.Convert(fb2File, converterSettings);
            infoDocument.FileName = "fb2info.xhtml";
            infoDocument.DocumentType = GuideTypeEnum.Notes;
            infoDocument.Type = SectionTypeEnum.Text;
            infoDocument.NotPartOfNavigation = true;
        }




        private void PassTextFromFb2ToEpub(EPubFileV2 epubFile, FB2File fb2File)
        {
            var converter = new Fb2EPubTextConverterV2(Settings.ConversionSettings, Images, _referencesManager, Settings.V2Settings.HTMLFileMaxSize);
            converter.Convert(epubFile,fb2File);
        }


        private void PassImagesDataFromFb2ToEpub(EPubFileV2 epubFile, FB2File fb2File)
        {
            Images.ConvertFb2ToEpubImages(fb2File.Images, epubFile.Images);
        }

        private void PassHeaderDataFromFb2ToEpub(EPubFileV2 epubFile, FB2File fb2File)
        {
            Logger.Log.Debug("Passing header data from FB2 to EPUB");

            if (fb2File.MainBody == null)
            {
                throw new NullReferenceException("MainBody section of the file passed is null");
            }

            var headerDataConverter = new HeaderDataConverterV2(Settings.ConversionSettings,Settings.V2Settings);
            headerDataConverter.Convert(epubFile,fb2File);
        }

        private void PassCoverImageFromFB2(CoverPage coverPage, EPubFileV2 epubFile)
        {
            // if we have at least one coverpage image
            if ((coverPage != null) && (coverPage.HasImages()) && (coverPage.CoverpageImages[0].HRef != null))
            {
                // we add just first one 
                epubFile.AddCoverImage(coverPage.CoverpageImages[0].HRef);
                Images.ImageIdUsed(coverPage.CoverpageImages[0].HRef);
            }
        }




        private void ConvertAnnotation(ItemTitleInfo titleInfo, EPubFileV2 epubFile)
        {
            if (titleInfo.Annotation != null)
            {
                epubFile.Title.Description = titleInfo.Annotation.ToString();
                epubFile.AnnotationPage = new AnnotationPageFile(HTMLElementType.XHTML11);
                var converterSettings = new ConverterOptionsV2
                {
                    CapitalDrop = Settings.ConversionSettings.CapitalDrop,
                    Images = Images,
                    MaxSize = Settings.V2Settings.HTMLFileMaxSize,
                    ReferencesManager = _referencesManager,
                };
                var annotationConverter = new AnnotationConverterV2();
                epubFile.AnnotationPage.BookAnnotation = (Div)annotationConverter.Convert(titleInfo.Annotation,
                   new AnnotationConverterParamsV2 { Settings = converterSettings, Level = 1 });
            }
        }


        protected override IEpubFile CreateEpub()
        {
            return new EPubFileV2(Settings.CommonSettings,Settings.V2Settings) { EmbedStyles = Settings.ConversionSettings.EmbedStyles };
        }


    }
}
