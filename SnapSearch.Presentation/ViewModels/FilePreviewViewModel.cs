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

        #region Constructor

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

        #endregion Constructor

        #region Events

        /// <summary>Fires at the very end of LoadFileAsync — all properties are set.</summary>
        public event Action? FileLoaded;

        /// <summary>Fires only after the view finishes populating the RichTextBox.</summary>
        public event Action<int>? ScrollToLineRequested;

        public event Action? PrintRequested;

        #endregion Events

        #region Properties

        public FileResultDto? CurrentFile
        {
            get => _currentFile;
            private set
            {
                SetProperty(ref _currentFile, value);
                // Immediately refresh all file-type computed flags
                OnPropertyChanged(nameof(IsTextFile));
                OnPropertyChanged(nameof(IsPdfFile));
                OnPropertyChanged(nameof(IsImageFile));
                OnPropertyChanged(nameof(IsUnsupportedFile));
            }
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
            set { SetProperty(ref _currentMatchIndex, value); OnPropertyChanged(nameof(MatchDisplay)); }
        }

        public int TotalMatches
        {
            get => _totalMatches;
            set { SetProperty(ref _totalMatches, value); OnPropertyChanged(nameof(MatchDisplay)); }
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

        // ── File type flags ───────────────────────────────────────────────────
        // IMPORTANT: IsTextFile must NOT include .pdf/.docx/.doc/.xlsx etc.
        // Those have dedicated viewers.  IsUnsupportedFile catches everything else
        // (docx, xlsx, pptx …) and shows the "Open with Default App" panel.
        public bool IsTextFile => CurrentFile != null && IsPlainText(CurrentFile.Extension);

        public bool IsPdfFile => CurrentFile?.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) == true;
        public bool IsImageFile => CurrentFile != null && IsImage(CurrentFile.Extension);

        /// <summary>
        /// True for any file that isn't plain text, PDF, or image.
        /// Includes .docx, .xlsx, .pptx, .doc, .xls, .ppt, and anything else.
        /// The view shows "Open with Default App" for these.
        /// </summary>
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
            // Reset all state before loading
            FileContent = string.Empty;
            TotalMatches = 0;
            CurrentMatchIndex = 0;
            SelectedMatch = null;
            ContentMatches.Clear();

            IsBusy = true;
            StatusMessage = "Loading...";

            // Setting CurrentFile fires all IsXxxFile property notifications immediately
            CurrentFile = file;
            Keyword = keyword;

            try
            {
                // Only read raw bytes for plain text files.
                // PDFs render via PdfiumViewer; Office docs open with default app.
                if (IsTextFile)
                    FileContent = await File.ReadAllTextAsync(file.FilePath);

                // Always fetch content matches when a keyword is present —
                // the service handles pdf/docx/plain-text extraction internally.
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var matches = await _searchService.GetContentMatchesAsync(file.FilePath, keyword);
                    foreach (var m in matches)
                        ContentMatches.Add(m);

                    TotalMatches = ContentMatches.Count;

                    if (TotalMatches > 0)
                    {
                        CurrentMatchIndex = 0;
                        SelectedMatch = ContentMatches[0];
                    }

                    StatusMessage = TotalMatches > 0
                        ? $"{TotalMatches} match(es) found."
                        : "No matches found.";
                }
                else
                {
                    StatusMessage = "File loaded.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading file: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[FilePreviewViewModel] LoadFileAsync error: {ex}");
            }
            finally
            {
                IsBusy = false;
                // Fire AFTER IsBusy=false so the loading overlay hides before content appears
                FileLoaded?.Invoke();
            }
        }

        /// <summary>
        /// Called by the view after RenderHighlightedContent() completes.
        /// Only now is BringIntoView safe — the blocks exist in the RichTextBox.
        /// </summary>
        public void OnTextRendered()
        {
            if (TotalMatches > 0 && ContentMatches.Count > 0)
                ScrollToLineRequested?.Invoke(ContentMatches[0].LineNumber);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Plain-text extensions that can be read as raw strings and shown in the RichTextBox.
        /// Deliberately excludes .pdf, .docx, .doc, .xlsx, .xls, .pptx, .ppt — those
        /// are handled by their own viewers or the "Open with Default App" fallback.
        /// </summary>
        private static bool IsPlainText(string ext) =>
            ext.ToLower() is
                // documents
                ".txt" or ".log" or ".md" or ".rtf" or
                // data
                ".csv" or ".json" or ".xml" or ".yaml" or ".yml" or ".toml" or
                // config
                ".ini" or ".cfg" or ".config" or ".env" or ".properties" or
                // code — all the common ones
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
            if (CurrentMatchIndex >= TotalMatches - 1)
                return;
            CurrentMatchIndex++;
            SelectedMatch = ContentMatches[CurrentMatchIndex];
            ScrollToLineRequested?.Invoke(ContentMatches[CurrentMatchIndex].LineNumber);
        }

        private void GoToPreviousMatch(object? _)
        {
            if (CurrentMatchIndex <= 0)
                return;
            CurrentMatchIndex--;
            SelectedMatch = ContentMatches[CurrentMatchIndex];
            ScrollToLineRequested?.Invoke(ContentMatches[CurrentMatchIndex].LineNumber);
        }

        private async Task ExecutePrintAsync(object? _)
        {
            if (CurrentFile == null)
                return;
            var userId = SessionContext.Instance.CurrentUser?.Id;
            var username = SessionContext.Instance.CurrentUser?.Username ?? string.Empty;
            await _accessLogService.LogAsync(userId, username, ActionType.PrintFile, CurrentFile.FilePath);
            PrintRequested?.Invoke();
        }

        private async Task ExecuteExportAsync(object? _)
        {
            if (CurrentFile == null)
                return;
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
            if (CurrentFile == null)
                return;
            System.Windows.Clipboard.SetText(CurrentFile.FilePath);
            StatusMessage = "Path copied to clipboard.";
        }

        #endregion Private Methods
    }
}