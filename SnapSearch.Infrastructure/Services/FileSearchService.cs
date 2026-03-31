using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using System.Text;

namespace SnapSearch.Infrastructure.Services
{
    public class FileSearchService : IFileSearchService
    {
        #region Fields

        private static readonly string[] PreviewableExtensions =
        {
            ".txt", ".log", ".csv", ".xml", ".json", ".md", ".ini", ".cfg", ".bat", ".ps1",
            ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt",
            ".cs", ".vb", ".fs", ".py", ".js", ".ts", ".java", ".cpp", ".c", ".h",
            ".go", ".rs", ".php", ".rb", ".html", ".htm", ".css", ".scss", ".sql",
            ".yaml", ".yml", ".toml", ".env", ".properties", ".config", ".sh", ".cmd"
        };

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

                    // Extension filter (double-check — pattern may be broad)
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

                    // Name match
                    bool nameMatch = request.AllowPartialMatch
                        ? fileInfo.Name.Contains(request.Keyword, StringComparison.OrdinalIgnoreCase)
                        : fileInfo.Name.Equals(request.Keyword, StringComparison.OrdinalIgnoreCase);

                    // Content match — run when SearchFileContents is on, regardless of nameMatch
                    // so that ContentMatchCount is always accurate when the file is previewed.
                    bool contentMatch = false;
                    int contentMatchCount = 0;

                    if (request.SearchFileContents)
                    {
                        var matchList = (await SearchFileContentAsync(
                            filePath, request.Keyword, cancellationToken)).ToList();
                        contentMatch = matchList.Count > 0;
                        contentMatchCount = matchList.Count;
                    }

                    // Include file if it matches by name OR by content
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

                // Plain text — use the expanded IsTextFile check
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
            catch { /* unreadable or locked file — silently skip */ }

            return matches;
        }

        public bool CanPreviewFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return PreviewableExtensions.Contains(ext);
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Expanded plain-text check — matches every extension the ViewModel treats as text.
        /// </summary>
        private static bool IsTextFile(string ext) =>
            ext is
                ".txt" or ".log" or ".md" or ".rtf" or
                ".csv" or ".json" or ".xml" or ".yaml" or ".yml" or ".toml" or
                ".ini" or ".cfg" or ".config" or ".env" or ".properties" or
                ".cs" or ".vb" or ".fs" or ".py" or ".js" or ".ts" or ".java" or
                ".cpp" or ".c" or ".h" or ".go" or ".rs" or ".php" or ".rb" or
                ".html" or ".htm" or ".css" or ".scss" or ".sql" or
                ".bat" or ".cmd" or ".ps1" or ".sh";

        private async Task<IEnumerable<ContentMatchDto>> SearchPdfContentAsync(
            string filePath, string keyword, CancellationToken cancellationToken)
        {
            var matches = new List<ContentMatchDto>();
            int matchIndex = 1;
            try
            {
                using var doc = UglyToad.PdfPig.PdfDocument.Open(filePath);
                foreach (var page in doc.GetPages())
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    var lines = page.Text.Split('\n');
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
            catch { /* invalid PDF or library unavailable */ }
            return await Task.FromResult(matches);
        }

        private async Task<IEnumerable<ContentMatchDto>> SearchDocxContentAsync(
            string filePath, string keyword, CancellationToken cancellationToken)
        {
            var matches = new List<ContentMatchDto>();
            int lineNumber = 1;
            int matchIndex = 1;
            try
            {
                using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null)
                    return matches;

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
            catch { /* invalid docx */ }
            return await Task.FromResult(matches);
        }

        #endregion Private Methods
    }
}