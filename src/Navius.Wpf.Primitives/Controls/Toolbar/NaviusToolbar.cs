using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.Toolbar;

/// <summary>
/// Tier B (custom lookless control), same "root owns state, discovers descendants via the
/// logical tree" shape as NaviusRadioGroup/NaviusToggleGroup. Unlike either of those, this root's
/// roving-focus scan is heterogeneous: it walks for plain <see cref="Control"/> descendants that
/// implement <see cref="IToolbarItem"/> (NaviusToolbarButton, NaviusToolbarLink,
/// NaviusToolbarToggleItem indiscriminately), including ones nested inside a
/// NaviusToolbarToggleGroup, since LogicalTreeWalker.Descendants recurses through that nesting
/// for free -- this is what gives the whole toolbar, including embedded toggle groups, a single
/// shared Tab stop per docs/parity/toolbar.md ("Crucially, NaviusToolbarToggleGroup does NOT
/// create its own roving-focus controller").
///
/// WPF's own ToolBar control is intentionally not used or derived from: it brings an
/// overflow-menu/ToolBarTray model this contract does not have (see the doc's "Open questions").
/// This is a plain ContentControl, closer to RadioGroup/ToggleGroup than to native ToolBar.
///
/// Known simplification: the tab stop is recomputed fresh on content-change and on every
/// keypress rather than tracked via per-item IsEnabledChanged subscriptions, so the contract's
/// "an item's disabled state flips at runtime notifies peers so the seated Tab stop can move" is
/// only reproduced at the next roving interaction, not the instant a descendant's IsEnabled
/// changes off-cycle. RadioGroup/ToggleGroup make the same simplification for their own
/// same-type item disable cascades.
/// </summary>
public class NaviusToolbar : ContentControl
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(string),
        typeof(NaviusToolbar),
        new PropertyMetadata("horizontal"));

    public static readonly DependencyProperty LoopProperty = DependencyProperty.Register(
        nameof(Loop),
        typeof(bool),
        typeof(NaviusToolbar),
        new PropertyMetadata(true));

    public static readonly DependencyProperty DirProperty = DependencyProperty.Register(
        nameof(Dir),
        typeof(string),
        typeof(NaviusToolbar),
        new PropertyMetadata(null));

    static NaviusToolbar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusToolbar),
            new FrameworkPropertyMetadata(typeof(NaviusToolbar)));
    }

    public NaviusToolbar()
    {
        Focusable = false;
        KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);
        PreviewKeyDown += HandlePreviewKeyDown;
    }

    /// <summary>"horizontal" (default) or "vertical"; drives the roving-focus arrow-key axis.</summary>
    public string Orientation
    {
        get => (string)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Arrow navigation wraps past first/last when true (default).</summary>
    public bool Loop
    {
        get => (bool)GetValue(LoopProperty);
        set => SetValue(LoopProperty, value);
    }

    /// <summary>Reading direction; falls back to FlowDirection when unset.</summary>
    public string? Dir
    {
        get => (string?)GetValue(DirProperty);
        set => SetValue(DirProperty, value);
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateRovingTabStops();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusToolbarAutomationPeer(this);

    private void UpdateRovingTabStops()
    {
        var items = GetItems();
        if (items.Count == 0)
        {
            return;
        }

        var tabStop = items.FirstOrDefault(i => i.IsEnabled);
        foreach (var item in items)
        {
            item.IsTabStop = ReferenceEquals(item, tabStop);
        }
    }

    // Named distinctly from UIElement.OnPreviewKeyDown (a different, single-argument virtual
    // method) so reflection-based test lookups by name stay unambiguous, matching
    // RadioGroup/ToggleGroup's own HandlePreviewKeyDown naming.
    private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        var items = GetItems();
        if (items.Count == 0)
        {
            return;
        }

        var isVertical = string.Equals(Orientation, "vertical", StringComparison.OrdinalIgnoreCase);
        var isRtl = string.Equals(Dir, "rtl", StringComparison.OrdinalIgnoreCase)
            || (Dir is null && FlowDirection == FlowDirection.RightToLeft);

        Control? target = e.Key switch
        {
            Key.Right when !isVertical => Move(items, isRtl ? -1 : 1),
            Key.Left when !isVertical => Move(items, isRtl ? 1 : -1),
            Key.Down when isVertical => Move(items, 1),
            Key.Up when isVertical => Move(items, -1),
            Key.Home => items.FirstOrDefault(i => i.IsEnabled),
            Key.End => items.LastOrDefault(i => i.IsEnabled),
            _ => null,
        };

        if (target is null)
        {
            return;
        }

        foreach (var item in items)
        {
            item.IsTabStop = ReferenceEquals(item, target);
        }

        FocusManager.SetFocusedElement(this, target);
        e.Handled = true;
    }

    private Control? Move(List<Control> items, int delta)
    {
        var focused = FocusManager.GetFocusedElement(this) as Control;
        var currentIndex = focused is null ? -1 : items.IndexOf(focused);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var index = currentIndex;
        for (var step = 0; step < items.Count; step++)
        {
            index += delta;

            if (Loop)
            {
                index = ((index % items.Count) + items.Count) % items.Count;
            }
            else if (index < 0 || index >= items.Count)
            {
                return null;
            }

            if (items[index].IsEnabled)
            {
                return items[index];
            }
        }

        return null;
    }

    /// <summary>
    /// Every IToolbarItem-marked Control in DOM/registration order, regardless of nesting under a
    /// NaviusToolbarToggleGroup -- the source of the single shared roving domain described above.
    /// </summary>
    private List<Control> GetItems() =>
        LogicalTreeWalker.Descendants<Control>(this).OfType<IToolbarItem>().Cast<Control>().ToList();
}
