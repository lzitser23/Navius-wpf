using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Menus;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class MenuTests : IDisposable
{
    // Flushes any Dispatcher-deferred native-window teardown left by a popup this test closed
    // (see TestCleanup.PumpDispatcher) before this test's dedicated STA thread exits.
    public void Dispose() => TestCleanup.PumpDispatcher();

    static MenuTests()
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

    private static readonly MethodInfo OnClickMethod =
        typeof(MenuItem).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo OnToggleMethod =
        typeof(System.Windows.Controls.Primitives.ToggleButton).GetMethod("OnToggle", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>Invokes the protected, most-derived MenuItem.OnClick(), just like a real click or Enter/Space.</summary>
    private static void SimulateClick(MenuItem item) => OnClickMethod.Invoke(item, null);

    /// <summary>Invokes the protected, most-derived ToggleButton.OnToggle(), just like a real click.</summary>
    private static void SimulateToggle(NaviusMenuTrigger trigger) => OnToggleMethod.Invoke(trigger, null);

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Menu.xaml"),
        });

        return scope;
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var trigger = new NaviusMenuTrigger { Resources = scope };
        var popup = new NaviusMenuPopup { Resources = scope };
        var item = new NaviusMenuItem { Resources = scope };
        var checkboxItem = new NaviusMenuCheckboxItem { Resources = scope };
        var radioItem = new NaviusMenuRadioItem { Resources = scope };
        var groupLabel = new NaviusMenuGroupLabel { Resources = scope };

        Assert.True(trigger.ApplyTemplate());
        Assert.True(popup.ApplyTemplate());
        Assert.True(item.ApplyTemplate());
        Assert.True(checkboxItem.ApplyTemplate());
        Assert.True(radioItem.ApplyTemplate());
        Assert.True(groupLabel.ApplyTemplate());
    }

    [StaFact]
    public void Trigger_ContentAlignment_ExplicitLeft_ForwardsToContentPresenter()
    {
        // Regression: this is the exact scenario a consumer sidebar hit -- setting
        // HorizontalContentAlignment="Left" on a NaviusMenuTrigger (label left, chevron/count
        // right in a row layout) did nothing because the template hardcoded Center.
        var content = new Border { Width = 20, Height = 10 };
        var trigger = new NaviusMenuTrigger
        {
            Content = content,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Width = 200,
            Height = 40,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Resources = CreateThemedScope(),
        };

        trigger.ApplyTemplate();
        trigger.Measure(new Size(200, 40));
        trigger.Arrange(new Rect(0, 0, 200, 40));

        var offset = content.TranslatePoint(new Point(0, 0), trigger);
        Assert.Equal(0, offset.X, 3);
    }

    [StaFact]
    public void Trigger_Toggle_OpensAssociatedMenu_AndSetsPlacementTarget()
    {
        var trigger = new NaviusMenuTrigger();
        var menu = new NaviusMenuPopup();
        trigger.Menu = menu;

        try
        {
            SimulateToggle(trigger);

            Assert.True(trigger.IsChecked);
            Assert.True(menu.IsOpen);
            Assert.Same(trigger, menu.PlacementTarget);
        }
        finally
        {
            menu.IsOpen = false;
        }
    }

    [StaFact]
    public void Trigger_ToggleTwice_ClosesAssociatedMenu()
    {
        var trigger = new NaviusMenuTrigger();
        var menu = new NaviusMenuPopup();
        trigger.Menu = menu;

        SimulateToggle(trigger);
        SimulateToggle(trigger);

        Assert.False(trigger.IsChecked);
        Assert.False(menu.IsOpen);
    }

    [StaFact]
    public void Trigger_MenuClosedExternally_ResetsIsCheckedToFalse()
    {
        var trigger = new NaviusMenuTrigger();
        var menu = new NaviusMenuPopup();
        trigger.Menu = menu;
        SimulateToggle(trigger);

        try
        {
            // Simulate a dismissal that didn't go through the trigger (Escape/outside click):
            // ContextMenu.Closed fires for every close path, so the trigger's own listener
            // should resync IsChecked without us calling SimulateToggle again.
            menu.RaiseEvent(new RoutedEventArgs(ContextMenu.ClosedEvent, menu));

            Assert.False(trigger.IsChecked);
        }
        finally
        {
            // The synthetic RaiseEvent above only simulates the Closed notification; it does not
            // flip the underlying native Popup's IsOpen, so close it for real here.
            menu.IsOpen = false;
        }
    }

    [StaFact]
    public void Item_Click_RaisesSelect_AndClosesOwningMenu()
    {
        var popup = new NaviusMenuPopup();
        var item = new NaviusMenuItem();
        popup.Items.Add(item);
        popup.IsOpen = true;

        var raised = 0;
        item.Select += (_, _) => raised++;

        SimulateClick(item);

        Assert.Equal(1, raised);
        Assert.False(popup.IsOpen);
    }

    [StaFact]
    public void Item_Click_PreventDefault_KeepsMenuOpen()
    {
        var popup = new NaviusMenuPopup();
        var item = new NaviusMenuItem();
        popup.Items.Add(item);
        popup.IsOpen = true;
        item.Select += (sender, e) => ((NaviusSelectEventArgs)e).PreventDefault();

        try
        {
            SimulateClick(item);

            Assert.True(popup.IsOpen);
        }
        finally
        {
            popup.IsOpen = false;
        }
    }

    [StaFact]
    public void Item_WithChildren_Click_OpensSubmenu_DoesNotCloseOwningMenu()
    {
        var popup = new NaviusMenuPopup();
        var header = new NaviusMenuItem();
        header.Items.Add(new NaviusMenuItem());
        popup.Items.Add(header);
        popup.IsOpen = true;

        try
        {
            var raised = 0;
            header.Select += (_, _) => raised++;

            SimulateClick(header);

            Assert.Equal(0, raised);
            Assert.True(popup.IsOpen);
        }
        finally
        {
            popup.IsOpen = false;
        }
    }

    [StaFact]
    public void Item_Click_ExecutesBoundCommand()
    {
        var executed = 0;
        var command = new RoutedCommand();
        var target = new NaviusMenuItem();
        var binding = new CommandBinding(command, (_, _) => executed++, (_, e) => e.CanExecute = true);
        target.CommandBindings.Add(binding);
        target.Command = command;

        SimulateClick(target);

        Assert.Equal(1, executed);
    }

    [StaFact]
    public void TextValue_SetsNativeTextSearchText()
    {
        var item = new NaviusMenuItem { TextValue = "Alpha" };

        Assert.Equal("Alpha", TextSearch.GetText(item));
    }

    [StaFact]
    public void CheckboxItem_Click_TogglesBetweenCheckedAndUnchecked()
    {
        var item = new NaviusMenuCheckboxItem();

        SimulateClick(item);
        Assert.True(item.Checked);
        Assert.True(item.IsChecked);

        SimulateClick(item);
        Assert.False(item.Checked);
        Assert.False(item.IsChecked);
    }

    [StaFact]
    public void CheckboxItem_Click_FromIndeterminate_GoesToChecked()
    {
        var item = new NaviusMenuCheckboxItem { Checked = null };
        Assert.True(item.IsIndeterminate);

        SimulateClick(item);

        Assert.True(item.Checked);
        Assert.False(item.IsIndeterminate);
    }

    [StaFact]
    public void CheckboxItem_CheckedChanged_EventFiresOnEveryChange()
    {
        var item = new NaviusMenuCheckboxItem();
        var raised = 0;
        item.CheckedChanged += (_, _) => raised++;

        item.Checked = true;
        item.Checked = null;

        Assert.Equal(2, raised);
    }

    [StaFact]
    public void RadioItem_Click_EnforcesSingleSelectionWithinGroup()
    {
        var popup = new NaviusMenuPopup();
        var a = new NaviusMenuRadioItem { Value = "a", GroupName = "g" };
        var b = new NaviusMenuRadioItem { Value = "b", GroupName = "g" };
        popup.Items.Add(a);
        popup.Items.Add(b);

        SimulateClick(a);
        Assert.True(a.IsChecked);
        Assert.False(b.IsChecked);

        SimulateClick(b);
        Assert.False(a.IsChecked);
        Assert.True(b.IsChecked);
    }

    [StaFact]
    public void RadioItem_Click_IgnoresSiblingsInDifferentGroup()
    {
        var popup = new NaviusMenuPopup();
        var a = new NaviusMenuRadioItem { Value = "a", GroupName = "left" };
        var b = new NaviusMenuRadioItem { Value = "b", GroupName = "right" };
        popup.Items.Add(a);
        popup.Items.Add(b);

        SimulateClick(a);
        SimulateClick(b);

        Assert.True(a.IsChecked);
        Assert.True(b.IsChecked);
    }

    [StaFact]
    public void RadioItem_Click_AlreadyChecked_StaysCheckedAndStillClosesMenu()
    {
        var popup = new NaviusMenuPopup();
        var a = new NaviusMenuRadioItem { Value = "a", GroupName = "g" };
        popup.Items.Add(a);
        popup.IsOpen = true;
        SimulateClick(a);
        popup.IsOpen = true;

        SimulateClick(a);

        Assert.True(a.IsChecked);
        Assert.False(popup.IsOpen);
    }

    [StaFact]
    public void Disabled_ViaIsEnabled_IsInherited()
    {
        var item = new NaviusMenuItem();
        _ = new StackPanel { IsEnabled = false, Children = { item } };

        Assert.False(item.IsEnabled);
    }

    [StaFact]
    public void AutomationPeer_CheckboxItem_ReportsToggleProvider()
    {
        var item = new NaviusMenuCheckboxItem { Checked = true };

        var peer = new MenuItemAutomationPeer(item);
        var toggleProvider = Assert.IsAssignableFrom<IToggleProvider>(peer);

        Assert.Equal(ToggleState.On, toggleProvider.ToggleState);
    }
}
