using SnapSearch.Application.Common.Health;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Presentation.Common;
using System.Windows.Input;
using System.Windows.Threading;

namespace SnapSearch.Presentation.ViewModels
{
    public class HealthViewModel : BaseViewModel
    {
        #region Fields

        private readonly IHealthCheckService _healthService;
        private readonly DispatcherTimer _timer;

        private HealthReport? _report;
        private string _error = string.Empty;
        private bool _isError;
        private bool _isLoading;

        #endregion Fields

        #region Public Constructors

        public HealthViewModel(IHealthCheckService healthService)
        {
            _healthService = healthService;

            RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _timer.Tick += async (_, _) => await LoadAsync();
            _timer.Start();

            // load immediately on construction
            _ = LoadAsync();
        }

        #endregion Public Constructors

        #region Properties

        public HealthReport? Report
        {
            get => _report;
            private set
            {
                SetProperty(ref _report, value);
                OnPropertyChanged(nameof(OverallStatusColor));
                OnPropertyChanged(nameof(OverallStatusIcon));
            }
        }

        public string Error
        {
            get => _error;
            private set => SetProperty(ref _error, value);
        }

        public bool IsError
        {
            get => _isError;
            private set => SetProperty(ref _isError, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string OverallStatusColor => Report?.Status switch
        {
            HealthStatus.Healthy => "#2ECC71",
            HealthStatus.Degraded => "#F39C12",
            HealthStatus.Unhealthy => "#E74C3C",
            _ => "Gray"
        };

        public string OverallStatusIcon => Report?.Status switch
        {
            HealthStatus.Healthy => "✓",
            HealthStatus.Degraded => "⚠",
            HealthStatus.Unhealthy => "✗",
            _ => "…"
        };

        public ICommand RefreshCommand { get; }

        #endregion Properties

        #region Public Methods

        public void StopTimer() => _timer.Stop();

        #endregion Public Methods

        #region Private Methods

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var report = await Task.Run(() => _healthService.CheckAsync());
                Report = report;
                Error = string.Empty;
                IsError = false;
            }
            catch (Exception ex)
            {
                Error = $"✗ {ex.Message}";
                IsError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion Private Methods
    }
}