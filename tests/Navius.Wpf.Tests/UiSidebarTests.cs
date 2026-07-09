using System.Windows.Input;
using Navius.Wpf.Ui.Sidebar;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiSidebarTests
{
    [Theory]
    [InlineData(-1, 5, Key.Down, 0)]
    [InlineData(0, 5, Key.Down, 1)]
    [InlineData(4, 5, Key.Down, 4)] // no wrap past the last item
    [InlineData(-1, 5, Key.Up, 4)]
    [InlineData(2, 5, Key.Up, 1)]
    [InlineData(0, 5, Key.Up, 0)] // no wrap past the first item
    [InlineData(3, 5, Key.Home, 0)]
    [InlineData(1, 5, Key.End, 4)]
    public void MoveFocus_ComputesExpectedIndex(int current, int count, Key key, int expected)
    {
        Assert.Equal(expected, SidebarNavigation.MoveFocus(current, count, key));
    }

    [Fact]
    public void MoveFocus_NoItems_ReturnsNegativeOne()
    {
        Assert.Equal(-1, SidebarNavigation.MoveFocus(-1, 0, Key.Down));
    }

    [Fact]
    public void MoveFocus_UnhandledKey_ReturnsNegativeOne()
    {
        Assert.Equal(-1, SidebarNavigation.MoveFocus(0, 5, Key.Space));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ToggleCollapsed_FlipsState(bool current, bool expected)
    {
        Assert.Equal(expected, SidebarNavigation.ToggleCollapsed(current));
    }

    [StaFact]
    public void NaviusSidebar_Defaults_AreExpanded()
    {
        var sidebar = new NaviusSidebar();

        Assert.False(sidebar.IsCollapsed);
        Assert.Equal(240d, sidebar.ExpandedWidth);
        Assert.Equal(64d, sidebar.CollapsedWidth);
    }

    [StaFact]
    public void NaviusSidebar_ToggleCollapsedCommand_FlipsIsCollapsed()
    {
        var sidebar = new NaviusSidebar();

        NaviusSidebar.ToggleCollapsedCommand.Execute(null, sidebar);
        Assert.True(sidebar.IsCollapsed);

        NaviusSidebar.ToggleCollapsedCommand.Execute(null, sidebar);
        Assert.False(sidebar.IsCollapsed);
    }

    [StaFact]
    public void NaviusSidebarItem_IsActive_SetsItemStatusAutomationProperty()
    {
        var item = new NaviusSidebarItem { IsActive = true };

        Assert.Equal("current", System.Windows.Automation.AutomationProperties.GetItemStatus(item));

        item.IsActive = false;

        Assert.Equal(string.Empty, System.Windows.Automation.AutomationProperties.GetItemStatus(item));
    }
}
