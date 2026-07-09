using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.ContextMenu;
using Navius.Wpf.Primitives.Controls.Menus;
using Navius.Wpf.Primitives.Positioning;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class ContextMenuTests
{
    static ContextMenuTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/ContextMenu.xaml"),
        });

        return scope;
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var trigger = new NaviusContextMenuTrigger { Resources = scope };
        var popup = new NaviusContextMenuPopup { Resources = scope };

        Assert.True(trigger.ApplyTemplate());
        Assert.True(popup.ApplyTemplate());
    }

    [StaFact]
    public void ApplyTemplate_ReusedMenuItemStack_Succeeds()
    {
        // The ContextMenu family reuses the Menu family's item classes verbatim; confirm
        // they template correctly once Themes/ContextMenu.xaml (which merges Themes/Menu.xaml)
        // is the only dictionary in scope.
        var scope = CreateThemedScope();
        var item = new NaviusMenuItem { Resources = scope };
        var checkboxItem = new NaviusMenuCheckboxItem { Resources = scope };
        var radioItem = new NaviusMenuRadioItem { Resources = scope };

        Assert.True(item.ApplyTemplate());
        Assert.True(checkboxItem.ApplyTemplate());
        Assert.True(radioItem.ApplyTemplate());
    }

    [StaFact]
    public void Menu_Set_AssignsNativeContextMenu()
    {
        var trigger = new NaviusContextMenuTrigger();
        var menu = new NaviusContextMenuPopup();

        trigger.Menu = menu;

        Assert.Same(menu, trigger.ContextMenu);
    }

    [StaFact]
    public void Disabled_SuppressesContextMenuServiceForTrigger()
    {
        var trigger = new NaviusContextMenuTrigger();

        trigger.Disabled = true;

        Assert.False(ContextMenuService.GetIsEnabled(trigger));
    }

    [StaFact]
    public void Enabled_ByDefault_ContextMenuServiceIsEnabled()
    {
        var trigger = new NaviusContextMenuTrigger();

        Assert.True(ContextMenuService.GetIsEnabled(trigger));
    }

    [StaFact]
    public void Side_Align_DefaultToRightStart()
    {
        var popup = new NaviusContextMenuPopup();

        Assert.Equal(PlacementSide.Right, popup.Side);
        Assert.Equal(PlacementAlign.Start, popup.Align);
    }
}
