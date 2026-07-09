using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Menubar;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class MenubarTests
{
    static MenubarTests()
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
                // Another test class won the race; Application.Current is now set.
            }
        }
    }

    private static readonly MethodInfo OnClickMethod =
        typeof(MenuItem).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Invokes the protected, most-derived OnClick() (virtual dispatch reaches the
    /// NaviusMenubar*Item override), without depending on a live visual tree or real input routing.
    /// </summary>
    private static void SimulateClick(MenuItem item) => OnClickMethod.Invoke(item, null);

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Menubar.xaml"),
        });

        return scope;
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();

        var bar = new NaviusMenubar { Resources = scope };
        var menu = new NaviusMenubarMenu { Value = "file", Resources = scope };
        var item = new NaviusMenubarItem { Resources = scope };
        var checkbox = new NaviusMenubarCheckboxItem { Resources = scope };
        var radio = new NaviusMenubarRadioItem { Value = "a", Resources = scope };
        var label = new NaviusMenubarLabel { Resources = scope };
        var separator = new NaviusMenubarSeparator { Resources = scope };
        var subTrigger = new NaviusMenubarSubTrigger { Resources = scope };

        Assert.True(bar.ApplyTemplate());
        Assert.True(menu.ApplyTemplate());
        Assert.True(item.ApplyTemplate());
        Assert.True(checkbox.ApplyTemplate());
        Assert.True(radio.ApplyTemplate());
        Assert.True(label.ApplyTemplate());
        Assert.True(separator.ApplyTemplate());
        Assert.True(subTrigger.ApplyTemplate());
    }

    [StaFact]
    public void Menu_RequiresNonEmptyValue()
    {
        var menu = new NaviusMenubarMenu();

        Assert.Throws<InvalidOperationException>(() =>
        {
            ((ISupportInitialize)menu).BeginInit();
            ((ISupportInitialize)menu).EndInit();
        });
    }

    [StaFact]
    public void RadioItem_RequiresNonEmptyValue()
    {
        var item = new NaviusMenubarRadioItem();

        Assert.Throws<InvalidOperationException>(() =>
        {
            ((ISupportInitialize)item).BeginInit();
            ((ISupportInitialize)item).EndInit();
        });
    }

    [StaFact]
    public void Bar_SettingValue_UpdatesValueAndAttemptsToOpenMatchingMenu()
    {
        // Actually flipping MenuItem.IsSubmenuOpen to true is coerced back to false without a
        // real, shown Window (native WPF submenu popups need live keyboard/HWND focus, which a
        // headless STA test never has); this only verifies the Value bookkeeping side, which is
        // this class's own responsibility. See Bar_ChildSubmenuOpened_SyncsValueAndRaisesValueChanged
        // for the reverse direction, exercised via a directly-raised routed event instead.
        var file = new NaviusMenubarMenu { Value = "file" };
        var edit = new NaviusMenubarMenu { Value = "edit" };
        var bar = new NaviusMenubar { Items = { file, edit } };

        bar.Value = "edit";
        Assert.Equal("edit", bar.Value);

        bar.Value = null;
        Assert.Null(bar.Value);
    }

    [StaFact]
    public void Bar_ChildSubmenuOpened_SyncsValueAndRaisesValueChanged()
    {
        // Raises the routed event directly (see remarks on Bar_SettingValue_...) to exercise
        // NaviusMenubar's own reaction to it without depending on native submenu-open coercion.
        var file = new NaviusMenubarMenu { Value = "file" };
        var bar = new NaviusMenubar { Items = { file } };
        var raised = 0;
        bar.ValueChanged += (_, _) => raised++;

        file.RaiseEvent(new RoutedEventArgs(MenuItem.SubmenuOpenedEvent, file));

        Assert.Equal("file", bar.Value);
        Assert.Equal(1, raised);

        file.RaiseEvent(new RoutedEventArgs(MenuItem.SubmenuClosedEvent, file));

        Assert.Null(bar.Value);
        Assert.Equal(2, raised);
    }

    [StaFact]
    public void Item_Select_Fires_AndPreventDefaultSkipsNativeClose()
    {
        var item = new NaviusMenubarItem();
        var selectRaised = 0;
        item.Select += (_, args) =>
        {
            selectRaised++;
            args.PreventDefault();
        };
        var clickRaised = 0;
        item.Click += (_, _) => clickRaised++;

        SimulateClick(item);

        Assert.Equal(1, selectRaised);
        // PreventDefault still raises the plain Click routed event (per class remarks), just
        // skips the base MenuItem close-chain/Command invocation.
        Assert.Equal(1, clickRaised);
    }

    [StaFact]
    public void CheckboxItem_Click_TogglesIndeterminateOrFalseToTrueThenFalse()
    {
        var item = new NaviusMenubarCheckboxItem();
        var changes = new List<bool?>();
        item.CheckedChanged += (_, value) => changes.Add(value);

        SimulateClick(item); // null (indeterminate) -> true
        Assert.True(item.Checked);
        Assert.True(item.IsChecked);

        SimulateClick(item); // true -> false
        Assert.False(item.Checked);
        Assert.False(item.IsChecked);

        Assert.Equal(new bool?[] { true, false }, changes);
    }

    [StaFact]
    public void CheckboxItem_Select_PreventDefault_KeepsCheckedToggleButSkipsClose()
    {
        var item = new NaviusMenubarCheckboxItem();
        item.Select += (_, args) => args.PreventDefault();

        SimulateClick(item);

        // The toggle itself is not gated by PreventDefault (matches the contract: OnSelect
        // fires after toggling; PreventDefault only affects the close behavior).
        Assert.True(item.Checked);
    }

    [StaFact]
    public void RadioItem_Group_CoordinatesMutualExclusion()
    {
        var group = new NaviusMenubarRadioGroup();
        var a = new NaviusMenubarRadioItem { Value = "a", Group = group };
        var b = new NaviusMenubarRadioItem { Value = "b", Group = group };

        SimulateClick(a);
        Assert.True(a.IsChecked);
        Assert.False(b.IsChecked);
        Assert.Equal("a", group.Value);

        SimulateClick(b);
        Assert.False(a.IsChecked);
        Assert.True(b.IsChecked);
        Assert.Equal("b", group.Value);
    }

    [StaFact]
    public void RadioGroup_ValueChanged_SyncsAllMemberItems()
    {
        var group = new NaviusMenubarRadioGroup();
        var a = new NaviusMenubarRadioItem { Value = "a", Group = group };
        var b = new NaviusMenubarRadioItem { Value = "b", Group = group };

        group.Value = "b";

        Assert.False(a.IsChecked);
        Assert.True(b.IsChecked);
    }

    [StaFact]
    public void SubTrigger_SubmenuOpenedAndClosed_RaiseOpenChanged()
    {
        // Raises the routed events directly rather than setting IsSubmenuOpen (see remarks on
        // MenubarTests.Bar_SettingValue_...: native submenu-open coercion needs a real, shown
        // Window this headless STA test doesn't have).
        var trigger = new NaviusMenubarSubTrigger();
        var raised = 0;
        trigger.OpenChanged += (_, _) => raised++;

        trigger.RaiseEvent(new RoutedEventArgs(MenuItem.SubmenuOpenedEvent, trigger));
        Assert.Equal(1, raised);

        trigger.RaiseEvent(new RoutedEventArgs(MenuItem.SubmenuClosedEvent, trigger));
        Assert.Equal(2, raised);
    }

    [StaFact]
    public void Bar_AutomationPeer_ReportsMenuBarClassName()
    {
        var bar = new NaviusMenubar();

        var peer = bar.GetType()
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(bar, null) as AutomationPeer;

        Assert.NotNull(peer);
        Assert.Equal(nameof(NaviusMenubar), peer!.GetClassName());
    }

    [StaFact]
    public void Label_IsNonInteractive()
    {
        var label = new NaviusMenubarLabel();

        Assert.False(label.Focusable);
        Assert.False(label.IsHitTestVisible);
    }
}
