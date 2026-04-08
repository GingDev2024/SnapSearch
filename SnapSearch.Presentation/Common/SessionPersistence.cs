using SnapSearch.Application.DTOs;
using System.IO;
using System.Text.Json;

namespace SnapSearch.Presentation.Common
{
    /// <summary>
    /// Saves/loads/clears the logged-in user to a local JSON file so the app
    /// can auto-login on the next launch without showing the login screen.
    /// File location: %AppData%\SnapSearch\session.json
    /// </summary>
    public static class SessionPersistence
    {
        private static readonly string SessionFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SnapSearch");

        private static readonly string SessionFile =
            Path.Combine(SessionFolder, "session.json");

        /// <summary>Persist the authenticated user to disk.</summary>
        public static void Save(UserDto user)
        {
            try
            {
                Directory.CreateDirectory(SessionFolder);
                var json = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SessionFile, json);
            }
            catch { /* non-critical */ }
        }

        /// <summary>
        /// Try to load a previously saved session.
        /// Returns null if no session file exists or the file is corrupt.
        /// </summary>
        public static UserDto? TryLoad()
        {
            try
            {
                if (!File.Exists(SessionFile))
                    return null;
                var json = File.ReadAllText(SessionFile);
                return JsonSerializer.Deserialize<UserDto>(json);
            }
            catch { return null; }
        }

        /// <summary>Delete the saved session — called on explicit Logout.</summary>
        public static void Clear()
        {
            try
            {
                if (File.Exists(SessionFile))
                    File.Delete(SessionFile);
            }
            catch { /* ignore */ }
        }
    }
}