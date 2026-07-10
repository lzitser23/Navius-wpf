using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Toolbar;

/// <summary>
/// Tier B: derives from the native Button (not NaviusButton) so it inherits Command/
/// CommandParameter/CommandTarget for free, matching the contract's "Uri/Command surface" for
/// what the web version renders as `&lt;a data-navius-toolbar-item&gt;`. Deliberately has no
/// Disabled property: the contract states the link "has no intrinsic disabled state in HTML and
/// always participates in roving," so unlike NaviusToolbarButton this never opts out of the
/// roving-focus scan via a soft-disabled mode -- a consumer can still set the native IsEnabled
/// directly, which is excluded from roving like any other item, but that is not a
/// contract-modeled "Disabled" concept here.
/// </summary>
public class NaviusToolbarLink : System.Windows.Controls.Button, IToolbarItem
{
    public static readonly DependencyProperty UriProperty = DependencyProperty.Register(
        nameof(Uri),
        typeof(System.Uri),
        typeof(NaviusToolbarLink),
        new PropertyMetadata(null));

    static NaviusToolbarLink()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusToolbarLink),
            new FrameworkPropertyMetadata(typeof(NaviusToolbarLink)));
    }

    /// <summary>The link's navigation target; the WPF analog of the web contract's `href` (supplied via Attributes there).</summary>
    public System.Uri? Uri
    {
        get => (System.Uri?)GetValue(UriProperty);
        set => SetValue(UriProperty, value);
    }
}
