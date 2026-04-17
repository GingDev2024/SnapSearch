using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapSearch.Application;
using SnapSearch.Application.Contracts;
using SnapSearch.Application.Services;
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

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                System.Windows.MessageBox.Show(
                    args.ExceptionObject?.ToString() ?? "Unknown error",
                    "Startup Crash");

            this.DispatcherUnhandledException += (s, args) =>
            {
                System.Windows.MessageBox.Show(args.Exception.ToString(), "UI Crash");
                args.Handled = true;
            };

            var iconUri = new Uri("pack://application:,,,/Resources/snapsearchlogo.ico", UriKind.Absolute);
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
                new RoutedEventHandler((sender, args) =>
                {
                    if (sender is Window window)
                        window.Icon = BitmapFrame.Create(iconUri);
                }));

            var services = new ServiceCollection();
            ConfigureServices(services);
            _services = services.BuildServiceProvider();

            ThemeManager.Apply(AppTheme.Dark);

            var savedUser = SessionPersistence.TryLoad();
            if (savedUser != null)
            {
                SessionContext.Instance.CurrentUser = savedUser;

                if (_services.GetRequiredService<IAuthService>() is AuthService authService)
                    authService.RestoreSession(savedUser);

                var shell = _services.GetRequiredService<MainShellWindow>();
                shell.Initialize();
                Current.MainWindow = shell;
                shell.Show();
            }
            else
            {
                var loginWindow = _services.GetRequiredService<LoginWindow>();
                Current.MainWindow = loginWindow;
                loginWindow.Show();
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static void ConfigureServices(IServiceCollection services)
        {
            var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SNAPSearch"
            );

            Directory.CreateDirectory(appDataPath);

            var exeBasePath = AppContext.BaseDirectory;

            var appSettingsPath = Path.Combine(appDataPath, "appsettings.json");
            var iniPath = Path.Combine(appDataPath, "snapsearch.ini");

            if (!File.Exists(appSettingsPath))
            {
                var defaultFile = Path.Combine(exeBasePath, "appsettings.json");
                if (File.Exists(defaultFile))
                    File.Copy(defaultFile, appSettingsPath);
            }

            if (!File.Exists(iniPath))
            {
                var defaultIni = Path.Combine(exeBasePath, "snapsearch.ini");
                if (File.Exists(defaultIni))
                    File.Copy(defaultIni, iniPath);
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(appDataPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddApplication();
            services.AddInfrastructure();

            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<MainShellViewModel>(sp => new MainShellViewModel(
                sp.GetRequiredService<IAuthService>(),
                () => sp.GetRequiredService<SearchViewModel>(),
                () => sp.GetRequiredService<UserManagementViewModel>(),
                () => sp.GetRequiredService<AccessLogViewModel>(),
                () => sp.GetRequiredService<SettingsViewModel>(),
                () => sp.GetRequiredService<IniEncryptorViewModel>(),
                () => sp.GetRequiredService<HealthViewModel>()
            ));

            services.AddTransient<SearchViewModel>();
            services.AddTransient<FilePreviewViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<AccessLogViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<HealthViewModel>();
            services.AddTransient<IniEncryptorViewModel>(sp =>
                new IniEncryptorViewModel(sp.GetRequiredService<IConfiguration>()));

            services.AddTransient<LoginWindow>();
            services.AddTransient<MainShellWindow>();
            services.AddTransient<FilePreviewWindow>();

            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
            });
        }

        #endregion Private Methods
    }
}