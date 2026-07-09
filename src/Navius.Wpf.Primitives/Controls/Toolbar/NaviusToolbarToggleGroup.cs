using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Toolbar;

/// <summary>
/// Tier B (lookless custom control). Deliberately NOT a subclass of NaviusToggleGroup even
/// though docs/parity/toolbar.md's "WPF strategy" invites reusing "whatever base the standalone
/// ToggleGroup/ToggleGroupItem WPF port produces": NaviusToggleGroup's private
/// UpdateRovingTabStops unconditionally owns IsTabStop assignment for its own items (even with
/// RovingFocus=false, every enabled item becomes its own independent Tab stop), which would
/// fight the ancestor NaviusToolbar's single shared roving domain across mixed control types.
/// This root never touches IsTabStop or arrow keys at all -- that is entirely NaviusToolbar's
/// job once NaviusToolbarToggleItem's IToolbarItem marker puts it in NaviusToolbar's scan.
///
/// What IS reused (ported, not inherited) is NaviusToggleGroup's pressed-set semantics: the same
/// "single mode clears on re-click of the pressed item, replaces on a different item; multiple
/// mode toggles set membership" ComputeNext behavior, driven off the same
/// ToggleButton.Checked/Unchecked routed-event bubbling pattern as NaviusToggleGroup and
/// NaviusRadioGroup use for their own roots. Type is a string ("single"/"multiple") rather than
/// NaviusToggleGroup's Multiple bool, matching this family's own contract parameter table.
/// </summary>
public class NaviusToolbarToggleGroup : ContentControl
{
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
        nameof(Type),
        typeof(string),
        typeof(NaviusToolbarToggleGroup),
        new PropertyMetadata("single"));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(IReadOnlyList<string>),
        typeof(NaviusToolbarToggleGroup),
        new PropertyMetadata(Array.Empty<string>(), OnValueChanged));

    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled),
        typeof(bool),
        typeof(NaviusToolbarToggleGroup),
        new PropertyMetadata(false, OnDisabledChanged));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusToolbarToggleGroup));

    private bool _isSyncing;

    static NaviusToolbarToggleGroup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusToolbarToggleGroup),
            new FrameworkPropertyMetadata(typeof(NaviusToolbarToggleGroup)));
    }

    public NaviusToolbarToggleGroup()
    {
        // Never its own Tab stop or focus scope, per the class remarks: roving belongs entirely
        // to the ancestor NaviusToolbar.
        Focusable = false;
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnItemChecked));
        AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(OnItemUnchecked));
    }

    /// <summary>"single" (default, at most one pressed) or "multiple" (any number pressed).</summary>
    public string Type
    {
        get => (string)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <summary>Controlled pressed set.</summary>
    public IReadOnlyList<string> Value
    {
        get => (IReadOnlyList<string>)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Disables every item in the group (combined with each item's own Disabled via OR).</summary>
    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    private bool IsMultiple => string.Equals(Type, "multiple", StringComparison.OrdinalIgnoreCase);

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        SyncCheckedFromValue();
        PushDisabledToItems();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new NaviusToolbarToggleGroupAutomationPeer(this);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusToolbarToggleGroup)d).SyncCheckedFromValue();

    private static void OnDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusToolbarToggleGroup)d).PushDisabledToItems();

    private void PushDisabledToItems()
    {
        foreach (var item in LogicalTreeWalker.Descendants<NaviusToolbarToggleItem>(this))
        {
            item.UpdateEffectiveDisabled();
        }
    }

    private void OnItemChecked(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || e.OriginalSource is not NaviusToolbarToggleItem item)
        {
            return;
        }

        if (IsMultiple)
        {
            if (!Value.Contains(item.Value))
            {
                ApplyValue(Value.Append(item.Value).ToList());
            }
        }
        else
        {
            _isSyncing = true;
            try
            {
                foreach (var other in LogicalTreeWalker.Descendants<NaviusToolbarToggleItem>(this))
                {
                    if (!ReferenceEquals(other, item))
                    {
                        other.IsChecked = false;
                    }
                }
            }
            finally
            {
                _isSyncing = false;
            }

            ApplyValue(new List<string> { item.Value });
        }
    }

    private void OnItemUnchecked(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || e.OriginalSource is not NaviusToolbarToggleItem item)
        {
            return;
        }

        ApplyValue(Value.Where(v => !string.Equals(v, item.Value, StringComparison.Ordinal)).ToList());
    }

    private void ApplyValue(List<string> next)
    {
        if (Value.SequenceEqual(next))
        {
            return;
        }

        Value = next;
        RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
    }

    private void SyncCheckedFromValue()
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;
        try
        {
            foreach (var item in LogicalTreeWalker.Descendants<NaviusToolbarToggleItem>(this))
            {
                item.IsChecked = Value.Contains(item.Value);
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }
}

internal sealed class NaviusToolbarToggleGroupAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusToolbarToggleGroupAutomationPeer(NaviusToolbarToggleGroup owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusToolbarToggleGroup);
}
