using System;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Stendahls.Sc.MediaIndexing.Extractors
{
    public class PdfContentExtractor : IContentExtractor
    {
        private string GetInfoValue(IDictionary<string, string> info, string key)
        {
            if (info == null)
                return null;
            if (!info.ContainsKey(key))
                return null;
            var value = info[key];
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();    // Ensure a new instance here
        }

        // TODO: Make this more generic to support other formats
        public void LoadItemWithMetaData(Item item)
        {
            MediaItem mediaItem = item;
            var ext = mediaItem.Extension.ToLowerInvariant();
            if (ext != "pdf" && mediaItem.MimeType != "application/pdf")
                return;

            string title, keywords, subject, content = null;
            try
            {
                using (var reader = new PdfReader(mediaItem.GetMediaStream()))
                {
                    var info = reader.Info;
                    title = GetInfoValue(info, "Title");
                    keywords = GetInfoValue(info, "Keywords");
                    subject = GetInfoValue(info, "Subject");

                    if (item.Fields["_Content"] != null)
                    {
                        var textWriter = new StringWriter();
                        var strategy = new SimpleTextExtractionStrategy();
                        int length = 0;
                        int cutOffLength = Settings.GetIntSetting("MediaIndexing.PdfTextCutOffLength", 64000);
                        for (int pagenumber = 1; pagenumber <= reader.NumberOfPages; pagenumber++)
                        {
                            var pageText = PdfTextExtractor.GetTextFromPage(reader, pagenumber, strategy);
                            ExtractText(new StringReader(pageText), textWriter, cutOffLength, ref length);
                            if (length >= cutOffLength)
                            {
                                Log.Info($"Breaking PDF content after {length} characters", nameof(PdfContentExtractor));
                                break;
                            }
                        }
                        content = textWriter.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unable to load PDF data from " + item.Paths.Path, ex, nameof(PdfContentExtractor));
                return;
            }

            // Write the shared content field
            MapMediaItemField(item, "_Content", content);
            if (item.Editing.IsEditing)
            {
                item.Editing.EndEdit();
            }

            // Write all version fields
            foreach (var itemVersion in item.Versions.GetVersions(true))
            {
                MapMediaItemField(itemVersion, "Title", title);
                MapMediaItemField(itemVersion, "Keywords", keywords);
                MapMediaItemField(itemVersion, "Description", subject);

                if (itemVersion.Editing.IsEditing)
                {
                    itemVersion.Editing.EndEdit();
                }
            }
        }

        private void MapMediaItemField(Item item, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var field = item.Fields[fieldName];
            if (field == null)
                return;

            if (field.Value != value)
            {
                if (!item.Editing.IsEditing)
                    item.Editing.BeginEdit();
                field.Value = value;
            }
        }

        /// <summary>
        /// Strips out all delimiters and other odd characters, keeping only 
        /// the letters (including support for latin, greek,cyrillic, arabic, 
        /// japanese, chinese, korean character sets). Used for indexing etc
        /// where only the real words are important.
        /// </summary>
        /// <param name="reader">stream to read from</param>
        /// <param name="writer">stream to write to</param>
        /// <param name="cutOffLength">stop processing at the next word delimiter after reaching this length</param>
        /// <param name="length">reference to the total extracted content length</param>
        public static void ExtractText(TextReader reader, TextWriter writer, int cutOffLength, ref int length)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                bool hasLineContent = false;
                bool isWhiteSpace = true;
                foreach (char c in line)
                {
                    // Accept all real letters and digits, including all languages.
                    // Ranges are picked from the Unicode Character Ranges specification
                    // http://jrgraphix.net/research/unicode.php
                    if (c >= '0' && c <= '9' ||
                        c >= 'A' && c <= 'Z' ||
                        c >= 'a' && c <= 'z' ||
                        c >= 0x00A1 && c < 0x02B0 || // Extended latin
                        c >= 0x0370 && c < 0x2000 || // Greek, Cyrillic, Hebrew, Arabic, Syriac etc
                        c >= 0x2E80 && c < 0xE000 || // CJK, Japanese etc
                        c >= 0xF900 && c < 0xFF00)  // specials
                    {
                        if (isWhiteSpace && hasLineContent)
                        {
                            writer.Write(' ');
                            length++;
                        }
                        isWhiteSpace = false;
                        writer.Write(c);
                        hasLineContent = true;
                        length++;
                    }
                    else
                    {
                        isWhiteSpace = true;
                        /*if (c > 0x7F && !(c >= 0x02B0 && c <= 0x36F) &&
                            !(c >= 0x2000 && c <= 0x2BFF))
                            Console.Write(c);*/
                    }

                    if (isWhiteSpace && length > cutOffLength)
                        return;
                }
                if (hasLineContent)
                    writer.WriteLine();
            }
        }
    }
}