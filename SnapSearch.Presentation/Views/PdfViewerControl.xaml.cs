using PdfiumViewer;

namespace SnapSearch.Presentation.Views
{
    public partial class PdfViewerControl : System.Windows.Controls.UserControl
    {
        #region Fields
        private PdfViewer? _pdfViewer;
        private PdfDocument? _currentDocument;
        #endregion Fields

        #region Public Constructors
        public PdfViewerControl()
        {
            InitializeComponent();

            _pdfViewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                ShowToolbar = false,
                ShowBookmarks = false,
            };

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
                _currentDocument = PdfDocument.Load(filePath);
                _pdfViewer.Document = _currentDocument;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF load error: {ex.Message}");
            }
        }
        #endregion Public Methods

        #region Private Methods
        private void CleanUp()
        {
            if (_pdfViewer != null)
                _pdfViewer.Document = null;

            _currentDocument?.Dispose();
            _currentDocument = null;
        }
        #endregion Private Methods
    }
}