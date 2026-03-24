using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnapSearch.Application;
using SnapSearch.Infrastructure;
using System.IO;
using System.Windows;

namespace SnapSearch.Presentation
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        #region Properties

        public IServiceProvider Services { get; private set; }

        #endregion Properties

        #region Protected Methods

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            Services = services.BuildServiceProvider();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        #endregion Protected Methods

        #region Private Methods

        private void ConfigureServices(IServiceCollection services)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();

            services.AddSingleton(configuration);

            services.AddApplication();
            services.AddInfrastructure();

            services.AddSingleton<MainWindow>();
        }

        #endregion Private Methods
    }
}