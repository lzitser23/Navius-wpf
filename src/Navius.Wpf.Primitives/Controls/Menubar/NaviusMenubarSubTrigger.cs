using System;
using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Tier A: derives from the native <see cref="MenuItem"/>. Collapses the contract's
/// NaviusMenubarSub (open-state owner) + NaviusMenubarSubTrigger (the header) +
/// NaviusMenubarSubContent (the floating surface) into one control: nest the submenu's
/// NaviusMenubarItem/CheckboxItem/RadioItem/Separator/Label children directly in this
/// SubTrigger's own Items, exactly like plain native WPF submenu nesting. WPF already gives
/// arbitrary-depth nesting, its own positioning/flip, roving focus, and dismissal for free, so
/// there is nothing left for a separate Sub/SubContent type to own.
/// </summary>
public class NaviusMenubarSubTrigger : MenuItem
{
    public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
        nameof(TextValue), typeof(string), typeof(NaviusMenubarSubTrigger),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DirProperty = DependencyProperty.Register(
        nameof(Dir), typeof(string), typeof(NaviusMenubarSubTrigger),
        new PropertyMetadata(null, OnDirChanged));

    public static readonly RoutedEvent OpenChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(OpenChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusMenubarSubTrigger));

    static NaviusMenubarSubTrigger()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenubarSubTrigger),
            new FrameworkPropertyMetadata(typeof(NaviusMenubarSubTrigger)));
    }

    public NaviusMenubarSubTrigger()
    {
        AddHandler(SubmenuOpenedEvent, new RoutedEventHandler(OnSubmenuOpened));
        AddHandler(SubmenuClosedEvent, new RoutedEventHandler(OnSubmenuClosed));
    }

    public string? TextValue
    {
        get => (string?)GetValue(TextValueProperty);
        set => SetValue(TextValueProperty, value);
    }

    /// <summary>Overrides cascaded FlowDirection for this submenu; falls back to inherited when null.</summary>
    public string? Dir
    {
        get => (string?)GetValue(DirProperty);
        set => SetValue(DirProperty, value);
    }

    /// <summary>Mirrors the contract's NaviusMenubarSub.OpenChanged; fires for both open and close.</summary>
    public event RoutedEventHandler OpenChanged
    {
        add => AddHandler(OpenChangedEvent, value);
        remove => RemoveHandler(OpenChangedEvent, value);
    }

    private static void OnDirChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var trigger = (NaviusMenubarSubTrigger)d;
        var dir = (string?)e.NewValue;
        trigger.FlowDirection = string.Equals(dir, "rtl", StringComparison.OrdinalIgnoreCase)
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }

    private void OnSubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, this))
        {
            RaiseEvent(new RoutedEventArgs(OpenChangedEvent, this));
        }
    }

    private void OnSubmenuClosed(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, this))
        {
            RaiseEvent(new RoutedEventArgs(OpenChangedEvent, this));
        }
    }
}
