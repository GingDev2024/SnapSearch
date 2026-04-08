using SnapSearch.Application.Contracts;
using SnapSearch.Application.DTOs;
using SnapSearch.Presentation.Common;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        private string _defaultDirectory = string.Empty;
        private int _maxResultsPerPage = 500;
        private int _sessionTimeoutMinutes = 30;
        private bool _enableSessionTimeout;

        // Search defaults
        private bool _searchSubDirectoriesByDefault = true;
        private bool _searchContentsDefault;
        private bool _allowPartialMatchDefault = true;
        private bool _useRegexDefault;

        // Size filter defaults
        private string _sizeMinKbDefault = string.Empty;
        private string _sizeMaxKbDefault = string.Empty;

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            LoadSettingsCommand = new AsyncRelayCommand(LoadSettingsAsync);
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync, _ => !IsBusy);
            BrowseDirectoryCommand = new RelayCommand(BrowseDirectory);

            _ = LoadSettingsAsync(null);
        }

        #region Properties

        public string DefaultDirectory
        {
            get => _defaultDirectory;
            set => SetProperty(ref _defaultDirectory, value);
        }

        public int MaxResultsPerPage
        {
            get => _maxResultsPerPage;
            set => SetProperty(ref _maxResultsPerPage, Math.Clamp(value, 100, 2000));
        }

        public string MaxResultsPerPageText
        {
            get => MaxResultsPerPage.ToString();
            set
            {
                if (int.TryParse(value, out var result))
                    MaxResultsPerPage = result;
            }
        }

        public int SessionTimeoutMinutes
        {
            get => _sessionTimeoutMinutes;
            set => SetProperty(ref _sessionTimeoutMinutes, Math.Max(0, value));
        }

        public string SessionTimeoutMinutesText
        {
            get => SessionTimeoutMinutes.ToString();
            set
            {
                if (int.TryParse(value, out var result))
                    SessionTimeoutMinutes = result;
            }
        }

        public bool EnableSessionTimeout
        {
            get => _enableSessionTimeout;
            set => SetProperty(ref _enableSessionTimeout, value);
        }

        // ── Search Defaults ───────────────────────────────────────────

        public bool SearchSubDirectoriesByDefault
        {
            get => _searchSubDirectoriesByDefault;
            set => SetProperty(ref _searchSubDirectoriesByDefault, value);
        }

        public bool SearchContentsDefault
        {
            get => _searchContentsDefault;
            set => SetProperty(ref _searchContentsDefault, value);
        }

        public bool AllowPartialMatchDefault
        {
            get => _allowPartialMatchDefault;
            set => SetProperty(ref _allowPartialMatchDefault, value);
        }

        public bool UseRegexDefault
        {
            get => _useRegexDefault;
            set => SetProperty(ref _useRegexDefault, value);
        }

        // ── Size Filter Defaults ──────────────────────────────────────

        /// <summary>Minimum default file size in KB (empty string = no limit).</summary>
        public string SizeMinKbDefault
        {
            get => _sizeMinKbDefault;
            set => SetProperty(ref _sizeMinKbDefault, value);
        }

        /// <summary>Maximum default file size in KB (empty string = no limit).</summary>
        public string SizeMaxKbDefault
        {
            get => _sizeMaxKbDefault;
            set => SetProperty(ref _sizeMaxKbDefault, value);
        }

        #endregion Properties

        #region Commands

        public ICommand LoadSettingsCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand BrowseDirectoryCommand { get; }

        #endregion Commands

        #region Private Methods

        private async Task LoadSettingsAsync(object? _)
        {
            IsBusy = true;
            try
            {
                DefaultDirectory = await _settingsService.GetDefaultSearchDirectoryAsync();

                var maxRes = await _settingsService.GetValueAsync("MaxResultsPerPage");
                var timeout = await _settingsService.GetValueAsync("SessionTimeoutMinutes");
                var enableTimeout = await _settingsService.GetValueAsync("EnableSessionTimeout");
                var subDirs = await _settingsService.GetValueAsync("SearchSubDirectoriesByDefault");
                var contents = await _settingsService.GetValueAsync("SearchContentsDefault");
                var partial = await _settingsService.GetValueAsync("AllowPartialMatchDefault");
                var regex = await _settingsService.GetValueAsync("UseRegexDefault");
                var sizeMin = await _settingsService.GetValueAsync("SizeMinKbDefault");
                var sizeMax = await _settingsService.GetValueAsync("SizeMaxKbDefault");

                MaxResultsPerPage = int.TryParse(maxRes, out var mr) ? mr : 500;
                SessionTimeoutMinutes = int.TryParse(timeout, out var st) ? st : 30;
                EnableSessionTimeout = enableTimeout == "true";

                // Search defaults — default to true for SubDirs and PartialMatch
                SearchSubDirectoriesByDefault = subDirs != "false";
                SearchContentsDefault = contents == "true";
                AllowPartialMatchDefault = partial != "false";
                UseRegexDefault = regex == "true";

                // Size filter defaults (empty string kept as-is)
                SizeMinKbDefault = sizeMin ?? string.Empty;
                SizeMaxKbDefault = sizeMax ?? string.Empty;

                StatusMessage = "Settings loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveSettingsAsync(object? _)
        {
            IsBusy = true;
            try
            {
                await Save("DefaultSearchDirectory", DefaultDirectory,
                           "Default search directory");

                await Save("MaxResultsPerPage", MaxResultsPerPage.ToString(),
                           "Maximum search results per page");

                await Save("SessionTimeoutMinutes", SessionTimeoutMinutes.ToString(),
                           "Session timeout in minutes");

                await Save("EnableSessionTimeout", EnableSessionTimeout.ToString().ToLower(),
                           "Enable auto-logout on inactivity");

                await Save("SearchSubDirectoriesByDefault", SearchSubDirectoriesByDefault.ToString().ToLower(),
                           "Search subdirectories by default");

                await Save("SearchContentsDefault", SearchContentsDefault.ToString().ToLower(),
                           "Search file contents by default");

                await Save("AllowPartialMatchDefault", AllowPartialMatchDefault.ToString().ToLower(),
                           "Allow partial keyword matching by default");

                await Save("UseRegexDefault", UseRegexDefault.ToString().ToLower(),
                           "Use regex keyword matching by default");

                await Save("SizeMinKbDefault", SizeMinKbDefault,
                           "Default minimum file size filter (KB)");

                await Save("SizeMaxKbDefault", SizeMaxKbDefault,
                           "Default maximum file size filter (KB)");

                StatusMessage = "Settings saved successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task Save(string key, string value, string description) =>
            _settingsService.SaveSettingAsync(new AppSettingDto
            {
                Key = key,
                Value = value,
                Description = description
            });

        private void BrowseDirectory(object? _)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select default search directory",
                SelectedPath = DefaultDirectory
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                DefaultDirectory = dlg.SelectedPath;
        }

        #endregion Private Methods
    }
}