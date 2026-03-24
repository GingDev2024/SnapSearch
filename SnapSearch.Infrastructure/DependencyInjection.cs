using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnapSearch.Infrastructure.Data;

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

            services.AddTransient<UnitOfWork>();
            return services;
        }

        #endregion Public Methods
    }
}