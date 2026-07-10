using System;
using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Tier A: derives from the native <see cref="MenuItem"/> with IsCheckable set (keeps the menu
/// open on click, native WPF behavior, matching the contract's radio-item default). Mutual
/// exclusion is coordinated through <see cref="Group"/> rather than nesting inside a
/// NaviusMenubarRadioGroup element; see that type's remarks for why.
/// </summary>
public class NaviusMenubarRadioItem : MenuItem
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusMenubarRadioItem),
        new PropertyMetadata(null));

    public static readonly DependencyProperty GroupProperty = DependencyProperty.Register(
        nameof(Group), typeof(NaviusMenubarRadioGroup), typeof(NaviusMenubarRadioItem),
        new PropertyMetadata(null, OnGroupChanged));

    public static readonly DependencyProperty TextValueProperty = DependencyProperty.Register(
        nameof(TextValue), typeof(string), typeof(NaviusMenubarRadioItem),
        new PropertyMetadata(null));

    static NaviusMenubarRadioItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenubarRadioItem),
            new FrameworkPropertyMetadata(typeof(NaviusMenubarRadioItem)));
    }

    public NaviusMenubarRadioItem()
    {
        IsCheckable = true;
    }

    /// <summary>Required identity of this item within its Group; throws if null/empty once initialized.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>The coordinator this item participates in. See remarks on <see cref="NaviusMenubarRadioGroup"/>.</summary>
    public NaviusMenubarRadioGroup? Group
    {
        get => (NaviusMenubarRadioGroup?)GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    public string? TextValue
    {
        get => (string?)GetValue(TextValueProperty);
        set => SetValue(TextValueProperty, value);
    }

    public event EventHandler<NaviusMenubarSelectEventArgs>? Select;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        if (string.IsNullOrEmpty(Value))
        {
            throw new InvalidOperationException(
                $"{nameof(NaviusMenubarRadioItem)}.{nameof(Value)} is required and must be non-empty.");
        }
    }

    /// <summary>
    /// Never calls base.OnClick(): native MenuItem's own IsCheckable click handling
    /// unconditionally toggles IsChecked, which would fight the Group-driven sync in
    /// <see cref="OnGroupValueChanged"/>. The Click routed event is still raised manually so
    /// external Click handlers keep working; a bound Command would not run (same accepted
    /// delta as NaviusMenubarCheckboxItem).
    /// </summary>
    protected override void OnClick()
    {
        if (Value is not null)
        {
            Group?.Select(Value);
        }

        var args = new NaviusMenubarSelectEventArgs();
        Select?.Invoke(this, args);

        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
    }

    private static void OnGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (NaviusMenubarRadioItem)d;

        if (e.OldValue is NaviusMenubarRadioGroup oldGroup)
        {
            oldGroup.ValueChanged -= item.OnGroupValueChanged;
        }

        if (e.NewValue is NaviusMenubarRadioGroup newGroup)
        {
            newGroup.ValueChanged += item.OnGroupValueChanged;
            item.IsChecked = string.Equals(item.Value, newGroup.Value, StringComparison.Ordinal);
        }
    }

    private void OnGroupValueChanged(object? sender, string? value) =>
        IsChecked = string.Equals(Value, value, StringComparison.Ordinal);
}
