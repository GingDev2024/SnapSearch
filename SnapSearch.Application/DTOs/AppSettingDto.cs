namespace SnapSearch.Application.DTOs
{
    public class AppSettingDto
    {
        #region Properties

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }

        #endregion Properties
    }
}