using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Navius.Wpf.Primitives.Controls.Sortable;

/// <summary>
/// One reorderable row (contract's NaviusSortableItem, role="listitem"). A lookless
/// <see cref="ContentControl"/> (not a ListBoxItem: there is no "selected" concept here, matching the
/// doc's WPF strategy note). It is both the pointer drag source and the roving-tabindex keyboard
/// target; the owning <see cref="NaviusSortable"/> owns all order/grab state and pushes
/// <see cref="IsKeyboardGrabbed"/>, IsTabStop, and PositionInSet/SizeOfSet onto each item.
///
/// The web's <c>data-dragging</c> / <c>data-drop-target</c> / <c>data-keyboard-grabbed</c> DOM
/// attributes become the <see cref="IsDragging"/> / <see cref="IsDropTarget"/> /
/// <see cref="IsKeyboardGrabbed"/> boolean DPs that drive Style triggers in Themes/Sortable.xaml.
/// </summary>
public class NaviusSortableItem : ContentControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusSortableItem), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(NaviusSortableItem), new PropertyMetadata(null));

    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled), typeof(bool), typeof(NaviusSortableItem),
        new PropertyMetadata(false, OnDisabledChanged));

    public static readonly DependencyProperty IsKeyboardGrabbedProperty = DependencyProperty.Register(
        nameof(IsKeyboardGrabbed), typeof(bool), typeof(NaviusSortableItem), new PropertyMetadata(false));

    public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(
        nameof(IsDragging), typeof(bool), typeof(NaviusSortableItem), new PropertyMetadata(false));

    public static readonly DependencyProperty IsDropTargetProperty = DependencyProperty.Register(
        nameof(IsDropTarget), typeof(bool), typeof(NaviusSortableItem), new PropertyMetadata(false));

    private Point _dragStart;
    private bool _dragArmed;

    static NaviusSortableItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSortableItem), new FrameworkPropertyMetadata(typeof(NaviusSortableItem)));
        FocusableProperty.OverrideMetadata(typeof(NaviusSortableItem), new FrameworkPropertyMetadata(true));
    }

    /// <summary>Stable key the owning <see cref="NaviusSortable"/> tracks order by (required).</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Accessible name used in announcements; falls back to <see cref="Value"/> when unset.</summary>
    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Per-item disabled: skipped by roving navigation and not a drag source.</summary>
    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    /// <summary>True while this item is keyboard-grabbed (contract's data-keyboard-grabbed/aria-grabbed).</summary>
    public bool IsKeyboardGrabbed
    {
        get => (bool)GetValue(IsKeyboardGrabbedProperty);
        set => SetValue(IsKeyboardGrabbedProperty, value);
    }

    /// <summary>True while this item is the active pointer-drag source (contract's data-dragging).</summary>
    public bool IsDragging
    {
        get => (bool)GetValue(IsDraggingProperty);
        set => SetValue(IsDraggingProperty, value);
    }

    /// <summary>True while a pointer drag is hovering this item as its drop target (contract's data-drop-target).</summary>
    public bool IsDropTarget
    {
        get => (bool)GetValue(IsDropTargetProperty);
        set => SetValue(IsDropTargetProperty, value);
    }

    /// <summary>The accessible name: explicit <see cref="Label"/> or, failing that, <see cref="Value"/>.</summary>
    public string AccessibleLabel => string.IsNullOrEmpty(Label) ? Value : Label!;

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusSortableItemAutomationPeer(this);

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);

        if (Disabled || !IsEnabled)
        {
            _dragArmed = false;
            return;
        }

        // Handle scoping: if this item contains any NaviusSortableItemHandle, drag may only start
        // from within one; otherwise the whole item is the drag source.
        if (HasHandle() && !IsWithinHandle(e.OriginalSource as DependencyObject))
        {
            _dragArmed = false;
            return;
        }

        _dragStart = e.GetPosition(null);
        _dragArmed = true;
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);

        if (!_dragArmed || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _dragArmed = false;

        var owner = FindOwner();
        if (owner is null || owner.Disabled)
        {
            return;
        }

        Focus();
        var data = new DataObject(NaviusSortable.DragFormat, this);
        IsDragging = true;
        try
        {
            DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
        }
        finally
        {
            IsDragging = false;
        }
    }

    internal bool HasHandle() => FindDescendant<NaviusSortableItemHandle>(this) is not null;

    private bool IsWithinHandle(DependencyObject? source)
    {
        var current = source;
        while (current is not null && current != this)
        {
            if (current is NaviusSortableItemHandle)
            {
                return true;
            }

            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return false;
    }

    private NaviusSortable? FindOwner()
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            current = VisualTreeHelper.GetParent(current);
            if (current is NaviusSortable owner)
            {
                return owner;
            }
        }

        return null;
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            var nested = FindDescendant<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void OnDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        (d as NaviusSortableItem)?.FindOwner()?.RefreshItemStates();
}
