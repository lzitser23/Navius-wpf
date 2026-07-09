using Microsoft.Win32;

namespace Navius.Wpf.Primitives.Controls.FileUpload;

/// <summary>
/// The OS file dialog behind an injectable seam so unit tests never open a real dialog: the web's
/// "hidden native input relays the click" pattern has no WPF analog, so the dialog call itself is
/// the abstraction point. <see cref="NaviusFileUpload.FilePicker"/> defaults to
/// <see cref="OpenFileDialogPicker"/> and tests swap in a stub.
/// </summary>
public interface IFilePicker
{
    /// <summary>Shows the picker; returns the chosen full paths (empty when cancelled).</summary>
    IReadOnlyList<string> PickFiles(string? accept, bool multiple);
}

/// <summary>
/// Default <see cref="IFilePicker"/> over <see cref="OpenFileDialog"/>. The accept string's
/// extension entries become the dialog filter; MIME entries cannot be expressed in
/// <see cref="OpenFileDialog.Filter"/> syntax, so they widen the filter to All files and the
/// engine's post-hoc <see cref="FileUploadEngine.AcceptMatch"/> (which runs on every selection,
/// dialog or drop) stays the single source of truth for rejection reporting.
/// </summary>
public sealed class OpenFileDialogPicker : IFilePicker
{
    public IReadOnlyList<string> PickFiles(string? accept, bool multiple)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = multiple,
            Filter = BuildFilter(accept),
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : Array.Empty<string>();
    }

    /// <summary>Builds an OpenFileDialog filter from the accept string's extension entries.</summary>
    internal static string BuildFilter(string? accept)
    {
        const string allFiles = "All files|*.*";
        if (string.IsNullOrWhiteSpace(accept))
        {
            return allFiles;
        }

        var patterns = new List<string>();
        var hasMimeEntry = false;
        foreach (var raw in accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw.StartsWith(".", StringComparison.Ordinal))
            {
                patterns.Add("*" + raw);
            }
            else
            {
                hasMimeEntry = true; // "image/*" or "application/pdf": not expressible as a dialog pattern.
            }
        }

        if (patterns.Count == 0 || hasMimeEntry)
        {
            return allFiles;
        }

        var joined = string.Join(";", patterns);
        return $"Accepted files ({joined})|{joined}|{allFiles}";
    }
}
