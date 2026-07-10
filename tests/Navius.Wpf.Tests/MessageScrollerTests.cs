using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls.MessageScroller;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

// ---------------------------------------------------------------------------------------------
// Engine tests: MessageScrollerEngine is pure (no WPF types), so these are plain [Fact]s driven
// entirely by (viewportHeight, extentHeight, offset) doubles and intent events. Geometry model:
// viewport 100 tall; each appended message adds 50 to the extent.
// ---------------------------------------------------------------------------------------------

public class MessageScrollerEngineTests
{
    private const double Viewport = 100;

    private static MessageScrollerEngine CreateFilledEngine(bool autoScroll, double extent = 500, double? offset = null)
    {
        var engine = new MessageScrollerEngine(autoScroll);
        engine.SyncGeometry(Viewport, extent, offset ?? (autoScroll ? extent - Viewport : 0));
        return engine;
    }

    /// <summary>Reader intent at the current geometry (wheel/keys/drag landing at
    /// <paramref name="offset"/>): the snapshot parameters just repeat what the engine already
    /// tracks, mirroring a ScrollChanged event with only an offset delta.</summary>
    private static void UserScroll(MessageScrollerEngine engine, double offset) =>
        engine.OnUserScrolled(engine.ViewportHeight, engine.ExtentHeight, offset);

    // --- defaults -----------------------------------------------------------------------------

    [Fact]
    public void Defaults_MatchTheContract()
    {
        var engine = new MessageScrollerEngine();

        Assert.False(engine.AutoScroll);
        Assert.Equal(8, engine.ScrollEdgeThreshold);
        Assert.False(engine.IsFollowing);
        Assert.False(engine.HasPendingJump);
        Assert.Equal(0, engine.NewMessageCount);
    }

    [Fact]
    public void AutoScrollConstruction_StartsFollowing()
    {
        var engine = new MessageScrollerEngine(autoScroll: true);

        Assert.True(engine.IsFollowing);
    }

    // --- live-edge tracking (auto-stick to bottom while at the edge) ---------------------------

    [Fact]
    public void Append_WhileFollowing_SticksToTheNewBottom()
    {
        var engine = CreateFilledEngine(autoScroll: true);
        Assert.Equal(400, engine.Offset);

        var offset = engine.OnItemsAppended(Viewport, 550);

        Assert.Equal(450, offset);
        Assert.True(engine.IsFollowing);
        Assert.Equal(0, engine.NewMessageCount);
    }

    [Fact]
    public void Append_RepeatedlyWhileFollowing_KeepsTrackingTheEdge()
    {
        var engine = CreateFilledEngine(autoScroll: true);

        for (var extent = 550d; extent <= 800; extent += 50)
        {
            var offset = engine.OnItemsAppended(Viewport, extent);
            Assert.Equal(extent - Viewport, offset);
        }

        Assert.True(engine.IsFollowing);
    }

    [Fact]
    public void Append_WithAutoScrollOff_NeverMovesTheReader()
    {
        // AutoScroll=false means there is no standing follow even at the very bottom.
        var engine = CreateFilledEngine(autoScroll: false, offset: 400);
        Assert.True(engine.IsAtEnd);
        Assert.False(engine.IsFollowing);

        var offset = engine.OnItemsAppended(Viewport, 550);

        Assert.Equal(400, offset);
    }

    [Fact]
    public void Resize_WhileFollowing_ResticksToTheNewBottom()
    {
        var engine = CreateFilledEngine(autoScroll: true);

        // Viewport shrinks (window resize): the live edge moves, the follower moves with it.
        var offset = engine.SyncGeometry(80, 500, engine.Offset);

        Assert.Equal(420, offset);
        Assert.True(engine.IsFollowing);
    }

    [Fact]
    public void Resize_WhileDisengaged_OnlyClampsTheOffset()
    {
        var engine = CreateFilledEngine(autoScroll: true, offset: 200);
        UserScroll(engine,200); // disengage

        var offset = engine.SyncGeometry(Viewport, 250, 200);

        Assert.Equal(150, offset); // clamped to the new MaxOffset, not re-stuck to the bottom
        Assert.False(engine.IsFollowing);
    }

    // --- intent disengage (any upward user scroll releases the follow) -------------------------

