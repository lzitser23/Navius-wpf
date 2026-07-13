using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Navius.Wpf.Ui.Internal;

namespace Navius.Wpf.Ui.Sidebar;

/// <summary>
/// Collapsible nav rail: a flat ItemsControl of <see cref="NaviusSidebarSection"/> and/or bare
/// <see cref="NaviusSidebarItem"/> children (same manual-composition anatomy as
/// Navius.Wpf.Ui.Breadcrumb.NaviusBreadcrumb), plus optional header/footer slots and a built-in
/// collapse toggle. <see cref="IsCollapsed"/> is registered via
/// DependencyProperty.RegisterAttached with <see cref="FrameworkPropertyMetadataOptions.Inherits"/>
/// so every descendant (section headers, item labels, the collapse toggle) can react to it via a
/// plain trigger without any manual state push-down. RegisterAttached matters: Register-applied
/// Inherits metadata only resolves on the owner type and never propagates to other element types,
/// so a plain registration left the template's "(sidebar:NaviusSidebar.IsCollapsed)" triggers
/// silently reading the false default forever (follow-up half of issue #28).
///
/// Roving keyboard focus (ArrowUp/Down/Home/End) walks the realized visual tree for
/// <see cref="NaviusSidebarItem"/> instances in visual order -- this flattens across section
/// boundaries deliberately, since the contract is one continuous nav list, sections are just visual
/// grouping. The actual index math is <see cref="SidebarNavigation.MoveFocus"/>, kept WPF-free so it
/// is unit tested directly; this class only supplies the focus-walking glue around it.
/// </summary>
[TemplatePart(Name = PartRoot, Type = typeof(FrameworkElement))]
public class NaviusSidebar : ItemsControl
{
    private const string PartRoot = "PART_Root";

