using SnapSearch.Application.DTOs;
using SnapSearch.Presentation.ViewModels;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading; // ADD THIS

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

                // FIX: yield to the dispatcher so WPF processes the IsTextFile
                // binding change and makes the ScrollViewer Visible BEFORE
                // we write into the RichTextBox. Without this, the FlowDocument
                // is populated while the control is still Collapsed and the
                // layout never runs, so the content appears blank.
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

                if (!string.IsNullOrEmpty(_vm.FileContent))
                    RenderHighlightedContent();
            };
        }
        #endregion Public Constructors

        #region Private Methods
        private void RenderHighlightedContent()
        {
            ContentRichTextBox.Document.Blocks.Clear();

            if (string.IsNullOrEmpty(_vm.FileContent))
                return;

            var keyword = _vm.Keyword;
            var lines = _vm.FileContent.Split('\n');

            foreach (var line in lines)
            {
                var linePara = new Paragraph { Margin = new Thickness(0) };

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
            if (!_vm.IsTextFile)
                return;

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