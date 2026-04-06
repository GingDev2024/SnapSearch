using SnapSearch.Domain.Helpers;

namespace SnapSearch.Domain.Entities
{
    public class User
    {
        #region Properties

        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Admin, ViewListOnly, ViewerOnly, ViewAndPrint, Compliance
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.Now;
        public DateTime UpdatedAt { get; set; } = TimeHelper.Now;

        #endregion Properties
    }
}