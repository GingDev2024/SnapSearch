using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using System.Text;
using System.Text.RegularExpressions;

namespace SnapSearch.Infrastructure.Services
{
    public class FileSearchService : IFileSearchService
    {
        #region Fields

        private static readonly string[] PreviewableExtensions =
        {
            ".txt", ".log", ".csv", ".xml", ".json", ".md", ".ini", ".cfg", ".bat", ".ps1",
            ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt",
            ".cs",  ".vb",  ".fs",  ".py",  ".js",  ".ts",  ".java",
            ".cpp", ".c",   ".h",   ".go",  ".rs",  ".php", ".rb",
            ".html",".htm", ".css", ".scss",".sql",  ".yaml",".yml",
            ".toml",".env", ".properties",  ".config",".sh", ".cmd"
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
                var pattern = string.IsNullOrWhiteSpace(request.ExtensionFilter) ? "*"
                    : $"*{request.ExtensionFilter}";
                files = Directory.EnumerateFiles(request.SearchDirectory, pattern, searchOption);
            }
            catch (UnauthorizedAccessException) { return results; }

            // Pre-compile regex once if needed
            Regex? nameRegex = null;
            Regex? contentRegex = null;
            if (request.UseRegex && !string.IsNullOrWhiteSpace(request.Keyword))
            {
                try
                {
                    var opts = RegexOptions.IgnoreCase | RegexOptions.Compiled;
                    nameRegex = new Regex(request.Keyword, opts);
                    contentRegex = new Regex(request.Keyword, opts);
                }
                catch { /* invalid regex — fall back to literal */ }
            }

            foreach (var filePath in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var fi = new FileInfo(filePath);
                    if (!fi.Exists)
                        continue;

                    // Extension double-check
                    if (!string.IsNullOrWhiteSpace(request.ExtensionFilter))
                    {
                        var ext = request.ExtensionFilter.StartsWith(".")
                            ? request.ExtensionFilter
                            : $".{request.ExtensionFilter}";
                        if (!fi.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    // Date filter
                    if (request.DateMin.HasValue && fi.LastWriteTime < request.DateMin.Value)
                        continue;
                    if (request.DateMax.HasValue && fi.LastWriteTime > request.DateMax.Value)
                        continue;

                    // Size filter (already in bytes in the DTO)
                    if (request.SizeMin.HasValue && fi.Length < request.SizeMin.Value)
                        continue;
                    if (request.SizeMax.HasValue && fi.Length > request.SizeMax.Value)
                        continue;

                    // Name match
                    bool nameMatch;
                    if (nameRegex != null)
                        nameMatch = nameRegex.IsMatch(fi.Name);
                    else if (request.AllowPartialMatch)
                        nameMatch = fi.Name.Contains(request.Keyword, StringComparison.OrdinalIgnoreCase);
                    else
                        nameMatch = fi.Name.Equals(request.Keyword, StringComparison.OrdinalIgnoreCase);

                    // Content match — always run when SearchFileContents is on
                    bool contentMatch = false;
                    int contentMatchCount = 0;

                    if (request.SearchFileContents)
                    {
                        var matchList = (await SearchFileContentAsync(
                            filePath, request.Keyword, cancellationToken,
                            contentRegex)).ToList();
                        contentMatch = matchList.Count > 0;
                        contentMatchCount = matchList.Count;
                    }

                    if (!nameMatch && !contentMatch)
                        continue;

                    results.Add(new FileResultDto
                    {
                        FileName = fi.Name,
                        FilePath = fi.FullName,
                        Extension = fi.Extension,
                        SizeBytes = fi.Length,
                        LastModified = fi.LastWriteTime,
                        CreatedAt = fi.CreationTime,
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
            string filePath, string keyword,
            CancellationToken cancellationToken = default,
            Regex? precompiledRegex = null)
        {
            var matches = new List<ContentMatchDto>();
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                if (ext == ".pdf")
                    return await SearchPdfContentAsync(filePath, keyword, cancellationToken, precompiledRegex);

                if (ext is ".docx" or ".doc")
                    return await SearchDocxContentAsync(filePath, keyword, cancellationToken, precompiledRegex);

                if (!IsTextFile(ext))
                    return matches;

                var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8, cancellationToken);
                for (int i = 0; i < lines.Length; i++)
                {
                    bool hit = precompiledRegex != null
                        ? precompiledRegex.IsMatch(lines[i])
                        : lines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase);

                    if (hit)
                    {
                        matches.Add(new ContentMatchDto
                        {
                            LineNumber = i + 1,
                            MatchIndex = matches.Count + 1,
                            LineContent = lines[i].Trim(),
                            Keyword = keyword,
                            PageNumber = 1
                        });
                    }
                }
            }
            catch { /* unreadable / locked file */ }

            return matches;
        }

        // IFileSearchService interface overload without regex param
        public Task<IEnumerable<ContentMatchDto>> SearchFileContentAsync(
            string filePath, string keyword, CancellationToken cancellationToken = default)
            => SearchFileContentAsync(filePath, keyword, cancellationToken, null);

        public bool CanPreviewFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return PreviewableExtensions.Contains(ext);
        }

        #endregion Public Methods

        #region Private Methods

        private static bool IsTextFile(string ext) =>
            ext is
                ".txt" or ".log" or ".md" or ".rtf" or
                ".csv" or ".json" or ".xml" or ".yaml" or ".yml" or ".toml" or
                ".ini" or ".cfg" or ".config" or ".env" or ".properties" or
                ".cs" or ".vb" or ".fs" or ".py" or ".js" or ".ts" or ".java" or
                ".cpp" or ".c" or ".h" or ".go" or ".rs" or ".php" or ".rb" or
                ".html" or ".htm" or ".css" or ".scss" or ".sql" or
                ".bat" or ".cmd" or ".ps1" or ".sh";

        //private static bool IsImage(string ext) =>
        //    ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or
        //           ".gif" or ".webp" or ".tiff" or ".tif" or ".ico";

        private async Task<IEnumerable<ContentMatchDto>> SearchPdfContentAsync(
            string filePath, string keyword,
            CancellationToken cancellationToken,
            Regex? regex)
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
                        bool hit = regex != null
                            ? regex.IsMatch(lines[i])
                            : lines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase);

                        if (hit)
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
            catch { }
            return await Task.FromResult(matches);
        }

        private async Task<IEnumerable<ContentMatchDto>> SearchDocxContentAsync(
            string filePath, string keyword,
            CancellationToken cancellationToken,
            Regex? regex)
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

                    bool hit = regex != null
                        ? regex.IsMatch(text)
                        : text.Contains(keyword, StringComparison.OrdinalIgnoreCase);

                    if (hit)
                        matches.Add(new ContentMatchDto
                        {
                            LineNumber = lineNumber,
                            MatchIndex = matchIndex++,
                            LineContent = text.Trim(),
                            Keyword = keyword,
                            PageNumber = 1
                        });

                    lineNumber++;
                }
            }
            catch { }
            return await Task.FromResult(matches);
        }

        #endregion Private Methods
    }
}