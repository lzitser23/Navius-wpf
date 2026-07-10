using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class PopoverTests : IDisposable
{
    // Flushes any Dispatcher-deferred native-window teardown left by a popup this test closed
    // (see TestCleanup.PumpDispatcher) before this test's dedicated STA thread exits.
    public void Dispose() => TestCleanup.PumpDispatcher();

    static PopoverTests()
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

    private static readonly MethodInfo OnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Invokes the protected, most-derived OnClick() (virtual dispatch reaches Button.OnClick,
    /// which raises Click), just like a real click, without depending on real input routing.
    /// </summary>
    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Popover.xaml"),
        });

        return scope;
    }

    /// <summary>Builds and templates a popover parented to a (never-shown) Window, so OpenCore's Window.GetWindow lookup succeeds.</summary>
    private static (NaviusPopover Popover, Window Window) CreateAppliedPopover()
    {
        var popover = new NaviusPopover { Resources = CreateThemedScope(), Content = new TextBlock { Text = "Trigger" } };
        var window = new Window { Content = popover };
        popover.ApplyTemplate();
        return (popover, window);
    }

    private static ButtonBase GetTriggerPart(NaviusPopover popover) =>
        (ButtonBase)popover.Template.FindName("PART_Trigger", popover)!;

    private static FrameworkElement GetPopupContentPart(NaviusPopover popover) =>
        (FrameworkElement)popover.Template.FindName("PART_PopupContent", popover)!;

    [StaFact]
    public void Defaults_MatchThePopoverContract()
    {
        var popover = new NaviusPopover();

        Assert.Equal(PlacementSide.Bottom, popover.Side);
        Assert.Equal(PlacementAlign.Center, popover.Align);
        Assert.False(popover.Modal);
        Assert.False(popover.IsOpen);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var popover = new NaviusPopover { Resources = CreateThemedScope() };

        Assert.True(popover.ApplyTemplate());
    }

    [StaFact]
    public void ClickingTrigger_TogglesOpenState()
    {
        var (popover, _) = CreateAppliedPopover();
        var trigger = GetTriggerPart(popover);

        SimulateClick(trigger);
        Assert.True(popover.IsOpen);

        SimulateClick(trigger);
        Assert.False(popover.IsOpen);
    }

    [StaFact]
    public void Open_PushesAFocusTrappedDismissableOverlaySession()
    {
        var (popover, window) = CreateAppliedPopover();

        try
        {
            popover.IsOpen = true;

            var session = OverlayStack.GetFor(window).Topmost;
            Assert.NotNull(session);
            Assert.True(session!.Options.CloseOnEscape);
            Assert.True(session.Options.CloseOnOutsideClick);
            Assert.True(session.Options.TrapFocus);
        }
        finally
        {
            popover.IsOpen = false;
        }
    }

    [StaFact]
    public void Open_RegistersThePopupContentAsAnInputRoot()
    {
        var (popover, window) = CreateAppliedPopover();

        try
        {
            popover.IsOpen = true;

            var session = OverlayStack.GetFor(window).Topmost!;
            var popupContent = GetPopupContentPart(popover);
            Assert.Contains(popupContent, session.InputRoots);
        }
        finally
        {
            popover.IsOpen = false;
        }
    }

    [StaFact]
    public void CloseCommand_ExecutedFromPopupContent_ClosesThePopover()
    {
        var (popover, _) = CreateAppliedPopover();
        popover.IsOpen = true;
        var popupContent = GetPopupContentPart(popover);

        NaviusPopover.CloseCommand.Execute(null, popupContent);

        Assert.False(popover.IsOpen);
    }

    [StaFact]
    public void TitleAndDescription_AreWiredToAutomationPropertiesOnThePopupContent()
    {
        var (popover, _) = CreateAppliedPopover();

        popover.Title = "Edit profile";
        popover.Description = "Update your name and email.";

        var popupContent = GetPopupContentPart(popover);
        Assert.Equal("Edit profile", AutomationProperties.GetName(popupContent));
        Assert.Equal("Update your name and email.", AutomationProperties.GetHelpText(popupContent));
    }

    [StaFact]
    public void Close_UnwindsTheOverlaySession()
    {
        var (popover, window) = CreateAppliedPopover();
        popover.IsOpen = true;
        Assert.NotNull(OverlayStack.GetFor(window).Topmost);

        popover.IsOpen = false;

        Assert.Null(OverlayStack.GetFor(window).Topmost);
    }
}
