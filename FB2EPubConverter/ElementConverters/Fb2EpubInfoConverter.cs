﻿using System;
using System.Linq;
using System.Text;
using EPubLibrary.ReferenceUtils;
using FB2Library;
using FB2Library.HeaderItems;
using XHTMLClassLibrary.BaseElements;
using XHTMLClassLibrary.BaseElements.BlockElements;
using XHTMLClassLibrary.BaseElements.InlineElements.TextBasedElements;
using XHTMLClassLibrary.BaseElements.ListElements;

namespace FB2EPubConverter.ElementConverters
{
    /// <summary>
    /// Used to convert FB2 information section into EPUB document content to generate FB2Info page(s)
    /// </summary>
    internal class Fb2EpubInfoConverter : BaseElementConverter
    {
        private static string GetAuthorAsSting(AuthorType author)
        {
            var sb = new StringBuilder();
            if ((author.FirstName != null) && !string.IsNullOrEmpty(author.FirstName.Text))
            {
                sb.AppendFormat("{0} ", author.FirstName.Text);
            }

            if ((author.MiddleName != null) && !string.IsNullOrEmpty(author.MiddleName.Text)) 
            {
                sb.AppendFormat("{0} ", author.MiddleName.Text);
            }

            if ((author.LastName != null) && !string.IsNullOrEmpty(author.LastName.Text))
            {
                sb.AppendFormat("{0} ", author.LastName.Text);
            }

            if ((author.NickName != null) && !string.IsNullOrEmpty(author.NickName.Text))
            {
                sb.AppendFormat(sb.Length == 0 ? "{0} " : "({0}) ", author.NickName.Text);
            }

            if ((author.UID != null) && !string.IsNullOrEmpty(author.UID.Text))
            {
                sb.AppendFormat(": {0}", author.UID.Text);
            }
            return sb.ToString();
        }


        public HTMLItem Convert(FB2File fb2File, HTMLElementType compatibility, ConverterOptions settings)
        {
            if (fb2File == null)
            {
                throw new ArgumentNullException("fb2File");
            }
            var info = new Div(compatibility);
            var header = new H3(compatibility);
            header.Add(new SimpleHTML5Text(compatibility) { Text = "FB2 document info"});
            info.Add(header);
            if (fb2File.DocumentInfo != null)
            {
                if ( !string.IsNullOrEmpty(fb2File.DocumentInfo.ID) )
                {
                    var p = new Paragraph(compatibility);
                    p.Add(new SimpleHTML5Text(compatibility) { Text = string.Format("Document ID:  {0}", fb2File.DocumentInfo.ID) });
                    info.Add(p);
                }
                if (fb2File.DocumentInfo.DocumentVersion.HasValue)
                {
                    var p = new Paragraph(compatibility);
                    p.Add(new SimpleHTML5Text(compatibility) { Text = string.Format("Document version:  {0}", fb2File.DocumentInfo.DocumentVersion) });
                    info.Add(p);                    
                }
                if ((fb2File.DocumentInfo.DocumentDate != null) && !string.IsNullOrEmpty(fb2File.DocumentInfo.DocumentDate.Text))
                {
                    var p = new Paragraph(compatibility);
                    p.Add(new SimpleHTML5Text(compatibility) { Text = string.Format("Document creation date:  {0}", fb2File.DocumentInfo.DocumentDate.Text) });
                    info.Add(p);
                }
                if ( (fb2File.DocumentInfo.ProgramUsed2Create != null) && !string.IsNullOrEmpty(fb2File.DocumentInfo.ProgramUsed2Create.Text) )
                {
                    var p = new Paragraph(compatibility);
                    p.Add(new SimpleHTML5Text(compatibility) { Text = string.Format("Created using:  {0} software", fb2File.DocumentInfo.ProgramUsed2Create.Text) });
                    info.Add(p);                    
                }
                if ((fb2File.DocumentInfo.SourceOCR != null) && !string.IsNullOrEmpty(fb2File.DocumentInfo.SourceOCR.Text))
                {
                    var p = new Paragraph(compatibility);
                    p.Add(new SimpleHTML5Text(compatibility) { Text = string.Format("OCR Source:  {0}", fb2File.DocumentInfo.SourceOCR.Text) });
                    info.Add(p);                                        
                }
                if ((fb2File.DocumentInfo.DocumentAuthors != null) && (fb2File.DocumentInfo.DocumentAuthors.Count > 0))
                {
                    var heading = new H4(compatibility);
                    heading.Add(new SimpleHTML5Text(compatibility) { Text = "Document authors :" });
                    info.Add(heading);
                    var authors = new UnorderedList(compatibility) ;
                    foreach (var author in fb2File.DocumentInfo.DocumentAuthors)
                    {
                        var li = new ListItem(compatibility);
                        li.Add(new SimpleHTML5Text(compatibility) { Text = GetAuthorAsSting(author)});
                        authors.Add(li);
                    }
                    info.Add(authors);
                }
                if ((fb2File.DocumentInfo.DocumentPublishers != null) && (fb2File.DocumentInfo.DocumentPublishers.Count > 0))
                {
                    var heading = new H4(compatibility);
                    heading.Add(new SimpleHTML5Text(compatibility) { Text = "Document publishers :" });
                    info.Add(heading);

                    var publishers = new UnorderedList(compatibility);
                    foreach (var publisher in fb2File.DocumentInfo.DocumentPublishers)
                    {
                        var li = new ListItem(compatibility);
                        li.Add(new SimpleHTML5Text(compatibility) { Text = GetAuthorAsSting(publisher) });
                        publishers.Add(li);
                    }
                    info.Add(publishers);
                }

                if ((fb2File.DocumentInfo.SourceURLs != null) && (fb2File.DocumentInfo.SourceURLs.Any()))
                {
                    var heading = new H4(compatibility);
                    heading.Add(new SimpleHTML5Text(compatibility) { Text = "Source URLs :" });
                    info.Add(heading);

                    var urls = new UnorderedList(compatibility);
                    foreach (var url in fb2File.DocumentInfo.SourceURLs)
                    {
                        var li = new ListItem(compatibility);
                        if (ReferencesUtils.IsExternalLink(url))
                        {
                            var link = new Anchor(compatibility);
                            link.HRef.Value = url;
                            link.Add(new SimpleHTML5Text(compatibility) {Text = url});
                            li.Add(link);
                        }
                        else
                        {
                            li.Add(new SimpleHTML5Text(compatibility) { Text = url });
                        }
                        urls.Add(li);
                    }
                    info.Add(urls);
                }

                if (fb2File.DocumentInfo.History != null)
                {
                    var heading = new H4(compatibility);
                    heading.Add(new SimpleHTML5Text(compatibility) { Text = "Document history:" });
                    info.Add(heading);
                    var annotationConverter = new AnnotationConverter();
                    info.Add(annotationConverter.Convert(fb2File.DocumentInfo.History, compatibility, new AnnotationConverterParams { Level = 1, Settings = settings }));
                    //Paragraph p = new Paragraph();
                    //p.Add(new SimpleHTML5Text() { Text = fb2File.DocumentInfo.History.ToString() });
                    //info.Add(p);                                                            
                }
            }

            // in case there is no elements - no need for a header
            if (info.SubElements().Count <= 1)
            {
                info.Remove(header);
            }

            info.GlobalAttributes.Class.Value = "fb2_info";
            return info;
            
        }
    }
}
