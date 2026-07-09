using System.IO;

namespace Navius.Wpf.Primitives.Controls.FileUpload;

/// <summary>
/// One selected file. The WPF stand-in for the web's <c>IBrowserFile</c>: dropped/browsed files
/// arrive as paths, so the record carries the path plus the name/size/content-type surface the
/// validation engine matches on. <see cref="ContentType"/> is resolved from the extension by
/// <see cref="FileUploadEngine.ContentTypeFor"/> (there is no browser to supply a MIME type).
/// </summary>
public sealed record FileEntry(string Path, string Name, long Size, string ContentType = "")
{
    /// <summary>Human-readable size, e.g. "1.4 KB".</summary>
    public string SizeText => FileUploadEngine.FormatSize(Size);

    /// <summary>Builds an entry from a filesystem path, probing size and inferring the MIME type.</summary>
    public static FileEntry FromPath(string path)
    {
        var info = new FileInfo(path);
        var size = info.Exists ? info.Length : 0;
        return new FileEntry(path, info.Name, size, FileUploadEngine.ContentTypeFor(info.Name));
    }
}

/// <summary>Why a selected/dropped file was refused before reaching the file list (mirrors the web enum).</summary>
public enum FileRejectionReason
{
    /// <summary>The file exceeded <c>MaxSize</c>.</summary>
    TooLarge,

    /// <summary>Adding the file would exceed <c>MaxFiles</c>.</summary>
    TooMany,

    /// <summary>The file did not match the <c>Accept</c> filter.</summary>
    WrongType,
}

/// <summary>A single rejected file and the reason it was refused.</summary>
public sealed record NaviusFileRejection(FileEntry File, FileRejectionReason Reason);

/// <summary>The outcome of validating one selection against the current list + limits.</summary>
public sealed record FileSelectionResult(
    IReadOnlyList<FileEntry> Next,
    IReadOnlyList<FileEntry> Accepted,
    IReadOnlyList<NaviusFileRejection> Rejected)
{
    public bool Invalid => Rejected.Count > 0;
}

/// <summary>
/// Pure, STA-free selection/validation math for <see cref="NaviusFileUpload"/>, ported from the
/// web root's <c>HandleFilesAsync</c>/<c>AcceptMatch</c>/<c>BuildStatus</c>. Every quirk of the
/// source is preserved deliberately: MaxFiles only enforced when Multiple; non-multiple replaces
/// the single file only when something was accepted; duplicates (same name + size) are silently
/// dropped (neither accepted nor rejected).
/// </summary>
public static class FileUploadEngine
{
    /// <summary>
    /// Validates <paramref name="incoming"/> against <paramref name="current"/> + limits, in the
    /// web source's exact check order: accept -> size -> count -> duplicate.
    /// </summary>
    public static FileSelectionResult Process(
        IReadOnlyList<FileEntry> current,
        IReadOnlyList<FileEntry> incoming,
        string? accept,
        long maxSize,
        int? maxFiles,
        bool multiple)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(incoming);

        var accepted = new List<FileEntry>();
        var rejected = new List<NaviusFileRejection>();
        var running = new List<FileEntry>(current);

        foreach (var file in incoming)
        {
            if (!AcceptMatch(file, accept))
            {
                rejected.Add(new NaviusFileRejection(file, FileRejectionReason.WrongType));
                continue;
            }

            if (maxSize > 0 && file.Size > maxSize)
            {
                rejected.Add(new NaviusFileRejection(file, FileRejectionReason.TooLarge));
                continue;
            }

            if (multiple && maxFiles.HasValue && running.Count >= maxFiles.Value)
            {
                rejected.Add(new NaviusFileRejection(file, FileRejectionReason.TooMany));
                continue;
            }

            if (multiple)
            {
                // A duplicate (same name + size) is not added, so it must not be counted as
                // accepted (it would falsely inflate the polite status and fire the accepted event).
                if (running.Any(x => Same(x, file)))
                {
                    continue;
                }

                running.Add(file);
            }

            accepted.Add(file);
        }

        IReadOnlyList<FileEntry> next = multiple
            ? running
            : (accepted.Count > 0 ? new[] { accepted[^1] } : current);

        return new FileSelectionResult(next, accepted, rejected);
    }

    /// <summary>
    /// The web's <c>AcceptMatch</c>: extension (<c>.ext</c>), MIME wildcard (<c>type/*</c>), or
    /// exact MIME entries, comma-separated. Null/whitespace accepts everything.
    /// </summary>
    public static bool AcceptMatch(FileEntry file, string? accept)
    {
        if (string.IsNullOrWhiteSpace(accept))
        {
            return true;
        }

        var name = file.Name ?? string.Empty;
        var type = file.ContentType ?? string.Empty;

        foreach (var raw in accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw.StartsWith(".", StringComparison.Ordinal))
            {
                if (name.EndsWith(raw, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (raw.EndsWith("/*", StringComparison.Ordinal))
            {
                var prefix = raw[..^1]; // "image/*" -> "image/"
                if (type.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (string.Equals(type, raw, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Duplicate detection, matched by name + size only (not content hash), per the web source.</summary>
    public static bool Same(FileEntry a, FileEntry b) =>
        string.Equals(a.Name, b.Name, StringComparison.Ordinal) && a.Size == b.Size;

    /// <summary>The polite status line for one selection, ported from the web's <c>BuildStatus</c>.</summary>
    public static string BuildStatus(int acceptedCount, int rejectedCount)
    {
        var parts = new List<string>();

        if (acceptedCount > 0)
        {
            parts.Add($"{acceptedCount} file{(acceptedCount == 1 ? "" : "s")} added");
        }

        if (rejectedCount > 0)
        {
            parts.Add($"{rejectedCount} file{(rejectedCount == 1 ? "" : "s")} rejected");
        }

        return parts.Count == 0 ? "No files added" : string.Join(". ", parts);
    }

    /// <summary>Human-readable byte size, e.g. "1.4 KB" (ported from FileUploadContext.FormatSize).</summary>
    public static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        var rounded = unit == 0 ? size.ToString("0") : size.ToString("0.#");
        return $"{rounded} {units[unit]}";
    }

    /// <summary>
    /// Minimal extension-to-MIME map so <c>type/*</c> and exact-MIME accept entries keep working
    /// without a browser to supply the content type. Unknown extensions yield an empty string
    /// (they can still match extension accept entries).
    /// </summary>
    public static string ContentTypeFor(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        if (dot < 0)
        {
            return string.Empty;
        }

        return fileName[dot..].ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".zip" => "application/zip",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => string.Empty,
        };
    }
}
