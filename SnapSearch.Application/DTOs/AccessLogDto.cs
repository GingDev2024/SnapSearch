namespace SnapSearch.Application.DTOs
{
    public class AccessLogDto
    {
        #region Properties

        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? SearchKeyword { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public DateTime AccessedAt { get; set; }
        public string? Details { get; set; }

        #endregion Properties
    }
}