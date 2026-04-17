using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using SnapSearch.Application.Contracts;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Enums;
using SnapSearch.Presentation.Common;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class PptxSlideData
    {
        #region Properties

        public string Header { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        #endregion Properties
    }

    public class ZipEntryInfo
    {
        #region Properties

        public string Name { get; set; } = string.Empty;
        public string SizeDisplay { get; set; } = string.Empty;
        public string CompressedDisplay { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }

        #endregion Properties
    }

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
        private string _pptxSlideInfo = string.Empty;
        private string _zipEntryCount = string.Empty;

        // ── File navigation ──────────────────────────────────────────
        private IReadOnlyList<FileResultDto> _fileList = Array.Empty<FileResultDto>();

        private int _fileIndex;

        #endregion Fields

        #region Constructor

        public FilePreviewViewModel(ISearchService searchService, IAccessLogService accessLogService)
        {
            _searchService = searchService;
            _accessLogService = accessLogService;

            System.Text.Encoding.RegisterProvider(
                System.Text.CodePagesEncodingProvider.Instance);

            NextMatchCommand = new RelayCommand(GoToNextMatch,
                _ => TotalMatches > 0 && CurrentMatchIndex < TotalMatches - 1);
            PreviousMatchCommand = new RelayCommand(GoToPreviousMatch,
                _ => TotalMatches > 0 && CurrentMatchIndex > 0);
            PrintCommand = new AsyncRelayCommand(ExecutePrintAsync, _ => CanPrint);
            ExportCommand = new AsyncRelayCommand(ExecuteExportAsync, _ => CanExport);
            CopyPathCommand = new RelayCommand(ExecuteCopyPath);
            SelectSheetCommand = new RelayCommand(ExecuteSelectSheet);

            // File navigation commands
            PreviousFileCommand = new RelayCommand(
                _ => NavigateFile(-1), _ => HasPreviousFile);
            NextFileCommand = new RelayCommand(
                _ => NavigateFile(+1), _ => HasNextFile);
        }

        #endregion Constructor

        #region Events

        public event Action? FileLoaded;

        public event Action<int>? ScrollToLineRequested;

        public event Action? PrintRequested;

        #endregion Events

        #region Properties — File Navigation

        public bool HasPreviousFile => _fileIndex > 0;
        public bool HasNextFile => _fileIndex < _fileList.Count - 1;

        public string FilePositionDisplay =>
            _fileList.Count > 1 ? $"{_fileIndex + 1} / {_fileList.Count}" : string.Empty;

        public ICommand PreviousFileCommand { get; }
        public ICommand NextFileCommand { get; }

        public void SetFileList(IReadOnlyList<FileResultDto> files, int index)
        {
            _fileList = files;
            _fileIndex = index;
            RefreshFileNavProps();
        }

        private void NavigateFile(int delta)
        {
            int next = _fileIndex + delta;
            if (next < 0 || next >= _fileList.Count)
                return;
            _fileIndex = next;
            RefreshFileNavProps();
            _ = LoadFileAsync(_fileList[_fileIndex], Keyword);
        }

        private void RefreshFileNavProps()
        {
            OnPropertyChanged(nameof(HasPreviousFile));
            OnPropertyChanged(nameof(HasNextFile));
            OnPropertyChanged(nameof(FilePositionDisplay));
        }

        #endregion Properties — File Navigation

        #region Properties — Current file & text

        public FileResultDto? CurrentFile
        {
            get => _currentFile;
            private set
            {
                SetProperty(ref _currentFile, value);
                OnPropertyChanged(nameof(IsTextFile));
                OnPropertyChanged(nameof(IsRtfFile));
                OnPropertyChanged(nameof(IsPdfFile));
                OnPropertyChanged(nameof(IsImageFile));
                OnPropertyChanged(nameof(IsDocxFile));
                OnPropertyChanged(nameof(IsDocFile));
                OnPropertyChanged(nameof(IsDocmFile));
                OnPropertyChanged(nameof(IsOdtFile));
                OnPropertyChanged(nameof(IsXlsxFile));
                OnPropertyChanged(nameof(IsXlsFile));
                OnPropertyChanged(nameof(IsXlsbFile));
                OnPropertyChanged(nameof(IsOdsFile));
                OnPropertyChanged(nameof(IsPptxFile));
                OnPropertyChanged(nameof(IsPptFile));
                OnPropertyChanged(nameof(IsOdpFile));
                OnPropertyChanged(nameof(IsVideoFile));
                OnPropertyChanged(nameof(IsAudioFile));
                OnPropertyChanged(nameof(IsZipFile));
                OnPropertyChanged(nameof(IsHtmlFile));
                OnPropertyChanged(nameof(IsAnyWordDoc));
                OnPropertyChanged(nameof(IsAnySpreadsheet));
                OnPropertyChanged(nameof(IsAnyPresentation));
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

        public string DocxText
        {
            get => _docxText;
            set => SetProperty(ref _docxText, value);
        }

        #endregion Properties — Current file & text

        #region Properties — Spreadsheet

        public DataTable? XlsxData
        {
            get => _xlsxData;
            set => SetProperty(ref _xlsxData, value);
        }

        public ObservableCollection<string> XlsxSheetNames
        {
            get => _xlsxSheetNames;
            set => SetProperty(ref _xlsxSheetNames, value);
        }

        public string? SelectedSheetName
        {
            get => _selectedSheetName;
            set
            {
                if (_selectedSheetName == value)
                    return;
                SetProperty(ref _selectedSheetName, value);
                if (value != null && CurrentFile != null)
                    _ = LoadSpreadsheetSheetAsync(CurrentFile.FilePath, value);
            }
        }

        #endregion Properties — Spreadsheet

        #region Properties — Presentation

        public ObservableCollection<PptxSlideData> PptxSlides { get; } = new();

        public string PptxSlideInfo
        {
            get => _pptxSlideInfo;
            set => SetProperty(ref _pptxSlideInfo, value);
        }

        #endregion Properties — Presentation

        #region Properties — ZIP

        public ObservableCollection<ZipEntryInfo> ZipEntries { get; } = new();

        public string ZipEntryCount
        {
            get => _zipEntryCount;
            set => SetProperty(ref _zipEntryCount, value);
        }

        #endregion Properties — ZIP

        #region Properties — Matches

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
                OnPropertyChanged(nameof(HasMatches));
            }
        }

        public bool HasMatches => TotalMatches > 0;

        public string MatchDisplay => TotalMatches > 0
            ? $"{CurrentMatchIndex + 1}/{TotalMatches}"
            : "No matches";

        public ContentMatchDto? SelectedMatch
        {
            get => _selectedMatch;
            set => SetProperty(ref _selectedMatch, value);
        }

        public ObservableCollection<ContentMatchDto> ContentMatches { get; } = new();

        #endregion Properties — Matches

        #region Properties — Permissions & Commands

        public bool CanPrint => SessionContext.Instance.HasPermission("PrintFile");
        public bool CanExport => SessionContext.Instance.HasPermission("ExportFile");
        public bool CanCopy => SessionContext.Instance.HasPermission("CopyFile");

        public ICommand NextMatchCommand { get; }
        public ICommand PreviousMatchCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CopyPathCommand { get; }
        public ICommand SelectSheetCommand { get; }

        #endregion Properties — Permissions & Commands

        #region File-Type Flags

        public bool IsTextFile => CurrentFile != null && IsPlainText(CurrentFile.Extension);
        public bool IsRtfFile => Ext(".rtf");
        public bool IsPdfFile => Ext(".pdf");
        public bool IsImageFile => CurrentFile != null && IsImage(CurrentFile.Extension);
        public bool IsDocxFile => Ext(".docx");
        public bool IsDocFile => Ext(".doc");
        public bool IsDocmFile => Ext(".docm");
        public bool IsOdtFile => Ext(".odt");
        public bool IsXlsxFile => Ext(".xlsx") || Ext(".xlsm");
        public bool IsXlsFile => Ext(".xls");
        public bool IsXlsbFile => Ext(".xlsb");
        public bool IsOdsFile => Ext(".ods");
        public bool IsPptxFile => Ext(".pptx");
        public bool IsPptFile => Ext(".ppt");
        public bool IsOdpFile => Ext(".odp");
        public bool IsVideoFile => CurrentFile != null && IsVideo(CurrentFile.Extension);
        public bool IsAudioFile => CurrentFile != null && IsAudio(CurrentFile.Extension);
        public bool IsZipFile => CurrentFile != null && IsZip(CurrentFile.Extension);
        public bool IsHtmlFile => CurrentFile != null && IsHtml(CurrentFile.Extension);
        public bool IsAnyWordDoc => IsDocxFile || IsDocFile || IsOdtFile || IsDocmFile;
        public bool IsAnySpreadsheet => IsXlsxFile || IsXlsFile || IsXlsbFile || IsOdsFile;
        public bool IsAnyPresentation => IsPptxFile || IsPptFile || IsOdpFile;

        public bool IsUnsupportedFile =>
            CurrentFile != null &&
            !IsTextFile && !IsRtfFile && !IsPdfFile && !IsImageFile &&
            !IsAnyWordDoc && !IsAnySpreadsheet && !IsAnyPresentation &&
            !IsVideoFile && !IsAudioFile && !IsZipFile && !IsHtmlFile;

        private bool Ext(string ext) =>
            CurrentFile?.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase) == true;

        #endregion File-Type Flags

        #region Public Methods

        public async Task LoadFileAsync(FileResultDto file, string keyword)
        {
            FileContent = string.Empty;
            DocxText = string.Empty;
            XlsxData = null;
            XlsxSheetNames.Clear();
            _selectedSheetName = null;
            OnPropertyChanged(nameof(SelectedSheetName));
            PptxSlides.Clear();
            ZipEntries.Clear();
            PptxSlideInfo = string.Empty;
            ZipEntryCount = string.Empty;
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
                else if (IsAnyWordDoc)
                {
                    DocxText = await Task.Run(() => ExtractWordText(file.FilePath));
                }
                else if (IsAnySpreadsheet)
                {
                    var (names, table) = await Task.Run(() => LoadSpreadsheetData(file.FilePath));
                    foreach (var n in names)
                        XlsxSheetNames.Add(n);
                    XlsxData = table;
                    _selectedSheetName = names.FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedSheetName));
                }
                else if (IsAnyPresentation)
                {
                    var slides = await Task.Run(() => ExtractPresentationSlides(file.FilePath));
                    foreach (var s in slides)
                        PptxSlides.Add(s);
                    PptxSlideInfo = $"{PptxSlides.Count} slide(s)";
                }
                else if (IsZipFile)
                {
                    var entries = await Task.Run(() => ReadZipEntries(file.FilePath));
                    foreach (var z in entries)
                        ZipEntries.Add(z);
                    ZipEntryCount = $"{ZipEntries.Count} file(s)";
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    if (IsTextFile || IsAnyWordDoc || IsAnyPresentation || IsAnySpreadsheet)
                    {
                        var matches = await _searchService
                            .GetContentMatchesAsync(file.FilePath, keyword);
                        foreach (var m in matches)
                            ContentMatches.Add(m);
                        TotalMatches = ContentMatches.Count;
                        if (TotalMatches > 0)
                        {
                            CurrentMatchIndex = 0;
                            SelectedMatch = ContentMatches[0];
                        }
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
                System.Diagnostics.Debug.WriteLine(
                    $"[FilePreviewViewModel] LoadFileAsync error: {ex}");
            }
            finally
            {
                IsBusy = false;
                FileLoaded?.Invoke();
            }
        }

        public void OnTextRendered()
        {
            if (TotalMatches > 0 && ContentMatches.Count > 0)
                ScrollToLineRequested?.Invoke(ContentMatches[0].LineNumber);
        }

        #endregion Public Methods

        #region File-Type Static Helpers

        private static bool IsPlainText(string ext) =>
            ext.ToLower() is ".txt" or ".log" or ".csv" or ".xml" or ".bat" or ".sh"
                          or ".ps1" or ".dot";

        private static bool IsImage(string ext) =>
            ext.ToLower() is ".png" or ".jpg" or ".jpeg" or ".bmp" or
                             ".gif" or ".webp" or ".tiff" or ".tif" or ".ico";

        private static bool IsVideo(string ext) =>
            ext.ToLower() is ".mp4" or ".avi" or ".wmv" or ".mkv" or ".mov"
                          or ".flv" or ".webm" or ".mpg" or ".mpeg";

        private static bool IsAudio(string ext) =>
            ext.ToLower() is ".mp3" or ".wav" or ".wma" or ".aac" or ".flac" or ".ogg" or ".m4a";

        private static bool IsZip(string ext) => ext.ToLower() == ".zip";

        private static bool IsHtml(string ext) => ext.ToLower() is ".html" or ".htm";

        #endregion File-Type Static Helpers

        #region Word Extraction — DOCX / DOC / ODT

        private static string ExtractWordText(string filePath) =>
            Path.GetExtension(filePath).ToLower() switch
            {
                ".docx" or ".docm" => ExtractDocxText(filePath),
                ".doc" => ExtractDocText(filePath),
                ".odt" => ExtractOdtText(filePath),
                _ => string.Empty
            };

        private static string ExtractDocxText(string filePath)
        {
            using var docx = WordprocessingDocument.Open(filePath, isEditable: false);
            var body = docx.MainDocumentPart?.Document?.Body;
            if (body == null)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var element in body.Elements())
            {
                if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph p)
                    sb.AppendLine(p.InnerText);
                else if (element is DocumentFormat.OpenXml.Wordprocessing.Table t)
                    foreach (var row in t.Elements<TableRow>())
                    {
                        var cells = row.Elements<TableCell>().Select(c => c.InnerText.Trim());
                        sb.AppendLine(string.Join("\t", cells));
                    }
            }
            var text = sb.ToString().Trim();
            return string.IsNullOrWhiteSpace(text)
                ? "[No readable text found in .docm file. Convert to .docx for full support.]"
                : $"[Preview limited — .docm format has partial support.]\n\n{text}";
        }

        private static string ExtractDocText(string filePath)
        {
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                var sb = new StringBuilder();
                int run = 0;
                for (int i = 0; i < bytes.Length; i++)
                {
                    byte b = bytes[i];
                    if (b >= 0x20 && b < 0x7F)
                    {
                        sb.Append((char) b);
                        run++;
                    }
                    else
                    {
                        if (run < 4)
                        {
                            if (sb.Length >= run)
                                sb.Remove(sb.Length - run, run);
                        }
                        else
                        {
                            sb.Append(' ');
                        }
                        run = 0;
                    }
                }
                var text = sb.ToString().Trim();
                return string.IsNullOrWhiteSpace(text)
                    ? "[No readable text found in .doc file. Convert to .docx for full support.]"
                    : $"[Preview limited — .doc format has partial support.]\n\n{text}";
            }
            catch (Exception ex)
            {
                return $"[Could not read .doc file: {ex.Message}\n\nTip: Convert to .docx for full support.]";
            }
        }

        private static string ExtractOdtText(string filePath)
        {
            using var zip = ZipFile.OpenRead(filePath);
            var entry = zip.GetEntry("content.xml");
            if (entry == null)
                return string.Empty;

            using var stream = entry.Open();
            using var reader = new System.Xml.XmlTextReader(stream);
            var sb = new StringBuilder();
            while (reader.Read())
            {
                if (reader.NodeType == System.Xml.XmlNodeType.Text ||
                    reader.NodeType == System.Xml.XmlNodeType.CDATA)
                    sb.Append(reader.Value);
                else if (reader.NodeType == System.Xml.XmlNodeType.Element &&
                         reader.LocalName is "p" or "h" or "table-row")
                    sb.AppendLine();
            }
            return sb.ToString().Trim();
        }

        #endregion Word Extraction — DOCX / DOC / ODT

        #region Spreadsheet Loading — XLSX / XLSM / XLS / XLSB / ODS

        private static (List<string> names, DataTable table) LoadSpreadsheetData(string filePath) =>
            Path.GetExtension(filePath).ToLower() switch
            {
                ".xlsx" or ".xlsm" => LoadViaClosedXml(filePath),
                ".xls" => LoadViaHssf(filePath),
                ".xlsb" => LoadViaExcelDataReader(filePath),
                ".ods" => LoadViaOds(filePath),
                _ => (new List<string>(), new DataTable())
            };

        private static (List<string>, DataTable) LoadViaClosedXml(string filePath)
        {
            using var wb = new XLWorkbook(filePath);
            var names = wb.Worksheets.Select(ws => ws.Name).ToList();
            var table = names.Count > 0
                ? BuildTableClosedXml(wb.Worksheet(names[0]))
                : new DataTable();
            return (names, table);
        }

        private static (List<string>, DataTable) LoadViaOds(string filePath)
        {
            var result = new List<string>();
            var table = new DataTable();
            try
            {
                using var zip = ZipFile.OpenRead(filePath);
                var entry = zip.GetEntry("content.xml");
                if (entry == null)
                    return (result, table);

                using var stream = entry.Open();
                var xdoc = System.Xml.Linq.XDocument.Load(stream);
                const string tableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
                const string textNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

                var sheets = xdoc.Descendants(System.Xml.Linq.XName.Get("table", tableNs)).ToList();
                if (sheets.Count == 0)
                    return (result, table);

                foreach (var sheet in sheets)
                    result.Add(sheet.Attribute(
                        System.Xml.Linq.XName.Get("name", tableNs))?.Value
                        ?? $"Sheet{result.Count + 1}");

                table = BuildTableFromOdsSheet(sheets[0], tableNs, textNs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ODS] Load error: {ex.Message}");
            }
            return (result, table);
        }

        private static DataTable BuildTableFromOdsSheet(
            System.Xml.Linq.XElement sheet, string tableNs, string textNs)
        {
            var dt = new DataTable();
            var rows = sheet.Elements(System.Xml.Linq.XName.Get("table-row", tableNs)).ToList();
            if (rows.Count == 0)
                return dt;

            static List<string> ExpandRow(System.Xml.Linq.XElement row, string tNs, string txNs)
            {
                var cells = new List<string>();
                foreach (var cell in row.Elements(System.Xml.Linq.XName.Get("table-cell", tNs)))
                {
                    int repeat = int.TryParse(
                        cell.Attribute(System.Xml.Linq.XName.Get("number-columns-repeated", tNs))?.Value,
                        out int r) ? r : 1;
                    var text = cell.Descendants(System.Xml.Linq.XName.Get("p", txNs))
                        .FirstOrDefault()?.Value ?? string.Empty;
                    for (int i = 0; i < repeat; i++)
                        cells.Add(text);
                }
                while (cells.Count > 0 && string.IsNullOrEmpty(cells[^1]))
                    cells.RemoveAt(cells.Count - 1);
                return cells;
            }

            var headers = ExpandRow(rows[0], tableNs, textNs);
            var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in headers)
            {
                var col = string.IsNullOrWhiteSpace(h) ? $"Column {dt.Columns.Count + 1}" : h;
                if (used.TryGetValue(col, out int cnt))
                { used[col] = cnt + 1; col = $"{col} ({cnt + 1})"; }
                else
                    used[col] = 1;
                dt.Columns.Add(col);
            }
            foreach (var row in rows.Skip(1))
            {
                var cells = ExpandRow(row, tableNs, textNs);
                if (cells.All(string.IsNullOrEmpty))
                    continue;
                var dr = dt.NewRow();
                for (int i = 0; i < Math.Min(cells.Count, dt.Columns.Count); i++)
                    dr[i] = cells[i];
                dt.Rows.Add(dr);
            }
            return dt;
        }

        private static (List<string>, DataTable) LoadViaHssf(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            IWorkbook wb = new HSSFWorkbook(fs);
            var names = Enumerable.Range(0, wb.NumberOfSheets).Select(i => wb.GetSheetName(i)).ToList();
            var table = names.Count > 0 ? BuildTableNpoi(wb.GetSheetAt(0)) : new DataTable();
            return (names, table);
        }

        private static (List<string>, DataTable) LoadViaExcelDataReader(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(fs);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });
            var names = ds.Tables.Cast<DataTable>().Select(t => t.TableName).ToList();
            var table = ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
            return (names, table);
        }

        private static DataTable BuildTableClosedXml(IXLWorksheet ws)
        {
            var table = new DataTable();
            var range = ws.RangeUsed();
            if (range == null)
                return table;
            var rows = range.RowsUsed().ToList();
            if (rows.Count == 0)
                return table;

            var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in rows[0].Cells())
            {
                var raw = cell.GetString().Trim();
                if (string.IsNullOrWhiteSpace(raw))
                    raw = $"Column {cell.Address.ColumnNumber}";
                if (used.TryGetValue(raw, out int cnt))
                { used[raw] = cnt + 1; raw = $"{raw} ({cnt + 1})"; }
                else
                    used[raw] = 1;
                table.Columns.Add(raw);
            }
            foreach (var row in rows.Skip(1))
            {
                var dr = table.NewRow();
                var cells = row.Cells().ToList();
                for (int i = 0; i < Math.Min(cells.Count, table.Columns.Count); i++)
                    dr[i] = cells[i].GetString();
                table.Rows.Add(dr);
            }
            return table;
        }

        private static DataTable BuildTableNpoi(NPOI.SS.UserModel.ISheet? sheet)
        {
            var table = new DataTable();
            if (sheet == null)
                return table;
            var headerRow = sheet.GetRow(sheet.FirstRowNum);
            if (headerRow == null)
                return table;

            var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.Cells)
            {
                var raw = cell.ToString()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(raw))
                    raw = $"Column {cell.ColumnIndex + 1}";
                if (used.TryGetValue(raw, out int cnt))
                { used[raw] = cnt + 1; raw = $"{raw} ({cnt + 1})"; }
                else
                    used[raw] = 1;
                table.Columns.Add(raw);
            }
            for (int r = sheet.FirstRowNum + 1; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null)
                    continue;
                var dr = table.NewRow();
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    var cell = row.GetCell(c);
                    dr[c] = cell == null ? string.Empty : NpoiCellValue(cell);
                }
                table.Rows.Add(dr);
            }
            return table;
        }

        private static string NpoiCellValue(NPOI.SS.UserModel.ICell cell)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return cell.CellType switch
            {
                NPOI.SS.UserModel.CellType.Numeric =>
                    DateUtil.IsCellDateFormatted(cell)
                        ? string.Format(inv, "{0:yyyy-MM-dd}", cell.DateCellValue)
                        : Convert.ToString(cell.NumericCellValue, inv),
                NPOI.SS.UserModel.CellType.Boolean => cell.BooleanCellValue.ToString(),
                NPOI.SS.UserModel.CellType.Formula => cell.CachedFormulaResultType switch
                {
                    NPOI.SS.UserModel.CellType.Numeric => Convert.ToString(cell.NumericCellValue, inv),
                    NPOI.SS.UserModel.CellType.String => cell.StringCellValue,
                    _ => cell.ToString() ?? string.Empty
                },
                _ => cell.ToString() ?? string.Empty
            };
        }

        private async Task LoadSpreadsheetSheetAsync(string filePath, string sheetName)
        {
            try
            {
                IsBusy = true;
                StatusMessage = $"Loading sheet: {sheetName}...";
                var ext = Path.GetExtension(filePath).ToLower();

                DataTable table = ext switch
                {
                    ".xlsx" or ".xlsm" => await Task.Run(() =>
                    {
                        using var wb = new XLWorkbook(filePath);
                        return BuildTableClosedXml(wb.Worksheet(sheetName));
                    }),
                    ".xls" => await Task.Run(() =>
                    {
                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                        IWorkbook wb = new HSSFWorkbook(fs);
                        return BuildTableNpoi(wb.GetSheet(sheetName));
                    }),
                    ".ods" => await Task.Run(() =>
                    {
                        using var zip = ZipFile.OpenRead(filePath);
                        var entry = zip.GetEntry("content.xml")!;
                        using var stream = entry.Open();
                        var xdoc = System.Xml.Linq.XDocument.Load(stream);
                        const string tableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
                        const string textNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
                        var sheet = xdoc
                            .Descendants(System.Xml.Linq.XName.Get("table", tableNs))
                            .FirstOrDefault(s => s.Attribute(
                                System.Xml.Linq.XName.Get("name", tableNs))?.Value == sheetName);
                        return sheet != null
                            ? BuildTableFromOdsSheet(sheet, tableNs, textNs)
                            : new DataTable();
                    }),
                    _ => await Task.Run(() =>
                    {
                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                        using var r = ExcelReaderFactory.CreateReader(fs);
                        var ds = r.AsDataSet(new ExcelDataSetConfiguration
                        {
                            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                        });
                        return ds.Tables[sheetName] ?? new DataTable();
                    })
                };

                XlsxData = table;
                StatusMessage = $"Sheet '{sheetName}' loaded.";
                FileLoaded?.Invoke();
            }
            catch (Exception ex) { StatusMessage = $"Error loading sheet: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        #endregion Spreadsheet Loading — XLSX / XLSM / XLS / XLSB / ODS

        #region Presentation Extraction — PPTX / PPT / ODP

        private static List<PptxSlideData> ExtractPresentationSlides(string filePath) =>
            Path.GetExtension(filePath).ToLower() switch
            {
                ".pptx" => ExtractPptxSlides(filePath),
                ".ppt" => ExtractPptSlides(filePath),
                ".odp" => ExtractOdpSlides(filePath),
                _ => new List<PptxSlideData>
                {
                    new() { Header = "Unsupported", Content = "Cannot preview this presentation format." }
                }
            };

        private static List<PptxSlideData> ExtractPptxSlides(string filePath)
        {
            var result = new List<PptxSlideData>();
            if (!File.Exists(filePath))
                return new List<PptxSlideData> { new() { Header = "Error", Content = "File not found." } };

            try
            {
                using var pres = PresentationDocument.Open(filePath, false);
                var presPart = pres.PresentationPart;
                if (presPart == null)
                    return result;

                int index = 1;
                foreach (var slidePart in presPart.SlideParts)
                {
                    var sb = new StringBuilder();
                    foreach (var para in slidePart.Slide
                        .Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                    {
                        var line = para.InnerText.Trim();
                        if (!string.IsNullOrWhiteSpace(line))
                            sb.AppendLine(line);
                    }
                    result.Add(new PptxSlideData
                    {
                        Header = $"Slide {index}",
                        Content = sb.Length > 0 ? sb.ToString().Trim() : "(No text content)"
                    });
                    index++;
                }
            }
            catch (OpenXmlPackageException)
            {
                result.Add(new PptxSlideData { Header = "Error", Content = "Invalid or corrupted PPTX file." });
            }
            catch (Exception ex)
            {
                result.Add(new PptxSlideData { Header = "Error", Content = ex.Message });
            }
            return result;
        }

        private static List<PptxSlideData> ExtractPptSlides(string filePath)
        {
            var result = new List<PptxSlideData>();
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                var sb = new StringBuilder();
                int run = 0;
                for (int i = 0; i < bytes.Length; i++)
                {
                    byte b = bytes[i];
                    if (b >= 0x20 && b < 0x7F)
                    { sb.Append((char) b); run++; }
                    else
                    {
                        if (run < 4)
                        { if (sb.Length >= run) sb.Remove(sb.Length - run, run); }
                        else
                            sb.Append(' ');
                        run = 0;
                    }
                }
                var text = sb.ToString().Trim();
                result.Add(new PptxSlideData
                {
                    Header = "Slide Content (limited preview)",
                    Content = string.IsNullOrWhiteSpace(text)
                        ? "[No readable text found. Convert to .pptx for full support.]"
                        : $"[Preview limited — .ppt format has partial support.]\n\n{text}"
                });
            }
            catch (Exception ex)
            {
                result.Add(new PptxSlideData
                {
                    Header = "Could not read .ppt",
                    Content = $"{ex.Message}\n\nTip: Convert to .pptx for full support."
                });
            }
            return result;
        }

        private static List<PptxSlideData> ExtractOdpSlides(string filePath)
        {
            var result = new List<PptxSlideData>();
            try
            {
                using var zip = ZipFile.OpenRead(filePath);
                var entry = zip.GetEntry("content.xml");
                if (entry == null)
                {
                    result.Add(new PptxSlideData { Header = "Error", Content = "Could not read ODP content." });
                    return result;
                }
                using var stream = entry.Open();
                var xdoc = System.Xml.Linq.XDocument.Load(stream);
                const string drawNs = "urn:oasis:names:tc:opendocument:xmlns:drawing:1.0";
                const string textNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

                var slides = xdoc.Descendants(System.Xml.Linq.XName.Get("page", drawNs)).ToList();
                for (int i = 0; i < slides.Count; i++)
                {
                    var texts = slides[i]
                        .Descendants(System.Xml.Linq.XName.Get("p", textNs))
                        .Select(p => p.Value.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t));
                    result.Add(new PptxSlideData
                    {
                        Header = $"Slide {i + 1}",
                        Content = string.Join(Environment.NewLine, texts) is { Length: > 0 } c
                            ? c : "(No text content)"
                    });
                }
            }
            catch (Exception ex)
            {
                result.Add(new PptxSlideData { Header = "Error", Content = ex.Message });
            }
            return result;
        }

        #endregion Presentation Extraction — PPTX / PPT / ODP

        #region ZIP Reading

        private static List<ZipEntryInfo> ReadZipEntries(string filePath)
        {
            var result = new List<ZipEntryInfo>();
            using var zip = ZipFile.OpenRead(filePath);
            foreach (var entry in zip.Entries.OrderBy(e => e.FullName))
                result.Add(new ZipEntryInfo
                {
                    Name = entry.FullName,
                    SizeDisplay = FormatBytes(entry.Length),
                    CompressedDisplay = FormatBytes(entry.CompressedLength),
                    LastModified = entry.LastWriteTime.DateTime
                });
            return result;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1_048_576)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / 1_048_576.0:F1} MB";
        }

        #endregion ZIP Reading

        #region Commands

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
            var ext = Path.GetExtension(CurrentFile.FileName)?.ToLower() ?? string.Empty;
            var filter = ext switch
            {
                ".pdf" => "PDF Files (*.pdf)|*.pdf",
                ".docx" => "Word Documents (*.docx)|*.docx",
                ".doc" => "Word 97-2003 (*.doc)|*.doc",
                ".odt" => "OpenDocument Text (*.odt)|*.odt",
                ".docm" => "Word Macro-Enabled (*.docm)|*.docm",
                ".xlsx" => "Excel Workbook (*.xlsx)|*.xlsx",
                ".xlsm" => "Excel Macro-Enabled (*.xlsm)|*.xlsm",
                ".xls" => "Excel 97-2003 (*.xls)|*.xls",
                ".xlsb" => "Excel Binary (*.xlsb)|*.xlsb",
                ".ods" => "OpenDocument Spreadsheet (*.ods)|*.ods",
                ".pptx" => "PowerPoint (*.pptx)|*.pptx",
                ".ppt" => "PowerPoint 97-2003 (*.ppt)|*.ppt",
                ".odp" => "OpenDocument Presentation (*.odp)|*.odp",
                ".rtf" => "Rich Text Format (*.rtf)|*.rtf",
                ".txt" => "Text Files (*.txt)|*.txt",
                ".png" => "PNG Images (*.png)|*.png",
                ".jpg" or ".jpeg" => "JPEG Images (*.jpg)|*.jpg",
                ".mp4" => "MP4 Video (*.mp4)|*.mp4",
                ".mp3" => "MP3 Audio (*.mp3)|*.mp3",
                ".zip" => "ZIP Archives (*.zip)|*.zip",
                _ => "All Files (*.*)|*.*"
            };
            filter += "|All Files (*.*)|*.*";

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Path.GetFileNameWithoutExtension(CurrentFile.FileName),
                DefaultExt = ext,
                Filter = filter
            };
            if (dlg.ShowDialog() != true)
                return;

            var chosen = Path.GetExtension(dlg.FileName)?.ToLower();
            if (!string.IsNullOrEmpty(chosen) && chosen != ext)
            {
                var r = System.Windows.MessageBox.Show(
                    $"Original is '{ext}', saving as '{chosen}'.\nFile will be copied as-is. Continue?",
                    "Extension Mismatch",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (r != System.Windows.MessageBoxResult.Yes)
                    return;
            }

            File.Copy(CurrentFile.FilePath, dlg.FileName, overwrite: true);

            var userId = SessionContext.Instance.CurrentUser?.Id;
            var username = SessionContext.Instance.CurrentUser?.Username ?? string.Empty;
            await _accessLogService.LogAsync(userId, username, ActionType.ExportFile,
                CurrentFile.FilePath, details: $"Exported to: {dlg.FileName}");

            StatusMessage = "File exported successfully.";
        }

        private void ExecuteCopyPath(object? _)
        {
            if (CurrentFile == null)
                return;
            System.Windows.Clipboard.SetText(CurrentFile.FilePath);
            StatusMessage = "Path copied to clipboard.";
        }

        private void ExecuteSelectSheet(object? param)
        {
            if (param is string sheetName && CurrentFile != null)
                SelectedSheetName = sheetName;
        }

        #endregion Commands
    }
}