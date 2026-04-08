using SnapSearch.Application.Contracts;
using SnapSearch.Application.DTOs;
using SnapSearch.Presentation.Common;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Fields

        private readonly ISettingsService _settingsService;

        // Existing
        private string _defaultDirectory = string.Empty;

        private bool _searchSubDirectoriesByDefault = true;
        private bool _searchContentsDefault;
        private bool _allowPartialMatchDefault = true;

        // New — size filter defaults
        private string _sizeMinKbDefault = string.Empty;

        private string _sizeMaxKbDefault = string.Empty;

        // New — pagination
        private int _maxResultsPerPage = 500;

        // New — session timeout
        private int _sessionTimeoutMinutes = 30;

        private bool _enableSessionTimeout;

        // New — regex default
        private bool _useRegexDefault;

        #endregion Fields

        #region Constructor

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            LoadSettingsCommand = new AsyncRelayCommand(LoadSettingsAsync);
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync, _ => !IsBusy);
            BrowseDirectoryCommand = new RelayCommand(BrowseDirectory);

            _ = LoadSettingsAsync(null);
        }

        #endregion Constructor

        #region Properties — Existing

        public string DefaultDirectory
        {
            get => _defaultDirectory;
            set => SetProperty(ref _defaultDirectory, value);
        }

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

        #endregion Properties — Existing

        #region Properties — New

        /// <summary>Default minimum file size in KB (empty = no limit).</summary>
        public string SizeMinKbDefault
        {
            get => _sizeMinKbDefault;
            set => SetProperty(ref _sizeMinKbDefault, value);
        }

        /// <summary>Default maximum file size in KB (empty = no limit).</summary>
        public string SizeMaxKbDefault
        {
            get => _sizeMaxKbDefault;
            set => SetProperty(ref _sizeMaxKbDefault, value);
        }

        /// <summary>How many results to show before "Load More" (100–2000).</summary>
        public int MaxResultsPerPage
        {
            get => _maxResultsPerPage;
            set => SetProperty(ref _maxResultsPerPage, Math.Clamp(value, 100, 2000));
        }

        /// <summary>Auto-logout after this many minutes of inactivity (0 = disabled).</summary>
        public int SessionTimeoutMinutes
        {
            get => _sessionTimeoutMinutes;
            set => SetProperty(ref _sessionTimeoutMinutes, Math.Max(0, value));
        }

        public bool EnableSessionTimeout
        {
            get => _enableSessionTimeout;
            set => SetProperty(ref _enableSessionTimeout, value);
        }

        /// <summary>Whether the Regex option is checked by default on the search panel.</summary>
        public bool UseRegexDefault
        {
            get => _useRegexDefault;
            set => SetProperty(ref _useRegexDefault, value);
        }

        #endregion Properties — New

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

                var subDir = await _settingsService.GetValueAsync("SearchSubDirectoriesByDefault");
                var contents = await _settingsService.GetValueAsync("SearchContentsDefault");
                var partial = await _settingsService.GetValueAsync("AllowPartialMatchDefault");
                var sizeMin = await _settingsService.GetValueAsync("SizeMinKbDefault");
                var sizeMax = await _settingsService.GetValueAsync("SizeMaxKbDefault");
                var maxRes = await _settingsService.GetValueAsync("MaxResultsPerPage");
                var timeout = await _settingsService.GetValueAsync("SessionTimeoutMinutes");
                var sto = await _settingsService.GetValueAsync("EnableSessionTimeout");
                var regex = await _settingsService.GetValueAsync("UseRegexDefault");

                SearchSubDirectoriesByDefault = subDir != "false";
                SearchContentsDefault = contents == "true";
                AllowPartialMatchDefault = partial != "false";
                SizeMinKbDefault = sizeMin ?? string.Empty;
                SizeMaxKbDefault = sizeMax ?? string.Empty;
                MaxResultsPerPage = int.TryParse(maxRes, out var mr) ? mr : 500;
                SessionTimeoutMinutes = int.TryParse(timeout, out var st) ? st : 30;
                EnableSessionTimeout = sto == "true";
                UseRegexDefault = regex == "true";

                StatusMessage = "Settings loaded.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task SaveSettingsAsync(object? _)
        {
            IsBusy = true;
            try
            {
                await _settingsService.SetDefaultSearchDirectoryAsync(DefaultDirectory);

                await Save("SearchSubDirectoriesByDefault", SearchSubDirectoriesByDefault.ToString().ToLower(),
                    "Search subdirectories by default");
                await Save("SearchContentsDefault", SearchContentsDefault.ToString().ToLower(),
                    "Search file contents by default");
                await Save("AllowPartialMatchDefault", AllowPartialMatchDefault.ToString().ToLower(),
                    "Allow partial name matching by default");
                await Save("SizeMinKbDefault", SizeMinKbDefault,
                    "Default minimum file size filter (KB)");
                await Save("SizeMaxKbDefault", SizeMaxKbDefault,
                    "Default maximum file size filter (KB)");
                await Save("MaxResultsPerPage", MaxResultsPerPage.ToString(),
                    "Maximum search results shown before Load More");
                await Save("SessionTimeoutMinutes", SessionTimeoutMinutes.ToString(),
                    "Auto-logout session timeout in minutes");
                await Save("EnableSessionTimeout", EnableSessionTimeout.ToString().ToLower(),
                    "Enable auto-logout on inactivity");
                await Save("UseRegexDefault", UseRegexDefault.ToString().ToLower(),
                    "Use regex keyword matching by default");

                StatusMessage = "Settings saved successfully.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
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