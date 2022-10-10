using System.Diagnostics;

namespace TextImporter
{
    [DebuggerDisplay("{Title}")]
    public class SongText
    {
        public string Title { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public string HtmlFilePath { get; set; } = string.Empty;
    }
}
