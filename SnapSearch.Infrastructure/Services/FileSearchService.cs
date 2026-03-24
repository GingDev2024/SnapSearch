using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using System.Text;

namespace SnapSearch.Infrastructure.Services
{
    public class FileSearchService : IFileSearchService
    {
        #region Fields

        private static readonly string[] PreviewableExtensions =
            { ".txt", ".log", ".csv", ".xml", ".json", ".md", ".ini", ".cfg", ".bat", ".ps1",
              ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt" };

        #endregion Fields

        #region Public Methods

        public async Task<IEnumerable<FileResultDto>> SearchAsync(
            FileSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            var results = new List<FileResultDto>();

            if (!Directory.Exists(request.SearchDirectory))
                return results;

            var searchOption = request.SearchSubDirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            IEnumerable<string> files;
            try
            {
                var pattern = string.IsNullOrWhiteSpace(request.ExtensionFilter)
                    ? "*"
                    : $"*{request.ExtensionFilter}";
                files = Directory.EnumerateFiles(request.SearchDirectory, pattern, searchOption);
            }
            catch (UnauthorizedAccessException)
            {
                return results;
            }

            foreach (var filePath in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var fileInfo = new FileInfo(filePath);
                    if (!fileInfo.Exists)
                        continue;

                    // Extension filter
                    if (!string.IsNullOrWhiteSpace(request.ExtensionFilter))
                    {
                        var ext = request.ExtensionFilter.StartsWith(".")
                            ? request.ExtensionFilter
                            : $".{request.ExtensionFilter}";
                        if (!fileInfo.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    // Date filter
                    if (request.DateMin.HasValue && fileInfo.LastWriteTime < request.DateMin.Value)
                        continue;
                    if (request.DateMax.HasValue && fileInfo.LastWriteTime > request.DateMax.Value)
                        continue;

                    // Size filter
                    if (request.SizeMin.HasValue && fileInfo.Length < request.SizeMin.Value)
                        continue;
                    if (request.SizeMax.HasValue && fileInfo.Length > request.SizeMax.Value)
                        continue;

                    // Name keyword match
                    bool nameMatch = request.AllowPartialMatch
                        ? fileInfo.Name.Contains(request.Keyword, StringComparison.OrdinalIgnoreCase)
                        : fileInfo.Name.Equals(request.Keyword, StringComparison.OrdinalIgnoreCase);

                    bool contentMatch = false;
                    int contentMatchCount = 0;

                    if (request.SearchFileContents && !nameMatch)
                    {
                        var matches = await SearchFileContentAsync(filePath, request.Keyword, cancellationToken);
                        var matchList = matches.ToList();
                        contentMatch = matchList.Count > 0;
                        contentMatchCount = matchList.Count;
                    }

                    if (!nameMatch && !contentMatch)
                        continue;

                    results.Add(new FileResultDto
                    {
                        FileName = fileInfo.Name,
                        FilePath = fileInfo.FullName,
                        Extension = fileInfo.Extension,
                        SizeBytes = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime,
                        CreatedAt = fileInfo.CreationTime,
                        HasContentMatch = contentMatch,
                        ContentMatchCount = contentMatchCount
                    });
                }
                catch (UnauthorizedAccessException) { continue; }
                catch (IOException) { continue; }
            }

            return results;
        }

        public async Task<IEnumerable<ContentMatchDto>> SearchFileContentAsync(
            string filePath, string keyword, CancellationToken cancellationToken = default)
        {
            var matches = new List<ContentMatchDto>();
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                if (ext == ".pdf")
                    return await SearchPdfContentAsync(filePath, keyword, cancellationToken);

                if (ext is ".docx" or ".doc")
                    return await SearchDocxContentAsync(filePath, keyword, cancellationToken);

                // Plain text fallback
                if (!IsTextFile(ext))
                    return matches;

                var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8, cancellationToken);
                for (int i = 0; i < lines.Length; i++)
                {
                    int idx = 0;
                    while ((idx = lines[i].IndexOf(keyword, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        matches.Add(new ContentMatchDto
                        {
                            LineNumber = i + 1,
                            MatchIndex = matches.Count + 1,
                            LineContent = lines[i].Trim(),
                            Keyword = keyword,
                            PageNumber = 1
                        });
                        idx += keyword.Length;
                    }
                }
            }
            catch { /* unreadable file */ }

            return matches;
        }

        public bool CanPreviewFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return PreviewableExtensions.Contains(ext);
        }

        #endregion Public Methods

        #region Private Methods

        private static bool IsTextFile(string ext) =>
            ext is ".txt" or ".log" or ".csv" or ".xml" or ".json"
                or ".md" or ".ini" or ".cfg" or ".bat" or ".ps1" or ".cs" or ".html" or ".htm";

        private async Task<IEnumerable<ContentMatchDto>> SearchPdfContentAsync(
                            string filePath, string keyword, CancellationToken cancellationToken)
        {
            var matches = new List<ContentMatchDto>();
            try
            {
                using var doc = UglyToad.PdfPig.PdfDocument.Open(filePath);
                int matchIndex = 1;
                foreach (var page in doc.GetPages())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var text = page.Text;
                    var lines = text.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            matches.Add(new ContentMatchDto
                            {
                                LineNumber = i + 1,
                                MatchIndex = matchIndex++,
                                LineContent = lines[i].Trim(),
                                Keyword = keyword,
                                PageNumber = page.Number
                            });
                        }
                    }
                }
            }
            catch { /* Not a valid PDF or library not available */ }
            return await Task.FromResult(matches);
        }

        private async Task<IEnumerable<ContentMatchDto>> SearchDocxContentAsync(
            string filePath, string keyword, CancellationToken cancellationToken)
        {
            var matches = new List<ContentMatchDto>();
            try
            {
                using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null)
                    return matches;

                int lineNumber = 1;
                int matchIndex = 1;
                foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var text = para.InnerText;
                    if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(new ContentMatchDto
                        {
                            LineNumber = lineNumber,
                            MatchIndex = matchIndex++,
                            LineContent = text.Trim(),
                            Keyword = keyword,
                            PageNumber = 1
                        });
                    }
                    lineNumber++;
                }
            }
            catch { /* Not a valid docx */ }
            return await Task.FromResult(matches);
        }

        #endregion Private Methods
    }
}