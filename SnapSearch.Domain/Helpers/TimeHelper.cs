namespace SnapSearch.Domain.Helpers
{
    public static class TimeHelper
    {
        #region Fields

        private static readonly TimeZoneInfo _pst =
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows()
                    ? "Singapore Standard Time"   // Windows TZ ID for UTC+8
                    : "Asia/Manila");

        #endregion Fields

        // Linux/macOS TZ ID

        #region Properties

        /// <summary>Current time in Philippine Standard Time (UTC+8).</summary>
        public static DateTime Now =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _pst);

        #endregion Properties

        #region Public Methods

        /// <summary>Converts any DateTime to PST. Treats Unspecified kind as UTC.</summary>
        public static DateTime ToPst(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();
            else if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(dt, _pst);
        }

        #endregion Public Methods
    }
}