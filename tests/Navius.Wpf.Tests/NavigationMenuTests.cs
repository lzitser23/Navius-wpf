using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Navius.Wpf.Primitives.Controls.NavigationMenu;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class NavigationMenuTests : IDisposable
{
    static NavigationMenuTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class won the race; Application.Current is now set.
            }
        }
    }

    private static readonly MethodInfo ButtonOnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo RegisterTriggerMethod =
        typeof(NavigationMenuHostBase).GetMethod("RegisterTrigger", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo ContentSubscribeMethod =
        typeof(NaviusNavigationMenuContent).GetMethod("Subscribe", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo BackdropSubscribeMethod =
        typeof(NaviusNavigationMenuBackdrop).GetMethod("Subscribe", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo ListPreviewKeyDownMethod = typeof(NaviusNavigationMenuList).GetMethod(
        "OnPreviewKeyDown",
        BindingFlags.NonPublic | BindingFlags.Instance,
        null,
        new[] { typeof(object), typeof(KeyEventArgs) },
        null)!;

    private static readonly ConstructorInfo KeyEventArgsCtor = typeof(KeyEventArgs).GetConstructor(
        new[] { typeof(KeyboardDevice), typeof(PresentationSource), typeof(int), typeof(Key) })!;

    // Lazily created (not a static field initializer) and disposed per test instance -- this
    // dummy 0x0 native window must not outlive the STA thread it was created on.
    private HwndSource? _testSource;

    private PresentationSource TestSource =>
        _testSource ??= new HwndSource(0, 0, 0, 0, 0, "NaviusNavigationMenuTests", IntPtr.Zero);

    public void Dispose()
    {
        _testSource?.Dispose();
        TestCleanup.PumpDispatcher();
    }

    private static void SimulateClick(ButtonBase button) => ButtonOnClickMethod.Invoke(button, null);

    private void SimulateKey(NaviusNavigationMenuList list, Key key)
    {
        var args = (KeyEventArgs)KeyEventArgsCtor.Invoke(new object?[] { Keyboard.PrimaryDevice, TestSource, 0, key });
        args.RoutedEvent = Keyboard.PreviewKeyDownEvent;
        ListPreviewKeyDownMethod.Invoke(list, new object[] { list, args });
        Assert.True(args.Handled);
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/NavigationMenu.xaml"),
        });

        return scope;
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();

        var root = new NaviusNavigationMenu { Resources = scope };
        var sub = new NaviusNavigationMenuSub { Resources = scope };
        var list = new NaviusNavigationMenuList { Resources = scope };
        var item = new NaviusNavigationMenuItem { Resources = scope };
        var trigger = new NaviusNavigationMenuTrigger { Resources = scope };
        var icon = new NaviusNavigationMenuIcon { Resources = scope };
        var link = new NaviusNavigationMenuLink { Resources = scope };
        var arrow = new NaviusNavigationMenuArrow { Resources = scope };
        var viewport = new NaviusNavigationMenuViewport { Resources = scope };
        var content = new NaviusNavigationMenuContent { Resources = scope };
        var backdrop = new NaviusNavigationMenuBackdrop { Resources = scope };

        Assert.True(root.ApplyTemplate());
        Assert.True(sub.ApplyTemplate());
        Assert.True(list.ApplyTemplate());
        Assert.True(item.ApplyTemplate());
        Assert.True(trigger.ApplyTemplate());
        Assert.True(icon.ApplyTemplate());
        Assert.True(link.ApplyTemplate());
        Assert.True(viewport.ApplyTemplate());
        Assert.True(content.ApplyTemplate());
        Assert.True(backdrop.ApplyTemplate());
        Assert.Equal(10.0, arrow.Width);
        Assert.Equal(5.0, arrow.Height);
    }

    [StaFact]
    public void UseSharedViewport_True_ThrowsNotSupported()
    {
        var root = new NaviusNavigationMenu();

        Assert.Throws<NotSupportedException>(() => root.UseSharedViewport = true);
    }

    [StaFact]
    public void Host_RequestOpen_SetsValueAndRaisesEvents()
    {
        var host = new NaviusNavigationMenu();
        var changed = new List<string?>();
        var completed = new List<bool>();
        host.ValueChanged += (_, v) => changed.Add(v);
        host.OpenChangeComplete += (_, open) => completed.Add(open);

        host.RequestOpen("products");

        Assert.Equal("products", host.Value);
        Assert.True(host.Open);
        Assert.Equal(new string?[] { "products" }, changed);
        Assert.Equal(new[] { true }, completed);
    }

    [StaFact]
    public void Host_RequestClose_OnlyClosesMatchingValue()
    {
        var host = new NaviusNavigationMenu();
        host.RequestOpen("products");

        host.RequestClose("other");
        Assert.Equal("products", host.Value);

        host.RequestClose("products");
        Assert.Null(host.Value);
        Assert.False(host.Open);
    }

    [StaFact]
    public void Host_Toggle_OpensThenCloses()
    {
        var host = new NaviusNavigationMenu();

        host.Toggle("products");
        Assert.Equal("products", host.Value);

        host.Toggle("products");
        Assert.Null(host.Value);
    }

    [StaFact]
    public void Item_Owner_ResolvesThroughAmbientInheritance()
    {
        var item = new NaviusNavigationMenuItem { Value = "products" };
        var probe = new ContentControl();
        item.Content = probe;

        Assert.Same(item, NaviusNavigationMenuItem.GetOwner(probe));
    }

    [StaFact]
    public void Trigger_OwningValue_ResolvesFromAmbientItem()
    {
        var item = new NaviusNavigationMenuItem { Value = "products" };
        var trigger = new NaviusNavigationMenuTrigger();
        item.Content = trigger;

        Assert.Equal("products", trigger.OwningValue);
    }

    [StaFact]
    public void Trigger_Click_TogglesHostValue()
    {
        var host = new NaviusNavigationMenu();
        var item = new NaviusNavigationMenuItem { Value = "products" };
        var trigger = new NaviusNavigationMenuTrigger();
        item.Content = trigger;
        host.Content = item;

        SimulateClick(trigger);
        Assert.Equal("products", host.Value);

        SimulateClick(trigger);
        Assert.Null(host.Value);
    }

    [StaFact]
    public void Trigger_ExpandCollapseProvider_ReflectsAndDrivesHostValue()
    {
        var host = new NaviusNavigationMenu();
        var item = new NaviusNavigationMenuItem { Value = "products" };
        var trigger = new NaviusNavigationMenuTrigger();
        item.Content = trigger;
        host.Content = item;

        var peer = (AutomationPeer)typeof(UIElement)
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(trigger, null)!;

        var provider = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!;
        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);

        provider.Expand();
        Assert.Equal("products", host.Value);
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);

        provider.Collapse();
        Assert.Null(host.Value);
        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
    }

    [StaFact]
    public void DisabledTrigger_ExpandCollapseProvider_ThrowsAndDoesNotOpen()
    {
        // Regression (DEFECT 2 sweep): a disabled navigation-menu trigger must not be expandable
        // through UIA. Disabled maps onto the inherited IsEnabled for this control.
        var host = new NaviusNavigationMenu();
        var item = new NaviusNavigationMenuItem { Value = "products" };
        var trigger = new NaviusNavigationMenuTrigger { IsEnabled = false };
        item.Content = trigger;
        host.Content = item;

        var peer = (AutomationPeer)typeof(UIElement)
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(trigger, null)!;

        Assert.False(peer.IsEnabled());

        var provider = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!;
        Assert.Throws<ElementNotEnabledException>(() => provider.Expand());
        Assert.Null(host.Value);
    }

    [StaFact]
    public void Content_Subscribe_TracksHostValueAndResolvesAnchor()
    {
        var host = new NaviusNavigationMenu();
        var item = new NaviusNavigationMenuItem { Value = "products" };
        var trigger = new NaviusNavigationMenuTrigger();
        var content = new NaviusNavigationMenuContent();
        item.Content = new StackPanel { Children = { trigger, content } };
        host.Content = item;

        RegisterTriggerMethod.Invoke(host, new object[] { "products", trigger });
        ContentSubscribeMethod.Invoke(content, null);

        Assert.False(content.IsOpen);
        Assert.Same(trigger, content.Anchor);

        host.Value = "products";
        Assert.True(content.IsOpen);

        host.Value = null;
        Assert.False(content.IsOpen);
    }

    [StaFact]
    public void Backdrop_Subscribe_TracksHostOpenState()
    {
        var host = new NaviusNavigationMenu();
        var backdrop = new NaviusNavigationMenuBackdrop();
        host.Content = backdrop;

        BackdropSubscribeMethod.Invoke(backdrop, null);

        Assert.False(backdrop.IsOpen);
        Assert.Equal(Visibility.Collapsed, backdrop.Visibility);

        host.Value = "products";
        Assert.True(backdrop.IsOpen);
        Assert.Equal(Visibility.Visible, backdrop.Visibility);
    }

    [StaFact]
    public void Link_Active_SetsAutomationItemStatusToPage()
    {
        var link = new NaviusNavigationMenuLink { Active = true };

        Assert.Equal("page", AutomationProperties.GetItemStatus(link));

        link.Active = false;
        Assert.Equal(string.Empty, AutomationProperties.GetItemStatus(link));
    }

    [StaFact]
    public void Link_Select_PreventDefault_StillRaisesClickButSkipsBase()
    {
        var link = new NaviusNavigationMenuLink();
        var selectRaised = 0;
        link.Select += (_, args) =>
        {
            selectRaised++;
            args.PreventDefault();
        };
        var clickRaised = 0;
        link.Click += (_, _) => clickRaised++;

        SimulateClick(link);

        Assert.Equal(1, selectRaised);
        Assert.Equal(1, clickRaised);
    }

    [StaFact]
    public void Sub_EstablishesOwnHostScope()
    {
        var root = new NaviusNavigationMenu();
        var sub = new NaviusNavigationMenuSub();
        root.Content = sub;

        Assert.Same(sub, NavigationMenuHostBase.GetHost(sub));
        Assert.Equal("vertical", sub.Orientation);

        var probe = new ContentControl();
        sub.Content = probe;
        Assert.Same(sub, NavigationMenuHostBase.GetHost(probe));
    }

    [StaFact]
    public void List_HomeAndEnd_HandleTheKeyWithoutThrowing()
    {
        var host = new NaviusNavigationMenu();
        var list = new NaviusNavigationMenuList();
        var itemA = new NaviusNavigationMenuItem { Value = "a" };
        itemA.Content = new NaviusNavigationMenuTrigger();
        var itemB = new NaviusNavigationMenuItem { Value = "b" };
        itemB.Content = new NaviusNavigationMenuLink();
        list.Items.Add(itemA);
        list.Items.Add(itemB);
        host.Content = list;

        SimulateKey(list, Key.End);
        SimulateKey(list, Key.Home);
    }
}
