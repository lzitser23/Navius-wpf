using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Breadcrumb;

/// <summary>
/// Decorative chevron glyph placed by the consumer between two <see cref="NaviusBreadcrumbItem"/>
/// instances (same manual-placement anatomy as shadcn's BreadcrumbSeparator). Purely presentational:
/// excluded from the keyboard tab order and reported as a non-control automation element so screen
/// readers skip it rather than announcing a meaningless "separator" between crumb names.
/// </summary>
public class NaviusBreadcrumbSeparator : Control
{
    static NaviusBreadcrumbSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusBreadcrumbSeparator),
            new FrameworkPropertyMetadata(typeof(NaviusBreadcrumbSeparator)));
    }

    public NaviusBreadcrumbSeparator()
    {
        Focusable = false;
        IsTabStop = false;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusBreadcrumbSeparatorAutomationPeer(this);
}

internal sealed class NaviusBreadcrumbSeparatorAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusBreadcrumbSeparatorAutomationPeer(NaviusBreadcrumbSeparator owner) : base(owner)
    {
    }

    protected override bool IsControlElementCore() => false;

    protected override string GetClassNameCore() => nameof(NaviusBreadcrumbSeparator);
}
