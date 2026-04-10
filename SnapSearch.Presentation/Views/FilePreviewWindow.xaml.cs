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

// Explicitly alias to resolve the DataFormats ambiguity between
// System.Windows.DataFormats and System.Windows.Forms.DataFormats
using WpfDataFormats = System.Windows.DataFormats;

namespace SnapSearch.Presentation.Views
{
    public partial class FilePreviewWindow : Window
    {
        #region Fields

        private readonly FilePreviewViewModel _vm;
        private readonly FileResultDto _pendingFile;
        private readonly string _pendingKeyword;

        private MediaElement? _activeMedia;
        private bool _isPlaying;

        #endregion Fields

        #region Constructor

        public FilePreviewWindow(FileResultDto file, string keyword)
        {
            InitializeComponent();

            _pendingFile = file;
            _pendingKeyword = keyword;

            _vm = App.GetService<FilePreviewViewModel>();
            DataContext = _vm;

            _vm.ScrollToLineRequested += ScrollToLine;
            _vm.PrintRequested += OnPrintRequested;
            _vm.FileLoaded += OnFileLoaded;

            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        #endregion Constructor

        #region Window Lifecycle

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWindowLoaded;
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            await _vm.LoadFileAsync(_pendingFile, _pendingKeyword);
        }

        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _activeMedia?.Stop();
        }

        #endregion Window Lifecycle

        #region FileLoaded → RenderContent

        private void OnFileLoaded()
        {
            Dispatcher.InvokeAsync(RenderContentAsync, DispatcherPriority.ApplicationIdle);
        }