    public static readonly DependencyProperty IsCollapsedProperty = DependencyProperty.RegisterAttached(
        nameof(IsCollapsed), typeof(bool), typeof(NaviusSidebar),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Inherits,
            OnIsCollapsedChanged));

    public static readonly DependencyProperty ExpandedWidthProperty = DependencyProperty.Register(
        nameof(ExpandedWidth), typeof(double), typeof(NaviusSidebar), new PropertyMetadata(240d));

    public static readonly DependencyProperty CollapsedWidthProperty = DependencyProperty.Register(
        nameof(CollapsedWidth), typeof(double), typeof(NaviusSidebar), new PropertyMetadata(64d));

    public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register(
        nameof(HeaderContent), typeof(object), typeof(NaviusSidebar), new PropertyMetadata(null));

    public static readonly DependencyProperty FooterContentProperty = DependencyProperty.Register(
        nameof(FooterContent), typeof(object), typeof(NaviusSidebar), new PropertyMetadata(null));

    public static readonly RoutedCommand ToggleCollapsedCommand = new(nameof(ToggleCollapsedCommand), typeof(NaviusSidebar));

    private FrameworkElement? _rootPart;

    static NaviusSidebar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSidebar),
            new FrameworkPropertyMetadata(typeof(NaviusSidebar)));

        CommandManager.RegisterClassCommandBinding(
            typeof(NaviusSidebar),
            new CommandBinding(ToggleCollapsedCommand, OnToggleCollapsedExecuted));
    }

    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>Attached-property accessor: the inherited collapse state on any descendant.</summary>
    public static bool GetIsCollapsed(DependencyObject element) => (bool)element.GetValue(IsCollapsedProperty);

    /// <summary>Attached-property accessor; on the sidebar itself prefer <see cref="IsCollapsed"/>.</summary>
    public static void SetIsCollapsed(DependencyObject element, bool value) => element.SetValue(IsCollapsedProperty, value);

    public double ExpandedWidth
    {
        get => (double)GetValue(ExpandedWidthProperty);
        set => SetValue(ExpandedWidthProperty, value);
    }

    public double CollapsedWidth
    {
        get => (double)GetValue(CollapsedWidthProperty);
        set => SetValue(CollapsedWidthProperty, value);
    }

    /// <summary>Content above the item list (e.g. a logo/brand lockup).</summary>
    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    /// <summary>Content below the item list, above the built-in collapse toggle (e.g. a user/account row).</summary>
    public object? FooterContent
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusSidebarAutomationPeer(this);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _rootPart = GetTemplateChild(PartRoot) as FrameworkElement;
        if (_rootPart is not null)
        {
            _rootPart.Width = IsCollapsed ? CollapsedWidth : ExpandedWidth;
        }
    }

    private static void OnToggleCollapsedExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is NaviusSidebar sidebar)
        {
            sidebar.SetCurrentValue(IsCollapsedProperty, SidebarNavigation.ToggleCollapsed(sidebar.IsCollapsed));
        }
    }

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // With RegisterAttached + Inherits this callback fires on every descendant the value
        // propagates to, not just the sidebar; only the sidebar itself animates its width.
        if (d is NaviusSidebar sidebar)
        {
            sidebar.AnimateWidth((bool)e.NewValue);
        }
    }

    private void AnimateWidth(bool isCollapsed)
    {
        if (_rootPart is null)
        {
            return;
        }

        var to = isCollapsed ? CollapsedWidth : ExpandedWidth;

        if (!ReducedMotion.AnimationsEnabled)
        {
            // Reduced-motion users get an instant width change instead of the ease, the same
            // ReducedMotion.AnimationsEnabled guard NaviusSkeleton/NaviusSpinner apply to their loops.
            _rootPart.Width = to;
            return;
        }

        if (PresentationSource.FromVisual(this) is null)
        {
            // No live render surface (design-time / not yet shown): resolve synchronously,
            // same fallback NaviusOverlaySurfaceBase.BeginOpacityAnimation uses.
            _rootPart.Width = to;
            return;
        }

        var animation = new DoubleAnimation(to, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };

        _rootPart.BeginAnimation(FrameworkElement.WidthProperty, animation);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || e.Key is not (Key.Down or Key.Up or Key.Home or Key.End) ||
            e.OriginalSource is not NaviusSidebarItem current)
        {
            return;
        }

        var items = CollectItems();
        var currentIndex = items.IndexOf(current);
        var nextIndex = SidebarNavigation.MoveFocus(currentIndex, items.Count, e.Key);

        if (nextIndex >= 0)
        {
            items[nextIndex].Focus();
            e.Handled = true;
        }
    }

    /// <summary>Every realized NaviusSidebarItem in visual order, flattened across any sections.</summary>
    internal List<NaviusSidebarItem> CollectItems()
    {
        var items = new List<NaviusSidebarItem>();
        CollectItemsRecursive(this, items);
        return items;
    }

    private static void CollectItemsRecursive(DependencyObject parent, List<NaviusSidebarItem> items)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is NaviusSidebarItem item)
            {
                items.Add(item);
            }

            CollectItemsRecursive(child, items);
        }
    }
}

/// <summary>
/// An ItemsControl gets ItemsControlAutomationPeer by default, whose GetChildrenCore returns only
/// item peers for the Items collection -- template chrome never enters the UIA tree. For
/// NaviusSidebar that silently dropped the HeaderContent/FooterContent ContentPresenters' descendants
/// and the built-in collapse Button, so assistive tech had no way to reach them at all (issue #28).
/// Deriving FrameworkElementAutomationPeer instead restores the default visual-tree child walk, the
/// same fix NaviusSidebarItem's peer applies for a different symptom of the same "wrong peer base
/// type" class of bug. Reported as Group rather than a nav-landmark type because UIA has no
/// navigation-landmark AutomationControlType; Group is the closest fit for a chrome container that
/// isn't itself interactive.
/// </summary>
internal sealed class NaviusSidebarAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusSidebarAutomationPeer(NaviusSidebar owner) : base(owner) { }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusSidebar);
}
