using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Controls;

namespace Navius.Wpf.Tests;

public class ScrollAreaTests
{
    static ScrollAreaTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check) because xunit runs test classes in
        // parallel on separate STA threads: another test class's static ctor can win the race.
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

    private static void Invoke(string methodName, NaviusScrollArea target, params object?[] args)
    {
        var method = typeof(NaviusScrollArea).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(target, args);
    }

    private static DispatcherTimer GetHideTimer(NaviusScrollArea target)
    {
        var field = typeof(NaviusScrollArea).GetField("_scrollHideTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (DispatcherTimer)field.GetValue(target)!;
    }

    [StaFact]
    public void Defaults_MatchTheScrollAreaContract()
    {
        var scrollArea = new NaviusScrollArea();

        Assert.False(scrollArea.IsHovering);
        Assert.False(scrollArea.IsScrolling);
        Assert.Equal(600, scrollArea.ScrollHideDelay);
    }

    [StaFact]
    public void MouseEnter_SetsIsHovering()
    {
        var scrollArea = new NaviusScrollArea();

        Invoke("OnMouseEnter", scrollArea, new MouseEventArgs(Mouse.PrimaryDevice, 0));

        Assert.True(scrollArea.IsHovering);
    }

    [StaFact]
    public void MouseLeave_ClearsIsHovering()
    {
        var scrollArea = new NaviusScrollArea();
        Invoke("OnMouseEnter", scrollArea, new MouseEventArgs(Mouse.PrimaryDevice, 0));

        Invoke("OnMouseLeave", scrollArea, new MouseEventArgs(Mouse.PrimaryDevice, 0));

        Assert.False(scrollArea.IsHovering);
    }

    [StaFact]
    public void ScrollActivity_WithVerticalChange_SetsIsScrolling()
    {
        var scrollArea = new NaviusScrollArea();

        Invoke("HandleScrollActivity", scrollArea, 10d, 0d);

        Assert.True(scrollArea.IsScrolling);
    }

    [StaFact]
    public void ScrollActivity_WithHorizontalChange_SetsIsScrolling()
    {
        var scrollArea = new NaviusScrollArea();

        Invoke("HandleScrollActivity", scrollArea, 0d, 10d);

        Assert.True(scrollArea.IsScrolling);
    }

    [StaFact]
    public void ScrollActivity_WithNoChange_DoesNotSetIsScrolling()
    {
        var scrollArea = new NaviusScrollArea();

        Invoke("HandleScrollActivity", scrollArea, 0d, 0d);

        Assert.False(scrollArea.IsScrolling);
    }

    [StaFact]
    public void ScrollActivity_StartsHideTimer_WithConfiguredDelay()
    {
        var scrollArea = new NaviusScrollArea { ScrollHideDelay = 250 };

        Invoke("HandleScrollActivity", scrollArea, 5d, 0d);

        var timer = GetHideTimer(scrollArea);
        Assert.True(timer.IsEnabled);
        Assert.Equal(TimeSpan.FromMilliseconds(250), timer.Interval);
    }

    [StaFact]
    public void HideTimerTick_ClearsIsScrolling()
    {
        var scrollArea = new NaviusScrollArea();
        Invoke("HandleScrollActivity", scrollArea, 5d, 0d);
        Assert.True(scrollArea.IsScrolling);

        Invoke("OnScrollHideTimerTick", scrollArea, null, EventArgs.Empty);

        Assert.False(scrollArea.IsScrolling);
        Assert.False(GetHideTimer(scrollArea).IsEnabled);
    }

    [StaFact]
    public void Template_AppliesAndExposesScrollBarAndCornerParts()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Navius.Wpf.Primitives;component/Themes/ScrollArea.xaml",
                UriKind.Absolute),
        };
        Application.Current.Resources.MergedDictionaries.Add(dictionary);

        try
        {
            var scrollArea = new NaviusScrollArea();
            // Elements outside a live visual/logical tree don't automatically pick up an implicit
            // (TargetType-keyed) style; wire it explicitly, same as WPF does internally once an
            // element is parented.
            scrollArea.SetResourceReference(FrameworkElement.StyleProperty, typeof(NaviusScrollArea));
            scrollArea.ApplyTemplate();

            Assert.NotNull(scrollArea.Template);
            Assert.NotNull(scrollArea.Template.FindName("PART_ScrollContentPresenter", scrollArea));
            Assert.IsType<ScrollBar>(scrollArea.Template.FindName("PART_VerticalScrollBar", scrollArea));
            Assert.IsType<ScrollBar>(scrollArea.Template.FindName("PART_HorizontalScrollBar", scrollArea));
            Assert.IsType<Border>(scrollArea.Template.FindName("PART_Corner", scrollArea));
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }
}