    [Fact]
    public void UserScrollAwayFromTheEdge_Disengages()
    {
        var engine = CreateFilledEngine(autoScroll: true);
        Assert.True(engine.IsFollowing);

        UserScroll(engine,200);

        Assert.False(engine.IsFollowing);
    }

    [Fact]
    public void UserScrollOnePixelPastTheThreshold_Disengages()
    {
        var engine = CreateFilledEngine(autoScroll: true); // MaxOffset 400, threshold 8

        UserScroll(engine,391); // distance from end = 9 > 8

        Assert.False(engine.IsFollowing);
    }

    [Fact]
    public void UserScrollWithinTheThreshold_StaysEngaged()
    {
        var engine = CreateFilledEngine(autoScroll: true); // MaxOffset 400, threshold 8

        UserScroll(engine,392); // distance from end = 8 <= 8: still "at" the edge

        Assert.True(engine.IsFollowing);
    }

    [Fact]
    public void AppendsAfterDisengage_DoNotMoveTheReader()
    {
        var engine = CreateFilledEngine(autoScroll: true);
        UserScroll(engine,200);

        var offset = engine.OnItemsAppended(Viewport, 550);

        Assert.Equal(200, offset);
        Assert.False(engine.IsFollowing);
    }

    // --- re-engage at edge ----------------------------------------------------------------------

    [Fact]
    public void UserScrollBackToTheEdge_ReEngages()
    {
        var engine = CreateFilledEngine(autoScroll: true);
        UserScroll(engine,200);
        Assert.False(engine.IsFollowing);

        UserScroll(engine,400);

        Assert.True(engine.IsFollowing);
    }

    [Fact]
    public void UserScrollBackWithinTheThreshold_ReEngages()
    {
        var engine = CreateFilledEngine(autoScroll: true); // MaxOffset 400, threshold 8

        UserScroll(engine,200);
        UserScroll(engine,393); // within 8 of the end

        Assert.True(engine.IsFollowing);
    }

    [Fact]
    public void UserScrollToTheEdge_WithAutoScrollOff_DoesNotEngage()
    {
        var engine = CreateFilledEngine(autoScroll: false, offset: 0);

        UserScroll(engine,400);

        Assert.False(engine.IsFollowing);
    }

    // --- disengage/re-engage hysteresis ---------------------------------------------------------

    [Fact]
    public void Hysteresis_DisengageAppendReEngageAppend_FollowsAgain()
    {
        var engine = CreateFilledEngine(autoScroll: true);

        UserScroll(engine,100); // disengage
        engine.OnItemsAppended(Viewport, 550); // reader stays at 100
        Assert.Equal(100, engine.Offset);

        UserScroll(engine,450); // back at the (new) edge: re-engage
        Assert.True(engine.IsFollowing);

        var offset = engine.OnItemsAppended(Viewport, 600);
        Assert.Equal(500, offset);
    }

    [Fact]
    public void Hysteresis_ThresholdBoundaryIsStableAcrossRepeatedScrolls()
    {
        var engine = CreateFilledEngine(autoScroll: true); // MaxOffset 400, threshold 8

        UserScroll(engine,391);
        Assert.False(engine.IsFollowing);
        UserScroll(engine,392);
        Assert.True(engine.IsFollowing);
        UserScroll(engine,391);
        Assert.False(engine.IsFollowing);
        UserScroll(engine,400);
        Assert.True(engine.IsFollowing);
    }

    [Fact]
    public void Hysteresis_CustomThresholdIsRespected()
    {
        var engine = new MessageScrollerEngine(autoScroll: true, scrollEdgeThreshold: 40);
        engine.SyncGeometry(Viewport, 500, 400);

        UserScroll(engine,360); // exactly 40 from the end: at the edge
        Assert.True(engine.IsFollowing);

        UserScroll(engine,359);
        Assert.False(engine.IsFollowing);
    }

    // --- prepend preservation --------------------------------------------------------------------

    [Fact]
    public void Prepend_ShiftsTheOffsetByExactlyTheGrowth()
    {
        var engine = CreateFilledEngine(autoScroll: true, offset: 200);
        UserScroll(engine,200);

        var offset = engine.OnItemsPrepended(Viewport, 650); // 150 of history above

        Assert.Equal(350, offset); // the anchor line under the reader's eye has not moved
    }

