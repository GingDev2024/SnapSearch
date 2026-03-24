using SnapSearch.Application.Contracts;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Enums;
using SnapSearch.Presentation.Common;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        #region Fields

        private readonly ISearchService _searchService;
        private readonly IAccessLogService _accessLogService;
        private readonly ISettingsService _settingsService;

        private string _keyword = string.Empty;
        private string _searchDirectory = string.Empty;

        private string? _extensionFilter;

        private DateTime? _dateMin;

        private DateTime? _dateMax;

        private bool _allowPartialMatch = true;

        private bool _searchFileContents;

        private bool _searchSubDirectories = true;

        private FileResultDto? _selectedFile;

        private int _totalResults;

        #endregion Fields

        #region Public Constructors

        public SearchViewModel(
            ISearchService searchService,
            IAccessLogService accessLogService,
            ISettingsService settingsService)
        {
            _searchService = searchService;
            _accessLogService = accessLogService;
            _settingsService = settingsService;

            SearchCommand = new AsyncRelayCommand(ExecuteSearchAsync, _ => !IsBusy);
            BrowseDirectoryCommand = new RelayCommand(ExecuteBrowseDirectory);
            OpenFilePreviewCommand = new AsyncRelayCommand(ExecuteOpenPreviewAsync, _ => CanViewFile && !IsBusy);
            ClearCommand = new RelayCommand(ExecuteClear);

            _ = LoadDefaultDirectoryAsync();
        }

        #endregion Public Constructors

        #region Events

        public event Action<FileResultDto, string>? OpenPreviewRequested;

        #endregion Events

        #region Properties

        public string Keyword
        {
            get => _keyword;
            set => SetProperty(ref _keyword, value);
        }

        public string SearchDirectory
        {
            get => _searchDirectory;
            set => SetProperty(ref _searchDirectory, value);
        }

        public string? ExtensionFilter
        {
            get => _extensionFilter;
            set => SetProperty(ref _extensionFilter, value);
        }

        public DateTime? DateMin
        {
            get => _dateMin;
            set => SetProperty(ref _dateMin, value);
        }

        public DateTime? DateMax
        {
            get => _dateMax;
            set => SetProperty(ref _dateMax, value);
        }

        public bool AllowPartialMatch
        {
            get => _allowPartialMatch;
            set => SetProperty(ref _allowPartialMatch, value);
        }

        public bool SearchFileContents
        {
            get => _searchFileContents;
            set => SetProperty(ref _searchFileContents, value);
        }

        public bool SearchSubDirectories
        {
            get => _searchSubDirectories;
            set => SetProperty(ref _searchSubDirectories, value);
        }

        public FileResultDto? SelectedFile
        {
            get => _selectedFile;
            set
            {
                SetProperty(ref _selectedFile, value);
                OnPropertyChanged(nameof(CanViewFile));
            }
        }

        public int TotalResults
        {
            get => _totalResults;
            set => SetProperty(ref _totalResults, value);
        }

        public ObservableCollection<FileResultDto> SearchResults { get; } = new();

        public bool CanViewFile => SelectedFile != null && SessionContext.Instance.HasPermission("ViewFile");
        public bool CanPrint => SessionContext.Instance.HasPermission("PrintFile");
        public bool CanExport => SessionContext.Instance.HasPermission("ExportFile");

        public ICommand SearchCommand { get; }
        public ICommand BrowseDirectoryCommand { get; }
        public ICommand OpenFilePreviewCommand { get; }
        public ICommand ClearCommand { get; }

        #endregion Properties

        #region Private Methods

        private async Task LoadDefaultDirectoryAsync()
        {
            SearchDirectory = await _settingsService.GetDefaultSearchDirectoryAsync();
        }

        private async Task ExecuteSearchAsync(object? _)
        {
            if (string.IsNullOrWhiteSpace(Keyword))
            {
                StatusMessage = "Please enter a search keyword.";
                return;
            }
            if (string.IsNullOrWhiteSpace(SearchDirectory) || !Directory.Exists(SearchDirectory))
            {
                StatusMessage = "Please select a valid search directory.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Searching...";
            SearchResults.Clear();

            try
            {
                var userId = SessionContext.Instance.CurrentUser?.Id ?? 0;
                var request = new FileSearchRequestDto
                {
                    Keyword = Keyword,
                    SearchDirectory = SearchDirectory,
                    ExtensionFilter = ExtensionFilter,
                    DateMin = DateMin,
                    DateMax = DateMax,
                    AllowPartialMatch = AllowPartialMatch,
                    SearchFileContents = SearchFileContents,
                    SearchSubDirectories = SearchSubDirectories
                };

                var results = await _searchService.SearchFilesAsync(request, userId);
                foreach (var r in results)
                    SearchResults.Add(r);

                TotalResults = SearchResults.Count;
                StatusMessage = $"Found {TotalResults} file(s).";
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

        private void ExecuteBrowseDirectory(object? _)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select search directory",
                SelectedPath = SearchDirectory
            };
            if (dialog.ShowDialog() == DialogResult.OK)
                SearchDirectory = dialog.SelectedPath;
        }

        private async Task ExecuteOpenPreviewAsync(object? _)
        {
            if (SelectedFile == null)
                return;

            var userId = SessionContext.Instance.CurrentUser?.Id;
            var username = SessionContext.Instance.CurrentUser?.Username ?? string.Empty;
            await _accessLogService.LogAsync(userId, username, ActionType.ViewFile, SelectedFile.FilePath);

            OpenPreviewRequested?.Invoke(SelectedFile, Keyword);
        }

        private void ExecuteClear(object? _)
        {
            Keyword = string.Empty;
            ExtensionFilter = null;
            DateMin = null;
            DateMax = null;
            SearchResults.Clear();
            TotalResults = 0;
            StatusMessage = string.Empty;
            SelectedFile = null;
        }

        #endregion Private Methods
    }
}