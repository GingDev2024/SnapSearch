using SnapSearch.Application.DTOs;
using SnapSearch.Presentation.ViewModels;
using System.IO;
using System.Windows;
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

            _vm = App.GetService<FilePreviewViewModel>();
            DataContext = _vm;

            _vm.ScrollToLineRequested += ScrollToLine;
            _vm.PrintRequested += OnPrintRequested;
            _vm.FileLoaded += OnFileLoaded;

            // Wait for the full visual tree (including WindowsFormsHost) to be ready
            Loaded += OnWindowLoaded;
        }

        #endregion Constructor

        #region Window Lifecycle

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnWindowLoaded;

            // Yield at Loaded priority so every control — especially WindowsFormsHost —
            // has its Win32 handle created before we push content into it.
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);

            await _vm.LoadFileAsync(_pendingFile, _pendingKeyword);
        }

        #endregion Window Lifecycle

        #region FileLoaded → RenderContent

        private void OnFileLoaded()
        {
            // FileLoaded fires from an async Task — always marshal back to UI thread.
            Dispatcher.InvokeAsync(RenderContent, DispatcherPriority.Render);
        }

        private void RenderContent()
        {
            if (_vm.CurrentFile == null)
                return;

            System.Diagnostics.Debug.WriteLine(
                $"[Preview] RenderContent — " +
                $"IsText={_vm.IsTextFile} IsPdf={_vm.IsPdfFile} " +
                $"IsImage={_vm.IsImageFile} IsUnsupported={_vm.IsUnsupportedFile} " +
                $"ContentLen={_vm.FileContent?.Length ?? 0}");

            // ── PDF ──────────────────────────────────────────────────────────
            if (_vm.IsPdfFile)
            {
                // Give PdfiumViewer one more layout pass at Background priority
                // so its WinForms handle is fully initialised before loading.
                Dispatcher.InvokeAsync(() =>
                {
                    try
                    { PdfViewer.LoadPdf(_vm.CurrentFile.FilePath); }
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
                _vm.OnTextRendered();   // now safe to scroll
                return;
            }

            // ── Unsupported (docx, xlsx, pptx …) ────────────────────────────
            // The XAML Visibility binding on the "unsupported" StackPanel
            // already shows the "Open with Default App" panel automatically.
        }

        #endregion FileLoaded → RenderContent

        #region Rendering Helpers

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

        private void RenderHighlightedContent()
        {
            var doc = ContentRichTextBox.Document;
            doc.Blocks.Clear();
            doc.PageWidth = 10000; // prevent per-character line wrapping

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

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    para.Inlines.Add(new Run(line));
                }
                else
                {
                    int last = 0, idx;
                    while ((idx = line.IndexOf(keyword, last, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
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

                doc.Blocks.Add(para);
            }
        }

        private void ScrollToLine(int lineNumber)
        {
            if (!_vm.IsTextFile)
                return;
            var blocks = ContentRichTextBox.Document.Blocks.ToList();
            if (lineNumber > 0 && lineNumber - 1 < blocks.Count)
                blocks[lineNumber - 1].BringIntoView();
        }

        #endregion Rendering Helpers

        #region UI Event Handlers

        private void OnPrintRequested()
        {
            var pd = new System.Windows.Controls.PrintDialog();
            if (pd.ShowDialog() == true)
                pd.PrintDocument(
                    ((IDocumentPaginatorSource) ContentRichTextBox.Document).DocumentPaginator,
                    _vm.CurrentFile?.FileName ?? "SnapSearch Print");
        }

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
            catch (Exception ex) { _vm.StatusMessage = $"Could not open file: {ex.Message}"; }
        }

        private void MatchList_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
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