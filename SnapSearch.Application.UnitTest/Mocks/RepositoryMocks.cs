using Moq;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;
using SnapSearch.Domain.Enums;

namespace SnapSearch.Application.UnitTests.Mocks
{
    public static class RepositoryMocks
    {
        #region AccessLog

        /// <summary>
        /// CreateAsync         → Task&lt;int&gt;  (returns new log Id)
        /// GetAllAsync         → Task&lt;IEnumerable&lt;AccessLog&gt;&gt;
        /// GetByUserIdAsync    → Task&lt;IEnumerable&lt;AccessLog&gt;&gt;
        /// GetByDateRangeAsync → Task&lt;IEnumerable&lt;AccessLog&gt;&gt;
        /// </summary>
        public static Mock<IAccessLogRepository> GetAccessLogRepository()
        {
            var logs = new List<AccessLog>
            {
                new AccessLog
                {
                    Id = 1,
                    UserId = 1,
                    Username = "alice",
                    Action = ActionType.Login.ToString(),
                    IpAddress = "127.0.0.1",
                    MacAddress = "00:00:00:00:00:01",
                    AccessedAt = new DateTime(2024, 1, 10, 8, 0, 0, DateTimeKind.Utc)
                },
                new AccessLog
                {
                    Id = 2,
                    UserId = 1,
                    Username = "alice",
                    Action = ActionType.Search.ToString(),
                    SearchKeyword = "invoice",
                    IpAddress = "127.0.0.1",
                    MacAddress = "00:00:00:00:00:01",
                    AccessedAt = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc)
                },
                new AccessLog
                {
                    Id = 3,
                    UserId = 2,
                    Username = "bob",
                    Action = ActionType.ViewFile.ToString(),
                    FilePath = @"C:\Docs\report.pdf",
                    IpAddress = "127.0.0.2",
                    MacAddress = "00:00:00:00:00:02",
                    AccessedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)
                }
            };

            var mock = new Mock<IAccessLogRepository>();

