using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Tier A: derives from the native Label. WPF's Label.Target only activates the target via
/// an access-key/mnemonic, not a plain mouse click the way HTML for= does, so click-to-focus
/// and the double/triple-click text-selection-suppression guard (mirroring the source's
/// e.Detail > 1 check) are authored explicitly here rather than translated from existing
/// code, since the Blazor component gets that behavior for free from the browser.
///
/// The contract's string `For` (a DOM id) is kept as a string dependency property rather
/// than becoming a bound object reference: it is resolved to a FrameworkElement by name
/// lookup (mirroring an HTML id lookup) the first time it's needed, walking up from this
/// element to find the nearest NameScope that has it. AutomationProperties.LabeledBy is set
/// on the resolved target to preserve the DOM for='s screen-reader label association.
/// </summary>
public class NaviusLabel : Label
{
    public static readonly DependencyProperty ForProperty = DependencyProperty.Register(
        nameof(For),
        typeof(string),
        typeof(NaviusLabel),
        new PropertyMetadata(null, OnForChanged));

    static NaviusLabel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusLabel),
            new FrameworkPropertyMetadata(typeof(NaviusLabel)));
    }

    public NaviusLabel()
    {
        Loaded += (_, _) => ResolveAndWireTarget();
    }

    /// <summary>The id of the associated control, resolved via name lookup (contract's For).</summary>
    public string? For
    {
        get => (string?)GetValue(ForProperty);
        set => SetValue(ForProperty, value);
    }

    private static void OnForChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusLabel)d).ResolveAndWireTarget();

    private void ResolveAndWireTarget()
    {
        var resolved = ResolveTarget();
        if (resolved is not null)
        {
            Target = resolved;
            AutomationProperties.SetLabeledBy(resolved, this);
        }
    }

    private FrameworkElement? ResolveTarget()
    {
        if (Target is FrameworkElement alreadyResolved)
        {
            return alreadyResolved;
        }

        if (string.IsNullOrEmpty(For))
        {
            return null;
        }

        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is FrameworkElement scopeCandidate && scopeCandidate.FindName(For) is FrameworkElement found)
            {
                return found;
            }

            current = LogicalTreeHelper.GetParent(current) ?? VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ClickCount > 1)
        {
            // Mirrors the source's `_preventSelect` guard (e.Detail > 1): suppress default
            // handling on double/triple click instead of moving focus a second time.
            e.Handled = true;
            return;
        }

        (Target as FrameworkElement ?? ResolveTarget())?.Focus();
    }
}
