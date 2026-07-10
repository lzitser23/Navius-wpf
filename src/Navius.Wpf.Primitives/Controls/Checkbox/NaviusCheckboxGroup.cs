using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Navius.Wpf.Primitives.Controls.Checkbox;

/// <summary>
/// Tier B (custom): WPF has no built-in "checkbox group with a derived select-all
/// indeterminate roll-up" concept, so this reimplements the contract's
/// NaviusCheckboxGroup as a lookless ContentControl. It owns the authoritative set of
/// checked child GroupValues (Value/AllValues), coordinating descendant NaviusCheckbox
/// state via routed-event bubbling (Checked/Unchecked) rather than an ItemsControl,
/// mirroring the source's arbitrary-child-content model (a div wrapping a RenderFragment).
///
/// The bubble &lt;input type="checkbox"&gt; form mirror is dropped (no WPF form
/// submission model). Disabled is not reimplemented as a new property: WPF's IsEnabled
/// already cascades to descendants, so disabling the group disables every child checkbox
/// for free.
/// </summary>
public class NaviusCheckboxGroup : ContentControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(IReadOnlyList<string>),
        typeof(NaviusCheckboxGroup),
        new PropertyMetadata(Array.Empty<string>(), OnValueChanged));

    public static readonly DependencyProperty AllValuesProperty = DependencyProperty.Register(
        nameof(AllValues),
        typeof(IReadOnlyList<string>),
        typeof(NaviusCheckboxGroup),
        new PropertyMetadata(Array.Empty<string>(), OnValueChanged));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(NaviusCheckboxGroup));

    private bool _isSyncing;

    static NaviusCheckboxGroup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCheckboxGroup),
            new FrameworkPropertyMetadata(typeof(NaviusCheckboxGroup)));
    }

    public NaviusCheckboxGroup()
    {
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(OnChildToggled));
        AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(OnChildToggled));
    }

    public IReadOnlyList<string> Value
    {
        get => (IReadOnlyList<string>)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public IReadOnlyList<string> AllValues
    {
        get => (IReadOnlyList<string>)GetValue(AllValuesProperty);
        set => SetValue(AllValuesProperty, value);
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        SyncChildrenFromValue();
        SyncSelectAllFromValue();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new NaviusCheckboxGroupAutomationPeer(this);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var group = (NaviusCheckboxGroup)d;
        group.SyncChildrenFromValue();
        group.SyncSelectAllFromValue();
    }

    private void OnChildToggled(object sender, RoutedEventArgs e)
    {
        if (_isSyncing || e.OriginalSource is not NaviusCheckbox item)
        {
            return;
        }

        if (item.IsSelectAll)
        {
            SetValueAndRaise(item.IsChecked == true ? AllValues : Array.Empty<string>());
            return;
        }

        if (string.IsNullOrEmpty(item.GroupValue))
        {
            return;
        }

        var current = new List<string>(Value);
        var isChecked = item.IsChecked == true;
        var present = current.Contains(item.GroupValue);

        if (isChecked && !present)
        {
            current.Add(item.GroupValue);
            SetValueAndRaise(current);
        }
        else if (!isChecked && present)
        {
            current.Remove(item.GroupValue);
            SetValueAndRaise(current);
        }
    }

    private void SetValueAndRaise(IReadOnlyList<string> value)
    {
        Value = value;
        RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
    }

    private void SyncChildrenFromValue()
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;
        try
        {
            foreach (var item in FindDescendants<NaviusCheckbox>(this))
            {
                if (item.IsSelectAll || string.IsNullOrEmpty(item.GroupValue))
                {
                    continue;
                }

                item.IsChecked = Value.Contains(item.GroupValue);
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void SyncSelectAllFromValue()
    {
        var allValues = AllValues;
        if (allValues.Count == 0)
        {
            return;
        }

        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;
        try
        {
            var checkedCount = allValues.Count(v => Value.Contains(v));
            bool? rollup = checkedCount == 0 ? false : checkedCount == allValues.Count ? true : null;

            foreach (var item in FindDescendants<NaviusCheckbox>(this))
            {
                if (item.IsSelectAll)
                {
                    item.IsChecked = rollup;
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Walks the logical tree (not visual) so descendants are discoverable immediately
    /// after Content is assigned, without requiring a layout pass or a live
    /// PresentationSource (matters for unit tests and for XAML parse-time ordering).
    /// </summary>
    private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject childObj)
            {
                continue;
            }

            if (childObj is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindDescendants<T>(childObj))
            {
                yield return descendant;
            }
        }
    }
}

internal sealed class NaviusCheckboxGroupAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusCheckboxGroupAutomationPeer(NaviusCheckboxGroup owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusCheckboxGroup);
}