    [Fact]
    public void Prepend_AtTheVeryTop_StillShifts()
    {
        var engine = CreateFilledEngine(autoScroll: false, offset: 0);

        var offset = engine.OnItemsPrepended(Viewport, 600);

        Assert.Equal(100, offset); // the row that was at the top stays at the top
    }

    [Fact]
    public void Prepend_DoesNotCountAsNewMessages()
    {
        var engine = CreateFilledEngine(autoScroll: true, offset: 200);
        UserScroll(engine,200);

        engine.OnItemsPrepended(Viewport, 650);

        Assert.Equal(0, engine.NewMessageCount);
    }

    [Fact]
    public void Prepend_DoesNotChangeTheFollowState()
    {
        var following = CreateFilledEngine(autoScroll: true);
        following.OnItemsPrepended(Viewport, 650);
        Assert.True(following.IsFollowing);

        var disengaged = CreateFilledEngine(autoScroll: true, offset: 100);
        UserScroll(disengaged, 100);
        disengaged.OnItemsPrepended(Viewport, 650);
        Assert.False(disengaged.IsFollowing);
    }

    // --- queued jump-to-bottom -------------------------------------------------------------------

    [Fact]
    public void Jump_WithContent_GoesToTheBottomImmediately()
    {
        var engine = CreateFilledEngine(autoScroll: true, offset: 100);
        UserScroll(engine,100);

        var offset = engine.RequestJumpToBottom();

        Assert.Equal(400, offset);
        Assert.False(engine.HasPendingJump);
        Assert.True(engine.IsFollowing);
    }

    [Fact]
    public void Jump_WithAutoScrollOff_IsOneTimeNotAStandingFollow()
    {
        // Web parity: scrollToEnd only re-engages follow when opts.autoScroll is true.
        var engine = CreateFilledEngine(autoScroll: false, offset: 100);

        var offset = engine.RequestJumpToBottom();

        Assert.Equal(400, offset);
        Assert.False(engine.IsFollowing);
    }

    [Fact]
    public void Jump_BeforeAnyContent_Queues()
    {
        var engine = new MessageScrollerEngine(autoScroll: true);

        var offset = engine.RequestJumpToBottom();

        Assert.Equal(0, offset);
        Assert.True(engine.HasPendingJump);
    }

    [Fact]
    public void QueuedJump_ResolvesOnTheFirstContentfulAppend()
    {
        var engine = new MessageScrollerEngine();
        engine.RequestJumpToBottom();

        var offset = engine.OnItemsAppended(Viewport, 500, 5);

        Assert.Equal(400, offset);
        Assert.False(engine.HasPendingJump);
        Assert.Equal(0, engine.NewMessageCount); // the jump consumed the batch as "seen"
    }

    [Fact]
    public void QueuedJump_ResolvesOnAGeometrySync()
    {
        var engine = new MessageScrollerEngine();
        engine.RequestJumpToBottom();

        var offset = engine.SyncGeometry(Viewport, 300, 0);

        Assert.Equal(200, offset);
        Assert.False(engine.HasPendingJump);
    }

    [Fact]
    public void QueuedJump_TakesPriorityOverPrependPreservation()
    {
        var engine = new MessageScrollerEngine();
        engine.RequestJumpToBottom();

        var offset = engine.OnItemsPrepended(Viewport, 500);

        Assert.Equal(400, offset); // the explicit jump wins over anchoring
        Assert.False(engine.HasPendingJump);
    }

    [Fact]
    public void Jump_ClearsTheUnseenCount()
    {
        var engine = CreateFilledEngine(autoScroll: true);
        UserScroll(engine,100);
        engine.OnItemsAppended(Viewport, 600, 2);
        Assert.Equal(2, engine.NewMessageCount);

        engine.RequestJumpToBottom();

        Assert.Equal(0, engine.NewMessageCount);
    }

    // --- new-message count while disengaged ------------------------------------------------------

    [Fact]
    public void NewMessageCount_AccumulatesAcrossAppendsWhileDisengaged()
    {
        var engine = CreateFilledEngine(autoScroll: true);
        UserScroll(engine,100);

        engine.OnItemsAppended(Viewport, 550);
        engine.OnItemsAppended(Viewport, 650, 2);

        Assert.Equal(3, engine.NewMessageCount);
    }

