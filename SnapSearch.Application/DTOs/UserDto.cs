using SnapSearch.Domain.Helpers;

namespace SnapSearch.Application.DTOs
{
    public class UserDto
    {
        #region Properties

        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.Now;

        #endregion Properties
    }
}