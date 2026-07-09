using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls;
using Navius.Wpf.Primitives.Overlays;
using Navius.Wpf.Primitives.Positioning;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class PreviewCardTests
{
    static PreviewCardTests()
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
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/PreviewCard.xaml"),
        });

        return scope;
    }

    /// <summary>Builds and templates a preview card parented to a (never-shown) Window, so OpenCore's Window.GetWindow lookup succeeds.</summary>
    private static (NaviusPreviewCard Card, Window Window) CreateAppliedCard()
    {
        var card = new NaviusPreviewCard { Resources = CreateThemedScope(), Content = new TextBlock { Text = "@navius" } };
        var window = new Window { Content = card };
        card.ApplyTemplate();
        return (card, window);
    }

    private static FrameworkElement GetPopupContentPart(NaviusPreviewCard card) =>
        (FrameworkElement)card.Template.FindName("PART_PopupContent", card)!;

    private static void Invoke(string methodName, NaviusPreviewCard target, params object?[] args)
    {
        var method = typeof(NaviusPreviewCard).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(target, args);
    }

    [StaFact]
    public void Defaults_MatchThePreviewCardContract()
    {
        var card = new NaviusPreviewCard();

        Assert.Equal(PlacementSide.Bottom, card.Side);
        Assert.Equal(PlacementAlign.Center, card.Align);
        Assert.Equal(600, card.OpenDelay);
        Assert.Equal(300, card.CloseDelay);
        Assert.False(card.IsOpen);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var card = new NaviusPreviewCard { Resources = CreateThemedScope() };

        Assert.True(card.ApplyTemplate());
    }

    [StaFact]
    public void GotKeyboardFocus_OpensImmediatelyWithoutTheOpenDelay()
    {
        var (card, _) = CreateAppliedCard();

        Invoke("OnTriggerGotKeyboardFocus", card, card, new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, card));

        Assert.True(card.IsOpen);
    }

    [StaFact]
    public void LostKeyboardFocus_DoesNotCloseSynchronously_PendingTheCloseDelay()
    {
        var (card, _) = CreateAppliedCard();
        Invoke("OnTriggerGotKeyboardFocus", card, card, new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, card));
        Assert.True(card.IsOpen);

        Invoke("OnTriggerLostKeyboardFocus", card, card, new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, card, null));

        // The CloseDelay timer hasn't ticked yet; still open immediately after blur.
        Assert.True(card.IsOpen);
    }

    [StaFact]
    public void PopupContentMouseEnter_CancelsAPendingClose()
    {
        var (card, _) = CreateAppliedCard();
        Invoke("OnTriggerGotKeyboardFocus", card, card, new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, card));
        Invoke("OnTriggerMouseLeave", card, card, new MouseEventArgs(Mouse.PrimaryDevice, 0));

        var exception = Record.Exception(() =>
            Invoke("OnPopupContentMouseEnter", card, card, new MouseEventArgs(Mouse.PrimaryDevice, 0)));

        Assert.Null(exception);
        Assert.True(card.IsOpen);
    }

    [StaFact]
    public void Open_PushesANonModalDismissableOverlaySession()
    {
        var (card, window) = CreateAppliedCard();

        card.IsOpen = true;

        var session = OverlayStack.GetFor(window).Topmost;
        Assert.NotNull(session);
        Assert.True(session!.Options.CloseOnEscape);
        Assert.True(session.Options.CloseOnOutsideClick);
        Assert.False(session.Options.TrapFocus);
    }

    [StaFact]
    public void Open_RegistersThePopupContentAsAnInputRoot()
    {
        var (card, window) = CreateAppliedCard();

        card.IsOpen = true;

        var session = OverlayStack.GetFor(window).Topmost!;
        var popupContent = GetPopupContentPart(card);
        Assert.Contains(popupContent, session.InputRoots);
    }

    [StaFact]
    public void Close_UnwindsTheOverlaySession()
    {
        var (card, window) = CreateAppliedCard();
        card.IsOpen = true;
        Assert.NotNull(OverlayStack.GetFor(window).Topmost);

        card.IsOpen = false;

        Assert.Null(OverlayStack.GetFor(window).Topmost);
    }
}
