using SnapSearch.Domain.Helpers;

namespace SnapSearch.Domain.Entities
{
    public class AccessLog
    {
        #region Properties

        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? SearchKeyword { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public DateTime AccessedAt { get; set; } = TimeHelper.Now;
        public string? Details { get; set; }

        #endregion Properties
    }
}