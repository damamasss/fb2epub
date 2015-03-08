﻿using System;
using System.IO;
using System.Reflection;
using ConverterContracts.Settings;
using EPubLibrary;
using EPubLibrary.CSS_Items;
using EPubLibrary.XHTML_Items;
using EPubLibraryContracts.Settings;
using Fb2ePubConverter;
using FB2EPubConverter.ElementConvertersV3;
using FB2Library;
using FB2Library.HeaderItems;
using TranslitRu;
using XHTMLClassLibrary.BaseElements;
using XHTMLClassLibrary.BaseElements.BlockElements;
using EPubLibrary.Content.Guide;

namespace FB2EPubConverter
{
    internal class Fb2EPubConverterEngineV3 : Fb2EPubConverterEngineBase
    {
        private readonly  HRefManagerV3 _referencesManager = new HRefManagerV3();

        private const string DefaultCSSFileName = "default_v3.css";

        protected override void ConvertContent(FB2File fb2File, IEpubFile epubFile)
        {
            var epubFileV3 = epubFile as EPubFileV3;
            if (epubFileV3 == null)
            {
                throw new ArrayTypeMismatchException(string.Format("Invalid ePub object type passed, expected EPubFileV3, got {0}",epubFile.GetType()));
            }
           
            PassHeaderDataFromFb2ToEpub(epubFileV3, fb2File);
            ConvertAnnotation(fb2File.TitleInfo, epubFileV3);
            PassCoverImageFromFB2(fb2File.TitleInfo.Cover, epubFileV3);
            SetupCSS(epubFileV3);
            SetupFonts(epubFileV3);
            PassTextFromFb2ToEpub(epubFileV3, fb2File);
            PassFb2InfoToEpub(epubFileV3, fb2File);
            UpdateInternalLinks(epubFileV3, fb2File);
            PassImagesDataFromFb2ToEpub(epubFileV3, fb2File);
            AddAboutInformation(epubFileV3);
        }

        private void SetupCSS(EPubFileV3 epubFile)
        {
            Assembly asm = Assembly.GetAssembly(GetType());
            string pathPreffix = Path.GetDirectoryName(asm.Location);
            if (!string.IsNullOrEmpty(Settings.ResourcesPath))
            {
                pathPreffix = Settings.ResourcesPath;
            }
            epubFile.CSSFiles.Add(new CSSFile { FilePathOnDisk = string.Format(@"{0}\CSS\{1}", pathPreffix, DefaultCSSFileName), FileName = DefaultCSSFileName });
        }


        private void SetupFonts(EPubFileV3 epubFile)
        {
            if (Settings.CommonSettings.Fonts == null)
            {
                Logger.Log.Warn("No fonts defined in configuration file.");
                return;
            }
            epubFile.SetEPubFonts(Settings.CommonSettings.Fonts, Settings.ResourcesPath, Settings.CommonSettings.DecorateFontNames);
        }

