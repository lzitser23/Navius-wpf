using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.Breadcrumb;

/// <summary>
/// Root nav landmark. Mirrors the shadcn/ui Breadcrumb anatomy: an ItemsControl whose Items are
/// simply <see cref="NaviusBreadcrumbItem"/> and <see cref="NaviusBreadcrumbSeparator"/> instances
/// interleaved by the consumer (same manual-separator-placement contract as shadcn's own
/// BreadcrumbList/BreadcrumbSeparator anatomy), wrapped in a horizontal, wrapping panel so a long
/// trail reflows on narrow widths instead of clipping.
/// </summary>
public class NaviusBreadcrumb : ItemsControl
{
    static NaviusBreadcrumb()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusBreadcrumb),
            new FrameworkPropertyMetadata(typeof(NaviusBreadcrumb)));
    }

    public NaviusBreadcrumb()
    {
        // "nav" landmark equivalent: this is a navigation trail, not a generic list.
        System.Windows.Automation.AutomationProperties.SetItemStatus(this, "breadcrumb");
    }
}
