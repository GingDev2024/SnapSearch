using SnapSearch.Domain.Helpers;

namespace SnapSearch.Application.DTOs
{
    public class CreateUserDto
    {
        #region Properties

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = TimeHelper.Now;

        #endregion Properties
    }
}