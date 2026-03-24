using Microsoft.Extensions.DependencyInjection;

namespace SnapSearch.Infrastructure
{
    public static class DependencyInjection
    {
        #region Public Methods

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            return services;
        }

        #endregion Public Methods
    }
}