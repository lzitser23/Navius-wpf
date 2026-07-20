using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls.Checkbox;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class CheckboxTests
{
    static CheckboxTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static readonly MethodInfo OnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Invokes the protected, most-derived OnClick() (virtual dispatch reaches
    /// ToggleButton.OnClick -> NaviusCheckbox.OnToggle, just like a real click), without
    /// depending on a live visual tree or real input routing.
    /// </summary>
    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Checkbox.xaml"),
        });

        return scope;
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var checkbox = new NaviusCheckbox { Resources = scope };
        var indicator = new NaviusCheckboxIndicator { Resources = scope };
        var group = new NaviusCheckboxGroup { Resources = scope };

        Assert.True(checkbox.ApplyTemplate());
        Assert.True(indicator.ApplyTemplate());
        Assert.True(group.ApplyTemplate());
    }

    [StaFact]
    public void DefaultTemplate_UsesSeparateContentAndIndicatorForegrounds()
    {
        var scope = CreateThemedScope();
        var checkbox = new NaviusCheckbox { Content = "Label", IsChecked = true, Resources = scope };

        Assert.True(checkbox.ApplyTemplate());

        var panel = Assert.IsType<StackPanel>(System.Windows.Media.VisualTreeHelper.GetChild(checkbox, 0));
        var box = Assert.IsType<Border>(panel.Children[0]);
        var indicator = Assert.IsType<NaviusCheckboxIndicator>(box.Child);
        Assert.Equal(checkbox.FindResource("Navius.Foreground"), checkbox.Foreground);
        Assert.Equal(checkbox.FindResource("Navius.PrimaryForeground"), indicator.Foreground);
    }

    [StaFact]
    public void DefaultState_IsUncheckedAndThreeState()
    {
        var checkbox = new NaviusCheckbox();

        Assert.False(checkbox.IsChecked);
        Assert.True(checkbox.IsThreeState);
    }

    [StaFact]
    public void Click_TogglesBetweenCheckedAndUnchecked()
    {
        var checkbox = new NaviusCheckbox();

        SimulateClick(checkbox);
        Assert.True(checkbox.IsChecked);

        SimulateClick(checkbox);
        Assert.False(checkbox.IsChecked);
    }

    [StaFact]
    public void Click_FromIndeterminate_GoesToChecked()
    {
        var checkbox = new NaviusCheckbox { IsChecked = null };

        SimulateClick(checkbox);

        Assert.True(checkbox.IsChecked);
    }

    [StaFact]
    public void ReadOnly_BlocksClickButStaysFocusable()
    {
        var checkbox = new NaviusCheckbox { ReadOnly = true };

        SimulateClick(checkbox);

        Assert.False(checkbox.IsChecked);
        Assert.True(checkbox.Focusable);
    }

    [StaFact]
    public void Disabled_CascadesFromAncestor()
    {
        var checkbox = new NaviusCheckbox();

        // Disabled maps onto the inherited IsEnabled (no custom NaviusCheckbox logic,
        // beyond ReadOnly); WPF's own IsEnabled property-value inheritance is what
        // blocks a descendant once an ancestor (e.g. a NaviusCheckboxGroup) is disabled.
        _ = new StackPanel { IsEnabled = false, Children = { checkbox } };

        Assert.False(checkbox.IsEnabled);
    }

    [StaFact]
    public void AutomationPeer_ReportsIndeterminateForNullChecked()
    {
        var checkbox = new NaviusCheckbox { IsChecked = null };

        var peer = new CheckBoxAutomationPeer(checkbox);

        Assert.Equal(ToggleState.Indeterminate, ((IToggleProvider)peer).ToggleState);
    }

    [StaFact]
    public void Group_CheckingChild_UpdatesValueAndRaisesEvent()
    {
        var a = new NaviusCheckbox { GroupValue = "a" };
        var b = new NaviusCheckbox { GroupValue = "b" };
        var group = new NaviusCheckboxGroup
        {
            AllValues = new[] { "a", "b" },
            Content = new StackPanel { Children = { a, b } },
        };

        var raised = 0;
        group.ValueChanged += (_, _) => raised++;

        SimulateClick(a);

        Assert.Equal(new[] { "a" }, group.Value);
        Assert.Equal(1, raised);
    }

    [StaFact]
    public void Group_SelectAll_ChecksAllChildrenAndRollsUp()
    {
        var a = new NaviusCheckbox { GroupValue = "a" };
        var b = new NaviusCheckbox { GroupValue = "b" };
        var selectAll = new NaviusCheckbox { IsSelectAll = true };
        var group = new NaviusCheckboxGroup
        {
            AllValues = new[] { "a", "b" },
            Content = new StackPanel { Children = { selectAll, a, b } },
        };

        SimulateClick(selectAll);

        Assert.True(a.IsChecked);
        Assert.True(b.IsChecked);
        Assert.Equal(new[] { "a", "b" }, group.Value.OrderBy(v => v));

        SimulateClick(a);

        Assert.False(a.IsChecked);
        Assert.Null(selectAll.IsChecked);
    }
}