    [Fact]
    public void NewMessageCount_StaysZeroWhileFollowing()
    {
        var engine = CreateFilledEngine(autoScroll: true);

        engine.OnItemsAppended(Viewport, 550);
        engine.OnItemsAppended(Viewport, 600);

        Assert.Equal(0, engine.NewMessageCount);
    }

    [Fact]
    public void NewMessageCount_ResetsAtTheEdgeEvenWithAutoScrollOff()
    {
        // With AutoScroll off there is no standing follow to re-engage, but reaching the live
        // edge still means the reader has seen everything: the count (and the button) must clear.
        var engine = CreateFilledEngine(autoScroll: false, offset: 400);
        engine.OnItemsAppended(Viewport, 550);
        Assert.Equal(1, engine.NewMessageCount);

        UserScroll(engine, 450);

        Assert.Equal(0, engine.NewMessageCount);
        Assert.False(engine.IsFollowing);
    }

    [Fact]
    public void NewMessageCount_ResetsWhenTheReaderScrollsBackToTheEdge()
    {
        var engine = CreateFilledEngine(autoScroll: true);
        UserScroll(engine,100);
        engine.OnItemsAppended(Viewport, 550, 4);
        Assert.Equal(4, engine.NewMessageCount);

        UserScroll(engine,450); // re-engage at the live edge

        Assert.Equal(0, engine.NewMessageCount);
    }

    // --- reset -----------------------------------------------------------------------------------

    [Fact]
    public void Reset_ClearsGeometryAndUnseenState()
    {
        var engine = CreateFilledEngine(autoScroll: true);
        UserScroll(engine,100);
        engine.OnItemsAppended(Viewport, 600, 3);
        engine.RequestJumpToBottom();
        UserScroll(engine,100);

        engine.Reset();

        Assert.Equal(0, engine.Offset);
        Assert.Equal(0, engine.ExtentHeight);
        Assert.Equal(0, engine.NewMessageCount);
        Assert.False(engine.HasPendingJump);
        Assert.True(engine.IsFollowing); // back to the AutoScroll-derived default
    }
}

// ---------------------------------------------------------------------------------------------
// Control wiring tests: NaviusMessageScroller applying the engine to template parts, DPs, and
// INotifyCollectionChanged classification. STA + Application guard; never Show().
// ---------------------------------------------------------------------------------------------

public class MessageScrollerControlTests
{
    static MessageScrollerControlTests()
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

