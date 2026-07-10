using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;

namespace Navius.Wpf.Ui.Breadcrumb;

/// <summary>
/// A single crumb. Folds shadcn's separate BreadcrumbLink/BreadcrumbPage parts into one control
/// (same folding precedent as NaviusOverlaySurfaceBase): <see cref="IsCurrentPage"/> false renders
/// as a clickable link (wired to <see cref="Command"/>/<see cref="Click"/>), true renders as plain,
/// non-interactive text and sets the current-page semantics that would otherwise be
/// aria-current="page" via <see cref="AutomationProperties.ItemStatusProperty"/> (WPF/UIA has no
/// dedicated "current page" concept; ItemStatus is the closest supported hook for assistive tech).
/// </summary>
public class NaviusBreadcrumbItem : ContentControl, ICommandSource
{
    public static readonly DependencyProperty IsCurrentPageProperty = DependencyProperty.Register(
        nameof(IsCurrentPage), typeof(bool), typeof(NaviusBreadcrumbItem),
        new PropertyMetadata(false, OnIsCurrentPageChanged));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(NaviusBreadcrumbItem), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(NaviusBreadcrumbItem), new PropertyMetadata(null));

    public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(
        nameof(CommandTarget), typeof(IInputElement), typeof(NaviusBreadcrumbItem), new PropertyMetadata(null));

    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
        nameof(Click), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusBreadcrumbItem));

    static NaviusBreadcrumbItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusBreadcrumbItem),
            new FrameworkPropertyMetadata(typeof(NaviusBreadcrumbItem)));
    }

    public NaviusBreadcrumbItem()
    {
        Focusable = true;
    }

    /// <summary>True for the trail's terminal, non-navigable entry (the current page).</summary>
    public bool IsCurrentPage
    {
        get => (bool)GetValue(IsCurrentPageProperty);
        set => SetValue(IsCurrentPageProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IInputElement? CommandTarget
    {
        get => (IInputElement?)GetValue(CommandTargetProperty);
        set => SetValue(CommandTargetProperty, value);
    }

    /// <summary>Raised on activation of a non-current crumb (mouse click or Enter/Space).</summary>
    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (!IsCurrentPage)
        {
            Activate();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsCurrentPage && (e.Key == Key.Enter || e.Key == Key.Space))
        {
            Activate();
            e.Handled = true;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusBreadcrumbItemAutomationPeer(this);

    private void Activate()
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));

        if (Command is null)
        {
            return;
        }

        var target = CommandTarget ?? this;
        if (Command is RoutedCommand routedCommand)
        {
            if (routedCommand.CanExecute(CommandParameter, target))
            {
                routedCommand.Execute(CommandParameter, target);
            }
        }
        else if (Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }

    private static void OnIsCurrentPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (NaviusBreadcrumbItem)d;
        if ((bool)e.NewValue)
        {
            AutomationProperties.SetItemStatus(item, "current");
            item.Focusable = false;
        }
        else
        {
            item.ClearValue(AutomationProperties.ItemStatusProperty);
            item.Focusable = true;
        }
    }
}

internal sealed class NaviusBreadcrumbItemAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusBreadcrumbItemAutomationPeer(NaviusBreadcrumbItem owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        ((NaviusBreadcrumbItem)Owner).IsCurrentPage ? AutomationControlType.Text : AutomationControlType.Hyperlink;

    protected override string GetClassNameCore() => nameof(NaviusBreadcrumbItem);
}
