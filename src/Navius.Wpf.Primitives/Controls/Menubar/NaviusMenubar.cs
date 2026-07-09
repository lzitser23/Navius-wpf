using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.Menubar;

/// <summary>
/// Tier A: derives from the native <see cref="Menu"/>. A WPF Menu is already exactly a
/// horizontal bar of top-level <see cref="MenuItem"/>s, each of which owns its own submenu
/// popup, roving/typeahead keyboard navigation and hover-to-switch ("follow focus") between
/// open siblings, so almost the entire contract's Trigger/Portal/Positioner/Popup/Arrow/
/// DismissableLayer/RovingFocus machinery collapses into this single native control for free.
/// See docs/parity/menubar.md "WPF implementation notes" for the full collapse table.
/// </summary>
public class NaviusMenubar : Menu
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(NaviusMenubar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(NaviusMenubar),
        new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

    public static readonly DependencyProperty DirProperty = DependencyProperty.Register(
        nameof(Dir), typeof(string), typeof(NaviusMenubar),
        new PropertyMetadata(null, OnDirChanged));

    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register(
        nameof(Loop), typeof(bool), typeof(NaviusMenubar),
        new PropertyMetadata(false));

    /// <summary>
    /// API-parity only: WPF's native Popup-per-submenu already scopes outside-press/Escape
    /// dismissal per open submenu; there is no additional scroll-lock/guard behavior to toggle.
    /// </summary>
    public static readonly DependencyProperty ModalProperty = DependencyProperty.Register(
        nameof(Modal), typeof(bool), typeof(NaviusMenubar),
        new PropertyMetadata(true));

    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusMenubar));

    private static readonly ItemsPanelTemplate VerticalPanel = CreateVerticalPanel();

    private ItemsPanelTemplate? _defaultHorizontalPanel;
    private bool _syncingFromSubmenuEvent;

    static NaviusMenubar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMenubar),
            new FrameworkPropertyMetadata(typeof(NaviusMenubar)));
    }

    public NaviusMenubar()
    {
        AddHandler(MenuItem.SubmenuOpenedEvent, new RoutedEventHandler(OnChildSubmenuOpened));
        AddHandler(MenuItem.SubmenuClosedEvent, new RoutedEventHandler(OnChildSubmenuClosed));
        PreviewKeyDown += OnPreviewKeyDownForLoop;
    }

    /// <summary>Controlled open-menu value (the open menu's Value, or null); pair with ValueChanged.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>"horizontal" (default, native Menu layout) or "vertical" (StackPanel item host).</summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>"ltr"/"rtl"; overrides cascaded FlowDirection. Null falls back to inherited FlowDirection.</summary>
    public string? Dir
    {
        get => (string?)GetValue(DirProperty);
        set => SetValue(DirProperty, value);
    }

    /// <summary>
    /// When false, arrow-key navigation across top-level triggers clamps at the ends instead of
    /// wrapping (native WPF menu navigation always wraps; this is a delta, see parity notes).
    /// </summary>
    public bool Loop
    {
        get => (bool)GetValue(LoopProperty);
        set => SetValue(LoopProperty, value);
    }

    /// <summary>API-parity flag; see remarks on <see cref="ModalProperty"/>.</summary>
    public bool Modal
    {
        get => (bool)GetValue(ModalProperty);
        set => SetValue(ModalProperty, value);
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusMenubarAutomationPeer(this);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bar = (NaviusMenubar)d;
        if (bar._syncingFromSubmenuEvent)
        {
            return;
        }

        bar.ApplyValueToChildren((string?)e.NewValue);
    }

    private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusMenubar)d).ApplyOrientation((Orientation)e.NewValue);

    private static void OnDirChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bar = (NaviusMenubar)d;
        var dir = (string?)e.NewValue;
        if (dir is not null)
        {
            bar.FlowDirection = string.Equals(dir, "rtl", StringComparison.OrdinalIgnoreCase)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }
    }

    private void ApplyOrientation(Orientation orientation)
    {
        _defaultHorizontalPanel ??= ItemsPanel;

        ItemsPanel = orientation == Orientation.Vertical ? VerticalPanel : _defaultHorizontalPanel;
    }

    private void ApplyValueToChildren(string? value)
    {
        foreach (var menu in FindTopLevelMenus())
        {
            var shouldOpen = string.Equals(menu.Value, value, StringComparison.Ordinal);
            if (menu.IsSubmenuOpen != shouldOpen)
            {
                menu.IsSubmenuOpen = shouldOpen;
            }
        }
    }

    private IEnumerable<NaviusMenubarMenu> FindTopLevelMenus()
    {
        foreach (var item in Items)
        {
            if (item is NaviusMenubarMenu menu)
            {
                yield return menu;
            }
        }
    }

    private void OnChildSubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not NaviusMenubarMenu menu || !Items.Contains(menu))
        {
            return;
        }

        _syncingFromSubmenuEvent = true;
        try
        {
            Value = menu.Value;
        }
        finally
        {
            _syncingFromSubmenuEvent = false;
        }

        RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));

        // Only one menu open at a time across the whole bar (contract invariant): closing the
        // rest is handled implicitly by native Menu (opening a sibling top-level submenu closes
        // any other open one), so no extra work is required here.
    }

    private void OnChildSubmenuClosed(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not NaviusMenubarMenu menu || !Items.Contains(menu) || menu.IsSubmenuOpen)
        {
            return;
        }

        if (!string.Equals(Value, menu.Value, StringComparison.Ordinal))
        {
            return;
        }

        _syncingFromSubmenuEvent = true;
        try
        {
            Value = null;
        }
        finally
        {
            _syncingFromSubmenuEvent = false;
        }

        RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
    }

    /// <summary>
    /// Best-effort Loop=false clamp: swallows the ArrowLeft/ArrowRight keypress that would move
    /// focus past the first/last top-level trigger, since native WPF menu navigation always
    /// wraps. Only applies while no submenu is open (matches the contract's trigger-row roving).
    /// </summary>
    private void OnPreviewKeyDownForLoop(object sender, KeyEventArgs e)
    {
        if (Loop || (e.Key != Key.Left && e.Key != Key.Right) || Value is not null)
        {
            return;
        }

        var menus = FindTopLevelMenus().ToList();
        if (menus.Count == 0)
        {
            return;
        }

        var isRtl = string.Equals(Dir, "rtl", StringComparison.OrdinalIgnoreCase)
            || (Dir is null && FlowDirection == FlowDirection.RightToLeft);
        var goingNext = e.Key == Key.Right != isRtl;

        var focusedIndex = menus.FindIndex(m => m.IsKeyboardFocusWithin);
        if (focusedIndex < 0)
        {
            return;
        }

        if ((goingNext && focusedIndex == menus.Count - 1) || (!goingNext && focusedIndex == 0))
        {
            e.Handled = true;
        }
    }

    private static ItemsPanelTemplate CreateVerticalPanel()
    {
        var factory = new FrameworkElementFactory(typeof(StackPanel));
        factory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
        return new ItemsPanelTemplate(factory);
    }
}

internal sealed class NaviusMenubarAutomationPeer : MenuAutomationPeer
{
    public NaviusMenubarAutomationPeer(NaviusMenubar owner) : base(owner)
    {
    }

    protected override string GetClassNameCore() => nameof(NaviusMenubar);
}
