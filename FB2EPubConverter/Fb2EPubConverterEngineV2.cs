﻿using System;
using System.IO;
using System.Reflection;
using EPubLibrary;
using Fb2ePubConverter;
using Fb2epubSettings;
using FB2Library;
using FB2Library.Elements;
using XHTMLClassLibrary.BaseElements;
using Logger = Fb2ePubConverter.Logger;
using EPubLibrary.CSS_Items;
using EPubLibrary.AppleEPubV2Extensions;
using EPubLibrary.Content.Guide;
using EPubLibrary.XHTML_Items;
using FB2EPubConverter.ElementConvertersV2;
using XHTMLClassLibrary.BaseElements.BlockElements;
using System.Linq;

namespace FB2EPubConverter
{
    internal class Fb2EPubConverterEngineV2 : Fb2EPubConverterEngineBase
    {
        private long _maxSize = 245 * 1024;
        private int _sectionCounter;


        /// <summary>
        /// Get/Set max document size in bytes
        /// </summary>
        public long MaxSize
        {
            get { return _maxSize; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("value");
                }
                _maxSize = value;
            }
        }

        protected override void ConvertContent(FB2File fb2File, EPubFile epubFile)
        {
            SetAdobeOptions(epubFile);
            PassHeaderDataFromFb2ToEpub(epubFile, fb2File);
            SetupCSS(epubFile);
            SetupFonts(epubFile);
            SetupAppleSettings(epubFile);
            PassTextFromFb2ToEpub(epubFile, fb2File);
            if (Settings.CommonSettings.Fb2Info)
            {
                PassFb2InfoToEpub(epubFile, fb2File);
            }
            UpdateInternalLinks(epubFile, fb2File);
            PassImagesDataFromFb2ToEpub(epubFile, fb2File);
        }

        private void SetAdobeOptions(EPubFile epubFile)
        {
            epubFile.AdobeTemplatePath = string.IsNullOrEmpty(Settings.V2Settings.AdobeTemplatePath) ? @".\Template\template.xpgt" : Settings.V2Settings.AdobeTemplatePath;
            epubFile.UseAdobeTemplate = Settings.V2Settings.EnableAdobeTemplate;
        }

