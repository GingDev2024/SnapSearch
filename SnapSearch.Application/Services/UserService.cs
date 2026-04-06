using AutoMapper;
using SnapSearch.Application.Common.Helpers;
using SnapSearch.Application.Contracts;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Entities;
using SnapSearch.Domain.Helpers;

namespace SnapSearch.Application.Services
{
    public class UserService : IUserService
    {
        #region Fields

        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        #endregion Fields

        #region Public Constructors

        public UserService(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            return user == null ? null : _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
        {
            var user = _mapper.Map<User>(dto);
            user.PasswordHash = PasswordHelper.Hash(dto.Password);
            user.CreatedAt = TimeHelper.Now;
            user.UpdatedAt = TimeHelper.Now;
            var id = await _userRepository.CreateAsync(user, cancellationToken);
            user.Id = id;
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> UpdateUserAsync(UpdateUserDto dto, CancellationToken cancellationToken = default)
        {
            var existing = await _userRepository.GetByIdAsync(dto.Id, cancellationToken);
            if (existing == null)
                return false;

            existing.Username = dto.Username;
            existing.Role = dto.Role;
            existing.IsActive = dto.IsActive;
            existing.UpdatedAt = TimeHelper.Now;

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
                existing.PasswordHash = PasswordHelper.Hash(dto.NewPassword);

            return await _userRepository.UpdateAsync(existing, cancellationToken);
        }

        public async Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _userRepository.DeleteAsync(id, cancellationToken);
        }

        #endregion Public Methods
    }
}