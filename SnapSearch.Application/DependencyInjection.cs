using Microsoft.Extensions.DependencyInjection;
using SnapSearch.Application.Contracts;
using SnapSearch.Application.Services;

namespace SnapSearch.Application
{
    public static class DependencyInjection
    {
        #region Public Methods

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg => { }, AppDomain.CurrentDomain.GetAssemblies());
            services.AddSingleton<IAuthService, AuthService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<ISearchService, SearchService>();
            services.AddTransient<IAccessLogService, AccessLogService>();
            services.AddTransient<ISettingsService, SettingsService>();

            return services;
        }

        #endregion Public Methods
    }
}