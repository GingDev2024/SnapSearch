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

        private string _defaultDirectory = string.Empty;
        private bool _searchSubDirectoriesByDefault = true;

        private bool _searchContentsDefault;

        private bool _allowPartialMatchDefault = true;

        #endregion Fields

        #region Public Constructors

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            LoadSettingsCommand = new AsyncRelayCommand(LoadSettingsAsync);
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync, _ => !IsBusy);
            BrowseDirectoryCommand = new RelayCommand(BrowseDirectory);

            _ = LoadSettingsAsync(null);
        }

        #endregion Public Constructors

        #region Properties

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

        public ICommand LoadSettingsCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand BrowseDirectoryCommand { get; }

        #endregion Properties

        #region Private Methods

        private async Task LoadSettingsAsync(object? _)
        {
            IsBusy = true;
            try
            {
                DefaultDirectory = await _settingsService.GetDefaultSearchDirectoryAsync();
                var subDir = await _settingsService.GetValueAsync("SearchSubDirectoriesByDefault");
                SearchSubDirectoriesByDefault = subDir != "false";

                var searchContents = await _settingsService.GetValueAsync("SearchContentsDefault");
                SearchContentsDefault = searchContents == "true";

                var partial = await _settingsService.GetValueAsync("AllowPartialMatchDefault");
                AllowPartialMatchDefault = partial != "false";

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
                await _settingsService.SaveSettingAsync(new AppSettingDto
                {
                    Key = "SearchSubDirectoriesByDefault",
                    Value = SearchSubDirectoriesByDefault.ToString().ToLower(),
                    Description = "Search subdirectories by default"
                });
                await _settingsService.SaveSettingAsync(new AppSettingDto
                {
                    Key = "SearchContentsDefault",
                    Value = SearchContentsDefault.ToString().ToLower(),
                    Description = "Search file contents by default"
                });
                await _settingsService.SaveSettingAsync(new AppSettingDto
                {
                    Key = "AllowPartialMatchDefault",
                    Value = AllowPartialMatchDefault.ToString().ToLower(),
                    Description = "Allow partial name matching by default"
                });
                StatusMessage = "Settings saved successfully.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

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