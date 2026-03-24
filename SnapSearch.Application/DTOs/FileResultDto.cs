namespace SnapSearch.Application.DTOs
{
    public class FileResultDto
    {
        #region Properties

        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string SizeDisplay => FormatSize(SizeBytes);
        public DateTime LastModified { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasContentMatch { get; set; }
        public int ContentMatchCount { get; set; }
        public string Directory => System.IO.Path.GetDirectoryName(FilePath) ?? string.Empty;

        #endregion Properties

        #region Private Methods

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        }

        #endregion Private Methods
    }
}