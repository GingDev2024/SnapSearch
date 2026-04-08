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

        private int _pageSize = 500;

        private readonly ISearchService _searchService;
        private readonly IAccessLogService _accessLogService;
        private readonly ISettingsService _settingsService;

        private string _keyword = string.Empty;
        private string _searchDirectory = string.Empty;
        private string? _extensionFilter;
        private DateTime? _dateMin;
        private DateTime? _dateMax;
        private long? _sizeMinKb;
        private long? _sizeMaxKb;
        private bool _allowPartialMatch = true;
        private bool _searchFileContents;
        private bool _searchSubDirectories = true;
        private bool _useRegex;

        private FileResultDto? _selectedFile;
        private int _totalResults;
        private int _displayedResults;
        private bool _hasMoreResults;
        private List<FileResultDto> _allResults = new();

        // Cancellation
        private CancellationTokenSource? _searchCts;

        // Autocomplete
        private bool _showSuggestions;

        private string _selectedSuggestion = string.Empty;

        #endregion Fields

        #region Constructor

        public SearchViewModel(
            ISearchService searchService,
            IAccessLogService accessLogService,
            ISettingsService settingsService)
        {
            _searchService = searchService;
            _accessLogService = accessLogService;
            _settingsService = settingsService;

            SearchCommand = new AsyncRelayCommand(ExecuteSearchAsync, _ => !IsBusy);
            CancelSearchCommand = new RelayCommand(ExecuteCancelSearch, _ => IsBusy);
            BrowseDirectoryCommand = new RelayCommand(ExecuteBrowseDirectory);
            OpenFilePreviewCommand = new AsyncRelayCommand(ExecuteOpenPreviewAsync, _ => CanViewFile && !IsBusy);
            ClearCommand = new RelayCommand(ExecuteClear);
            LoadMoreCommand = new RelayCommand(ExecuteLoadMore, _ => HasMoreResults && !IsBusy);
            ExportResultsCommand = new RelayCommand(ExecuteExportResults, _ => SearchResults.Count > 0 && !IsBusy);
            OpenSelectedCommand = new AsyncRelayCommand(ExecuteOpenPreviewAsync, _ => CanViewFile && !IsBusy);

            _ = LoadDefaultsAsync();
            _ = LoadKeywordSuggestionsAsync();
        }

        #endregion Constructor

        #region Events

        public event Action<FileResultDto, string>? OpenPreviewRequested;

        #endregion Events

        #region Properties — Search Filters

        public string Keyword
        {
            get => _keyword;
            set
            {
                SetProperty(ref _keyword, value);
                FilterSuggestions(value);
            }
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

        /// <summary>Minimum file size in KB (null = no limit).</summary>
        public long? SizeMinKb
        {
            get => _sizeMinKb;
            set => SetProperty(ref _sizeMinKb, value);
        }

        /// <summary>Maximum file size in KB (null = no limit).</summary>
        public long? SizeMaxKb
        {
            get => _sizeMaxKb;
            set => SetProperty(ref _sizeMaxKb, value);
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

        /// <summary>When true the keyword is treated as a .NET regex pattern.</summary>
        public bool UseRegex
        {
            get => _useRegex;
            set => SetProperty(ref _useRegex, value);
        }

        #endregion Properties — Search Filters

        #region Properties — Results

        public FileResultDto? SelectedFile
        {
            get => _selectedFile;
            set
            {
                SetProperty(ref _selectedFile, value);
                OnPropertyChanged(nameof(CanViewFile));
                OnPropertyChanged(nameof(SelectedFileInfo));
            }
        }

        public int TotalResults
        {
            get => _totalResults;
            set => SetProperty(ref _totalResults, value);
        }

        public int DisplayedResults
        {
            get => _displayedResults;
            set => SetProperty(ref _displayedResults, value);
        }

        public bool HasMoreResults
        {
            get => _hasMoreResults;
            set => SetProperty(ref _hasMoreResults, value);
        }

        /// <summary>Summary line shown in the status bar when a file is selected.</summary>
        public string SelectedFileInfo => SelectedFile == null
            ? string.Empty
            : $"{SelectedFile.FileName}  ·  {SelectedFile.SizeDisplay}  ·  {SelectedFile.LastModified:MM/dd/yyyy HH:mm}";

        public ObservableCollection<FileResultDto> SearchResults { get; } = new();

        #endregion Properties — Results

        #region Properties — Autocomplete

        public ObservableCollection<string> AllSuggestions { get; } = new();
        public ObservableCollection<string> FilteredSuggestions { get; } = new();

        public bool ShowSuggestions
        {
            get => _showSuggestions;
            set => SetProperty(ref _showSuggestions, value);
        }

        public string SelectedSuggestion
        {
            get => _selectedSuggestion;
            set
            {
                SetProperty(ref _selectedSuggestion, value);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Keyword = value;
                    ShowSuggestions = false;
                }
            }
        }

        #endregion Properties — Autocomplete

        #region Properties — Permissions / Commands

        public bool CanViewFile => SelectedFile != null && SessionContext.Instance.HasPermission("ViewFile");
        public bool CanPrint => SessionContext.Instance.HasPermission("PrintFile");
        public bool CanExport => SessionContext.Instance.HasPermission("ExportFile");

        public ICommand SearchCommand { get; }
        public ICommand CancelSearchCommand { get; }
        public ICommand BrowseDirectoryCommand { get; }
        public ICommand OpenFilePreviewCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand LoadMoreCommand { get; }
        public ICommand ExportResultsCommand { get; }
        public ICommand OpenSelectedCommand { get; }

        #endregion Properties — Permissions / Commands

        #region Private Methods — Initialization

        private async Task LoadDefaultsAsync()
        {
            SearchDirectory = await _settingsService.GetDefaultSearchDirectoryAsync();

            var sub = await _settingsService.GetValueAsync("SearchSubDirectoriesByDefault");
            var contents = await _settingsService.GetValueAsync("SearchContentsDefault");
            var partial = await _settingsService.GetValueAsync("AllowPartialMatchDefault");
            var maxRes = await _settingsService.GetValueAsync("MaxResultsPerPage");

            if (sub != null)
                SearchSubDirectories = sub != "false";
            if (contents != null)
                SearchFileContents = contents == "true";
            if (partial != null)
                AllowPartialMatch = partial != "false";

            if (int.TryParse(maxRes, out var mr) && mr > 0)
                _pageSize = Math.Clamp(mr, 100, 2000);
        }

        private async Task LoadKeywordSuggestionsAsync()
        {
            try
            {
                var userId = SessionContext.Instance.CurrentUser?.Id ?? 0;
                var history = await _searchService.GetSearchHistoryAsync(userId);

                AllSuggestions.Clear();
                foreach (var h in history
                    .Select(h => h.Keyword)
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(50))
                {
                    AllSuggestions.Add(h);
                }
            }
            catch { /* non-critical — ignore */ }
        }

        private void FilterSuggestions(string input)
        {
            FilteredSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
            {
                ShowSuggestions = false;
                return;
            }

            var matches = AllSuggestions
                .Where(s => s.Contains(input, StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList();

            foreach (var m in matches)
                FilteredSuggestions.Add(m);

            ShowSuggestions = FilteredSuggestions.Count > 0;
        }

        #endregion Private Methods — Initialization

        #region Private Methods — Commands

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

            // Cancel any previous search
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            IsBusy = true;
            ShowSuggestions = false;
            StatusMessage = "Searching…";
            SearchResults.Clear();
            _allResults.Clear();
            TotalResults = 0;
            DisplayedResults = 0;
            HasMoreResults = false;

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
                    SizeMin = SizeMinKb.HasValue ? SizeMinKb.Value * 1024 : null,
                    SizeMax = SizeMaxKb.HasValue ? SizeMaxKb.Value * 1024 : null,
                    AllowPartialMatch = AllowPartialMatch,
                    SearchFileContents = SearchFileContents,
                    SearchSubDirectories = SearchSubDirectories
                };

                var results = (await _searchService.SearchFilesAsync(request, userId, token)).ToList();

                _allResults = results;
                TotalResults = results.Count;

                // Show first page
                var page = results.Take(_pageSize).ToList();
                foreach (var r in page)
                    SearchResults.Add(r);

                DisplayedResults = SearchResults.Count;
                HasMoreResults = results.Count > _pageSize;

                StatusMessage = token.IsCancellationRequested
                    ? "Search cancelled."
                    : $"Found {TotalResults} file(s).{(HasMoreResults ? $" Showing first {_pageSize}." : "")}";

                // Refresh autocomplete suggestions with the new keyword
                if (!AllSuggestions.Contains(Keyword, StringComparer.OrdinalIgnoreCase))
                    AllSuggestions.Insert(0, Keyword);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Search cancelled.";
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

        private void ExecuteCancelSearch(object? _)
        {
            _searchCts?.Cancel();
            StatusMessage = "Cancelling…";
        }

        private void ExecuteLoadMore(object? _)
        {
            var nextPage = _allResults.Skip(DisplayedResults).Take(_pageSize).ToList();
            foreach (var r in nextPage)
                SearchResults.Add(r);

            DisplayedResults += nextPage.Count;
            HasMoreResults = DisplayedResults < TotalResults;
            StatusMessage = $"Showing {DisplayedResults} of {TotalResults} file(s).";
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
            _searchCts?.Cancel();
            Keyword = string.Empty;
            ExtensionFilter = null;
            DateMin = null;
            DateMax = null;
            SizeMinKb = null;
            SizeMaxKb = null;
            SearchResults.Clear();
            _allResults.Clear();
            TotalResults = 0;
            DisplayedResults = 0;
            HasMoreResults = false;
            StatusMessage = string.Empty;
            SelectedFile = null;
            ShowSuggestions = false;
        }

        /// <summary>
        /// Exports ALL current search results (not just the displayed page) to a CSV file.
        /// </summary>
        private void ExecuteExportResults(object? _)
        {
            if (_allResults.Count == 0 && SearchResults.Count == 0)
            {
                StatusMessage = "No results to export.";
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"SearchResults_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true)
                return;

            try
            {
                var source = _allResults.Count > 0 ? _allResults : SearchResults.ToList();
                var lines = new List<string>
                {
                    "FileName,Extension,Size,LastModified,Matches,Directory,FilePath"
                };

                foreach (var r in source)
                {
                    lines.Add(
                        $"\"{r.FileName}\"," +
                        $"\"{r.Extension}\"," +
                        $"\"{r.SizeDisplay}\"," +
                        $"\"{r.LastModified:yyyy-MM-dd HH:mm}\"," +
                        $"{r.ContentMatchCount}," +
                        $"\"{r.Directory}\"," +
                        $"\"{r.FilePath}\"");
                }

                File.WriteAllLines(dlg.FileName, lines);
                StatusMessage = $"Exported {lines.Count - 1} result(s) to CSV.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }

        #endregion Private Methods — Commands
    }
}