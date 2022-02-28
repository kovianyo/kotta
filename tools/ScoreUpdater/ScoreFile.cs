using System;
using System.Diagnostics;

namespace ScoreUpdater
{
    [DebuggerDisplay("FilePath: {FilePath}")]
    public class ScoreFile
    {
        public string FilePath { get; set; } = string.Empty;

        public string? Source { get; set; } = string.Empty;

        public DateTime? UploadDate { get; set; }
    }
}
