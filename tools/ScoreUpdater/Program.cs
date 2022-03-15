using System.Linq;
using System.IO;
using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ScoreUpdater
{
    public class Program
    {
        private static readonly Random _random = new Random();

        public static void Main(string[] args)
        {
            var uploaded = GetUploadedScores().ToArray();

            var storedScoreFiles = GetStoredScores(uploaded).ToArray();

            var shouldBeUploaded = GetScoresToBeUploaded(uploaded);

            UploadScores(shouldBeUploaded);

            var updatedScoreFiles = GetUpdatedScoreFiles(storedScoreFiles, shouldBeUploaded);

            WriteUpdatedScoreFiles(updatedScoreFiles);
        }

        private static IEnumerable<ScoreFile> GetUploadedScores()
        {
            var filePaths = GetFilePaths().ToArray();

            Console.WriteLine($"Found {filePaths.Count()} files.");

            var scoreFiles = filePaths.Select(x => GetScoreFile(x)).Where(x => x != null).Select(x => x!).ToArray();

            var notUploaded = scoreFiles.Where(x => string.IsNullOrEmpty(x.Source)).ToArray();

            var uploaded = scoreFiles.Where(x => !string.IsNullOrEmpty(x.Source)).ToArray();

            Console.WriteLine($"{uploaded.Count()} uploaded files.");

            return uploaded;
        }

        private static IEnumerable<string> GetFilePaths()
        {
            var paths = new[]
            {
                "/home/kovi/kovi/git/kotta/scores/daloskonyv",
                "/home/kovi/kovi/git/kotta/scores/notaskonyv/main",
                "/home/kovi/kovi/git/kotta/scores/notaskonyv/main/karacsonyi",
                "/home/kovi/kovi/git/kotta/scores/tiedadal",
                "/home/kovi/kovi/git/kotta/scores/tiedadal/karacsonyi",
                "/home/kovi/kovi/git/kotta/scores/moldvai",
                "/home/kovi/kovi/git/kotta/scores/moldvai/enekek",
                "/home/kovi/kovi/git/kotta/scores/moldvai/enekek/karacsonyi",
                "/home/kovi/kovi/git/kotta/scores/moldvai/tancok",
                "/home/kovi/kovi/git/kotta/scores/moldvai/tancok/Kezes",
            };

            var filePaths = paths.SelectMany(x => GetFilePaths(x)).ToArray();

            return filePaths;

        }

        private static IEnumerable<string> GetFilePaths(string path)
        {
            var enumerationOptions = new EnumerationOptions { RecurseSubdirectories = false };
            var filePaths = Directory.GetFiles(path, "*.mscx", enumerationOptions).Where(x => !x.Contains("downloaded")).OrderBy(x => x);

            return filePaths;
        }

        private static IEnumerable<ScoreFile> GetStoredScores(IEnumerable<ScoreFile> uploaded)
        {
            var storedScoreFiles = Utils.XmlDeserializeFile<ScoreFile[]>("ScoreFiles.xml") ?? Array.Empty<ScoreFile>();

            foreach (var storedScoreFile in storedScoreFiles)
            {
                var uploadedScoreFile = uploaded.FirstOrDefault(x => x.FilePath == storedScoreFile.FilePath);

                if (uploadedScoreFile != null)
                {
                    uploadedScoreFile.UploadDate = storedScoreFile.UploadDate;
                }
            }

            return storedScoreFiles;
        }

        private static IEnumerable<ScoreFile> GetScoresToBeUploaded(IEnumerable<ScoreFile> uploaded)
        {
            var shouldBeUploaded = uploaded.Where(x => ShouldBeUploaded(x)).ToArray();

            Console.WriteLine($"{shouldBeUploaded.Count()} files to be uploaded:");

            shouldBeUploaded.ToList().ForEach(x => Console.WriteLine(x.FilePath));

            return shouldBeUploaded;
        }

        private static void UploadScores(IEnumerable<ScoreFile> shouldBeUploaded)
        {
            int index = 0;

            foreach (var up in shouldBeUploaded)
            {
                index++;
                Console.WriteLine($"({index}/{shouldBeUploaded.Count()}) Uploading '{up.FilePath}'...");
                Upload(up);
            }
        }

        private static ScoreFile? GetScoreFile(string filePath)
        {
            string xml = File.ReadAllText(filePath);

            var document = XDocument.Parse(xml);

            var metaTags = document.Descendants("metaTag");

            var sourceMetaTag = metaTags.SingleOrDefault(x => x.Attributes().Any(y => y.Name == "name" && y.Value == "source"));

            string? source = sourceMetaTag?.Value;

            if (sourceMetaTag != null)
            {
                var scoreFile = new ScoreFile
                {
                    FilePath = filePath,
                    Source = source,
                };


                return scoreFile;
            }

            return null;
        }

        private static bool ShouldBeUploaded(ScoreFile scoreFile)
        {
            var fileInfo = new FileInfo(scoreFile.FilePath);

            bool shouldBeUploaded = scoreFile.UploadDate == null || fileInfo.LastWriteTimeUtc > scoreFile.UploadDate;

            return shouldBeUploaded;
        }

        private static void Upload(ScoreFile scoreFile)
        {
            try
            {
                var fileInfo = new FileInfo(scoreFile.FilePath);

                if (scoreFile.UploadDate == null || fileInfo.LastWriteTimeUtc > scoreFile.UploadDate)
                {
                    int random = _random.Next(1000);

                    Thread.Sleep(1000 + random);

                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "musescore-portable",
                        Arguments = $"--save-online {scoreFile.FilePath}",
                    };

                    var process = Process.Start(processStartInfo);

                    if (process != null)
                    {
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            Console.WriteLine($"There was an error uploading '{scoreFile.FilePath}'.");
                            return;
                        }
                    }

                    scoreFile.UploadDate = DateTime.UtcNow;
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private static IEnumerable<ScoreFile> GetUpdatedScoreFiles(IEnumerable<ScoreFile> storedScoreFiles, IEnumerable<ScoreFile> shouldBeUploaded)
        {
            var updatedScoreFiles = new List<ScoreFile>(storedScoreFiles);

            foreach (var upd in shouldBeUploaded)
            {
                var score = storedScoreFiles.FirstOrDefault(x => x.FilePath == upd.FilePath);

                if (score == null)
                {
                    updatedScoreFiles.Add(upd);
                }
                else
                {
                    score.UploadDate = upd.UploadDate;
                }
            }

            return updatedScoreFiles;
        }

        private static void WriteUpdatedScoreFiles(IEnumerable<ScoreFile> updatedScoreFiles)
        {
            string xml = Utils.XmlSerializeUtf8(updatedScoreFiles.ToArray());

            File.WriteAllText("ScoreFiles.xml", xml);

        }
    }
}
