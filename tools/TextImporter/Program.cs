using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace TextImporter
{
    public class Program
    {
        public static void Main(/*string[] args*/)
        {
            var filePaths = GetFilePaths("/home/kovi/kovi/git/kotta/tools/TextImporter/html/");

            var songTexts = filePaths.Select(x => GetSongText(x)).Where(x => x != null).Select(x => x!).ToArray();

            songTexts.ToList().ForEach(x => AddText(x));
        }

        private static IEnumerable<string> GetFilePaths(string path)
        {
            var enumerationOptions = new EnumerationOptions { RecurseSubdirectories = false };
            var filePaths = Directory.GetFiles(path, "*.html", enumerationOptions).OrderBy(x => x).ToArray();

            return filePaths;
        }

        private static SongText? GetSongText(string filePath)
        {
            try
            {
                var htmlDocument = new HtmlDocument();
                htmlDocument.Load(filePath);

                string title = htmlDocument.DocumentNode.Descendants("h1").Single().InnerText;

                var texts = htmlDocument.DocumentNode.Descendants("pre").Select(x => x.InnerText.Trim()).ToArray();

                string text = string.Join("\n\n", texts);

                var songText = new SongText
                {
                    Title = title,
                    Text = text,
                    HtmlFilePath = filePath,
                };

                return songText;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"There was an error parsing '{filePath}', {exception}.");
            }

            return null;
        }

        private static void AddText(SongText songText)
        {
            string? mscxFilePath = GetMscxFilePath(songText);

            if (string.IsNullOrEmpty(mscxFilePath))
            {
                return;
            }

            UpdateMscx(mscxFilePath, songText);

            ReformatMscx(mscxFilePath);
        }

        private static string? GetMscxFilePath(SongText songText)
        {
            string title = songText.Title.Replace(" ", "_").Replace("'", "");
            string cleanedTitle = RemoveDiacritics(title);

            string mscxFilePath = Path.Combine("/home/kovi/kovi/git/kotta/scores/daloskonyv/", $"{cleanedTitle}.mscx");

            if (!File.Exists(mscxFilePath))
            {
                Console.Error.WriteLine($"Could not find file '{mscxFilePath}'.");
                return null;
            }

            return mscxFilePath;
        }

        private static void UpdateMscx(string mscxFilePath, SongText songText)
        {
            var document = XDocument.Load(mscxFilePath, LoadOptions.PreserveWhitespace);

            var lastMeasure = document.Descendants("Measure").Last();

            var tBoxElement = new XElement("TBox");
            tBoxElement.Add(new XElement("height", "1"));

            var textElement = new XElement("Text");
            textElement.Add(new XElement("stlye", "Frame"));
            textElement.Add(new XElement("text", songText.Text));
            tBoxElement.Add(textElement);

            lastMeasure.AddAfterSelf(tBoxElement);

            document.Save(mscxFilePath);
        }

        private static void ReformatMscx(string mscxFilePath)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "musescore-portable",
                Arguments = $"-o {mscxFilePath} {mscxFilePath}",
            };

            var process = Process.Start(processStartInfo);

            if (process != null)
            {
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"There was an error processing '{mscxFilePath}'.");
                }
            }
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
