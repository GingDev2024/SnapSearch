using Microsoft.Extensions.DependencyInjection;

namespace SnapSearch.Application
{
    public static class DependencyInjection
    {
        #region Public Methods

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            return services;
        }

        #endregion Public Methods
    }
}