using Dapper;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        #region Fields

        private readonly UnitOfWork _uow;

        #endregion Fields

        #region Public Constructors

        public UserRepository(UnitOfWork uow)
        {
            _uow = uow;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_GetUserById @Id",
                new { Id = id },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.QueryFirstOrDefaultAsync<User>(cmd);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_GetUserByUsername @Username",
                new { Username = username },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.QueryFirstOrDefaultAsync<User>(cmd);
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_GetAllUsers",
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.QueryAsync<User>(cmd);
        }

        public async Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_CreateUser @Username, @PasswordHash, @Role, @IsActive, @CreatedAt",
                new
                {
                    user.Username,
                    user.PasswordHash,
                    user.Role,
                    user.IsActive,
                    user.CreatedAt
                },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.ExecuteScalarAsync<int>(cmd);
        }

        public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_UpdateUser @Id, @Username, @PasswordHash, @Role, @IsActive, @UpdatedAt",
                new
                {
                    user.Id,
                    user.Username,
                    user.PasswordHash,
                    user.Role,
                    user.IsActive,
                    user.UpdatedAt
                },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            var rows = await _uow.Connection.ExecuteAsync(cmd);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_DeleteUser @Id",
                new { Id = id },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            var rows = await _uow.Connection.ExecuteAsync(cmd);
            return rows > 0;
        }

        public async Task<User?> AuthenticateAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_AuthenticateUser @Username, @PasswordHash",
                new { Username = username, PasswordHash = passwordHash },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.QueryFirstOrDefaultAsync<User>(cmd);
        }

        #endregion Public Methods
    }
}