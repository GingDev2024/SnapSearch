using AutoMapper;
using Moq;
using Shouldly;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using SnapSearch.Application.Services;
using SnapSearch.Application.UnitTests.Mocks;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Application.UnitTests.Services
{
    public class UserServiceTests
    {
        #region Fields

        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly UserService _sut;

        #endregion Fields

        #region Constructors

        public UserServiceTests()
        {
            _userRepositoryMock = RepositoryMocks.GetUserRepository();
            _mapperMock = new Mock<IMapper>();
            _sut = new UserService(_userRepositoryMock.Object, _mapperMock.Object);
        }

        #endregion Constructors

        #region GetAllUsersAsync

        [Fact]
        public async Task GetAllUsersAsync_ShouldReturn_MappedDtos()
        {
            // Seed has 3 users: alice, bob, carol
            var dtos = new List<UserDto>
            {
                new UserDto { Id = 1, Username = "alice" },
                new UserDto { Id = 2, Username = "bob"   },
                new UserDto { Id = 3, Username = "carol" }
            };

            _mapperMock
                .Setup(m => m.Map<IEnumerable<UserDto>>(It.IsAny<IEnumerable<User>>()))
                .Returns(dtos);

            var result = await _sut.GetAllUsersAsync();

            result.ShouldBeOfType<List<UserDto>>();
            result.Count().ShouldBe(3);
        }

        [Fact]
        public async Task GetAllUsersAsync_ShouldCall_Repository_Once()
        {
            _mapperMock
                .Setup(m => m.Map<IEnumerable<UserDto>>(It.IsAny<IEnumerable<User>>()))
                .Returns(new List<UserDto>());

            await _sut.GetAllUsersAsync();

            _userRepositoryMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion GetAllUsersAsync

        #region GetUserByIdAsync

        [Fact]
        public async Task GetUserByIdAsync_ExistingId_ShouldReturn_MappedDto()
        {
            // Id=1 (alice) exists in seed
            var dto = new UserDto { Id = 1, Username = "alice" };

            _mapperMock
                .Setup(m => m.Map<UserDto>(It.Is<User>(u => u.Id == 1)))
                .Returns(dto);

            var result = await _sut.GetUserByIdAsync(1);

            result.ShouldNotBeNull();
            result.ShouldBe(dto);
        }

        [Fact]
        public async Task GetUserByIdAsync_UnknownId_ShouldReturnNull()
        {
            var result = await _sut.GetUserByIdAsync(999);

            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetUserByIdAsync_UnknownId_ShouldNotCall_Mapper()
        {
            await _sut.GetUserByIdAsync(999);

            _mapperMock.Verify(m => m.Map<UserDto>(It.IsAny<User>()), Times.Never);
        }

        #endregion GetUserByIdAsync

        #region CreateUserAsync

        [Fact]
        public async Task CreateUserAsync_ShouldHash_Password_BeforeSaving()
        {
            var dto = new CreateUserDto { Username = "dave", Password = "plaintext", Role = "Admin" };
            var user = new User { Username = "dave", Role = "Admin" };

            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(new UserDto());

            await _sut.CreateUserAsync(dto);

            _userRepositoryMock.Verify(r => r.CreateAsync(
                It.Is<User>(u => !string.IsNullOrEmpty(u.PasswordHash)
                              && u.PasswordHash != "plaintext"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldAssign_IdFromRepository()
        {
            var dto = new CreateUserDto { Username = "dave", Password = "pass" };
            var user = new User { Username = "dave" };

            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            _mapperMock
                .Setup(m => m.Map<UserDto>(It.Is<User>(u => u.Id == 4)))  // seed max=3, next=4
                .Returns(new UserDto { Id = 4, Username = "dave" });

            var result = await _sut.CreateUserAsync(dto);

            result.Id.ShouldBe(4);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldReturn_MappedDto()
        {
            var dto = new CreateUserDto { Username = "dave", Password = "pass" };
            var user = new User { Username = "dave" };
            var userDto = new UserDto { Id = 4, Username = "dave" };

            _mapperMock.Setup(m => m.Map<User>(dto)).Returns(user);
            _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(userDto);

            var result = await _sut.CreateUserAsync(dto);

            result.ShouldBe(userDto);
        }

        #endregion CreateUserAsync

        #region UpdateUserAsync

        [Fact]
        public async Task UpdateUserAsync_ExistingUser_ShouldUpdate_Fields_AndReturnTrue()
        {
            // Id=2 (bob) exists in seed
            var dto = new UpdateUserDto
            {
                Id = 2,
                Username = "bob-updated",
                Role = "Compliance",
                IsActive = false
            };

            var result = await _sut.UpdateUserAsync(dto);

            result.ShouldBeTrue();

            // Verify the repository received the mutated entity
            _userRepositoryMock.Verify(r => r.UpdateAsync(
                It.Is<User>(u => u.Id == 2
                              && u.Username == "bob-updated"
                              && u.Role == "Compliance"
                              && u.IsActive == false),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_UnknownUser_ShouldReturnFalse_AndNotCallUpdate()
        {
            var dto = new UpdateUserDto { Id = 999, Username = "ghost" };

            var result = await _sut.UpdateUserAsync(dto);

            result.ShouldBeFalse();
            _userRepositoryMock.Verify(
                r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateUserAsync_WithNewPassword_ShouldHash_AndReplacePasswordHash()
        {
            // Id=1 (alice) has PasswordHash = "hashed_alice" in seed
            var dto = new UpdateUserDto
            {
                Id = 1,
                Username = "alice",
                Role = "Admin",
                IsActive = true,
                NewPassword = "newpassword"
            };

            await _sut.UpdateUserAsync(dto);

            _userRepositoryMock.Verify(r => r.UpdateAsync(
                It.Is<User>(u => u.PasswordHash != "hashed_alice"
                              && u.PasswordHash != "newpassword"
                              && !string.IsNullOrEmpty(u.PasswordHash)),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_WithEmptyNewPassword_ShouldNotChange_PasswordHash()
        {
            // Id=1 (alice) has PasswordHash = "hashed_alice" in seed
            var dto = new UpdateUserDto
            {
                Id = 1,
                Username = "alice",
                Role = "Admin",
                IsActive = true,
                NewPassword = ""
            };

            await _sut.UpdateUserAsync(dto);

            _userRepositoryMock.Verify(r => r.UpdateAsync(
                It.Is<User>(u => u.PasswordHash == "hashed_alice"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldSet_UpdatedAt_ToUtcNow()
        {
            var before = DateTime.UtcNow;

            var dto = new UpdateUserDto { Id = 1, Username = "alice", Role = "Admin", IsActive = true };

            await _sut.UpdateUserAsync(dto);

            var after = DateTime.UtcNow;

            _userRepositoryMock.Verify(r => r.UpdateAsync(
                It.Is<User>(u => u.UpdatedAt >= before && u.UpdatedAt <= after),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion UpdateUserAsync

        #region DeleteUserAsync

        [Fact]
        public async Task DeleteUserAsync_ExistingUser_ShouldReturnTrue()
        {
            // Id=3 (carol) exists in seed
            var result = await _sut.DeleteUserAsync(3);

            result.ShouldBeTrue();
            _userRepositoryMock.Verify(r => r.DeleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_UnknownUser_ShouldReturnFalse()
        {
            var result = await _sut.DeleteUserAsync(999);

            result.ShouldBeFalse();
        }

        #endregion DeleteUserAsync
    }
}