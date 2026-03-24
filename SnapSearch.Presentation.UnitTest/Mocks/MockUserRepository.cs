using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Presentation.UnitTest.Mocks
{
    public class MockUserRepository : IUserRepository
    {
        #region Fields

        private readonly List<User> _users = new()
        {
            new User { Id = 1, Username = "admin", PasswordHash = "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=", Role = "Admin", IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Id = 2, Username = "viewer", PasswordHash = "hashedpw", Role = "ViewerOnly", IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { Id = 3, Username = "inactive", PasswordHash = "hashedpw", Role = "ViewerOnly", IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        private int _nextId = 4;

        #endregion Fields

        #region Public Methods

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.FirstOrDefault(u => u.Username == username));

        public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<User>>(_users.ToList());

        public Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            user.Id = _nextId++;
            _users.Add(user);
            return Task.FromResult(user.Id);
        }

        public Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing == null)
                return Task.FromResult(false);
            existing.Username = user.Username;
            existing.PasswordHash = user.PasswordHash;
            existing.Role = user.Role;
            existing.IsActive = user.IsActive;
            existing.UpdatedAt = user.UpdatedAt;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return Task.FromResult(false);
            user.IsActive = false;
            return Task.FromResult(true);
        }

        public Task<User?> AuthenticateAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
        {
            var user = _users.FirstOrDefault(u =>
                u.Username == username && u.PasswordHash == passwordHash && u.IsActive);
            return Task.FromResult(user);
        }

        #endregion Public Methods
    }
}