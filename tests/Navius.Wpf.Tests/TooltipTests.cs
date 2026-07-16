using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class TooltipTests : IDisposable
{
    static TooltipTests()
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

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tooltip.xaml"),
        });

        return scope;
    }

    /// <summary>Builds and templates a tooltip parented to a (never-shown) Window, so OpenCore's Window.GetWindow lookup succeeds.</summary>
    private static NaviusTooltip CreateAppliedTooltip()
    {
        var tooltip = new NaviusTooltip { Resources = CreateThemedScope(), Content = new TextBlock { Text = "Trigger" } };
        _ = new Window { Content = tooltip };
        tooltip.ApplyTemplate();
        return tooltip;
    }

    private static void Invoke(string methodName, NaviusTooltip target, params object?[] args)
    {
        var method = typeof(NaviusTooltip).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(target, args);
    }

    // Lazily created (not a static field initializer) and disposed per test instance -- this
    // dummy 0x0 native window must not outlive the STA thread it was created on.
    private System.Windows.Interop.HwndSource? _testSource;

    public void Dispose()
    {
        _testSource?.Dispose();
        TestCleanup.PumpDispatcher();
    }

    [StaFact]
    public void Defaults_MatchTheTooltipContract()
    {
        var tooltip = new NaviusTooltip();

        Assert.Equal(PlacementSide.Top, tooltip.Side);
        Assert.Equal(PlacementAlign.Center, tooltip.Align);
        Assert.False(tooltip.IsOpen);
        Assert.False(tooltip.DisableHoverableContent);
        Assert.Null(tooltip.DelayDuration);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var tooltip = new NaviusTooltip { Resources = CreateThemedScope() };

        Assert.True(tooltip.ApplyTemplate());
    }

    [StaFact]
    public void ContentAlignment_StretchesTriggerContent()
    {
        var content = new Border();
        var tooltip = new NaviusTooltip
        {
            Resources = CreateThemedScope(),
            Content = content,
            Width = 240,
            Height = 80,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };

        tooltip.ApplyTemplate();
        tooltip.Measure(new Size(240, 80));
        tooltip.Arrange(new Rect(0, 0, 240, 80));

        Assert.Equal(240, content.ActualWidth);
        Assert.Equal(80, content.ActualHeight);
    }

    [StaFact]
    public void DefaultContentAlignment_PreservesStretchLayout()
    {
        var content = new Border();
        var tooltip = new NaviusTooltip
        {
            Resources = CreateThemedScope(),
            Content = content,
            Width = 240,
            Height = 80
        };

        tooltip.ApplyTemplate();
        tooltip.Measure(new Size(240, 80));
        tooltip.Arrange(new Rect(0, 0, 240, 80));

        Assert.Equal(240, content.ActualWidth);
        Assert.Equal(80, content.ActualHeight);
    }

    [StaFact]
    public void GotKeyboardFocus_OpensImmediatelyAndMarksInstant()
    {
        var tooltip = CreateAppliedTooltip();

        try
        {
            Invoke("OnTriggerGotKeyboardFocus", tooltip, tooltip, new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, tooltip));

            Assert.True(tooltip.IsOpen);
            Assert.True(tooltip.IsInstant);
        }
        finally
        {
            tooltip.IsOpen = false;
        }
    }

    [StaFact]
    public void EscapeWhileOpen_ForceCloses()
    {
        var tooltip = CreateAppliedTooltip();
        Invoke("OnTriggerGotKeyboardFocus", tooltip, tooltip, new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, tooltip));
        Assert.True(tooltip.IsOpen);

        var keyArgs = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSourceStub(), 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        Invoke("OnTriggerPreviewKeyDown", tooltip, tooltip, keyArgs);

        Assert.False(tooltip.IsOpen);
        Assert.True(keyArgs.Handled);
    }

    [StaFact]
    public void PointerDownOnTrigger_WhileOpen_ForceCloses()
    {
        var tooltip = CreateAppliedTooltip();
        Invoke("OnTriggerGotKeyboardFocus", tooltip, tooltip, new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, tooltip));
        Assert.True(tooltip.IsOpen);

        var mouseArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent };
        Invoke("OnTriggerPreviewMouseDown", tooltip, tooltip, mouseArgs);

        Assert.False(tooltip.IsOpen);
    }

    [StaFact]
    public void MouseLeave_WithHoverableContentEnabled_DoesNotCloseSynchronously()
    {
        var tooltip = CreateAppliedTooltip();

        try
        {
            Invoke("OnTriggerGotKeyboardFocus", tooltip, tooltip, new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, tooltip));
            Assert.True(tooltip.IsOpen);

            Invoke("OnTriggerMouseLeave", tooltip, tooltip, new MouseEventArgs(Mouse.PrimaryDevice, 0));

            // The 60ms hover grace timer hasn't ticked yet; still open immediately after leave.
            Assert.True(tooltip.IsOpen);
        }
        finally
        {
            tooltip.IsOpen = false;
        }
    }

    [StaFact]
    public void MouseLeave_WithHoverableContentDisabled_ClosesImmediately()
    {
        var tooltip = CreateAppliedTooltip();
        tooltip.DisableHoverableContent = true;
        Invoke("OnTriggerGotKeyboardFocus", tooltip, tooltip, new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, tooltip));
        Assert.True(tooltip.IsOpen);

        Invoke("OnTriggerMouseLeave", tooltip, tooltip, new MouseEventArgs(Mouse.PrimaryDevice, 0));

        Assert.False(tooltip.IsOpen);
    }

    [StaFact]
    public void Open_PushesAnOverlaySessionOnTheOwningWindowsStack()
    {
        var tooltip = new NaviusTooltip { Resources = CreateThemedScope() };
        var window = new Window { Content = tooltip };
        tooltip.ApplyTemplate();

        tooltip.IsOpen = true;

        Assert.NotNull(OverlayStack.GetFor(window).Topmost);

        tooltip.IsOpen = false;

        Assert.Null(OverlayStack.GetFor(window).Topmost);
    }

    [StaFact]
    public void NaviusTooltipService_SkipDelay_TrueOnlyWithinTheGraceWindow()
    {
        NaviusTooltipService.Reset();
        Assert.False(NaviusTooltipService.ShouldSkipDelay());

        NaviusTooltipService.NotifyClosed();
        Assert.True(NaviusTooltipService.ShouldSkipDelay());

        NaviusTooltipService.Reset();
        Assert.False(NaviusTooltipService.ShouldSkipDelay());
    }

    private PresentationSource PresentationSourceStub() =>
        _testSource ??= new System.Windows.Interop.HwndSource(0, 0, 0, 0, 0, "NaviusTooltipTests", System.IntPtr.Zero);
}
