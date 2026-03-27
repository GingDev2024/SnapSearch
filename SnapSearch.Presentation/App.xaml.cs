using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapSearch.Application;
using SnapSearch.Application.Contracts;
using SnapSearch.Infrastructure;
using SnapSearch.Presentation.Common;
using SnapSearch.Presentation.ViewModels;
using SnapSearch.Presentation.Views;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SnapSearch.Presentation
{
    public partial class App : System.Windows.Application
    {
        #region Fields

        private static IServiceProvider _services = null!;

        #endregion Fields

        #region Public Methods

        public static T GetService<T>() where T : notnull
            => _services.GetRequiredService<T>();

        #endregion Public Methods

        #region Protected Methods

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Load the icon
            var iconUri = new Uri("pack://application:,,,/Resources/snapsearchlogo.ico", UriKind.Absolute);

            // Set the icon for each new window when it's created
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
                new RoutedEventHandler((sender, args) =>
                {
                    if (sender is Window window)
                    {
                        window.Icon = BitmapFrame.Create(iconUri);
                    }
                }));

            var services = new ServiceCollection();
            ConfigureServices(services);
            _services = services.BuildServiceProvider();

            ThemeManager.Apply(AppTheme.Dark);

            // Show login first
            var loginWindow = _services.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        #endregion Protected Methods

        #region Private Methods

        private static void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Application layer (AutoMapper, service implementations)
            services.AddApplication();

            // Infrastructure layer (Dapper repos, FileSearchService)
            services.AddInfrastructure();

            // --- ViewModels ---
            // Singleton: auth state is shared
            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<MainShellViewModel>(sp => new MainShellViewModel(
                sp.GetRequiredService<IAuthService>(),
                () => sp.GetRequiredService<SearchViewModel>(),
                () => sp.GetRequiredService<UserManagementViewModel>(),
                () => sp.GetRequiredService<AccessLogViewModel>(),
                () => sp.GetRequiredService<SettingsViewModel>()
            ));

            // Transient: each navigation creates a fresh VM
            services.AddTransient<SearchViewModel>();
            services.AddTransient<FilePreviewViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<AccessLogViewModel>();
            services.AddTransient<SettingsViewModel>();

            // --- Windows ---
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainShellWindow>();
            services.AddTransient<FilePreviewWindow>();

            // --- Services ---
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
            });
        }

        #endregion Private Methods
    }
}