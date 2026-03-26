using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Infrastructure.Data;
using SnapSearch.Infrastructure.Repositories;
using SnapSearch.Infrastructure.Services;

namespace SnapSearch.Infrastructure
{
    public static class DependencyInjection
    {
        #region Public Methods

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services
                .AddTransient<AppDbContext>(sp =>
                {
                    var configuration = sp.GetRequiredService<IConfiguration>();
                    return new AppDbContext(configuration);
                });

            // Repositories
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IAccessLogRepository, AccessLogRepository>();
            services.AddTransient<IAppSettingRepository, AppSettingRepository>();
            services.AddTransient<ISearchHistoryRepository, SearchHistoryRepository>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            // Services
            services.AddTransient<IFileSearchService, FileSearchService>();

            // DbContext
            services.AddTransient<AppDbContext>();
            return services;
        }

        #endregion Public Methods
    }
}