        private async void RenderContentAsync()
        {
            if (_vm.CurrentFile == null)
                return;

            // ── PDF ──────────────────────────────────────────────────────────
            if (_vm.IsPdfFile)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    { PdfViewer.LoadPdf(_vm.CurrentFile.FilePath); }
                    catch (Exception ex) { _vm.StatusMessage = $"PDF error: {ex.Message}"; }
                }, DispatcherPriority.Background);
                return;
            }

            // ── Image ────────────────────────────────────────────────────────
            if (_vm.IsImageFile)
            { LoadImage(_vm.CurrentFile.FilePath); return; }

            // ── RTF — WPF RichTextBox loads RTF natively ─────────────────────
            if (_vm.IsRtfFile)
            { LoadRtf(_vm.CurrentFile.FilePath); return; }

            // ── Plain text / code ────────────────────────────────────────────
            if (_vm.IsTextFile)
            {
                if (string.IsNullOrEmpty(_vm.FileContent))
                { _vm.StatusMessage = "File is empty."; return; }
                RenderHighlightedContent();
                _vm.OnTextRendered();
                return;
            }

            // ── Word family: DOCX, DOC, ODT ──────────────────────────────────
            if (_vm.IsAnyWordDoc)
            {
                if (string.IsNullOrEmpty(_vm.DocxText))
                { _vm.StatusMessage = "Document is empty or could not be read."; return; }
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
                RenderDocxContent();
                return;
            }

            // ── Spreadsheet family: XLSX, XLSM, XLS, XLSB, ODS ──────────────
            if (_vm.IsAnySpreadsheet)
            {
                if (_vm.XlsxData == null)
                { _vm.StatusMessage = "Spreadsheet could not be loaded."; return; }
                XlsxDataGrid.ItemsSource = null;
                XlsxDataGrid.Columns.Clear();
                XlsxDataGrid.ItemsSource = _vm.XlsxData.DefaultView;
                AdjustXlsxColumnWidthsAfterLayout();
                return;
            }

            // ── Presentation family: PPTX, PPT, ODP — bound via PptxSlides ──
            if (_vm.IsAnyPresentation)
                return;

            // ── Video ────────────────────────────────────────────────────────
            if (_vm.IsVideoFile)
            {
                _activeMedia = VideoPlayer;
                VideoPlayer.Source = new Uri(_vm.CurrentFile.FilePath);
                VideoPlayer.Volume = VolumeSlider.Value;
                VideoPlayer.Play();
                _isPlaying = true;
                PlayPauseButton.Content = "⏸ Pause";
                return;
            }

            // ── Audio ────────────────────────────────────────────────────────
            if (_vm.IsAudioFile)
            {
                _activeMedia = AudioPlayer;
                AudioPlayer.Source = new Uri(_vm.CurrentFile.FilePath);
                AudioPlayer.Volume = AudioVolumeSlider.Value;
                AudioPlayer.Play();
                _isPlaying = true;
                AudioPlayPauseButton.Content = "⏸ Pause";
                return;
            }

            // ── ZIP — entries already bound ──────────────────────────────────
            if (_vm.IsZipFile)
                return;

            // ── HTML ─────────────────────────────────────────────────────────
            if (_vm.IsHtmlFile)
            { HtmlBrowser.Navigate(new Uri(_vm.CurrentFile.FilePath)); return; }
        }

        #endregion FileLoaded → RenderContent

        #region Rendering Helpers

        private static void AppendHighlightedLine(
            Paragraph para, string line, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            { para.Inlines.Add(new Run(line)); return; }

            int last = 0;
            while (true)
            {
                int idx = line.IndexOf(keyword, last, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    break;

                if (idx > last)
                    para.Inlines.Add(new Run(line[last..idx]));

                para.Inlines.Add(new Run(line.Substring(idx, keyword.Length))
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(79, 142, 247)),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Bold
                });

                last = idx + keyword.Length;
            }

            if (last < line.Length)
                para.Inlines.Add(new Run(line[last..]));
        }

        private void LoadImage(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                PreviewImage.Source = bitmap;
            }
            catch (Exception ex) { _vm.StatusMessage = $"Could not load image: {ex.Message}"; }
        }

        /// <summary>
        /// Loads an RTF file into WPF's RichTextBox using the built-in RTF data format.
        /// Uses the WpfDataFormats alias to avoid ambiguity with System.Windows.Forms.DataFormats.
        /// </summary>
        private void LoadRtf(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var range = new TextRange(
                    RtfRichTextBox.Document.ContentStart,
                    RtfRichTextBox.Document.ContentEnd);
                // Use the alias — unambiguous reference to System.Windows.DataFormats.Rtf
                range.Load(fs, WpfDataFormats.Rtf);
            }
            catch (Exception ex) { _vm.StatusMessage = $"Could not load RTF: {ex.Message}"; }
        }

        private void RenderHighlightedContent()
        {
            var doc = ContentRichTextBox.Document;
            doc.Blocks.Clear();
            doc.PageWidth = 10000;

            foreach (var line in _vm.FileContent.Split('\n'))
            {
                var para = new Paragraph
                {
                    Margin = new Thickness(0),
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Left,
                    KeepTogether = true
                };
                AppendHighlightedLine(para, line, _vm.Keyword);
                doc.Blocks.Add(para);
            }
        }

        private void RenderDocxContent()
        {
            var doc = DocxRichTextBox.Document;
            doc.Blocks.Clear();
            doc.Foreground = System.Windows.Media.Brushes.Black;

            foreach (var line in _vm.DocxText.Split('\n'))
            {
                var para = new Paragraph
                {
                    Margin = new Thickness(0, 0, 0, 4),
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Left,
                    LineHeight = 20,
                    Foreground = System.Windows.Media.Brushes.Black
                };
                AppendHighlightedLine(para, line, _vm.Keyword);
                doc.Blocks.Add(para);
            }

            DocxRichTextBox.ScrollToHome();
        }

        private void ScrollToLine(int lineNumber)
        {
            BlockCollection? blocks = null;
            if (_vm.IsTextFile)
                blocks = ContentRichTextBox.Document.Blocks;
            if (_vm.IsAnyWordDoc)
                blocks = DocxRichTextBox.Document.Blocks;
            if (blocks == null)
                return;

            var list = blocks.ToList();
            if (lineNumber > 0 && lineNumber - 1 < list.Count)
                list[lineNumber - 1].BringIntoView();
        }

        private void XlsxDataGrid_AutoGeneratingColumn(
            object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column is not DataGridTextColumn col)
                return;
            col.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            col.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters =
                {
                    new Setter(TextBlock.PaddingProperty,
                               new Thickness(8, 4, 8, 4)),
                    new Setter(TextBlock.VerticalAlignmentProperty,
                               VerticalAlignment.Center),
                    new Setter(TextBlock.TextTrimmingProperty,
                               TextTrimming.CharacterEllipsis)
                }
            };
        }

        private void AdjustXlsxColumnWidthsAfterLayout()
        {
            EventHandler? handler = null;
            handler = (s, e) =>
            {
                XlsxDataGrid.LayoutUpdated -= handler;
                if (XlsxDataGrid.Columns.Count == 0)
                    return;
                double total = XlsxDataGrid.Columns.Sum(c => c.ActualWidth);
                double available = XlsxDataGrid.ActualWidth
                                   - SystemParameters.VerticalScrollBarWidth
                                   - XlsxDataGrid.RowHeaderActualWidth;
                if (total < available)
                    foreach (var col in XlsxDataGrid.Columns)
                        col.Width = new DataGridLength(
                            col.ActualWidth > 0 ? col.ActualWidth : 1,
                            DataGridLengthUnitType.Star);
                else
                    foreach (var col in XlsxDataGrid.Columns)
                        col.Width = new DataGridLength(1,
                            DataGridLengthUnitType.SizeToCells);
            };
            XlsxDataGrid.LayoutUpdated += handler;
        }

        #endregion Rendering Helpers

        #region Media Playback Handlers

        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e) =>
            _vm.StatusMessage = "Media loaded — playing.";

        private void MediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e) =>
            _vm.StatusMessage = $"Media error: {e.ErrorException?.Message}";

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_activeMedia == null)
                return;
            if (_isPlaying)
            {
                _activeMedia.Pause();
                _isPlaying = false;
                if (PlayPauseButton != null)
                    PlayPauseButton.Content = "▶ Play";
                if (AudioPlayPauseButton != null)
                    AudioPlayPauseButton.Content = "▶ Play";
            }
            else
            {
                _activeMedia.Play();
                _isPlaying = true;
                if (PlayPauseButton != null)
                    PlayPauseButton.Content = "⏸ Pause";
                if (AudioPlayPauseButton != null)
                    AudioPlayPauseButton.Content = "⏸ Pause";
            }
        }

        private void MediaStop_Click(object sender, RoutedEventArgs e)
        {
            _activeMedia?.Stop();
            _isPlaying = false;
            if (PlayPauseButton != null)
                PlayPauseButton.Content = "▶ Play";
            if (AudioPlayPauseButton != null)
                AudioPlayPauseButton.Content = "▶ Play";
        }

        private void MediaRestart_Click(object sender, RoutedEventArgs e)
        {
            if (_activeMedia == null)
                return;
            _activeMedia.Stop();
            _activeMedia.Play();
            _isPlaying = true;
            if (PlayPauseButton != null)
                PlayPauseButton.Content = "⏸ Pause";
            if (AudioPlayPauseButton != null)
                AudioPlayPauseButton.Content = "⏸ Pause";
        }

        private void VolumeSlider_ValueChanged(
            object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_activeMedia != null)
                _activeMedia.Volume = e.NewValue;
        }

        #endregion Media Playback Handlers

        #region Print

        private static FlowDocument BuildXlsxFlowDocument(
            System.Data.DataTable table, string fileName)
        {
            var doc = new FlowDocument
            {
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 11,
                PagePadding = new Thickness(40),
                ColumnWidth = double.PositiveInfinity
            };

            doc.Blocks.Add(new Paragraph(new Run(fileName))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var flowTable = new Table();
            doc.Blocks.Add(flowTable);

            for (int i = 0; i < table.Columns.Count; i++)
                flowTable.Columns.Add(new TableColumn
                { Width = new GridLength(1, GridUnitType.Star) });

            var hGroup = new TableRowGroup();
            flowTable.RowGroups.Add(hGroup);
            var hRow = new TableRow();
            hGroup.Rows.Add(hRow);
            foreach (System.Data.DataColumn col in table.Columns)
                hRow.Cells.Add(new TableCell(new Paragraph(new Run(col.ColumnName)))
                {
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(4, 2, 4, 2),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    BorderBrush = System.Windows.Media.Brushes.Black
                });

            var dGroup = new TableRowGroup();
            flowTable.RowGroups.Add(dGroup);
            foreach (System.Data.DataRow row in table.Rows)
            {
                var tr = new TableRow();
                dGroup.Rows.Add(tr);
                foreach (var cell in row.ItemArray)
                    tr.Cells.Add(new TableCell(
                        new Paragraph(new Run(cell?.ToString() ?? "")))
                    { Padding = new Thickness(5) });
            }

            return doc;
        }

        private void OnPrintRequested()
        {
            var pd = new System.Windows.Controls.PrintDialog();
            if (pd.ShowDialog() != true)
                return;

            var fileName = _vm.CurrentFile?.FileName ?? "SnapSearch Print";

            try
            {
                if (_vm.IsPdfFile)
                {
                    _vm.StatusMessage = "Printing PDF...";
                    PdfViewer.PrintAllPages(pd, fileName);
                    _vm.StatusMessage = "PDF sent to printer.";
                }
                else if (_vm.IsAnyWordDoc)
                {
                    pd.PrintDocument(
                        ((IDocumentPaginatorSource) DocxRichTextBox.Document).DocumentPaginator,
                        fileName);
                }
                else if (_vm.IsRtfFile)
                {
                    pd.PrintDocument(
                        ((IDocumentPaginatorSource) RtfRichTextBox.Document).DocumentPaginator,
                        fileName);
                }
                else if (_vm.IsAnySpreadsheet && _vm.XlsxData != null)
                {
                    var doc = BuildXlsxFlowDocument(_vm.XlsxData, fileName);
                    doc.PageWidth = pd.PrintableAreaWidth;
                    doc.PageHeight = pd.PrintableAreaHeight;
                    pd.PrintDocument(
                        ((IDocumentPaginatorSource) doc).DocumentPaginator, fileName);
                    _vm.StatusMessage = "Spreadsheet sent to printer.";
                }
                else
                {
                    pd.PrintDocument(
                        ((IDocumentPaginatorSource) ContentRichTextBox.Document).DocumentPaginator,
                        fileName);
                }
            }
            catch (Exception ex) { _vm.StatusMessage = $"Print error: {ex.Message}"; }
        }

        #endregion Print

        #region UI Event Handlers

        private void OpenWithDefaultApp_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.CurrentFile == null)
                return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _vm.CurrentFile.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { _vm.StatusMessage = $"Could not open: {ex.Message}"; }
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