            mock.Setup(r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccessLog log, CancellationToken _) =>
                {
                    var newId = logs.Max(l => l.Id) + 1;
                    log.Id = newId;
                    logs.Add(log);
                    return newId;
                });

            mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => logs.ToList());

            mock.Setup(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int userId, CancellationToken _) =>
                    logs.Where(l => l.UserId == userId).ToList());

            mock.Setup(r => r.GetByDateRangeAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DateTime from, DateTime to, CancellationToken _) =>
                    logs.Where(l => l.AccessedAt >= from && l.AccessedAt <= to).ToList());

            return mock;
        }

        #endregion AccessLog

        #region User

        /// <summary>
        /// GetByIdAsync       → Task&lt;User?&gt;
        /// GetByUsernameAsync → Task&lt;User?&gt;
        /// GetAllAsync        → Task&lt;IEnumerable&lt;User&gt;&gt;
        /// CreateAsync        → Task&lt;int&gt;  (returns new user Id)
        /// UpdateAsync        → Task&lt;bool&gt;
        /// DeleteAsync        → Task&lt;bool&gt;
        /// AuthenticateAsync  → Task&lt;User?&gt;
        /// </summary>
        public static Mock<IUserRepository> GetUserRepository()
        {
            var users = new List<User>
            {
                new User
                {
                    Id = 1,
                    Username = "alice",
                    PasswordHash = "hashed_alice",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 2,
                    Username = "bob",
                    PasswordHash = "hashed_bob",
                    Role = "ViewAndPrint",
                    IsActive = true,
                    CreatedAt = new DateTime(2023, 7, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = 3,
                    Username = "carol",
                    PasswordHash = "hashed_carol",
                    Role = "ViewListOnly",
                    IsActive = false,
                    CreatedAt = new DateTime(2023, 8, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            };

            var mock = new Mock<IUserRepository>();

            mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => users.ToList());

            mock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) =>
                    users.FirstOrDefault(u => u.Id == id));

            mock.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string username, CancellationToken _) =>
                    users.FirstOrDefault(u => u.Username == username));

            mock.Setup(r => r.AuthenticateAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string username, string hash, CancellationToken _) =>
                    users.FirstOrDefault(u => u.Username == username && u.PasswordHash == hash));

            mock.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User user, CancellationToken _) =>
                {
                    var newId = users.Max(u => u.Id) + 1;
                    user.Id = newId;
                    users.Add(user);
                    return newId;
                });

            mock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User updated, CancellationToken _) =>
                {
                    var index = users.FindIndex(u => u.Id == updated.Id);
                    if (index < 0)
                        return false;
                    users[index] = updated;
                    return true;
                });

            mock.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) =>
                {
                    var user = users.FirstOrDefault(u => u.Id == id);
                    if (user == null)
                        return false;
                    users.Remove(user);
                    return true;
                });

            return mock;
        }

        #endregion User

        #region SearchHistory

        /// <summary>
        /// CreateAsync      → Task&lt;int&gt;  (returns new history Id)
        /// GetByUserIdAsync → Task&lt;IEnumerable&lt;SearchHistory&gt;&gt;
        /// GetAllAsync      → Task&lt;IEnumerable&lt;SearchHistory&gt;&gt;
        /// </summary>
        public static Mock<ISearchHistoryRepository> GetSearchHistoryRepository()
        {
            var history = new List<SearchHistory>
            {
                new SearchHistory
                {
                    Id = 1,
                    UserId = 1,
                    Keyword = "invoice",
                    SearchDirectory = @"C:\Docs",
                    FileExtensionFilter = ".pdf",
                    ResultCount = 5,
                    SearchedAt = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc)
                },
                new SearchHistory
                {
                    Id = 2,
                    UserId = 1,
                    Keyword = "contract",
                    SearchDirectory = @"C:\Legal",
                    FileExtensionFilter = ".docx",
                    ResultCount = 2,
                    SearchedAt = new DateTime(2024, 1, 11, 10, 0, 0, DateTimeKind.Utc)
                },
                new SearchHistory
                {
                    Id = 3,
                    UserId = 2,
                    Keyword = "report",
                    SearchDirectory = @"C:\Reports",
                    FileExtensionFilter = ".xlsx",
                    ResultCount = 0,
                    SearchedAt = new DateTime(2024, 1, 12, 11, 0, 0, DateTimeKind.Utc)
                }
            };

            var mock = new Mock<ISearchHistoryRepository>();

            mock.Setup(r => r.CreateAsync(It.IsAny<SearchHistory>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SearchHistory entry, CancellationToken _) =>
                {
                    var newId = history.Max(h => h.Id) + 1;
                    entry.Id = newId;
                    history.Add(entry);
                    return newId;
                });

            mock.Setup(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int userId, CancellationToken _) =>
                    history.Where(h => h.UserId == userId).ToList());

            mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => history.ToList());

            return mock;
        }

        #endregion SearchHistory

        #region AppSetting

        /// <summary>
        /// GetByKeyAsync → Task&lt;AppSetting?&gt;
        /// GetAllAsync   → Task&lt;IEnumerable&lt;AppSetting&gt;&gt;
        /// UpsertAsync   → Task&lt;bool&gt;
        /// DeleteAsync   → Task&lt;bool&gt;  (keyed by string Key)
        /// </summary>
        public static Mock<IAppSettingRepository> GetAppSettingRepository()
        {
            var settings = new List<AppSetting>
            {
                new AppSetting
                {
                    Id = 1,
                    Key = "DefaultSearchDirectory",
                    Value = @"C:\Documents",
                    Description = "Default directory for file searches"
                },
                new AppSetting
                {
                    Id = 2,
                    Key = "Theme",
                    Value = "Dark",
                    Description = "UI Theme"
                },
                new AppSetting
                {
                    Id = 3,
                    Key = "MaxResults",
                    Value = "100",
                    Description = "Maximum search results to display"
                }
            };

            var mock = new Mock<IAppSettingRepository>();

            mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => settings.ToList());

            mock.Setup(r => r.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string key, CancellationToken _) =>
                    settings.FirstOrDefault(s => s.Key == key));

            mock.Setup(r => r.UpsertAsync(It.IsAny<AppSetting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppSetting setting, CancellationToken _) =>
                {
                    var existing = settings.FirstOrDefault(s => s.Key == setting.Key);
                    if (existing != null)
                    {
                        existing.Value = setting.Value;
                        existing.Description = setting.Description;
                    }
                    else
                    {
                        setting.Id = settings.Max(s => s.Id) + 1;
                        settings.Add(setting);
                    }
                    return true;
                });

            // DeleteAsync key is string (matching AppSettingRepository.DeleteAsync signature)
            mock.Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string key, CancellationToken _) =>
                {
                    var setting = settings.FirstOrDefault(s => s.Key == key);
                    if (setting == null)
                        return false;
                    settings.Remove(setting);
                    return true;
                });

            return mock;
        }

        #endregion AppSetting
    }
}