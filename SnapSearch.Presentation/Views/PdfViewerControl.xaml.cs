using PdfiumViewer;

namespace SnapSearch.Presentation.Views
{
    public partial class PdfViewerControl : System.Windows.Controls.UserControl
    {
        #region Fields

        private PdfViewer? _pdfViewer;
        private PdfDocument? _currentDocument;
        private string? _currentFilePath;
        private PdfSearchManager? _searchManager;
        private int _currentMatchIndex = -1;
        private int _totalMatches;

        #endregion Fields

        #region Public Constructors

        public PdfViewerControl()
        {
            InitializeComponent();

            try
            {
                _pdfViewer = new PdfViewer
                {
                    Dock = System.Windows.Forms.DockStyle.Fill,
                    ShowToolbar = false,
                    ShowBookmarks = false,
                };
                FormsHost.Child = _pdfViewer;
            }
            catch (DllNotFoundException)
            {
                // pdfium.dll missing — show a friendly message instead of crashing
                var label = new System.Windows.Controls.TextBlock
                {
                    Text = "PDF viewer unavailable: pdfium.dll not found next to the exe.",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Margin = new System.Windows.Thickness(24)
                };
                Content = label;
                return;
            }

            FormsHost.Child = _pdfViewer;
            Unloaded += (_, _) => CleanUp();
        }

        #endregion Public Constructors

        #region Public Methods

        public void LoadPdf(string filePath)
        {
            if (_pdfViewer == null) return;
            CleanUp();

            try
            {
                _currentFilePath = filePath;
                _currentDocument = PdfDocument.Load(filePath);
                _pdfViewer.Document = _currentDocument;

                _searchManager = new PdfSearchManager(_pdfViewer.Renderer)
                {
                    HighlightAllMatches = true,
                    MatchCase = false,
                    MatchWholeWord = false,
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PdfViewerControl] Load error: {ex.Message}");
            }
        }

        // Called from FilePreviewWindow after LoadPdf
        public void SetKeyword(string keyword)
        {
            if (_searchManager == null || string.IsNullOrWhiteSpace(keyword)) return;

            // Search() highlights all matches in yellow automatically via PdfRenderer.Markers
            bool found = _searchManager.Search(keyword);

            if (found)
            {
                _totalMatches = _searchManager
                    .GetType()
                    .GetField("_matches", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_searchManager) is PdfMatches m ? m.Items.Count : 0;

                _currentMatchIndex = 0;
                // Scroll to and blue-highlight the first match
                _searchManager.FindNext(forward: true);
            }
        }

        // Called when user clicks a match in the right-side list
        public void GoToMatch(int index)
        {
            if (_searchManager == null) return;
            if (index < 0) return;

            bool forward = index >= _currentMatchIndex;
            int steps = Math.Abs(index - _currentMatchIndex);

            for (int i = 0; i < steps; i++)
                _searchManager.FindNext(forward: forward);

            _currentMatchIndex = index;
        }

        public void PrintPdfPages(System.Windows.Controls.PrintDialog pd, string title)
        {
            if (string.IsNullOrEmpty(_currentFilePath)) return;

            using var doc = PdfDocument.Load(_currentFilePath);
            const int dpi = 200;
            int pw = (int)(pd.PrintableAreaWidth * dpi / 96.0);
            int ph = (int)(pd.PrintableAreaHeight * dpi / 96.0);

            for (int i = 0; i < doc.PageCount; i++)
            {
                using System.Drawing.Image img = doc.Render(i, pw, ph, dpi, dpi, false);
                var bmp = ConvertToWpfBitmap((System.Drawing.Bitmap)img);
                var ctrl = new System.Windows.Controls.Image
                {
                    Source = bmp,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
                var size = new System.Windows.Size(pd.PrintableAreaWidth, pd.PrintableAreaHeight);
                ctrl.Measure(size);
                ctrl.Arrange(new System.Windows.Rect(size));
                ctrl.UpdateLayout();
                pd.PrintVisual(ctrl, $"{title} — Page {i + 1} of {doc.PageCount}");
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static System.Windows.Media.Imaging.BitmapSource ConvertToWpfBitmap(
            System.Drawing.Bitmap bmp)
        {
            var h = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    h, IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(h); }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr h);

        private void CleanUp()
        {
            _searchManager?.Reset();
            _searchManager = null;
            _currentMatchIndex = -1;
            _totalMatches = 0;

            if (_pdfViewer != null)
                _pdfViewer.Document = null;

            _currentDocument?.Dispose();
            _currentDocument = null;
            _currentFilePath = null;
        }

        #endregion Private Methods
    }
}