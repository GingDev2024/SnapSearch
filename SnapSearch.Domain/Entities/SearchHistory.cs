using SnapSearch.Domain.Helpers;

namespace SnapSearch.Domain.Entities
{
    public class SearchHistory
    {
        #region Properties

        public int Id { get; set; }
        public int UserId { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public string? SearchDirectory { get; set; }
        public string? FileExtensionFilter { get; set; }
        public int ResultCount { get; set; }
        public DateTime SearchedAt { get; set; } = TimeHelper.Now;

        #endregion Properties
    }
}