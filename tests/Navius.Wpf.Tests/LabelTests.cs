using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class LabelTests
{
    static LabelTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static readonly MethodInfo OnMouseDownMethod =
        typeof(NaviusLabel).GetMethod("OnMouseDown", BindingFlags.NonPublic | BindingFlags.Instance)!;

    // MouseButtonEventArgs.ClickCount has only an internal setter; reflection reaches it
    // without needing InternalsVisibleTo, so a real double/triple click can be simulated.
    private static readonly PropertyInfo ClickCountProperty =
        typeof(MouseButtonEventArgs).GetProperty("ClickCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static MouseButtonEventArgs MakeMouseDown(int clickCount)
    {
        var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseDownEvent };
        ClickCountProperty.SetValue(args, clickCount);
        return args;
    }

    private static void SimulateMouseDown(NaviusLabel label, MouseButtonEventArgs args) =>
        OnMouseDownMethod.Invoke(label, new object[] { args });

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Label.xaml"),
        });

        var label = new NaviusLabel { Resources = scope };

        Assert.True(label.ApplyTemplate());
    }

    [StaFact]
    public void DefaultState_HasNoFor()
    {
        var label = new NaviusLabel();

        Assert.Null(label.For);
        Assert.Null(label.Target);
    }

    [StaFact]
    public void For_ResolvesTargetByNameAndSetsLabeledBy()
    {
        var root = new StackPanel();
        NameScope.SetNameScope(root, new NameScope());
        var target = new TextBox();
        root.RegisterName("TargetBox", target);
        root.Children.Add(target);

        var label = new NaviusLabel();
        root.Children.Add(label);

        label.For = "TargetBox";

        Assert.Same(target, label.Target);
        Assert.Same(label, AutomationProperties.GetLabeledBy(target));
    }

    [StaFact]
    public void For_Changed_RewiresTargetAndClearsOldAssociation()
    {
        var root = new StackPanel();
        NameScope.SetNameScope(root, new NameScope());
        var first = new TextBox();
        var second = new TextBox();
        root.RegisterName("FirstBox", first);
        root.RegisterName("SecondBox", second);
        root.Children.Add(first);
        root.Children.Add(second);
        var label = new NaviusLabel();
        root.Children.Add(label);

        label.For = "FirstBox";
        label.For = "SecondBox";

        Assert.Same(second, label.Target);
        Assert.Null(AutomationProperties.GetLabeledBy(first));
        Assert.Same(label, AutomationProperties.GetLabeledBy(second));
    }

    [StaFact]
    public void For_Cleared_ClearsTargetAndAssociation()
    {
        var root = new StackPanel();
        NameScope.SetNameScope(root, new NameScope());
        var target = new TextBox();
        root.RegisterName("TargetBox", target);
        root.Children.Add(target);
        var label = new NaviusLabel();
        root.Children.Add(label);

        label.For = "TargetBox";
        label.For = null;

        Assert.Null(label.Target);
        Assert.Null(AutomationProperties.GetLabeledBy(target));
    }

    [StaFact]
    public void MouseDown_SingleClick_DoesNotSuppressDefault()
    {
        var label = new NaviusLabel();

        var args = MakeMouseDown(clickCount: 1);
        SimulateMouseDown(label, args);

        Assert.False(args.Handled);
    }

    [StaFact]
    public void MouseDown_MultiClick_SuppressesDefault_ToPreventTextSelection()
    {
        var label = new NaviusLabel();

        var args = MakeMouseDown(clickCount: 2);
        SimulateMouseDown(label, args);

        Assert.True(args.Handled);
    }
}