    private static readonly MethodInfo HandleScrollChangedMethod =
        typeof(NaviusMessageScroller).GetMethod("HandleScrollChanged", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>Drives the private scroll handler with plain doubles, same technique as
    /// ScrollAreaTests (ScrollChangedEventArgs' constructor is internal to PresentationFramework).</summary>
    private static void SimulateScrollChanged(
        NaviusMessageScroller scroller,
        double extentHeight, double extentHeightChange,
        double viewportHeight, double viewportHeightChange,
        double verticalOffset, double verticalChange) =>
        HandleScrollChangedMethod.Invoke(
            scroller,
            new object[] { extentHeight, extentHeightChange, viewportHeight, viewportHeightChange, verticalOffset, verticalChange });

    private static MessageScrollerEngine GetEngine(NaviusMessageScroller scroller)
    {
        var field = typeof(NaviusMessageScroller).GetField("_engine", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (MessageScrollerEngine)field.GetValue(scroller)!;
    }

    private static NaviusMessageScroller CreateTemplatedScroller(out ResourceDictionary dictionary)
    {
        dictionary = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, dictionary);
        dictionary.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/Navius.Wpf.Primitives;component/Themes/MessageScroller.xaml",
                UriKind.Absolute),
        });
        Application.Current.Resources.MergedDictionaries.Add(dictionary);

        var scroller = new NaviusMessageScroller();
        // Elements outside a live visual/logical tree don't automatically pick up an implicit
        // (TargetType-keyed) style; wire it explicitly, same as ScrollAreaTests.
        scroller.SetResourceReference(FrameworkElement.StyleProperty, typeof(NaviusMessageScroller));
        scroller.ApplyTemplate();
        return scroller;
    }

    [StaFact]
    public void Defaults_MatchTheContract()
    {
        var scroller = new NaviusMessageScroller();

        Assert.False(scroller.AutoScroll);
        Assert.Equal(8, scroller.ScrollEdgeThreshold);
        Assert.False(scroller.IsFollowing);
        Assert.Equal(0, scroller.NewMessageCount);
    }

    [StaFact]
    public void LiveSetting_IsPolite()
    {
        var scroller = new NaviusMessageScroller();

        Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(scroller));
    }

    [StaFact]
    public void Template_ExposesScrollViewerAndJumpButtonParts()
    {
        var scroller = CreateTemplatedScroller(out var dictionary);

        try
        {
            Assert.NotNull(scroller.Template);
            Assert.IsAssignableFrom<ScrollViewer>(scroller.Template.FindName("PART_ScrollViewer", scroller));
            Assert.IsAssignableFrom<ButtonBase>(scroller.Template.FindName("PART_JumpToLatestButton", scroller));
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void AutoScrollDp_FlowsIntoTheEngine()
    {
        var scroller = new NaviusMessageScroller();
        Assert.False(GetEngine(scroller).AutoScroll);

        scroller.AutoScroll = true;

        Assert.True(GetEngine(scroller).AutoScroll);
    }

    [StaFact]
    public void ScrollEdgeThresholdDp_FlowsIntoTheEngine()
    {
        var scroller = new NaviusMessageScroller { ScrollEdgeThreshold = 32 };

        Assert.Equal(32, GetEngine(scroller).ScrollEdgeThreshold);
    }

    [StaFact]
    public void CollectionAddAtEnd_WhileFollowing_TracksTheLiveEdge()
    {
        var messages = new ObservableCollection<string> { "a", "b" };
        var scroller = new NaviusMessageScroller { AutoScroll = true, ItemsSource = messages };
        SimulateScrollChanged(scroller, 100, 100, 50, 50, 50, 0); // initial layout at the edge

        messages.Add("c"); // classified as an append by OnItemsChanged
        SimulateScrollChanged(scroller, 150, 50, 50, 0, 50, 0); // extent grows below

        Assert.True(scroller.IsFollowing);
        Assert.Equal(0, scroller.NewMessageCount);
        Assert.Equal(100, GetEngine(scroller).Offset); // stuck to the new bottom
    }

    [StaFact]
    public void CollectionAddAtEnd_WhileDisengaged_CountsUnseenAndDoesNotMove()
    {
        var messages = new ObservableCollection<string> { "a", "b", "c", "d" };
        var scroller = new NaviusMessageScroller { AutoScroll = true, ItemsSource = messages };
        SimulateScrollChanged(scroller, 200, 200, 50, 50, 150, 0);
        SimulateScrollChanged(scroller, 200, 0, 50, 0, 20, -130); // reader scrolls up: disengage
        Assert.False(scroller.IsFollowing);

        messages.Add("e");
        SimulateScrollChanged(scroller, 250, 50, 50, 0, 20, 0);

        Assert.Equal(20, GetEngine(scroller).Offset); // never move the reader
        Assert.Equal(1, scroller.NewMessageCount);
    }

    [StaFact]
    public void CollectionInsertAtStart_PreservesTheAnchor()
    {
        var messages = new ObservableCollection<string> { "a", "b", "c", "d" };
        var scroller = new NaviusMessageScroller { ItemsSource = messages };
        SimulateScrollChanged(scroller, 200, 200, 50, 50, 100, 0);

        messages.Insert(0, "older"); // classified as a prepend by OnItemsChanged
        SimulateScrollChanged(scroller, 250, 50, 50, 0, 100, 0);

        Assert.Equal(150, GetEngine(scroller).Offset); // shifted by exactly the growth
        Assert.Equal(0, scroller.NewMessageCount); // history is not "new messages"
    }

    [StaFact]
    public void CollectionInsertAtStart_OfAnEmptyCollection_IsAnAppendNotAPrepend()
    {
        var messages = new ObservableCollection<string>();
        var scroller = new NaviusMessageScroller { AutoScroll = true, ItemsSource = messages };

        messages.Add("first"); // NewStartingIndex == 0, but nothing existed above it
        SimulateScrollChanged(scroller, 50, 50, 100, 100, 0, 0);

        Assert.True(scroller.IsFollowing);
        Assert.Equal(0, scroller.NewMessageCount);
    }

    [StaFact]
    public void UserScrollBackToTheEdge_ResetsTheUnseenCount()
    {
        var messages = new ObservableCollection<string> { "a", "b", "c", "d" };
        var scroller = new NaviusMessageScroller { AutoScroll = true, ItemsSource = messages };
        SimulateScrollChanged(scroller, 200, 200, 50, 50, 150, 0);
        SimulateScrollChanged(scroller, 200, 0, 50, 0, 20, -130);

        messages.Add("e");
        SimulateScrollChanged(scroller, 250, 50, 50, 0, 20, 0);
        Assert.Equal(1, scroller.NewMessageCount);

        SimulateScrollChanged(scroller, 250, 0, 50, 0, 200, 180); // back at the live edge

        Assert.True(scroller.IsFollowing);
        Assert.Equal(0, scroller.NewMessageCount);
    }

    [StaFact]
    public void JumpButton_AppearsOnlyWhenDisengagedWithUnseenContent()
    {
        var scroller = CreateTemplatedScroller(out var dictionary);

        try
        {
            var button = (ButtonBase)scroller.Template.FindName("PART_JumpToLatestButton", scroller)!;
            var messages = new ObservableCollection<string> { "a", "b", "c", "d" };
            scroller.ItemsSource = messages;
            scroller.AutoScroll = true;

            SimulateScrollChanged(scroller, 200, 200, 50, 50, 150, 0);
            Assert.Equal(Visibility.Collapsed, button.Visibility); // following: no button

            SimulateScrollChanged(scroller, 200, 0, 50, 0, 20, -130); // disengage
            Assert.Equal(Visibility.Collapsed, button.Visibility); // disengaged but nothing unseen yet

            messages.Add("e");
            SimulateScrollChanged(scroller, 250, 50, 50, 0, 20, 0);
            Assert.Equal(Visibility.Visible, button.Visibility); // disengaged + unseen content
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void JumpToBottom_ClearsTheUnseenCountAndHidesTheButton()
    {
        var scroller = CreateTemplatedScroller(out var dictionary);

        try
        {
            var button = (ButtonBase)scroller.Template.FindName("PART_JumpToLatestButton", scroller)!;
            var messages = new ObservableCollection<string> { "a", "b", "c", "d" };
            scroller.ItemsSource = messages;
            scroller.AutoScroll = true;

            SimulateScrollChanged(scroller, 200, 200, 50, 50, 150, 0);
            SimulateScrollChanged(scroller, 200, 0, 50, 0, 20, -130);
            messages.Add("e");
            SimulateScrollChanged(scroller, 250, 50, 50, 0, 20, 0);
            Assert.Equal(Visibility.Visible, button.Visibility);

            scroller.JumpToBottom();

            Assert.Equal(0, scroller.NewMessageCount);
            Assert.True(scroller.IsFollowing);
            Assert.Equal(Visibility.Collapsed, button.Visibility);
        }
        finally
        {
            Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }
    }

    [StaFact]
    public void CollectionReset_ClearsTheEngineState()
    {
        var messages = new ObservableCollection<string> { "a", "b", "c", "d" };
        var scroller = new NaviusMessageScroller { AutoScroll = true, ItemsSource = messages };
        SimulateScrollChanged(scroller, 200, 200, 50, 50, 150, 0);
        SimulateScrollChanged(scroller, 200, 0, 50, 0, 20, -130); // disengage
        messages.Add("e");
        SimulateScrollChanged(scroller, 250, 50, 50, 0, 20, 0);
        Assert.Equal(1, scroller.NewMessageCount);

        messages.Clear();

        Assert.Equal(0, scroller.NewMessageCount);
        Assert.True(scroller.IsFollowing);
        Assert.Equal(0, GetEngine(scroller).ExtentHeight);
    }

    [StaFact]
    public void PureResize_DoesNotDisengageOrCountMessages()
    {
        var messages = new ObservableCollection<string> { "a", "b", "c", "d" };
        var scroller = new NaviusMessageScroller { AutoScroll = true, ItemsSource = messages };
        SimulateScrollChanged(scroller, 200, 200, 50, 50, 150, 0);
        Assert.True(scroller.IsFollowing);

        // Viewport grows (window resize): no item change was classified, no reader intent.
        SimulateScrollChanged(scroller, 200, 0, 80, 30, 120, 0);

        Assert.True(scroller.IsFollowing);
        Assert.Equal(0, scroller.NewMessageCount);
        Assert.Equal(120, GetEngine(scroller).Offset); // re-stuck to the new MaxOffset (200 - 80)
    }
}
