using System.Linq;
using Navius.Wpf.Ui.Pagination;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiPaginationTests
{
    [Fact]
    public void BuildPageList_FewerPagesThanWindow_ShowsEveryPageNoEllipsis()
    {
        var pages = PaginationEngine.BuildPageList(totalPages: 5, currentPage: 1);

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, pages.Select(p => p.Page));
        Assert.DoesNotContain(pages, p => p.IsEllipsis);
    }

    [Fact]
    public void BuildPageList_CurrentPageInMiddle_CollapsesBothSides()
    {
        var pages = PaginationEngine.BuildPageList(totalPages: 20, currentPage: 10, siblingCount: 1, boundaryCount: 1);

        // boundary(1) + ellipsis + siblings(9,10,11) + ellipsis + boundary(20)
        Assert.Equal("1", pages[0].ToString());
        Assert.True(pages[1].IsEllipsis);
        Assert.Equal(new[] { 9, 10, 11 }, pages.Where(p => !p.IsEllipsis).Skip(1).Take(3).Select(p => p.Page));
        Assert.True(pages[^2].IsEllipsis);
        Assert.Equal("20", pages[^1].ToString());
    }

    [Fact]
    public void BuildPageList_CurrentPageNearStart_OnlyCollapsesTrailingSide()
    {
        var pages = PaginationEngine.BuildPageList(totalPages: 20, currentPage: 1, siblingCount: 1, boundaryCount: 1);

        Assert.False(pages[0].IsEllipsis);
        Assert.True(pages.Last().IsEllipsis == false); // last is the boundary page, not ellipsis
        Assert.Single(pages, p => p.IsEllipsis);
    }

    [Fact]
    public void BuildPageList_CurrentPageClampedIntoRange()
    {
        var pages = PaginationEngine.BuildPageList(totalPages: 5, currentPage: 999);

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, pages.Select(p => p.Page));
    }

    [Fact]
    public void BuildPageList_ZeroTotalPages_ReturnsEmpty()
    {
        var pages = PaginationEngine.BuildPageList(totalPages: 0, currentPage: 1);

        Assert.Empty(pages);
    }

    [StaFact]
    public void NaviusPagination_PreviousNext_ClampAtBounds()
    {
        var pagination = new NaviusPagination { TotalPages = 3, CurrentPage = 1 };

        Assert.False(NaviusPagination.PreviousCommand.CanExecute(null, pagination));
        Assert.True(NaviusPagination.NextCommand.CanExecute(null, pagination));

        pagination.CurrentPage = 3;

        Assert.True(NaviusPagination.PreviousCommand.CanExecute(null, pagination));
        Assert.False(NaviusPagination.NextCommand.CanExecute(null, pagination));
    }

    [StaFact]
    public void NaviusPagination_GoToPage_ClampsIntoValidRange()
    {
        var pagination = new NaviusPagination { TotalPages = 5, CurrentPage = 1 };

        NaviusPagination.GoToPageCommand.Execute(99, pagination);

        Assert.Equal(5, pagination.CurrentPage);
    }
}
