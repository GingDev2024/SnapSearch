using SnapSearch.Application.Contracts;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Enums;
using SnapSearch.Presentation.Common;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class FilePreviewViewModel : BaseViewModel
    {
        #region Fields

        private readonly ISearchService _searchService;
        private readonly IAccessLogService _accessLogService;

        private FileResultDto? _currentFile;
        private string _keyword = string.Empty;
        private string _fileContent = string.Empty;
        private int _currentMatchIndex;
        private int _totalMatches;
        private ContentMatchDto? _selectedMatch;

        #endregion Fields

        #region Public Constructors

        public FilePreviewViewModel(ISearchService searchService, IAccessLogService accessLogService)
        {
            _searchService = searchService;
            _accessLogService = accessLogService;

            NextMatchCommand = new RelayCommand(GoToNextMatch, _ => TotalMatches > 0 && CurrentMatchIndex < TotalMatches - 1);
            PreviousMatchCommand = new RelayCommand(GoToPreviousMatch, _ => TotalMatches > 0 && CurrentMatchIndex > 0);
            PrintCommand = new AsyncRelayCommand(ExecutePrintAsync, _ => CanPrint);
            ExportCommand = new AsyncRelayCommand(ExecuteExportAsync, _ => CanExport);
            CopyPathCommand = new RelayCommand(ExecuteCopyPath);
        }

        #endregion Public Constructors

        #region Events

        public event Action<int>? ScrollToLineRequested;
        public event Action? PrintRequested;

        #endregion Events

        #region Properties

        public FileResultDto? CurrentFile
        {
            get => _currentFile;
            set => SetProperty(ref _currentFile, value);
        }

        public string Keyword
        {
            get => _keyword;
            set => SetProperty(ref _keyword, value);
        }

        public string FileContent
        {
            get => _fileContent;
            set => SetProperty(ref _fileContent, value);
        }

        public int CurrentMatchIndex
        {
            get => _currentMatchIndex;
            set
            {
                SetProperty(ref _currentMatchIndex, value);
                OnPropertyChanged(nameof(MatchDisplay));
            }
        }

        public int TotalMatches
        {
            get => _totalMatches;
            set
            {
                SetProperty(ref _totalMatches, value);
                OnPropertyChanged(nameof(MatchDisplay));
            }
        }

        public string MatchDisplay => TotalMatches > 0
            ? $"{CurrentMatchIndex + 1}/{TotalMatches}"
            : "No matches";

        public ContentMatchDto? SelectedMatch
        {
            get => _selectedMatch;
            set => SetProperty(ref _selectedMatch, value);
        }

        public ObservableCollection<ContentMatchDto> ContentMatches { get; } = new();

        public bool CanPrint => SessionContext.Instance.HasPermission("PrintFile");
        public bool CanExport => SessionContext.Instance.HasPermission("ExportFile");
        public bool CanCopy => SessionContext.Instance.HasPermission("CopyFile");

        // --- file type detection ---
        public bool IsTextFile => CurrentFile != null && IsPlainText(CurrentFile.Extension);
        public bool IsPdfFile => CurrentFile?.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) == true;
        public bool IsImageFile => CurrentFile != null && IsImage(CurrentFile.Extension);
        public bool IsUnsupportedFile => CurrentFile != null && !IsTextFile && !IsPdfFile && !IsImageFile;

        public ICommand NextMatchCommand { get; }
        public ICommand PreviousMatchCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CopyPathCommand { get; }

        #endregion Properties

        #region Public Methods

        public async Task LoadFileAsync(FileResultDto file, string keyword)
        {
            CurrentFile = file;
            Keyword = keyword;
            ContentMatches.Clear();
            CurrentMatchIndex = 0;
            FileContent = string.Empty;
            IsBusy = true;
            StatusMessage = "Loading file...";

            try
            {
                // only read raw text for text files
                if (IsPlainText(file.Extension))
                    FileContent = await File.ReadAllTextAsync(file.FilePath);

                // load keyword matches
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var matches = await _searchService.GetContentMatchesAsync(file.FilePath, keyword);
                    foreach (var m in matches)
                        ContentMatches.Add(m);

                    TotalMatches = ContentMatches.Count;
                    if (TotalMatches > 0)
                    {
                        SelectedMatch = ContentMatches[0];
                        ScrollToLineRequested?.Invoke(ContentMatches[0].LineNumber);
                    }
                    StatusMessage = $"{TotalMatches} match(es) found.";
                }
                else
                {
                    StatusMessage = "File loaded.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading file: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                // notify all type flags so bindings update
                OnPropertyChanged(nameof(IsTextFile));
                OnPropertyChanged(nameof(IsPdfFile));
                OnPropertyChanged(nameof(IsImageFile));
                OnPropertyChanged(nameof(IsUnsupportedFile));
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static bool IsPlainText(string ext) =>
            ext.ToLower() is
                // documents
                ".txt" or ".log" or ".md" or ".rtf" or
                // data
                ".csv" or ".json" or ".xml" or ".yaml" or ".yml" or ".toml" or
                // config
                ".ini" or ".cfg" or ".config" or ".env" or ".properties" or
                // code
                ".cs" or ".vb" or ".fs" or ".py" or ".js" or ".ts" or ".java" or
                ".cpp" or ".c" or ".h" or ".go" or ".rs" or ".php" or ".rb" or
                ".html" or ".htm" or ".css" or ".scss" or ".sql" or
                // scripts
                ".bat" or ".cmd" or ".ps1" or ".sh";

        private static bool IsImage(string ext) =>
            ext.ToLower() is ".png" or ".jpg" or ".jpeg" or ".bmp" or
                             ".gif" or ".webp" or ".tiff" or ".tif" or ".ico";

        private void GoToNextMatch(object? _)
        {
            if (CurrentMatchIndex < TotalMatches - 1)
            {
                CurrentMatchIndex++;
                SelectedMatch = ContentMatches[CurrentMatchIndex];
                ScrollToLineRequested?.Invoke(ContentMatches[CurrentMatchIndex].LineNumber);
            }
        }

        private void GoToPreviousMatch(object? _)
        {
            if (CurrentMatchIndex > 0)
            {
                CurrentMatchIndex--;
                SelectedMatch = ContentMatches[CurrentMatchIndex];
                ScrollToLineRequested?.Invoke(ContentMatches[CurrentMatchIndex].LineNumber);
            }
        }

        private async Task ExecutePrintAsync(object? _)
        {
            if (CurrentFile == null) return;
            var userId = SessionContext.Instance.CurrentUser?.Id;
            var username = SessionContext.Instance.CurrentUser?.Username ?? string.Empty;
            await _accessLogService.LogAsync(userId, username, ActionType.PrintFile, CurrentFile.FilePath);
            PrintRequested?.Invoke();
        }

        private async Task ExecuteExportAsync(object? _)
        {
            if (CurrentFile == null) return;
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = CurrentFile.FileName,
                Filter = "All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                File.Copy(CurrentFile.FilePath, dlg.FileName, overwrite: true);
                var userId = SessionContext.Instance.CurrentUser?.Id;
                var username = SessionContext.Instance.CurrentUser?.Username ?? string.Empty;
                await _accessLogService.LogAsync(userId, username, ActionType.ExportFile,
                    CurrentFile.FilePath, details: $"Exported to: {dlg.FileName}");
                StatusMessage = "File exported successfully.";
            }
        }

        private void ExecuteCopyPath(object? _)
        {
            if (CurrentFile == null) return;
            System.Windows.Clipboard.SetText(CurrentFile.FilePath);
            StatusMessage = "Path copied to clipboard.";
        }

        #endregion Private Methods
    }
}