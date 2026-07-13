using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Navius.Wpf.Ui.ButtonGroup;

/// <summary>
/// Segmented button row: an ItemsControl whose children are <see cref="NaviusButtonGroupItem"/>
/// instances placed directly by the consumer (a plain UIElement collection, like a StackPanel's
/// Children, not a data-bound ItemsSource). The single radius envelope comes from a clip on the
/// group's own container in Themes/ButtonGroup.xaml (every item stays a plain square segment; the
/// rounded silhouette is masked in at the group boundary, so no per-item corner math is needed).
/// Each item still needs to know whether it is the last one, so only its own trailing edge grows a
/// second hairline (every other item's single border already becomes the shared divider with its
/// next sibling); see <see cref="IsLastItemProperty"/>.
/// </summary>
public class NaviusButtonGroup : ItemsControl
{
    /// <summary>
    /// Registered via RegisterAttached, not Register: Register-applied Inherits metadata only
    /// resolves on the owner type and never propagates to other element types, so the item
    /// template's "(buttonGroup:NaviusButtonGroup.Orientation)" trigger read the Horizontal
    /// default forever (same gap fixed for NaviusSidebar.IsCollapsed). Attached registration
    /// makes the inheriting metadata the default for every element type, which is what lets each
    /// item read the group's value.
    /// </summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.RegisterAttached(
        nameof(Orientation), typeof(Orientation), typeof(NaviusButtonGroup),
        new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.Inherits));

    private static readonly DependencyPropertyKey IsLastItemPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "IsLastItem", typeof(bool), typeof(NaviusButtonGroup), new PropertyMetadata(false));

    public static readonly DependencyProperty IsLastItemProperty = IsLastItemPropertyKey.DependencyProperty;

    static NaviusButtonGroup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusButtonGroup),
            new FrameworkPropertyMetadata(typeof(NaviusButtonGroup)));
    }

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Attached-property accessor: the inherited orientation on any descendant.</summary>
    public static Orientation GetOrientation(DependencyObject element) => (Orientation)element.GetValue(OrientationProperty);

    /// <summary>Attached-property accessor; on the group itself prefer <see cref="Orientation"/>.</summary>
    public static void SetOrientation(DependencyObject element, Orientation value) => element.SetValue(OrientationProperty, value);

    public static bool GetIsLastItem(DependencyObject obj) => (bool)obj.GetValue(IsLastItemProperty);

    /// <summary>Pure position computation, split out for direct unit testing (no container/generator needed).</summary>
    public static bool IsLast(int index, int count) => count > 0 && index == count - 1;

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        // Index comes from Items.IndexOf(item), not ItemContainerGenerator.IndexFromContainer:
        // the generator's index map is only populated once its own generation pass has run, so
        // relying on it here would make this unreachable from a direct/manual call (as in unit
        // tests) and racy relative to WPF's own generation order. Items.IndexOf needs neither.
        StampPosition(element, Items.IndexOf(item));
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        // Container generation for newly added/removed items can complete after this call
        // returns; deferring to Loaded priority ensures every container exists before positions
        // are (re)stamped, so an earlier "last" item correctly loses that status once a new one
        // is appended after it.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, RefreshAllPositions);
    }

    private void RefreshAllPositions()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(i) is DependencyObject container)
            {
                StampPosition(container, i);
            }
        }
    }

    private void StampPosition(DependencyObject container, int index) =>
        container.SetValue(IsLastItemPropertyKey, IsLast(index, Items.Count));
}
