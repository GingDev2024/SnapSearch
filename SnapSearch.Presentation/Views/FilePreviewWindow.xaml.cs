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
        #endregion Fields

        #region Public Constructors
        public FilePreviewWindow(FileResultDto file, string keyword)
        {
            InitializeComponent();
            _vm = App.GetService<FilePreviewViewModel>();
            DataContext = _vm;
            _vm.ScrollToLineRequested += ScrollToLine;
            _vm.PrintRequested += OnPrintRequested;

            Loaded += async (_, _) =>
            {
                await _vm.LoadFileAsync(file, keyword);

                // flush bindings FIRST so Visibility updates before we load
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

                // PDF
                if (_vm.IsPdfFile && _vm.CurrentFile != null)
                    PdfViewer.LoadPdf(_vm.CurrentFile.FilePath);

                // Image
                if (_vm.IsImageFile && _vm.CurrentFile != null)
                    LoadImage(_vm.CurrentFile.FilePath);

                // Text
                if (!string.IsNullOrEmpty(_vm.FileContent))
                    RenderHighlightedContent();
            };
        }
        #endregion Public Constructors

        #region Private Methods

        private void LoadImage(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                PreviewImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                _vm.StatusMessage = $"Could not load image: {ex.Message}";
            }
        }

        private void RenderHighlightedContent()
        {
            ContentRichTextBox.Document.Blocks.Clear();
            ContentRichTextBox.Document.PageWidth = 10000; // ADD THIS — prevents letter-by-letter wrapping

            if (string.IsNullOrEmpty(_vm.FileContent))
                return;

            var keyword = _vm.Keyword;
            var lines = _vm.FileContent.Split('\n');

            foreach (var line in lines)
            {
                var linePara = new Paragraph 
                { 
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Left,
                    KeepTogether = true

                };

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    linePara.Inlines.Add(new Run(line));
                }
                else
                {
                    int lastIndex = 0;
                    int idx;
                    while ((idx = line.IndexOf(keyword, lastIndex,
                               StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        if (idx > lastIndex)
                            linePara.Inlines.Add(new Run(line[lastIndex..idx]));

                        linePara.Inlines.Add(new Run(line.Substring(idx, keyword.Length))
                        {
                            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(79, 142, 247)),
                            Foreground = System.Windows.Media.Brushes.White,
                            FontWeight = FontWeights.Bold
                        });
                        lastIndex = idx + keyword.Length;
                    }

                    if (lastIndex < line.Length)
                        linePara.Inlines.Add(new Run(line[lastIndex..]));
                }

                ContentRichTextBox.Document.Blocks.Add(linePara);
            }
        }

        private void ScrollToLine(int lineNumber)
        {
            if (!_vm.IsTextFile) return;

            var blocks = ContentRichTextBox.Document.Blocks.ToList();
            if (lineNumber > 0 && lineNumber - 1 < blocks.Count)
                blocks[lineNumber - 1].BringIntoView();
        }

        private void OnPrintRequested()
        {
            var pd = new System.Windows.Controls.PrintDialog();
            if (pd.ShowDialog() == true)
                pd.PrintDocument(
                    ((IDocumentPaginatorSource)ContentRichTextBox.Document).DocumentPaginator,
                    _vm.CurrentFile?.FileName ?? "SnapSearch Print");
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
            catch (Exception ex)
            {
                _vm.StatusMessage = $"Could not open file: {ex.Message}";
            }
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

        #endregion Private Methods
    }
}