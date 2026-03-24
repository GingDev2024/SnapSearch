using AutoMapper;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Application.Common.Profiles
{
    public class MappingProfile : Profile
    {
        #region Public Constructors

        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true));

            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // AccessLog mappings
            CreateMap<AccessLog, AccessLogDto>();

            // AppSetting mappings
            CreateMap<AppSetting, AppSettingDto>();
            CreateMap<AppSettingDto, AppSetting>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            // SearchHistory mappings
            CreateMap<SearchHistory, SearchHistoryDto>();
        }

        #endregion Public Constructors
    }
}