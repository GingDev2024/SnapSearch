using SnapSearch.Application.Contracts;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Helpers;
using SnapSearch.Presentation.Common;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class AccessLogViewModel : BaseViewModel
    {
        #region Fields

        private readonly IAccessLogService _accessLogService;

        private DateTime _fromDate = DateTime.Today.AddDays(-7);
        private DateTime _toDate = TimeHelper.Now;
        private string _filterText = string.Empty;
        private ObservableCollection<AccessLogDto> _allLogs = new();

        #endregion Fields

        #region Public Constructors

        public AccessLogViewModel(IAccessLogService accessLogService)
        {
            _accessLogService = accessLogService;
            LoadLogsCommand = new AsyncRelayCommand(LoadLogsAsync, _ => !IsBusy);
            ExportCsvCommand = new RelayCommand(ExportToCsv, _ => Logs.Count > 0);

            _ = LoadLogsAsync(null);
        }

        #endregion Public Constructors

        #region Properties

        public ObservableCollection<AccessLogDto> Logs { get; } = new();

        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                SetProperty(ref _filterText, value);
                ApplyFilter();
            }
        }

        public ICommand LoadLogsCommand { get; }
        public ICommand ExportCsvCommand { get; }

        #endregion Properties

        #region Private Methods

        private async Task LoadLogsAsync(object? _)
        {
            IsBusy = true;
            try
            {
                _allLogs.Clear();
                Logs.Clear();
                var logs = await _accessLogService.GetLogsByDateRangeAsync(
                    FromDate.Date, ToDate.Date.AddDays(1).AddTicks(-1));

                foreach (var l in logs)
                    _allLogs.Add(l);
                ApplyFilter();
                StatusMessage = $"{Logs.Count} log(s) loaded.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private void ApplyFilter()
        {
            Logs.Clear();
            var filter = FilterText?.ToLower() ?? string.Empty;

            var filtered = string.IsNullOrWhiteSpace(FilterText)
                ? _allLogs
                : _allLogs.Where(l =>
                    (l.Username?.ToLower().Contains(filter) ?? false) ||
                    (l.IpAddress?.ToLower().Contains(filter) ?? false) ||
                    (l.MacAddress?.ToLower().Contains(filter) ?? false) ||
                    (l.Action?.ToLower().Contains(filter) ?? false) ||
                    (l.Details?.ToLower().Contains(filter) ?? false) ||
                    (l.SearchKeyword?.ToLower().Contains(filter) ?? false) ||
                    (l.FilePath?.ToLower().Contains(filter) ?? false) ||
                    l.AccessedAt.ToString("yyyy-MM-dd HH:mm:ss").Contains(filter));

            foreach (var l in filtered)
                Logs.Add(l);
        }

        private void ExportToCsv(object? _)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"AccessLogs_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() != true)
                return;

            var lines = new List<string>
            {
                "Id,Username,Action,FilePath,SearchKeyword,IpAddress,MacAddress,AccessedAt,Details"
            };
            foreach (var l in Logs)
                lines.Add($"{l.Id},\"{l.Username}\",\"{l.Action}\",\"{l.FilePath}\",\"{l.SearchKeyword}\"," +
                          $"\"{l.IpAddress}\",\"{l.MacAddress}\",\"{l.AccessedAt:yyyy-MM-dd HH:mm:ss}\",\"{l.Details}\"");

            System.IO.File.WriteAllLines(dlg.FileName, lines);
            StatusMessage = $"Exported {lines.Count - 1} log(s) to CSV.";
        }

        #endregion Private Methods
    }
}