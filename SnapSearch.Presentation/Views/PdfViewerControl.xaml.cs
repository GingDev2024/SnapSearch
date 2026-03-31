using PdfiumViewer;

namespace SnapSearch.Presentation.Views
{
    public partial class PdfViewerControl : System.Windows.Controls.UserControl
    {
        #region Fields

        // The Windows Forms PDF viewer control (PdfiumViewer uses WinForms internally)
        private PdfViewer? _pdfViewer;

        // The currently loaded PDF document — we keep a reference so we can
        // render pages to bitmaps when printing
        private PdfDocument? _currentDocument;

        // We also store the file path so we can re-open the document for printing
        // (PdfDocument is sometimes already disposed by the time print is called)
        private string? _currentFilePath;

        #endregion Fields

        #region Constructor

        public PdfViewerControl()
        {
            InitializeComponent();

            // Create the WinForms PdfViewer and host it inside the WPF WindowsFormsHost
            _pdfViewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                ShowToolbar  = false,   // hide the built-in toolbar — we have our own
                ShowBookmarks = false,  // hide the bookmarks panel
            };

            // FormsHost is the <WindowsFormsHost> declared in the XAML of this UserControl
            FormsHost.Child = _pdfViewer;

            // When this control is removed from the visual tree, free resources
            Unloaded += (_, _) => CleanUp();
        }

        #endregion Constructor

        #region Public Methods

        /// <summary>
        /// Loads a PDF file from disk and shows it in the viewer.
        /// Call this from the code-behind after the window has loaded.
        /// </summary>
        public void LoadPdf(string filePath)
        {
            if (_pdfViewer == null) return;

            // Dispose the previous document before loading a new one
            CleanUp();

            try
            {
                _currentFilePath = filePath;
                _currentDocument  = PdfDocument.Load(filePath);
                _pdfViewer.Document = _currentDocument;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PdfViewerControl] PDF load error: {ex.Message}");
            }
        }

        public void PrintAllPages(System.Windows.Controls.PrintDialog pd, string title)
        {
            // We need a file path to re-open the document for rendering
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                System.Diagnostics.Debug.WriteLine("[PdfViewerControl] No file loaded — cannot print.");
                return;
            }

            // Open a fresh copy of the PDF just for rendering/printing
            // (avoids issues if the viewer's own document is in a certain state)
            using var printDoc = PdfDocument.Load(_currentFilePath);

            // How big is the printable area on the selected paper/printer?
            // These are in WPF device-independent units (1 unit = 1/96 inch)
            double pageWidthWpf  = pd.PrintableAreaWidth;
            double pageHeightWpf = pd.PrintableAreaHeight;

            // We will render at 200 DPI for crisp output.
            // WPF device-independent units are at 96 DPI, so:
            //   physical pixels = wpf units * (renderDpi / 96)
            const int renderDpi = 200;
            int renderWidthPx  = (int)(pageWidthWpf  * renderDpi / 96.0);
            int renderHeightPx = (int)(pageHeightWpf * renderDpi / 96.0);

            int totalPages = printDoc.PageCount;

            for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
            {
                // ── Step 1: Render this PDF page into a GDI+ Bitmap ──────────
                // RenderPage returns a System.Drawing.Image (GDI+)
                using System.Drawing.Image gdiImage = printDoc.Render(
                    pageIndex,
                    renderWidthPx,
                    renderHeightPx,
                    renderDpi,
                    renderDpi,
                    false   // false = normal orientation (not rotated)
                );

                // ── Step 2: Convert GDI+ Bitmap → WPF BitmapSource ───────────
                // WPF cannot use System.Drawing.Bitmap directly, so we convert it.
                var wpfBitmap = ConvertGdiBitmapToWpf((System.Drawing.Bitmap)gdiImage);

                // ── Step 3: Put the bitmap into a WPF Image control ───────────
                var imageControl = new System.Windows.Controls.Image
                {
                    Source  = wpfBitmap,
                    Stretch = System.Windows.Media.Stretch.Uniform // keep aspect ratio
                };

                var pageSize = new System.Windows.Size(pageWidthWpf, pageHeightWpf);
                imageControl.Measure(pageSize);
                imageControl.Arrange(new System.Windows.Rect(pageSize));
                imageControl.UpdateLayout();

                // ── Step 5: Send this page to the printer ─────────────────────
                pd.PrintVisual(imageControl, $"{title} — Page {pageIndex + 1} of {totalPages}");
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Converts a System.Drawing.Bitmap (GDI+) into a WPF BitmapSource.
        /// This is necessary because WPF's Image control cannot consume GDI+ bitmaps directly.
        /// </summary>
        private static System.Windows.Media.Imaging.BitmapSource ConvertGdiBitmapToWpf(
            System.Drawing.Bitmap gdiBitmap)
        {
            // Get a native handle (HBITMAP) for the GDI+ bitmap
            var hBitmap = gdiBitmap.GetHbitmap();

            try
            {
                // CreateBitmapSourceFromHBitmap wraps the native bitmap into a WPF BitmapSource
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                // Always delete the native GDI object to avoid a memory leak
                DeleteObject(hBitmap);
            }
        }

        /// <summary>
        /// Win32 API call to free a native GDI bitmap handle.
        /// Required after calling GetHbitmap() to avoid memory leaks.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Releases the PDF document and clears the viewer.
        /// Called when a new file is loaded or when the control is unloaded.
        /// </summary>
        private void CleanUp()
        {
            if (_pdfViewer != null)
                _pdfViewer.Document = null; // detach first so PdfiumViewer releases its lock

            _currentDocument?.Dispose();
            _currentDocument  = null;
            _currentFilePath  = null;
        }

        #endregion Private Methods
    }
}