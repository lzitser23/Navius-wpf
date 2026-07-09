using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Primitives.Controls.NavigationMenu;

/// <summary>
/// Tier B: hosts the top-level trigger/link row and its roving-focus controller. Cycles focus
/// across <see cref="NaviusNavigationMenuTrigger"/> and top-level <see cref="NaviusNavigationMenuLink"/>
/// descendants ("rovable" elements), horizontally or vertically depending on the ambient host's
/// <see cref="NavigationMenuHostBase.Orientation"/>. Mirrors NaviusRadioGroup's own
/// PreviewKeyDown-based roving implementation (native WPF has no aria-orientation-aware
/// composite-widget primitive to reuse here either).
/// </summary>
public class NaviusNavigationMenuList : ItemsControl
{
    static NaviusNavigationMenuList()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusNavigationMenuList),
            new FrameworkPropertyMetadata(typeof(NaviusNavigationMenuList)));
    }

    private static readonly ItemsPanelTemplate HorizontalPanel = CreatePanel(Orientation.Horizontal);
    private static readonly ItemsPanelTemplate VerticalPanel = CreatePanel(Orientation.Vertical);

    public NaviusNavigationMenuList()
    {
        KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.None);
        PreviewKeyDown += OnPreviewKeyDown;
        Loaded += (_, _) =>
        {
            ApplyOrientationPanel();
            UpdateRovingTabStops();
        };
    }

    private void ApplyOrientationPanel()
    {
        var host = NavigationMenuHostBase.GetHost(this);
        var isVertical = string.Equals(host?.Orientation, "vertical", StringComparison.OrdinalIgnoreCase);
        ItemsPanel = isVertical ? VerticalPanel : HorizontalPanel;
    }

    private static ItemsPanelTemplate CreatePanel(Orientation orientation)
    {
        var factory = new FrameworkElementFactory(typeof(StackPanel));
        factory.SetValue(StackPanel.OrientationProperty, orientation);
        return new ItemsPanelTemplate(factory);
    }

    private IEnumerable<UIElement> Rovables => FindRovables(this);

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var host = NavigationMenuHostBase.GetHost(this);
        var isVertical = string.Equals(host?.Orientation, "vertical", StringComparison.OrdinalIgnoreCase);

        var items = Rovables.ToList();
        if (items.Count == 0)
        {
            return;
        }

        var forwardKey = isVertical ? Key.Down : Key.Right;
        var backwardKey = isVertical ? Key.Up : Key.Left;

        UIElement? target = e.Key switch
        {
            _ when e.Key == forwardKey => Move(items, 1),
            _ when e.Key == backwardKey => Move(items, -1),
            Key.Home => items.FirstOrDefault(),
            Key.End => items.LastOrDefault(),
            _ => null,
        };

        if (target is null)
        {
            return;
        }

        Keyboard.Focus(target);
        e.Handled = true;
    }

    private UIElement? Move(List<UIElement> items, int delta)
    {
        var currentIndex = items.FindIndex(i => i is IInputElement input && input.IsKeyboardFocused);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var index = currentIndex + delta;
        if (index < 0 || index >= items.Count)
        {
            return null;
        }

        return items[index];
    }

    internal void UpdateRovingTabStops()
    {
        var host = NavigationMenuHostBase.GetHost(this);
        var items = Rovables.ToList();
        if (items.Count == 0)
        {
            return;
        }

        var openTrigger = items.OfType<NaviusNavigationMenuTrigger>()
            .FirstOrDefault(t => host is not null && string.Equals(t.OwningValue, host.Value, StringComparison.Ordinal));

        var tabStop = (UIElement?)openTrigger ?? items[0];

        foreach (var item in items)
        {
            if (item is Control control)
            {
                control.IsTabStop = ReferenceEquals(item, tabStop);
            }
        }
    }

    private static IEnumerable<UIElement> FindRovables(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject childObj)
            {
                continue;
            }

            if (childObj is NaviusNavigationMenuTrigger or NaviusNavigationMenuLink)
            {
                yield return (UIElement)childObj;
                continue;
            }

            foreach (var descendant in FindRovables(childObj))
            {
                yield return descendant;
            }
        }
    }
}
