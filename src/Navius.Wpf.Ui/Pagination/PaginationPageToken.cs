namespace Navius.Wpf.Ui.Pagination;

/// <summary>A single rendered slot in the pagination strip: either a clickable page number or an ellipsis gap.</summary>
public sealed class PaginationPageToken
{
    private PaginationPageToken(int page, bool isEllipsis)
    {
        Page = page;
        IsEllipsis = isEllipsis;
    }

    /// <summary>1-based page number. Meaningless (0) when <see cref="IsEllipsis"/> is true.</summary>
    public int Page { get; }

    public bool IsEllipsis { get; }

    public static PaginationPageToken ForPage(int page) => new(page, isEllipsis: false);

    public static PaginationPageToken Ellipsis { get; } = new(0, isEllipsis: true);

    public override string ToString() => IsEllipsis ? "..." : Page.ToString();
}
