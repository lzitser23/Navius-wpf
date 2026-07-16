using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Navius.Wpf.Primitives.Theming;
using Navius.Wpf.Ui.ButtonGroup;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiButtonGroupTests
{
    static UiButtonGroupTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check) because xunit runs test classes in
        // parallel on separate STA threads: another test class's static ctor can win the race.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    [StaFact]
    public void NaviusButtonGroupItem_ContentAlignment_ExplicitLeft_ForwardsToContentPresenter()
    {
        // Regression: the ContentPresenter hardcoded Center and ignored HorizontalContentAlignment.
        var content = new Border { Width = 20, Height = 10 };
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Ui;component/Themes/ButtonGroup.xaml"),
        });
        var item = new NaviusButtonGroupItem
        {
            Content = content,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Width = 200,
            Height = 40,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Resources = scope,
        };

        item.ApplyTemplate();
        item.Measure(new Size(200, 40));
        item.Arrange(new Rect(0, 0, 200, 40));

        var offset = content.TranslatePoint(new Point(0, 0), item);
        Assert.Equal(0, offset.X, 3);
    }

    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_ReportsButtonControlType()
    {
        var item = new NaviusButtonGroupItem();

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.Button, peer!.GetAutomationControlType());
    }

    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_ExposesInvokePattern()
    {
        var item = new NaviusButtonGroupItem();

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);

        Assert.IsAssignableFrom<IInvokeProvider>(peer!.GetPattern(PatternInterface.Invoke));
    }

    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_DisabledInvoke_Throws()
    {
        var item = new NaviusButtonGroupItem { IsEnabled = false };

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        var invoke = (IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke);

        Assert.Throws<ElementNotEnabledException>(() => invoke.Invoke());
    }

    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_EnabledInvoke_RaisesClick()
    {
        var item = new NaviusButtonGroupItem();
        var clicked = false;
        item.Click += (_, _) => clicked = true;

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        ((IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke)).Invoke();

        // Invoke queues activation on the dispatcher per the UIA contract, so it has not run yet;
        // pump at Background priority (below the Input priority the peer queues at) to flush it.
        PumpDispatcher();

        Assert.True(clicked);
    }

    [StaFact]
    public void NaviusButtonGroupItem_AutomationPeer_EnabledInvoke_ExecutesBoundCommandWithParameter()
    {
        object? received = null;
        var executions = 0;
        var command = new RelayCommand(p => { received = p; executions++; }, _ => true);
        var parameter = new object();
        var clicks = 0;
        var item = new NaviusButtonGroupItem { Command = command, CommandParameter = parameter };
        item.Click += (_, _) => clicks++;

        var peer = UIElementAutomationPeer.CreatePeerForElement(item);
        ((IInvokeProvider)peer!.GetPattern(PatternInterface.Invoke)).Invoke();

        // Invoke is queued on the dispatcher (UIA contract); pump before asserting the command ran.
        PumpDispatcher();

        Assert.Equal(1, executions);
        Assert.Same(parameter, received);
        Assert.Equal(1, clicks);
    }

    [StaFact]
    public void NaviusButtonGroup_Orientation_InheritsToRealizedItems()
    {
        var (group, first, _) = CreateThemedGroup();

        Assert.Equal(Orientation.Horizontal, NaviusButtonGroup.GetOrientation(first));

        group.Orientation = Orientation.Vertical;

        // Regression: Orientation was registered with Register rather than RegisterAttached, so
        // its Inherits metadata never left NaviusButtonGroup itself -- items always read the
        // Horizontal default and the vertical divider trigger below never fired.
        Assert.Equal(Orientation.Vertical, NaviusButtonGroup.GetOrientation(first));
    }

    [StaFact]
    public void NaviusButtonGroup_VerticalOrientation_MovesNonLastItemDividerToBottomEdge()
    {
        var (group, first, last) = CreateThemedGroup();

        // Horizontal default: a non-last item's open trailing (right) edge is the shared divider
        // with its next sibling; the last item closes the run with a full border.
        Assert.Equal(new Thickness(1, 1, 0, 1), first.BorderThickness);
        Assert.Equal(new Thickness(1), last.BorderThickness);

        group.Orientation = Orientation.Vertical;
        group.UpdateLayout();

        // Vertical: the open edge moves to the bottom; the last item still keeps its full border.
        Assert.Equal(new Thickness(1, 1, 1, 0), first.BorderThickness);
        Assert.Equal(new Thickness(1), last.BorderThickness);

        group.Orientation = Orientation.Horizontal;
        group.UpdateLayout();

        Assert.Equal(new Thickness(1, 1, 0, 1), first.BorderThickness);
    }

    /// <summary>
    /// A themed two-item group, laid out so the items are realized containers with the implicit
    /// item style applied (the Style.Triggers under test set BorderThickness on the item itself).
    /// </summary>
    private static (NaviusButtonGroup Group, NaviusButtonGroupItem First, NaviusButtonGroupItem Last) CreateThemedGroup()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Ui;component/Themes/ButtonGroup.xaml"),
        });

        var group = new NaviusButtonGroup
        {
            Resources = scope,
            Style = (Style)scope[typeof(NaviusButtonGroup)],
        };
        var first = new NaviusButtonGroupItem { Content = "One" };
        var last = new NaviusButtonGroupItem { Content = "Two" };
        group.Items.Add(first);
        group.Items.Add(last);

        group.ApplyTemplate();
        group.Measure(new Size(300, 100));
        group.Arrange(new Rect(0, 0, 300, 100));
        group.UpdateLayout();

        // IsLastItem (re)stamping defers to Loaded priority in OnItemsChanged; flush it so the
        // last item's full-border trigger state is settled before assertions.
        PumpDispatcher();

        return (group, first, last);
    }

    private static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool> _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => _canExecute(parameter);

        public void Execute(object? parameter) => _execute(parameter);
    }
}
