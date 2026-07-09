using System.Windows;
using System.Windows.Controls.Primitives;

namespace Navius.Wpf.Primitives.Controls.Menus;

/// <summary>
/// Tier A: derives from the native ToggleButton (inheriting ToggleButtonAutomationPeer's
/// UIA TogglePattern, roughly matching aria-expanded/aria-haspopup) and owns the association
/// with a NaviusMenuPopup via the Menu property: clicking toggles IsChecked and, on the
/// resulting Checked/Unchecked transition, opens or closes the associated popup with itself
/// as PlacementTarget. IsChecked doubles as the contract's `data-popup-open` (see
/// Themes/Menu.xaml's IsChecked trigger).
///
/// The popup's own Closed event (fired for every dismissal path: Escape, outside click, or
/// an item's OnSelect closing it) resets IsChecked back to false, keeping the trigger's
/// visual state in sync even when the close didn't originate from a trigger click.
/// </summary>
public class NaviusMenuTrigger : ToggleButton
{
    public static readonly DependencyProperty MenuProperty = DependencyProperty.Register(
        nameof(Menu),
        typeof(NaviusMenuPopup),
        typeof(NaviusMenuTrigger),
        new PropertyMetadata(null, OnMenuChanged));

    static NaviusMenuTrigger()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenuTrigger),
            new FrameworkPropertyMetadata(typeof(NaviusMenuTrigger)));
    }

    public NaviusMenuPopup? Menu
    {
        get => (NaviusMenuPopup?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    protected override void OnToggle()
    {
        base.OnToggle();

        if (Menu is null)
        {
            return;
        }

        if (IsChecked == true)
        {
            Menu.PlacementTarget = this;
            Menu.IsOpen = true;
        }
        else
        {
            Menu.IsOpen = false;
        }
    }

    private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var trigger = (NaviusMenuTrigger)d;

        if (e.OldValue is System.Windows.Controls.ContextMenu oldMenu)
        {
            oldMenu.Closed -= trigger.OnMenuClosed;
        }

        if (e.NewValue is System.Windows.Controls.ContextMenu newMenu)
        {
            newMenu.Closed += trigger.OnMenuClosed;
        }
    }

    private void OnMenuClosed(object sender, RoutedEventArgs e) => IsChecked = false;
}