        private void AddAboutInformation(EPubFileV3 epubFile)
        {
            Assembly asm = Assembly.GetAssembly(GetType());
            string version = "???";
            if (asm != null)
            {
                version = asm.GetName().Version.ToString();
            }
            epubFile.InjectLKRLicense = true;
            epubFile.CreatorSoftwareString = string.Format(@"Fb2epub v{0} [http://www.fb2epub.net]", version);

            if (!Settings.CommonSettings.SkipAboutPage)
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

        private void PassImagesDataFromFb2ToEpub(EPubFileV3 epubFile, FB2File fb2File)
        {
            Images.ConvertFb2ToEpubImages(fb2File.Images, epubFile.Images);
        }


        private void UpdateInternalLinks(EPubFileV3 epubFile, FB2File fb2File)
        {
            _referencesManager.RemoveInvalidAnchors();
            _referencesManager.RemoveInvalidImages(fb2File.Images);
            _referencesManager.RemapAnchors(epubFile);
        }


        private void PassTextFromFb2ToEpub(EPubFileV3 epubFile, FB2File fb2File)
        {
            var converter = new Fb2EPubTextConverterV3(Settings.CommonSettings, Images, _referencesManager, Settings.V3Settings.HTMLFileMaxSize);
            converter.Convert(epubFile, fb2File);
        }

        /// <summary>
        /// Passes FB2 info to the EPub file to be added at the end of the book
        /// </summary>
        /// <param name="epubFile">destination epub object</param>
        /// <param name="fb2File">source fb2 object</param>
        private void PassFb2InfoToEpub(EPubFileV3 epubFile, FB2File fb2File)
        {
            if (!Settings.CommonSettings.Fb2Info)
            {
                return;
            }
            BookDocument infoDocument = epubFile.AddDocument("FB2 Info");
            var converterSettings = new ConverterOptionsV3
            {
                CapitalDrop = false,
                Images = Images,
                MaxSize = Settings.V3Settings.HTMLFileMaxSize,
                ReferencesManager = _referencesManager,
            };
            var infoConverter = new Fb2EpubInfoConverterV3();
            infoDocument.Content = infoConverter.Convert(fb2File, converterSettings);
            infoDocument.FileName = "fb2info.xhtml";
            infoDocument.DocumentType = GuideTypeEnum.Notes;
            infoDocument.Type = SectionTypeEnum.Text;
            infoDocument.NotPartOfNavigation = true;
        }

        private void PassHeaderDataFromFb2ToEpub(EPubFileV3 epubFile, FB2File fb2File)
        {
            Logger.Log.Debug("Passing header data from FB2 to EPUB");

            if (fb2File.MainBody == null)
            {
                throw new NullReferenceException("MainBody section of the file passed is null");
            }
            var headerDataConverter = new HeaderDataConverterV3(Settings.CommonSettings,Settings.V3Settings);
            headerDataConverter.Convert(epubFile, fb2File);
        }

        private void PassCoverImageFromFB2(CoverPage coverPage, EPubFileV3 epubFile)
        {
            // if we have at least one coverpage image
            if ((coverPage != null) && (coverPage.HasImages()) && (coverPage.CoverpageImages[0].HRef != null))
            {
                // we add just first one 
                epubFile.AddCoverImage(coverPage.CoverpageImages[0].HRef);
                Images.ImageIdUsed(coverPage.CoverpageImages[0].HRef);
            }
        }

        protected override void PassEPubSettings(IEpubFile epubFile)
        {
            base.PassEPubSettings(epubFile);
            epubFile.ContentFileLimit = Settings.V3Settings.HTMLFileMaxSize;
            var epubV3File = epubFile as EPubFileV3;
            if (epubV3File == null)
            {
                throw new ArgumentException(@"The epub object passed is not V3 object","epubFile");
            }
            epubV3File.GenerateCompatibleTOC = Settings.V3Settings.GenerateV2CompatibleTOC;
        }

        private void PassPublisherInfoFromFB2(FB2File fb2File, EPubFileV3 epubFile)
        {
            if (fb2File.PublishInfo.BookTitle != null)
            {
                var bookTitle = new Title
                {
                    TitleName =
                        Rus2Lat.Instance.Translate(fb2File.PublishInfo.BookTitle.Text, epubFile.TranslitMode),
                    Language =
                        !string.IsNullOrEmpty(fb2File.PublishInfo.BookTitle.Language)
                            ? fb2File.PublishInfo.BookTitle.Language
                            : fb2File.TitleInfo.Language
                };
                if ((Settings.CommonSettings.IgnoreTitle != IgnoreInfoSourceOptions.IgnorePublishTitle) && (Settings.CommonSettings.IgnoreTitle != IgnoreInfoSourceOptions.IgnoreMainAndPublish) &&
                    Settings.CommonSettings.IgnoreTitle != IgnoreInfoSourceOptions.IgnoreSourceAndPublish)
                {
                    bookTitle.TitleType = TitleType.PublishInfo;
                    epubFile.Title.BookTitles.Add(bookTitle);
                }
            }


            if (fb2File.PublishInfo.ISBN != null)
            {
                var bookId = new Identifier
                {
                    IdentifierName = "BookISBN",
                    ID = fb2File.PublishInfo.ISBN.Text,
                    Scheme = "ISBN"
                };
                epubFile.Title.Identifiers.Add(bookId);
            }


            if (fb2File.PublishInfo.Publisher != null)
            {
                epubFile.Title.Publisher.PublisherName = Rus2Lat.Instance.Translate(fb2File.PublishInfo.Publisher.Text, epubFile.TranslitMode);
            }


            try
            {
                if (fb2File.PublishInfo.Year.HasValue)
                {
                    var date = new DateTime(fb2File.PublishInfo.Year.Value, 1, 1);
                    epubFile.Title.DatePublished = date;
                }
            }
            catch (FormatException ex)
            {
                Logger.Log.DebugFormat("Date reading format exception: {0}", ex);
            }
            catch (Exception exAll)
            {
                Logger.Log.ErrorFormat("Date reading exception: {0}", exAll);
            }
        }


        private void ConvertAnnotation(ItemTitleInfo titleInfo, EPubFileV3 epubFile)
        {
            if (titleInfo.Annotation != null)
            {
                epubFile.Title.Description = titleInfo.Annotation.ToString();
                epubFile.AnnotationPage = new AnnotationPageFile(HTMLElementType.HTML5);
                var converterSettings = new ConverterOptionsV3
                {
                    CapitalDrop = Settings.CommonSettings.CapitalDrop,
                    Images = Images,
                    MaxSize = Settings.V3Settings.HTMLFileMaxSize,
                    ReferencesManager = _referencesManager,
                };
                var annotationConverter = new AnnotationConverterV3();
                epubFile.AnnotationPage.BookAnnotation = (Div)annotationConverter.Convert(titleInfo.Annotation,
                   new AnnotationConverterParamsV3 { Settings = converterSettings, Level = 1 });
            }
        }


        //private void PassSeriesData(FB2File fb2File, EPubFileV3 epubFile)
        //{
        //    epubFile.Collections.CollectionMembers.Clear();
        //    foreach (var seq in fb2File.TitleInfo.Sequences)
        //    {
        //        if (!string.IsNullOrEmpty(seq.Name))
        //        {
        //            var collectionMember = new CollectionMember
        //            {
        //                CollectionName = seq.Name,
        //                Type = CollectionType.Series,
        //                CollectionPosition = seq.Number
        //            };
        //            epubFile.Collections.CollectionMembers.Add(collectionMember);
        //            foreach (var subseq in seq.SubSections.Where(subseq => !string.IsNullOrEmpty(subseq.Name)))
        //            {
        //                collectionMember = new CollectionMember
        //                {
        //                    CollectionName = subseq.Name,
        //                    Type = CollectionType.Set,
        //                    CollectionPosition = subseq.Number
        //                };
        //                epubFile.Collections.CollectionMembers.Add(collectionMember);
        //            }
        //        }
        //    }
        //}

        protected override IEpubFile CreateEpub()
        {
            return new EPubFileV3(Settings.V3Settings.V3SubStandard == EPubV3SubStandard.V30 ? V3Standard.V30 : V3Standard.V301)
            {
                FlatStructure = Settings.CommonSettings.Flat,
                EmbedStyles = Settings.CommonSettings.EmbedStyles
            };
        }
    }
}
