using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Ui.CollectionPicker;

/// <summary>
/// A token-styled collection selector. Selection, keyboard navigation, item templates,
/// items panels, and UI Automation are inherited from WPF's native <see cref="ListBox"/>.
/// </summary>
public class NaviusCollectionPicker : ListBox
{
    static NaviusCollectionPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCollectionPicker),
            new FrameworkPropertyMetadata(typeof(NaviusCollectionPicker)));
    }

    protected override DependencyObject GetContainerForItemOverride() => new NaviusCollectionPickerItem();

    protected override bool IsItemItsOwnContainerOverride(object item) => item is NaviusCollectionPickerItem;
}

/// <summary>The selectable item container used by <see cref="NaviusCollectionPicker"/>.</summary>
public class NaviusCollectionPickerItem : ListBoxItem
{
    static NaviusCollectionPickerItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCollectionPickerItem),
            new FrameworkPropertyMetadata(typeof(NaviusCollectionPickerItem)));
    }
}
