using System;
using System.Collections.Generic;

namespace Navius.Wpf.Ui.Pagination;

/// <summary>
/// Pure, deterministic page-list computation with zero WPF dependency (same testing precedent as
/// Navius.Wpf.Primitives' AutocompleteEngine). Always keeps the first <paramref name="boundaryCount"/>
/// and last <paramref name="boundaryCount"/> pages visible, plus <paramref name="siblingCount"/> pages
/// on each side of the current page, collapsing any remaining gaps into a single ellipsis token.
/// </summary>
public static class PaginationEngine
{
    public static IReadOnlyList<PaginationPageToken> BuildPageList(
        int totalPages,
        int currentPage,
        int siblingCount = 1,
        int boundaryCount = 1)
    {
        if (totalPages <= 0)
        {
            return Array.Empty<PaginationPageToken>();
        }

        siblingCount = Math.Max(0, siblingCount);
        boundaryCount = Math.Max(0, boundaryCount);
        currentPage = Math.Clamp(currentPage, 1, totalPages);

        // Small enough to just show every page: boundary+sibling windows plus the two ellipsis
        // slots would not save any space.
        var maxVisible = (2 * boundaryCount) + (2 * siblingCount) + 3;
        if (totalPages <= maxVisible)
        {
            var all = new List<PaginationPageToken>(totalPages);
            for (var i = 1; i <= totalPages; i++)
            {
                all.Add(PaginationPageToken.ForPage(i));
            }

            return all;
        }

        var pages = new SortedSet<int>();
        for (var i = 1; i <= boundaryCount; i++)
        {
            pages.Add(i);
        }

        for (var i = totalPages - boundaryCount + 1; i <= totalPages; i++)
        {
            pages.Add(i);
        }

        for (var i = currentPage - siblingCount; i <= currentPage + siblingCount; i++)
        {
            if (i >= 1 && i <= totalPages)
            {
                pages.Add(i);
            }
        }

        var result = new List<PaginationPageToken>();
        var previous = 0;
        foreach (var page in pages)
        {
            if (previous != 0 && page - previous > 1)
            {
                result.Add(PaginationPageToken.Ellipsis);
            }

            result.Add(PaginationPageToken.ForPage(page));
            previous = page;
        }

        return result;
    }
}
