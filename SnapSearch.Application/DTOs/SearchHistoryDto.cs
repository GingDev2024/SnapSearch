namespace SnapSearch.Application.DTOs
{
    public class SearchHistoryDto
    {
        #region Properties

        public int Id { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public string? SearchDirectory { get; set; }
        public string? FileExtensionFilter { get; set; }
        public int ResultCount { get; set; }
        public DateTime SearchedAt { get; set; }

        #endregion Properties
    }
}