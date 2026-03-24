using AutoMapper;
using Moq;
using Shouldly;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using SnapSearch.Application.Services;
using SnapSearch.Application.UnitTests.Mocks;
using SnapSearch.Domain.Entities;
using SnapSearch.Domain.Enums;

namespace SnapSearch.Application.UnitTests.Services
{
    public class AuthServiceTests
    {
        #region Fields

        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IAccessLogRepository> _accessLogRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly AuthService _sut;

        #endregion Fields

        #region Constructors

        public AuthServiceTests()
        {
            _userRepositoryMock = RepositoryMocks.GetUserRepository();
            _accessLogRepositoryMock = RepositoryMocks.GetAccessLogRepository();
            _mapperMock = new Mock<IMapper>();

            _sut = new AuthService(
                _userRepositoryMock.Object,
                _accessLogRepositoryMock.Object,
                _mapperMock.Object);
        }

        #endregion Constructors

        #region LoginAsync

        [Fact]
        public async Task LoginAsync_ValidCredentials_ShouldReturnUserDto_AndSetCurrentUser()
        {
            // alice exists in seed data with hash "hashed_alice"
            var user = new User { Id = 1, Username = "alice", Role = "Admin" };
            var userDto = new UserDto { Id = 1, Username = "alice", Role = "Admin" };

            _userRepositoryMock
                .Setup(r => r.AuthenticateAsync("alice", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

            var result = await _sut.LoginAsync(new LoginDto { Username = "alice", Password = "anypassword" });

            result.ShouldNotBeNull();
            result.ShouldBe(userDto);
            _sut.CurrentUser.ShouldBe(userDto);
            _sut.IsAuthenticated.ShouldBeTrue();
        }

        [Fact]
        public async Task LoginAsync_InvalidCredentials_ShouldReturnNull_AndNotSetCurrentUser()
        {
            _userRepositoryMock
                .Setup(r => r.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?) null);

            var result = await _sut.LoginAsync(new LoginDto { Username = "ghost", Password = "wrong" });

            result.ShouldBeNull();
            _sut.CurrentUser.ShouldBeNull();
            _sut.IsAuthenticated.ShouldBeFalse();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ShouldCreateLoginAccessLog()
        {
            var user = new User { Id = 1, Username = "alice" };
            var userDto = new UserDto { Id = 1, Username = "alice" };

            _userRepositoryMock
                .Setup(r => r.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

            AccessLog? captured = null;
            _accessLogRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
                .Callback<AccessLog, CancellationToken>((log, _) => captured = log)
                .ReturnsAsync(99);

            await _sut.LoginAsync(new LoginDto { Username = "alice", Password = "pass" });

            _accessLogRepositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()),
                Times.Once);

            captured.ShouldNotBeNull();
            captured!.Action.ShouldBe(ActionType.Login.ToString());
            captured.UserId.ShouldBe(user.Id);
            captured.Username.ShouldBe(user.Username);
        }

        [Fact]
        public async Task LoginAsync_InvalidCredentials_ShouldNotCreateAccessLog()
        {
            _userRepositoryMock
                .Setup(r => r.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?) null);

            await _sut.LoginAsync(new LoginDto { Username = "nobody", Password = "nope" });

            _accessLogRepositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion LoginAsync

        #region LogoutAsync

        [Fact]
        public async Task LogoutAsync_WhenLoggedIn_ShouldClearCurrentUser_AndCreateLogoutLog()
        {
            // Arrange — log in first
            var user = new User { Id = 1, Username = "alice" };
            var userDto = new UserDto { Id = 1, Username = "alice", Role = "Admin" };

            _userRepositoryMock
                .Setup(r => r.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

            _accessLogRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _sut.LoginAsync(new LoginDto { Username = "alice", Password = "pass" });

            // Capture the logout log specifically
            AccessLog? capturedLogout = null;
            _accessLogRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
                .Callback<AccessLog, CancellationToken>((log, _) => capturedLogout = log)
                .ReturnsAsync(2);

            // Act
            await _sut.LogoutAsync(user.Id);

            // Assert
            _sut.CurrentUser.ShouldBeNull();
            _sut.IsAuthenticated.ShouldBeFalse();
            capturedLogout.ShouldNotBeNull();
            capturedLogout!.Action.ShouldBe(ActionType.Logout.ToString());
            capturedLogout.UserId.ShouldBe(user.Id);
        }

        [Fact]
        public async Task LogoutAsync_WhenNotLoggedIn_ShouldNotCreateAccessLog()
        {
            // No prior login; CurrentUser is null
            await _sut.LogoutAsync(1);

            _accessLogRepositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion LogoutAsync

        #region IsAuthenticated

        [Fact]
        public void IsAuthenticated_BeforeLogin_ShouldBeFalse()
        {
            _sut.IsAuthenticated.ShouldBeFalse();
        }

        [Fact]
        public async Task IsAuthenticated_AfterSuccessfulLogin_ShouldBeTrue()
        {
            var user = new User { Id = 1, Username = "alice" };
            var userDto = new UserDto { Id = 1, Username = "alice" };

            _userRepositoryMock
                .Setup(r => r.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

            _accessLogRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _sut.LoginAsync(new LoginDto { Username = "alice", Password = "pass" });

            _sut.IsAuthenticated.ShouldBeTrue();
        }

        #endregion IsAuthenticated

        #region HasPermission

        [Theory]
        [InlineData("Admin", "ChangeSettings", true)]
        [InlineData("Admin", "ManageUsers", true)]
        [InlineData("Admin", "ViewLogs", true)]
        [InlineData("Admin", "Search", true)]
        [InlineData("Admin", "ViewFile", true)]
        [InlineData("Admin", "PrintFile", true)]
        [InlineData("Admin", "ExportFile", true)]
        [InlineData("Admin", "CopyFile", true)]
        [InlineData("Admin", "SaveFile", true)]
        public void HasPermission_AdminRole_ShouldHaveAllPermissions(string role, string permission, bool expected)
        {
            SetCurrentUser(role);
            _sut.HasPermission(permission).ShouldBe(expected);
        }

        [Theory]
        [InlineData("ViewListOnly", "Search", true)]
        [InlineData("ViewListOnly", "ViewFile", false)]
        [InlineData("ViewListOnly", "PrintFile", false)]
        [InlineData("ViewListOnly", "ExportFile", false)]
        [InlineData("ViewListOnly", "CopyFile", false)]
        [InlineData("ViewListOnly", "SaveFile", false)]
        [InlineData("ViewListOnly", "ManageUsers", false)]
        [InlineData("ViewListOnly", "ViewLogs", false)]
        public void HasPermission_ViewListOnlyRole_ShouldHaveRestrictedPermissions(string role, string permission, bool expected)
        {
            SetCurrentUser(role);
            _sut.HasPermission(permission).ShouldBe(expected);
        }

        [Theory]
        [InlineData("ViewAndPrint", "Search", true)]
        [InlineData("ViewAndPrint", "ViewFile", true)]
        [InlineData("ViewAndPrint", "PrintFile", true)]
        [InlineData("ViewAndPrint", "ExportFile", false)]
        [InlineData("ViewAndPrint", "CopyFile", false)]
        [InlineData("ViewAndPrint", "ManageUsers", false)]
        public void HasPermission_ViewAndPrintRole_ShouldHaveCorrectPermissions(string role, string permission, bool expected)
        {
            SetCurrentUser(role);
            _sut.HasPermission(permission).ShouldBe(expected);
        }

        [Theory]
        [InlineData("Compliance", "Search", true)]
        [InlineData("Compliance", "ViewFile", true)]
        [InlineData("Compliance", "ExportFile", true)]
        [InlineData("Compliance", "CopyFile", true)]
        [InlineData("Compliance", "SaveFile", true)]
        [InlineData("Compliance", "PrintFile", false)]
        [InlineData("Compliance", "ManageUsers", false)]
        public void HasPermission_ComplianceRole_ShouldHaveCorrectPermissions(string role, string permission, bool expected)
        {
            SetCurrentUser(role);
            _sut.HasPermission(permission).ShouldBe(expected);
        }

        [Fact]
        public void HasPermission_WhenNotAuthenticated_ShouldAlwaysReturnFalse()
        {
            _sut.HasPermission("Search").ShouldBeFalse();
            _sut.HasPermission("ManageUsers").ShouldBeFalse();
        }

        [Fact]
        public void HasPermission_UnknownPermission_ShouldReturnFalse()
        {
            SetCurrentUser("Admin");
            _sut.HasPermission("NonExistentPermission").ShouldBeFalse();
        }

        #endregion HasPermission

        #region Helpers

        /// <summary>
        /// Forces CurrentUser via reflection so HasPermission tests
        /// remain independent of LoginAsync behaviour.
        /// </summary>
        private void SetCurrentUser(string role)
        {
            typeof(AuthService)
                .GetProperty(nameof(AuthService.CurrentUser))!
                .SetValue(_sut, new UserDto { Id = 1, Username = "testuser", Role = role });
        }

        #endregion Helpers
    }
}