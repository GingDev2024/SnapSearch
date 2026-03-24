namespace SnapSearch.Application.DTOs
{
    public class ContentMatchDto
    {
        #region Properties

        public int LineNumber { get; set; }
        public int MatchIndex { get; set; }
        public string LineContent { get; set; } = string.Empty;
        public string Keyword { get; set; } = string.Empty;
        public int PageNumber { get; set; }

        #endregion Properties
    }
}