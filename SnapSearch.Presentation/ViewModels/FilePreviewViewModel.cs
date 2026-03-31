using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SnapSearch.Application.Contracts;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Enums;
using SnapSearch.Presentation.Common;
using System.Collections.ObjectModel;
using System.Data;
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
        private string _docxText = string.Empty;
        private int _currentMatchIndex;
        private int _totalMatches;
        private ContentMatchDto? _selectedMatch;
        private DataTable? _xlsxData;
        private ObservableCollection<string> _xlsxSheetNames = new();
        private string? _selectedSheetName;

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
            SelectSheetCommand = new RelayCommand(ExecuteSelectSheet);
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
                OnPropertyChanged(nameof(IsTextFile));
                OnPropertyChanged(nameof(IsPdfFile));
                OnPropertyChanged(nameof(IsImageFile));
                OnPropertyChanged(nameof(IsDocxFile));
                OnPropertyChanged(nameof(IsXlsxFile));
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

        /// <summary>Extracted plain text from a .docx file for RichTextBox rendering.</summary>
        public string DocxText
        {
            get => _docxText;
            set => SetProperty(ref _docxText, value);
        }

        /// <summary>Parsed sheet data for XLSX DataGrid binding.</summary>
        public DataTable? XlsxData
        {
            get => _xlsxData;
            set => SetProperty(ref _xlsxData, value);
        }

        /// <summary>List of sheet names in the XLSX file.</summary>
        public ObservableCollection<string> XlsxSheetNames
        {
            get => _xlsxSheetNames;
            set => SetProperty(ref _xlsxSheetNames, value);
        }

        /// <summary>Currently selected sheet name.</summary>
        public string? SelectedSheetName
        {
            get => _selectedSheetName;
            set
            {
                if (_selectedSheetName == value) return;
                SetProperty(ref _selectedSheetName, value);
                if (value != null && CurrentFile != null)
                    _ = LoadXlsxSheetAsync(CurrentFile.FilePath, value);
            }
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
        public bool IsTextFile => CurrentFile != null && IsPlainText(CurrentFile.Extension);
        public bool IsPdfFile => CurrentFile?.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) == true;
        public bool IsImageFile => CurrentFile != null && IsImage(CurrentFile.Extension);
        public bool IsDocxFile => CurrentFile != null && IsDocx(CurrentFile.Extension);
        public bool IsXlsxFile => CurrentFile != null && IsXlsx(CurrentFile.Extension);

        public bool IsUnsupportedFile =>
            CurrentFile != null &&
            !IsTextFile && !IsPdfFile && !IsImageFile && !IsDocxFile && !IsXlsxFile;

        public ICommand NextMatchCommand { get; }
        public ICommand PreviousMatchCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CopyPathCommand { get; }
        public ICommand SelectSheetCommand { get; }

        #endregion Properties

        #region Public Methods

        public async Task LoadFileAsync(FileResultDto file, string keyword)
        {
            // Reset all state
            FileContent = string.Empty;
            DocxText = string.Empty;
            XlsxData = null;
            XlsxSheetNames.Clear();
            _selectedSheetName = null;
            OnPropertyChanged(nameof(SelectedSheetName));
            TotalMatches = 0;
            CurrentMatchIndex = 0;
            SelectedMatch = null;
            ContentMatches.Clear();

            IsBusy = true;
            StatusMessage = "Loading...";

            CurrentFile = file;
            Keyword = keyword;

            try
            {
                if (IsTextFile)
                {
                    FileContent = await File.ReadAllTextAsync(file.FilePath);
                }
                else if (IsDocxFile)
                {
                    DocxText = await Task.Run(() => ExtractDocxText(file.FilePath));
                }
                else if (IsXlsxFile)
                {
                    // Parse entirely on background thread — returns all data
                    var (sheetNames, firstTable) = await Task.Run(() => LoadXlsxWorkbookData(file.FilePath));

                    // Back on UI thread — assign before FileLoaded fires
                    XlsxSheetNames.Clear();
                    foreach (var name in sheetNames)
                        XlsxSheetNames.Add(name);

                    XlsxData = firstTable;
                    _selectedSheetName = sheetNames.FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedSheetName));
                }

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
                // XlsxData is guaranteed set before this fires
                FileLoaded?.Invoke();
            }
        }

        public void OnTextRendered()
        {
            if (TotalMatches > 0 && ContentMatches.Count > 0)
                ScrollToLineRequested?.Invoke(ContentMatches[0].LineNumber);
        }

        #endregion Public Methods

        #region Private Methods — File Type Helpers

        private static bool IsPlainText(string ext) =>
            ext.ToLower() is
                ".txt" or ".log" or ".md" or ".rtf" or
                ".csv" or ".json" or ".xml" or ".yaml" or ".yml" or ".toml" or
                ".ini" or ".cfg" or ".config" or ".env" or ".properties" or
                ".cs" or ".vb" or ".fs" or ".py" or ".js" or ".ts" or ".java" or
                ".cpp" or ".c" or ".h" or ".go" or ".rs" or ".php" or ".rb" or
                ".html" or ".htm" or ".css" or ".scss" or ".sql" or
                ".bat" or ".cmd" or ".ps1" or ".sh";

        private static bool IsImage(string ext) =>
            ext.ToLower() is ".png" or ".jpg" or ".jpeg" or ".bmp" or
                             ".gif" or ".webp" or ".tiff" or ".tif" or ".ico";

        private static bool IsDocx(string ext) =>
            ext.ToLower() is ".docx" or ".doc";

        private static bool IsXlsx(string ext) =>
            ext.ToLower() is ".xlsx" or ".xls";

        #endregion Private Methods — File Type Helpers

        #region Private Methods — DOCX

        private static string ExtractDocxText(string filePath)
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            var sb = new System.Text.StringBuilder();
            foreach (var paragraph in body.Elements<Paragraph>())
                sb.AppendLine(paragraph.InnerText);

            return sb.ToString();
        }

        #endregion Private Methods — DOCX

        #region Private Methods — XLSX

        /// <summary>
        /// Parses the workbook entirely on a background thread.
        /// Returns sheet names + the first sheet's DataTable.
        /// </summary>
        private static (List<string> sheetNames, DataTable table) LoadXlsxWorkbookData(string filePath)
        {
            using var workbook = new XLWorkbook(filePath);
            var sheetNames = workbook.Worksheets.Select(ws => ws.Name).ToList();
            var table = sheetNames.Count > 0
                ? BuildDataTable(workbook.Worksheet(sheetNames[0]))
                : new DataTable();

            return (sheetNames, table);
        }

        /// <summary>
        /// Loads a specific sheet when the user clicks a tab.
        /// </summary>
        private async Task LoadXlsxSheetAsync(string filePath, string sheetName)
        {
            try
            {
                IsBusy = true;
                StatusMessage = $"Loading sheet: {sheetName}...";

                var table = await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook(filePath);
                    return BuildDataTable(workbook.Worksheet(sheetName));
                });

                XlsxData = table;
                StatusMessage = $"Sheet '{sheetName}' loaded.";
                FileLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading sheet: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[FilePreviewViewModel] LoadXlsxSheetAsync error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Builds a DataTable from a worksheet.
        /// FIX: Trims header names and deduplicates them so DuplicateNameException never throws.
        /// Example: two columns both named "Test1 " become "Test1" and "Test1 (2)".
        /// </summary>
        private static DataTable BuildDataTable(IXLWorksheet ws)
        {
            var table = new DataTable();
            var range = ws.RangeUsed();
            if (range == null) return table;

            var rows = range.RowsUsed().ToList();
            if (rows.Count == 0) return table;

            // ── Column headers with duplicate resolution ─────────────────────
            var usedNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in rows[0].Cells())
            {
                // Trim whitespace — "Test1 " and "Test1" are treated as the same name
                var raw = cell.GetString().Trim();

                if (string.IsNullOrWhiteSpace(raw))
                    raw = $"Column {cell.Address.ColumnNumber}";

                if (usedNames.TryGetValue(raw, out int count))
                {
                    // Name already used — append incrementing suffix
                    usedNames[raw] = count + 1;
                    raw = $"{raw} ({count + 1})";
                }
                else
                {
                    usedNames[raw] = 1;
                }

                table.Columns.Add(raw);
            }

            // ── Data rows ────────────────────────────────────────────────────
            foreach (var row in rows.Skip(1))
            {
                var dataRow = table.NewRow();
                var cells = row.Cells().ToList();
                for (int i = 0; i < Math.Min(cells.Count, table.Columns.Count); i++)
                    dataRow[i] = cells[i].GetString();
                table.Rows.Add(dataRow);
            }

            return table;
        }

        #endregion Private Methods — XLSX

        #region Private Methods — Commands

        private void GoToNextMatch(object? _)
        {
            if (CurrentMatchIndex >= TotalMatches - 1) return;
            CurrentMatchIndex++;
            SelectedMatch = ContentMatches[CurrentMatchIndex];
            ScrollToLineRequested?.Invoke(ContentMatches[CurrentMatchIndex].LineNumber);
        }

        private void GoToPreviousMatch(object? _)
        {
            if (CurrentMatchIndex <= 0) return;
            CurrentMatchIndex--;
            SelectedMatch = ContentMatches[CurrentMatchIndex];
            ScrollToLineRequested?.Invoke(ContentMatches[CurrentMatchIndex].LineNumber);
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

        private void ExecuteSelectSheet(object? param)
        {
            if (param is string sheetName && CurrentFile != null)
                SelectedSheetName = sheetName;
        }

        #endregion Private Methods — Commands
    }
}