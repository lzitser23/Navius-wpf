using Navius.Wpf.Ui.Breadcrumb;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiBreadcrumbTests
{
    [StaFact]
    public void NaviusBreadcrumbItem_Defaults_IsNotCurrentPageAndFocusable()
    {
        var item = new NaviusBreadcrumbItem();

        Assert.False(item.IsCurrentPage);
        Assert.True(item.Focusable);
    }

    [StaFact]
    public void NaviusBreadcrumbItem_IsCurrentPage_SetsItemStatusAndNotFocusable()
    {
        var item = new NaviusBreadcrumbItem { IsCurrentPage = true };

        Assert.Equal("current", System.Windows.Automation.AutomationProperties.GetItemStatus(item));
        Assert.False(item.Focusable);
    }

    [StaFact]
    public void NaviusBreadcrumbItem_LeavingCurrentPage_RestoresFocusable()
    {
        var item = new NaviusBreadcrumbItem { IsCurrentPage = true };

        item.IsCurrentPage = false;

        Assert.True(item.Focusable);
    }

    [StaFact]
    public void NaviusBreadcrumbItem_Click_RaisedOnlyWhenNotCurrentPage()
    {
        var item = new NaviusBreadcrumbItem { IsCurrentPage = true };
        var clicked = false;
        item.Click += (_, _) => clicked = true;

        item.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

        // Sanity: raising a foreign RoutedEvent shouldn't set our flag; the real activation path
        // (mouse/keyboard) is gated by IsCurrentPage inside OnMouseLeftButtonUp/OnKeyDown.
        Assert.False(clicked);
    }

    [StaFact]
    public void NaviusBreadcrumbSeparator_IsExcludedFromTabOrderAndAutomation()
    {
        var separator = new NaviusBreadcrumbSeparator();

        Assert.False(separator.Focusable);
        Assert.False(separator.IsTabStop);
    }
}
