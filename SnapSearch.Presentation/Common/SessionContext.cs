using SnapSearch.Application.DTOs;

namespace SnapSearch.Presentation.Common
{
    public class SessionContext
    {
        #region Fields

        private static SessionContext? _instance;

        #endregion Fields

        #region Properties

        public static SessionContext Instance => _instance ??= new SessionContext();

        public UserDto? CurrentUser { get; set; }
        public bool IsAuthenticated => CurrentUser != null;

        #endregion Properties

        #region Public Methods

        public bool HasPermission(string permission)
        {
            if (CurrentUser == null)
                return false;
            var role = CurrentUser.Role;
            return permission switch
            {
                "ChangeSettings" => role == "Admin",
                "ManageUsers" => role == "Admin",
                "ViewLogs" => role == "Admin",
                "Search" => true,
                "ViewFile" => role != "ViewListOnly",
                "PrintFile" => role is "Admin" or "ViewAndPrint",
                "ExportFile" => role is "Admin" or "Compliance",
                "CopyFile" => role is "Admin" or "Compliance",
                "SaveFile" => role is "Admin" or "Compliance",
                _ => false
            };
        }

        public void Clear() => CurrentUser = null;

        #endregion Public Methods
    }
}