namespace SnapSearch.Application.DTOs
{
    public class FileSearchRequestDto
    {
        #region Properties

        public string Keyword { get; set; } = string.Empty;
        public string SearchDirectory { get; set; } = string.Empty;
        public string? ExtensionFilter { get; set; }
        public DateTime? DateMin { get; set; }
        public DateTime? DateMax { get; set; }
        public long? SizeMin { get; set; }
        public long? SizeMax { get; set; }
        public bool AllowPartialMatch { get; set; } = true;
        public bool SearchFileContents { get; set; } = false;
        public bool SearchSubDirectories { get; set; } = true;

        /// <summary>
        /// When true the Keyword is treated as a .NET regular expression.
        /// FileSearchService pre-compiles the pattern once and applies it
        /// to both file names and file content.
        /// </summary>
        public bool UseRegex { get; set; } = false;

        #endregion Properties
    }
}