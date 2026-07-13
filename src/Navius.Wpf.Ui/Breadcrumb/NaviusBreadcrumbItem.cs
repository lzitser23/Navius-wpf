using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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

    /// <summary>
    /// Runs the same activation path as a mouse click or Enter/Space (raise Click, execute the bound
    /// Command), so a UIA Invoke drives the existing behavior without duplicating it. Called by the
    /// peer's IInvokeProvider.Invoke; the peer only exposes Invoke on a non-current crumb.
    /// </summary>
    internal void AutomationInvoke() => Activate();

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

/// <summary>
/// Reports Hyperlink for a navigable crumb and Text for the current page. A non-current crumb also
/// exposes UIA InvokePattern so assistive tech can activate it (mouse and keyboard already run the
/// same activation path); the current page reports Text and exposes no Invoke pattern.
/// </summary>
internal sealed class NaviusBreadcrumbItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    public NaviusBreadcrumbItemAutomationPeer(NaviusBreadcrumbItem owner) : base(owner)
    {
    }

    private NaviusBreadcrumbItem Item => (NaviusBreadcrumbItem)Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        Item.IsCurrentPage ? AutomationControlType.Text : AutomationControlType.Hyperlink;

    protected override string GetClassNameCore() => nameof(NaviusBreadcrumbItem);

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Invoke && !Item.IsCurrentPage
            ? this
            : base.GetPattern(patternInterface);

    void IInvokeProvider.Invoke()
    {
        // GetPattern only hands out this provider for a non-current crumb, but a UIA client can cache
        // the provider while the crumb is navigable and call Invoke after it becomes the current page.
        // Guard IsCurrentPage here too so a cached provider cannot activate the terminal, non-navigable
        // entry (which exposes no Invoke pattern and runs no activation path).
        if (!Item.IsEnabled || Item.IsCurrentPage)
        {
            throw new ElementNotEnabledException();
        }

        // The UIA IInvokeProvider.Invoke contract requires this call to return immediately, so queue
        // the activation onto the owner's dispatcher rather than running it inline, matching WPF's
        // native ButtonAutomationPeer. This keeps the UIA client from blocking when activation opens
        // a modal dialog or does other synchronous work.
        Item.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(Item.AutomationInvoke));
    }
}
