using Dapper;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        #region Fields

        private readonly IUnitOfWork _uow;

        #endregion Fields

        #region Public Constructors

        public UserRepository(IUnitOfWork uow) => _uow = uow;

        #endregion Public Constructors

        #region Public Methods

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM Users WHERE Id = @Id";
            return await _uow.Connection.QueryFirstOrDefaultAsync<User>(
                new CommandDefinition(sql, new { Id = id }, _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM Users WHERE Username = @Username";
            return await _uow.Connection.QueryFirstOrDefaultAsync<User>(
                new CommandDefinition(sql, new { Username = username }, _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM Users";
            return await _uow.Connection.QueryAsync<User>(
                new CommandDefinition(sql, transaction: _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            var sql = @"
                INSERT INTO Users (Username, PasswordHash, Role, IsActive, CreatedAt)
                VALUES (@Username, @PasswordHash, @Role, @IsActive, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _uow.Connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, user, _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            var sql = @"
                UPDATE Users
                SET Username = @Username,
                    PasswordHash = @PasswordHash,
                    Role = @Role,
                    IsActive = @IsActive,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            var rows = await _uow.Connection.ExecuteAsync(
                new CommandDefinition(sql, user, _uow.Transaction, cancellationToken: cancellationToken));
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var sql = "DELETE FROM Users WHERE Id = @Id";
            var rows = await _uow.Connection.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id }, _uow.Transaction, cancellationToken: cancellationToken));
            return rows > 0;
        }

        public async Task<User?> AuthenticateAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM Users WHERE Username = @Username AND PasswordHash = @PasswordHash";
            return await _uow.Connection.QueryFirstOrDefaultAsync<User>(
                new CommandDefinition(sql, new { Username = username, PasswordHash = passwordHash }, _uow.Transaction, cancellationToken: cancellationToken));
        }

        #endregion Public Methods
    }
}