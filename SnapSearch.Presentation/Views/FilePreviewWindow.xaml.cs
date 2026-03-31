using SnapSearch.Application.DTOs;
using SnapSearch.Presentation.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SnapSearch.Presentation.Views
{
    public partial class FilePreviewWindow : Window
    {
        #region Fields

        private readonly FilePreviewViewModel _vm;
        private readonly FileResultDto _pendingFile;
        private readonly string _pendingKeyword;

        #endregion Fields

        #region Constructor

        public FilePreviewWindow(FileResultDto file, string keyword)
        {
            InitializeComponent();

            _pendingFile = file;
            _pendingKeyword = keyword;

            // Get the ViewModel from the DI container
            _vm = App.GetService<FilePreviewViewModel>();
            DataContext = _vm;

            // Subscribe to events raised by the ViewModel
            _vm.ScrollToLineRequested += ScrollToLine;
            _vm.PrintRequested += OnPrintRequested;
            _vm.FileLoaded += OnFileLoaded;

            Loaded += OnWindowLoaded;
        }

        #endregion Constructor

        #region Window Lifecycle

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe so this only runs once
            Loaded -= OnWindowLoaded;

            // Let WPF finish its first layout pass before we start loading data
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);

            // Now load the file — this populates the ViewModel properties
            await _vm.LoadFileAsync(_pendingFile, _pendingKeyword);
        }

        #endregion Window Lifecycle

        #region FileLoaded → RenderContent

        // Called by the ViewModel when all data is ready.
        // We marshal to the UI thread at Render priority so the visual tree
        // is fully updated before we start writing into RichTextBoxes etc.
        private void OnFileLoaded()
        {
            Dispatcher.InvokeAsync(RenderContentAsync, DispatcherPriority.Render);
        }

        private async void RenderContentAsync()
        {
            if (_vm.CurrentFile == null)
                return;

            System.Diagnostics.Debug.WriteLine(
                $"[Preview] RenderContent — " +
                $"IsText={_vm.IsTextFile} IsPdf={_vm.IsPdfFile} " +
                $"IsImage={_vm.IsImageFile} IsDocx={_vm.IsDocxFile} " +
                $"IsXlsx={_vm.IsXlsxFile} IsUnsupported={_vm.IsUnsupportedFile}");

            // ── PDF ──────────────────────────────────────────────────────────
            if (_vm.IsPdfFile)
            {
                // PdfViewerControl wraps a WinForms control, so we load it
                // at Background priority to avoid blocking the UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    try { PdfViewer.LoadPdf(_vm.CurrentFile.FilePath); }
                    catch (Exception ex) { _vm.StatusMessage = $"PDF error: {ex.Message}"; }
                }, DispatcherPriority.Background);
                return;
            }

            // ── Image ────────────────────────────────────────────────────────
            if (_vm.IsImageFile)
            {
                LoadImage(_vm.CurrentFile.FilePath);
                return;
            }

            // ── Text / code ──────────────────────────────────────────────────
            if (_vm.IsTextFile)
            {
                if (string.IsNullOrEmpty(_vm.FileContent))
                {
                    _vm.StatusMessage = "File is empty.";
                    return;
                }
                RenderHighlightedContent();
                _vm.OnTextRendered();
                return;
            }

            // ── DOCX ─────────────────────────────────────────────────────────
            if (_vm.IsDocxFile)
            {
                if (string.IsNullOrEmpty(_vm.DocxText))
                {
                    _vm.StatusMessage = "Document is empty.";
                    return;
                }
                RenderDocxContent();
                return;
            }

            // ── XLSX ─────────────────────────────────────────────────────────
            // XlsxData is guaranteed non-null here because the ViewModel sets it
            // on the UI thread BEFORE firing the FileLoaded event.
            if (_vm.IsXlsxFile)
            {
                if (_vm.XlsxData == null)
                {
                    _vm.StatusMessage = "Spreadsheet data could not be loaded.";
                    return;
                }

                // Clear columns first so AutoGeneratingColumn fires fresh on every load.
                // Without this, switching files can show stale columns.
                XlsxDataGrid.ItemsSource = null;
                XlsxDataGrid.Columns.Clear();
                XlsxDataGrid.ItemsSource = _vm.XlsxData.DefaultView;
                return;
            }

            // ── Unsupported ──────────────────────────────────────────────────
            // The XAML Visibility binding on the "Unsupported" StackPanel handles
            // showing the "Open with Default App" button automatically.
        }

        #endregion FileLoaded → RenderContent

        #region Rendering Helpers

        /// <summary>
        /// Loads an image file from disk and displays it in the PreviewImage control.
        /// We load via a FileStream so the file is not locked after loading.
        /// </summary>
        private void LoadImage(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // load fully into memory
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // makes the bitmap thread-safe and faster to render
                PreviewImage.Source = bitmap;
            }
            catch (Exception ex) { _vm.StatusMessage = $"Could not load image: {ex.Message}"; }
        }

        /// <summary>
        /// Writes the plain-text file content into the ContentRichTextBox,
        /// highlighting every occurrence of the search keyword in blue.
        /// </summary>
        private void RenderHighlightedContent()
        {
            var doc = ContentRichTextBox.Document;
            doc.Blocks.Clear();
            doc.PageWidth = 10000; // very wide so lines don't wrap

            var keyword = _vm.Keyword;
            var lines = _vm.FileContent.Split('\n');

            foreach (var line in lines)
            {
                var para = new Paragraph
                {
                    Margin = new Thickness(0),
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Left,
                    KeepTogether = true
                };

                AppendHighlightedLine(para, line, keyword);
                doc.Blocks.Add(para);
            }
        }

        /// <summary>
        /// Writes the extracted DOCX text into the DocxRichTextBox,
        /// highlighting every occurrence of the search keyword in blue.
        /// </summary>
        private void RenderDocxContent()
        {
            var doc = DocxRichTextBox.Document;
            doc.Blocks.Clear();

            var keyword = _vm.Keyword;
            var lines = _vm.DocxText.Split('\n');

            foreach (var line in lines)
            {
                var para = new Paragraph
                {
                    Margin = new Thickness(0, 0, 0, 4),
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Left,
                    LineHeight = 20
                };

                AppendHighlightedLine(para, line, keyword);
                doc.Blocks.Add(para);
            }
        }

        /// <summary>
        /// Adds a single line of text to a Paragraph, wrapping every match of
        /// the keyword in a blue highlighted Run so it stands out visually.
        /// </summary>
        private static void AppendHighlightedLine(Paragraph para, string line, string keyword)
        {
            // No keyword — just add the plain text
            if (string.IsNullOrWhiteSpace(keyword))
            {
                para.Inlines.Add(new Run(line));
                return;
            }

            int last = 0, idx;

            // Walk through every occurrence of the keyword (case-insensitive)
            while ((idx = line.IndexOf(keyword, last, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                // Add the non-highlighted text that comes BEFORE this match
                if (idx > last)
                    para.Inlines.Add(new Run(line[last..idx]));

                // Add the matched keyword with a blue background
                para.Inlines.Add(new Run(line.Substring(idx, keyword.Length))
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(79, 142, 247)),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Bold
                });

                last = idx + keyword.Length;
            }

            // Add any remaining text after the last match
            if (last < line.Length)
                para.Inlines.Add(new Run(line[last..]));
        }

        /// <summary>
        /// Scrolls the viewer to make the specified line number visible.
        /// Used when the user clicks a match in the right-hand matches panel.
        /// </summary>
        private void ScrollToLine(int lineNumber)
        {
            if (_vm.IsTextFile)
            {
                var blocks = ContentRichTextBox.Document.Blocks.ToList();
                if (lineNumber > 0 && lineNumber - 1 < blocks.Count)
                    blocks[lineNumber - 1].BringIntoView();
            }
            else if (_vm.IsDocxFile)
            {
                var blocks = DocxRichTextBox.Document.Blocks.ToList();
                if (lineNumber > 0 && lineNumber - 1 < blocks.Count)
                    blocks[lineNumber - 1].BringIntoView();
            }
        }

        /// <summary>
        /// Called automatically by the DataGrid for each column it generates
        /// from the DataTable.  We control column width and cell padding here.
        ///
        /// Strategy:
        ///   - Measure each column to fit its content (SizeToCells).
        ///   - After ALL columns are generated and the grid has done its first
        ///     layout pass, decide whether to stretch columns to fill the grid
        ///     or keep them content-sized with a scrollbar (see OnXlsxDataGridLoaded).
        /// </summary>
        private void XlsxDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column is not DataGridTextColumn col) return;

            // Size to content for now — we may switch to Star later
            col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);

            // Style the text inside each cell
            col.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.PaddingProperty,           new Thickness(8, 4, 8, 4)),
                    new Setter(TextBlock.VerticalAlignmentProperty,  VerticalAlignment.Center),
                    new Setter(TextBlock.TextTrimmingProperty,       TextTrimming.CharacterEllipsis)
                }
            };

            // Hook the Loaded event so we can check total column width vs available
            // space AFTER the grid has finished its first layout pass.
            // We unsubscribe first to avoid multiple subscriptions when switching sheets.
            XlsxDataGrid.Loaded -= OnXlsxDataGridLoaded;
            XlsxDataGrid.Loaded += OnXlsxDataGridLoaded;
        }

        /// <summary>
        /// Fired once after the DataGrid completes its first layout pass.
        /// Decides whether columns should stretch (Star) or stay content-sized.
        /// </summary>
        private void OnXlsxDataGridLoaded(object sender, RoutedEventArgs e)
        {
            // Only run once per data-load
            XlsxDataGrid.Loaded -= OnXlsxDataGridLoaded;

            if (XlsxDataGrid.Columns.Count == 0) return;

            // Total width all columns currently occupy
            double totalColumnWidth = XlsxDataGrid.Columns.Sum(c => c.ActualWidth);

            // Space available inside the grid (subtract scrollbar and row-header widths)
            double available = XlsxDataGrid.ActualWidth
                               - SystemParameters.VerticalScrollBarWidth
                               - XlsxDataGrid.RowHeaderActualWidth;

            if (totalColumnWidth < available)
            {
                // All columns fit — stretch them proportionally so they fill the grid.
                // We use each column's current content width as its Star weight so
                // wider-content columns get proportionally more space.
                foreach (var col in XlsxDataGrid.Columns)
                {
                    double weight = col.ActualWidth > 0 ? col.ActualWidth : 1;
                    col.Width = new DataGridLength(weight, DataGridLengthUnitType.Star);
                }
            }
            else
            {
                // Columns are wider than the panel — keep content size and let the
                // horizontal scrollbar handle overflow.
                foreach (var col in XlsxDataGrid.Columns)
                    col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            }
        }

        #endregion Rendering Helpers

        #region UI Event Handlers

        /// <summary>
        /// Handles the Print button for ALL file types in one place.
        ///
        /// PDF  → renders every page to a bitmap via PdfiumViewer and sends
        ///         each one to the printer using PrintVisual.
        /// DOCX → uses the FlowDocument paginator (built-in WPF print support).
        /// Text → same as DOCX but uses the ContentRichTextBox document.
        /// </summary>
        private void OnPrintRequested()
        {
            // Show the standard Windows print dialog
            var pd = new System.Windows.Controls.PrintDialog();
            if (pd.ShowDialog() != true) return; // user cancelled

            var fileName = _vm.CurrentFile?.FileName ?? "SnapSearch Print";

            try
            {
                if (_vm.IsPdfFile)
                {
                    // ── PDF printing ─────────────────────────────────────────
                    // PdfViewerControl.PrintAllPages renders each page as a
                    // high-DPI bitmap and calls pd.PrintVisual() for each one.
                    _vm.StatusMessage = "Printing PDF...";
                    PdfViewer.PrintAllPages(pd, fileName);
                    _vm.StatusMessage = "PDF sent to printer.";
                }
                else if (_vm.IsDocxFile)
                {
                    // ── DOCX printing ────────────────────────────────────────
                    // WPF FlowDocument has a built-in paginator that handles
                    // page breaks, margins etc. automatically.
                    pd.PrintDocument(
                        ((IDocumentPaginatorSource)DocxRichTextBox.Document).DocumentPaginator,
                        fileName);
                }
                else
                {
                    // ── Text / code file printing ────────────────────────────
                    // Same approach as DOCX — use the ContentRichTextBox's
                    // FlowDocument paginator.
                    pd.PrintDocument(
                        ((IDocumentPaginatorSource)ContentRichTextBox.Document).DocumentPaginator,
                        fileName);
                }
            }
            catch (Exception ex)
            {
                _vm.StatusMessage = $"Print error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[FilePreviewWindow] Print error: {ex}");
            }
        }

        private void OpenWithDefaultApp_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.CurrentFile == null) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _vm.CurrentFile.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { _vm.StatusMessage = $"Could not open file: {ex.Message}"; }
        }

        private void MatchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm.SelectedMatch != null)
                ScrollToLine(_vm.SelectedMatch.LineNumber);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        #endregion UI Event Handlers
    }
}