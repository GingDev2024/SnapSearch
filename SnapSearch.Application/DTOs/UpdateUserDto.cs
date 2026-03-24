namespace SnapSearch.Application.DTOs
{
    public class UpdateUserDto
    {
        #region Properties

        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? NewPassword { get; set; }

        #endregion Properties
    }
}