        private void SetupAppleSettings(EPubFile epubFile)
        {
            if (epubFile == null)
            {
                throw new ArgumentNullException("epubFile");
            }
            // setup epub2 options
            epubFile.AppleOptions.Reset();
            foreach (var platform in Settings.V2Settings.AppleConverterEPubSettings.V2Settings.Platforms)
            {
                var targetPlatform = new AppleTargetPlatform();
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
                switch (platform.OrientationLock)
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
            ReferencesManager.RemoveInvalidAnchors();
            ReferencesManager.RemoveInvalidImages(fb2File.Images);
            ReferencesManager.RemapAnchors(epubFile);
        }


        /// <summary>
        /// Passes FB2 info to the EPub file to be added at the end of the book
        /// </summary>
        /// <param name="epubFile">destination epub object</param>
        /// <param name="fb2File">source fb2 object</param>
        private void PassFb2InfoToEpub(EPubFile epubFile, FB2File fb2File)
        {
            BookDocument infoDocument = epubFile.AddDocument("FB2 Info");
            var converterSettings = new ConverterOptionsV2
            {
                CapitalDrop = false,
                Images = Images,
                MaxSize = MaxSize,
                ReferencesManager = ReferencesManager,
            };
            var infoConverter = new Fb2EpubInfoConverterV2();
            infoDocument.Content = infoConverter.Convert(fb2File, converterSettings);
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
            epubFile.CSSFiles.Add(new CSSFile { FilePathOnDisk = string.Format(@"{0}\{1}", pathPreffix, @"CSS\default.css"), FileName = "default.css" });
        }

        private void SetupFonts(EPubFile epubFile)
        {
            if (Settings.CommonSettings.Fonts == null)
            {
                Logger.Log.Warn("No fonts defined in configuration file.");
                return;
            }
            epubFile.SetEPubFonts(Settings.CommonSettings.Fonts, Settings.ResourcesPath, Settings.CommonSettings.DecorateFontNames);
        }


        private void PassTextFromFb2ToEpub(EPubFile epubFile, FB2File fb2File)
        {
            // create second title page
            if ((fb2File.MainBody.Title != null) && (!string.IsNullOrEmpty(fb2File.MainBody.Title.ToString())))
            {
                string docTitle = fb2File.MainBody.Title.ToString();
                Logger.Log.DebugFormat("Adding section : {0}", docTitle);
                BookDocument addTitlePage = epubFile.AddDocument(docTitle);
                addTitlePage.DocumentType = GuideTypeEnum.TitlePage;
                addTitlePage.Content = new Div(HTMLElementType.XHTML11);
                var converterSettings = new ConverterOptionsV2
                {
                    CapitalDrop = Settings.CommonSettings.CapitalDrop,
                    Images = Images,
                    MaxSize = MaxSize,
                    ReferencesManager = ReferencesManager,
                };
                var titleConverter = new TitleConverterV2();
                addTitlePage.Content.Add(titleConverter.Convert(fb2File.MainBody.Title, 
                    new TitleConverterParamsV2 { Settings = converterSettings, TitleLevel = 2 }));
                addTitlePage.NavigationParent = null;
                addTitlePage.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
                addTitlePage.NotPartOfNavigation = true;
            }

            BookDocument mainDocument = null;
            if (!string.IsNullOrEmpty(fb2File.MainBody.Name))
            {
                string docTitle = fb2File.MainBody.Name;
                Logger.Log.DebugFormat("Adding section : {0}", docTitle);
                mainDocument = epubFile.AddDocument(docTitle);
                mainDocument.DocumentType = GuideTypeEnum.Text;
                mainDocument.Content = new Div(HTMLElementType.XHTML11);
                mainDocument.NavigationParent = null;
                mainDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
            }

            if ((fb2File.MainBody.ImageName != null) && !string.IsNullOrEmpty(fb2File.MainBody.ImageName.HRef))
            {
                if (mainDocument == null)
                {
                    string newDocTitle = ((fb2File.MainBody.Title != null) && (!string.IsNullOrEmpty(fb2File.MainBody.Title.ToString()))) ? fb2File.MainBody.Title.ToString() : "main";
                    mainDocument = epubFile.AddDocument(newDocTitle);
                    mainDocument.DocumentType = GuideTypeEnum.Text;
                    mainDocument.Content = new Div(HTMLElementType.XHTML11);
                    mainDocument.NavigationParent = null;
                    mainDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
                }
                if (Images.IsImageIdReal(fb2File.MainBody.ImageName.HRef))
                {
                    var enclosing = new Div(HTMLElementType.XHTML11); // we use the enclosing so the user can style center it
                    var converterSettings = new ConverterOptionsV2
                    {
                        CapitalDrop = Settings.CommonSettings.CapitalDrop,
                        Images = Images,
                        MaxSize = MaxSize,
                        ReferencesManager = ReferencesManager,
                    };

                    var imageConverter = new ImageConverterV2();
                    enclosing.Add(imageConverter.Convert(fb2File.MainBody.ImageName,  new ImageConverterParamsV2 { Settings = converterSettings }));
                    enclosing.GlobalAttributes.Class.Value = "body_image";
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
                    mainDocument.Content = new Div(HTMLElementType.XHTML11);
                    mainDocument.NavigationParent = null;
                    mainDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
                }
                var converterSettings = new ConverterOptionsV2
                {
                    CapitalDrop = Settings.CommonSettings.CapitalDrop,
                    Images = Images,
                    MaxSize = MaxSize,
                    ReferencesManager = ReferencesManager,
                };

                var epigraphConverter = new MainEpigraphConverterV2();
                mainDocument.Content.Add(epigraphConverter.Convert(ep, 
                    new EpigraphConverterParams { Settings = converterSettings, Level = 1 }));
            }

            Logger.Log.Debug("Adding main sections");
            foreach (var section in fb2File.MainBody.Sections)
            {
                AddSection(epubFile, section, mainDocument, false);
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
            var sectionDocument = epubFile.AddDocument(docTitle);
            sectionDocument.DocumentType = GuideTypeEnum.Glossary;
            sectionDocument.Type = SectionTypeEnum.Links;
            sectionDocument.Content = new Div(HTMLElementType.XHTML11);
            if (bodyItem.Title != null)
            {
                var converterSettings = new ConverterOptionsV2
                {
                    CapitalDrop = false,
                    Images = Images,
                    MaxSize = MaxSize,
                    ReferencesManager = ReferencesManager,
                };
                var titleConverter = new TitleConverterV2();
                sectionDocument.Content.Add(titleConverter.Convert(bodyItem.Title,
                    new TitleConverterParamsV2 { Settings = converterSettings, TitleLevel = 1 }));
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
            var sectionDocument = epubFile.AddDocument(docTitle);
            sectionDocument.DocumentType = GuideTypeEnum.Text;
            sectionDocument.Type = SectionTypeEnum.Text;
            sectionDocument.Content = new Div(HTMLElementType.XHTML11);
            if (bodyItem.Title != null)
            {
                var converterSettings = new ConverterOptionsV2
                {
                    CapitalDrop = Settings.CommonSettings.CapitalDrop,
                    Images = Images,
                    MaxSize = MaxSize,
                    ReferencesManager = ReferencesManager,
                };
                var titleConverter = new TitleConverterV2();
                sectionDocument.Content.Add(titleConverter.Convert(bodyItem.Title, 
                    new TitleConverterParamsV2 { Settings = converterSettings, TitleLevel = 1 }));
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

        private void PassImagesDataFromFb2ToEpub(EPubFile epubFile, FB2File fb2File)
        {
            Images.ConvertFb2ToEpubImages(fb2File.Images, epubFile.Images);
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


            PassTitleInfoFromFB2EPub(fb2File, epubFile);

            // Getting information from FB2 document section
            var bookId = new Identifier
            {
                ID =
                    !string.IsNullOrEmpty(fb2File.DocumentInfo.ID) ? fb2File.DocumentInfo.ID : Guid.NewGuid().ToString(),
                IdentifierName = "FB2BookID",
                Scheme = "URI"
            };
            epubFile.Title.Identifiers.Add(bookId);

            if ((fb2File.DocumentInfo.SourceOCR != null) && !string.IsNullOrEmpty(fb2File.DocumentInfo.SourceOCR.Text))
            {
                epubFile.Title.Source = new Source { SourceData = fb2File.DocumentInfo.SourceOCR.Text };
            }

            foreach (var docAuthor in fb2File.DocumentInfo.DocumentAuthors)
            {
                var person = new PersoneWithRole
                {
                    PersonName =
                        epubFile.Transliterator.Translate(GenerateAuthorString(docAuthor), epubFile.TranslitMode),
                    FileAs = GenerateFileAsString(docAuthor),
                    Role = RolesEnum.Adapter
                };
                if (fb2File.TitleInfo != null)
                {
                    person.Language = fb2File.TitleInfo.Language;
                }
                epubFile.Title.Contributors.Add(person);
            }

            // Getting information from FB2 Source Title Info section
            if ((fb2File.SourceTitleInfo.BookTitle != null) && !string.IsNullOrEmpty(fb2File.SourceTitleInfo.BookTitle.Text))
            {
                var bookTitle = new Title
                {
                    TitleName =
                        epubFile.Transliterator.Translate(fb2File.SourceTitleInfo.BookTitle.Text, epubFile.TranslitMode),
                    Language =
                        string.IsNullOrEmpty(fb2File.SourceTitleInfo.BookTitle.Language) && (fb2File.TitleInfo != null)
                            ? fb2File.TitleInfo.Language
                            : fb2File.SourceTitleInfo.BookTitle.Language
                };
                if ((Settings.CommonSettings.IgnoreTitle != IgnoreTitleOptions.IgnoreSourceTitle) && (Settings.CommonSettings.IgnoreTitle != IgnoreTitleOptions.IgnoreMainAndSource)
                    && Settings.CommonSettings.IgnoreTitle != IgnoreTitleOptions.IgnoreSourceAndPublish)
                {
                    bookTitle.TitleType = TitleType.SourceInfo;
                    epubFile.Title.BookTitles.Add(bookTitle);
                }

                epubFile.Title.Languages.Add(fb2File.SourceTitleInfo.Language);
            }

            // add authors
            foreach (var author in fb2File.SourceTitleInfo.BookAuthors)
            {
                var person = new PersoneWithRole
                {
                    PersonName =
                        epubFile.Transliterator.Translate(
                            string.Format("{0} {1} {2}", author.FirstName, author.MiddleName, author.LastName),
                            epubFile.TranslitMode),
                    FileAs = GenerateFileAsString(author),
                    Role = RolesEnum.Author,
                    Language = fb2File.SourceTitleInfo.Language
                };
                epubFile.Title.Creators.Add(person);
            }

            foreach (var translator in fb2File.SourceTitleInfo.Translators)
            {
                var person = new PersoneWithRole
                {
                    PersonName =
                        epubFile.Transliterator.Translate(
                            string.Format("{0} {1} {2}", translator.FirstName, translator.MiddleName,
                                translator.LastName), epubFile.TranslitMode),
                    FileAs = GenerateFileAsString(translator),
                    Role = RolesEnum.Translator,
                    Language = fb2File.SourceTitleInfo.Language
                };
                epubFile.Title.Contributors.Add(person);
            }


            foreach (var genre in fb2File.SourceTitleInfo.Genres)
            {
                var item = new Subject
                {
                    SubjectInfo =
                        epubFile.Transliterator.Translate(Fb2GenreToDescription(genre.Genre), epubFile.TranslitMode)
                };
                if (epubFile.Title.Subjects.Contains(item))
                {
                    epubFile.Title.Subjects.Add(item);
                }

            }


            if (fb2File.PublishInfo.BookName != null)
            {
                var bookTitle = new Title
                {
                    TitleName =
                        epubFile.Transliterator.Translate(fb2File.PublishInfo.BookName.Text, epubFile.TranslitMode),
                    Language =
                        !string.IsNullOrEmpty(fb2File.PublishInfo.BookName.Language)
                            ? fb2File.PublishInfo.BookName.Language
                            : fb2File.TitleInfo.Language
                };
                if ((Settings.CommonSettings.IgnoreTitle != IgnoreTitleOptions.IgnorePublishTitle) && (Settings.CommonSettings.IgnoreTitle != IgnoreTitleOptions.IgnoreMainAndPublish) &&
                    Settings.CommonSettings.IgnoreTitle != IgnoreTitleOptions.IgnoreSourceAndPublish)
                {
                    bookTitle.TitleType = TitleType.PublishInfo;
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


            if (fb2File.PublishInfo.Publisher != null)
            {
                epubFile.Title.Publisher.PublisherName = epubFile.Transliterator.Translate(fb2File.PublishInfo.Publisher.Text, epubFile.TranslitMode);
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

            // if we have at least one coverpage image
            if ((fb2File.TitleInfo.Cover != null) && (fb2File.TitleInfo.Cover.HasImages()) && (fb2File.TitleInfo.Cover.CoverpageImages[0].HRef != null))
            {
                // we add just first one 
                epubFile.AddCoverImage(fb2File.TitleInfo.Cover.CoverpageImages[0].HRef);
                Images.ImageIdUsed(fb2File.TitleInfo.Cover.CoverpageImages[0].HRef);
            }

            epubFile.Title.DateFileCreation = DateTime.Now;

            PassCalibreMetadata(fb2File, epubFile);
            PassSeriesData(fb2File, epubFile);
        }

        private void PassTitleInfoFromFB2EPub(FB2File fb2File, EPubFile epubFile)
        {
            // cReate new Title page
            epubFile.TitlePage = new TitlePageFile(HTMLElementType.XHTML11);

            // in case main body title is not defined (empty) 
            if ((fb2File.TitleInfo != null) && (fb2File.TitleInfo.BookTitle != null))
            {
                epubFile.TitlePage.BookTitle = fb2File.TitleInfo.BookTitle.Text;
            }

            // Pass all sequences 
            epubFile.AllSequences.Clear();

            if (fb2File.TitleInfo != null)
            {
                ConvertSequences(fb2File, epubFile);

                // Getting information from FB2 Title section
                ConvertMainTitle(fb2File, epubFile);

                ConvertAnnotation(fb2File, epubFile);

                // add authors
                ConvertAuthors(fb2File, epubFile);

                // add translators
                ConvertTranslators(fb2File, epubFile);

                // genres
                ConvertGenres(fb2File, epubFile);

            }
        }

        protected override void ConvertAnnotation(FB2File fb2File, EPubFile epubFile)
        {
            if (fb2File.TitleInfo.Annotation != null)
            {
                epubFile.Title.Description = fb2File.TitleInfo.Annotation.ToString();
                epubFile.AnnotationPage = new AnnotationPageFile(Settings.StandardVersion == EPubVersion.VEpub20 ? HTMLElementType.XHTML11 : HTMLElementType.HTML5);
                var converterSettings = new ConverterOptionsV2
                {
                    CapitalDrop = Settings.CommonSettings.CapitalDrop,
                    Images = Images,
                    MaxSize = MaxSize,
                    ReferencesManager = ReferencesManager,
                };
                var annotationConverter = new AnnotationConverterV2();
                epubFile.AnnotationPage.BookAnnotation = (Div)annotationConverter.Convert(fb2File.TitleInfo.Annotation,
                   new AnnotationConverterParams { Settings = converterSettings, Level = 1 });
            }
        }

        /// <summary>
        /// Pass sequences to epub collections
        /// </summary>
        /// <param name="fb2File"></param>
        /// <param name="epubFile"></param>
        private void PassSeriesData(FB2File fb2File, EPubFile epubFile)
        {
            epubFile.Collections.CollectionMembers.Clear();
            foreach (var seq in fb2File.TitleInfo.Sequences)
            {
                if (!string.IsNullOrEmpty(seq.Name))
                {
                    var collectionMember = new CollectionMember
                    {
                        CollectionName = seq.Name,
                        Type = CollectionType.Series,
                        CollectionPosition = seq.Number
                    };
                    epubFile.Collections.CollectionMembers.Add(collectionMember);
                    foreach (var subseq in seq.SubSections.Where(subseq => !string.IsNullOrEmpty(subseq.Name)))
                    {
                        collectionMember = new CollectionMember
                        {
                            CollectionName = subseq.Name,
                            Type = CollectionType.Set,
                            CollectionPosition = subseq.Number
                        };
                        epubFile.Collections.CollectionMembers.Add(collectionMember);
                    }
                }
            }
        }

        private void PassCalibreMetadata(FB2File fb2File, EPubFile epubFile)
        {
            epubFile.AddCalibreMetadata = Settings.V2Settings.AddCalibreMetadata;

            if (!Settings.V2Settings.AddCalibreMetadata)
            {
                return;
            }
            if (fb2File.TitleInfo != null && fb2File.TitleInfo.BookTitle != null &&
                !string.IsNullOrEmpty(fb2File.TitleInfo.BookTitle.Text))
            {
                epubFile.CalibreMetadata.TitleForSort = fb2File.TitleInfo.BookTitle.Text;
            }
            if (fb2File.TitleInfo != null && fb2File.TitleInfo.Sequences.Count > 0 &&
                !string.IsNullOrEmpty(fb2File.TitleInfo.Sequences[0].Name))
            {
                epubFile.CalibreMetadata.SeriesName = fb2File.TitleInfo.Sequences[0].Name;
                if (fb2File.TitleInfo.Sequences[0].Number.HasValue)
                {
                    epubFile.CalibreMetadata.SeriesIndex = fb2File.TitleInfo.Sequences[0].Number.Value;
                }
            }
        }

        /// <summary>
        /// Reset the object to default (nothing converted) state
        /// </summary>
        protected override void Reset()
        {
            base.Reset();
            _sectionCounter = 0;
        }

        private void AddSection(EPubFile epubFile, SectionItem section, BookDocument navParent, bool fbeNotesSection)
        {
            string docTitle = string.Empty;
            if (section.Title != null)
            {
                docTitle = section.Title.ToString();
            }
            Logger.Log.DebugFormat("Adding section : {0}", docTitle);
            BookDocument sectionDocument = null;
            bool firstDocumentOfSplit = true;
            var converterSettings = new ConverterOptionsV2
            {
                CapitalDrop = !fbeNotesSection && Settings.CommonSettings.CapitalDrop,
                Images = Images,
                MaxSize = MaxSize,
                ReferencesManager = ReferencesManager,
            };
            var sectionConverter = new SectionConverterV2
            {
                LinkSection = fbeNotesSection,
                RecursionLevel = GetRecursionLevel(navParent),
                Settings = converterSettings
            };
            foreach (var subitem in sectionConverter.Convert(section))
            {
                sectionDocument = epubFile.AddDocument(docTitle);
                sectionDocument.DocumentType = (navParent == null) ? GuideTypeEnum.Text : navParent.DocumentType;
                sectionDocument.Type = (navParent == null) ? SectionTypeEnum.Text : navParent.Type;
                sectionDocument.Content = subitem;
                sectionDocument.NavigationParent = navParent;
                sectionDocument.FileName = string.Format("section{0}.xhtml", ++_sectionCounter);
                if (!firstDocumentOfSplit || (sectionDocument.Type == SectionTypeEnum.Links))
                {
                    sectionDocument.NotPartOfNavigation = true;
                }
                firstDocumentOfSplit = false;
            }
            Logger.Log.Debug("Adding sub-sections");
            foreach (var subSection in section.SubSections)
            {
                AddSection(epubFile, subSection, sectionDocument, fbeNotesSection);
            }
        }

        private static int GetRecursionLevel(BookDocument navParent)
        {
            if (navParent == null)
            {
                return 1;
            }
            return navParent.NavigationLevel + 1;
        }

    }
}