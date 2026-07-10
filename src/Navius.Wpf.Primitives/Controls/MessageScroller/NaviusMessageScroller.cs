using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Navius.Wpf.Primitives.Controls.MessageScroller;

/// <summary>
/// Tier B: a lookless <see cref="ItemsControl"/> wrapping a <see cref="ScrollViewer"/>
/// (PART_ScrollViewer) plus a JumpToLatest button (PART_JumpToLatestButton), applying
/// <see cref="MessageScrollerEngine"/> to real ScrollChanged geometry and to
/// <see cref="INotifyCollectionChanged"/> item adds. The web contract's separate
/// Provider/root/viewport/content parts collapse into this one class per its own parity doc's WPF
/// strategy ("Build it as a lookless Control/ItemsControl wrapping a ScrollViewer"): the
/// ItemsControl instance is simultaneously the scroll frame, the region, and the log.
///
/// Item adds are classified purely by index: an add at the current end is treated as new content
/// (auto-follow, counted toward <see cref="NewMessageCount"/> while disengaged); an add at index 0
/// (with items already present) is treated as older history being prepended above (anchor
/// preserved, never counted as new). Adds at any other index, and removes/replaces/moves, fall
/// back to a plain re-sync of the scrollable geometry (see docs/parity/message-scroller.md's WPF
/// implementation notes).
/// </summary>
[TemplatePart(Name = PartScrollViewer, Type = typeof(ScrollViewer))]
[TemplatePart(Name = PartJumpToLatestButton, Type = typeof(ButtonBase))]
public class NaviusMessageScroller : ItemsControl
{
    private const string PartScrollViewer = "PART_ScrollViewer";
    private const string PartJumpToLatestButton = "PART_JumpToLatestButton";

    public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.Register(
        nameof(AutoScroll), typeof(bool), typeof(NaviusMessageScroller),
        new PropertyMetadata(false, OnAutoScrollChanged));

    public static readonly DependencyProperty ScrollEdgeThresholdProperty = DependencyProperty.Register(
        nameof(ScrollEdgeThreshold), typeof(double), typeof(NaviusMessageScroller),
        new PropertyMetadata(8d, OnScrollEdgeThresholdChanged));

    private static readonly DependencyPropertyKey IsFollowingPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFollowing), typeof(bool), typeof(NaviusMessageScroller),
        new PropertyMetadata(false));

    public static readonly DependencyProperty IsFollowingProperty = IsFollowingPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey NewMessageCountPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(NewMessageCount), typeof(int), typeof(NaviusMessageScroller),
        new PropertyMetadata(0));

    public static readonly DependencyProperty NewMessageCountProperty = NewMessageCountPropertyKey.DependencyProperty;

    private readonly MessageScrollerEngine _engine = new();
    private ScrollViewer? _scrollViewer;
    private ButtonBase? _jumpButton;
    private bool _applyingEngineOffset;
    private (bool IsAppend, int Count)? _pendingChange;

    static NaviusMessageScroller()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusMessageScroller),
            new FrameworkPropertyMetadata(typeof(NaviusMessageScroller)));
    }

    public NaviusMessageScroller()
    {
        // Contract: role="log" aria-relevant="additions" on Content. RaiseNotificationEvent
        // (AnnounceNewMessages below) is the primary announcement path (Toast precedent); this is
        // the LiveSetting fallback for AT/OS combinations where that event isn't supported.
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
    }

    /// <summary>Follow new content only while already at the live edge. Mirrors
    /// MessageScrollerProvider.AutoScroll (default false).</summary>
    public bool AutoScroll
    {
        get => (bool)GetValue(AutoScrollProperty);
        set => SetValue(AutoScrollProperty, value);
    }

    /// <summary>Distance in DIPs from the bottom edge that still counts as being at it. Mirrors
    /// ScrollEdgeThreshold (default 8).</summary>
    public double ScrollEdgeThreshold
    {
        get => (double)GetValue(ScrollEdgeThresholdProperty);
        set => SetValue(ScrollEdgeThresholdProperty, value);
    }

    /// <summary>True while the reader is at the live edge and new content pulls the view with it.</summary>
    public bool IsFollowing => (bool)GetValue(IsFollowingProperty);

    /// <summary>Messages appended while disengaged that the reader has not yet seen. Drives the
    /// JumpToLatest badge; reset to 0 on re-engage (scroll back to the edge, or jump).</summary>
    public int NewMessageCount => (int)GetValue(NewMessageCountProperty);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
        }

        if (_jumpButton is not null)
        {
            _jumpButton.Click -= OnJumpToLatestClick;
        }

        _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
        _jumpButton = GetTemplateChild(PartJumpToLatestButton) as ButtonBase;

        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }

        if (_jumpButton is not null)
        {
            _jumpButton.Click += OnJumpToLatestClick;
        }

        UpdateVisualState();
    }

    /// <summary>Programmatic jump-to-latest (the JumpToLatest button's own action, exposed for app
    /// code too; the WPF analog of MessageScrollerContext.ScrollToEndAsync).</summary>
    public void JumpToBottom() => OnJumpToLatestClick(this, new RoutedEventArgs());

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                var priorCount = Items.Count - e.NewItems!.Count;
                _pendingChange = e.NewStartingIndex == 0 && priorCount > 0
                    ? (IsAppend: false, Count: 0)
                    : (IsAppend: true, Count: e.NewItems.Count);
                break;
            case NotifyCollectionChangedAction.Reset:
                _pendingChange = null;
                _engine.Reset();
                UpdateVisualState();
                break;
            default:
                // Remove/Replace/Move: no anchor-preservation contract for these (out of scope);
                // the next ScrollChanged falls back to a plain geometry re-sync.
                _pendingChange = null;
                break;
        }
    }

    private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NaviusMessageScroller)d;
        control._engine.AutoScroll = (bool)e.NewValue;
        control.UpdateVisualState();
    }

    private static void OnScrollEdgeThresholdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusMessageScroller)d)._engine.ScrollEdgeThreshold = (double)e.NewValue;

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e) =>
        HandleScrollChanged(e.ExtentHeight, e.ExtentHeightChange, e.ViewportHeight, e.ViewportHeightChange, e.VerticalOffset, e.VerticalChange);

    /// <summary>
    /// Split out of <see cref="OnScrollChanged"/> so tests can drive it with plain doubles instead
    /// of constructing a ScrollChangedEventArgs (its constructor is internal to
    /// PresentationFramework; see NaviusScrollArea's own HandleScrollActivity for the same reason).
    /// </summary>
    private void HandleScrollChanged(
        double extentHeight, double extentHeightChange,
        double viewportHeight, double viewportHeightChange,
        double verticalOffset, double verticalChange)
    {
        if (_applyingEngineOffset)
        {
            // The consequence of our own ScrollToVerticalOffset call below, not reader intent or a
            // fresh content change: the engine's fields are already authoritative.
            _applyingEngineOffset = false;
            UpdateVisualState();
            return;
        }

        if (extentHeightChange != 0 && _pendingChange is { } change)
        {
            _pendingChange = null;
            var wasFollowing = _engine.IsFollowing;
            var newOffset = change.IsAppend
                ? _engine.OnItemsAppended(viewportHeight, extentHeight, change.Count)
                : _engine.OnItemsPrepended(viewportHeight, extentHeight);

            ApplyOffsetIfNeeded(newOffset, verticalOffset);

            if (change.IsAppend && !wasFollowing && _engine.NewMessageCount > 0)
            {
                AnnounceNewMessages();
            }

            UpdateVisualState();
            return;
        }

        if (verticalChange != 0)
        {
            _engine.OnUserScrolled(viewportHeight, extentHeight, verticalOffset);
            UpdateVisualState();
            return;
        }

        // A pure resize (viewport and/or extent changed with no classified item change and no
        // reader-driven offset move), e.g. the window resizing.
        var resynced = _engine.SyncGeometry(viewportHeight, extentHeight, verticalOffset);
        ApplyOffsetIfNeeded(resynced, verticalOffset);
        UpdateVisualState();
    }

    private void OnJumpToLatestClick(object sender, RoutedEventArgs e)
    {
        var target = _engine.RequestJumpToBottom();
        var current = _scrollViewer?.VerticalOffset ?? target;
        ApplyOffsetIfNeeded(target, current);
        UpdateVisualState();
    }

    private void ApplyOffsetIfNeeded(double target, double current)
    {
        if (_scrollViewer is null || Math.Abs(target - current) < 0.5)
        {
            return;
        }

        _applyingEngineOffset = true;
        _scrollViewer.ScrollToVerticalOffset(target);
    }

    private void UpdateVisualState()
    {
        SetValue(IsFollowingPropertyKey, _engine.IsFollowing);
        SetValue(NewMessageCountPropertyKey, _engine.NewMessageCount);

        if (_jumpButton is not null)
        {
            _jumpButton.Visibility = !_engine.IsFollowing && _engine.NewMessageCount > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Contract: appended rows announce via the content region's live-region role while
    /// disengaged, without narrating every message a still-following reader already sees.
    /// RaiseNotificationEvent requires Windows 10 1709+ and a listening AT; unsupported
    /// combinations fall back to this control's LiveSetting=Polite (set in the constructor), same
    /// tradeoff as NaviusToast.Announce.
    /// </summary>
    private void AnnounceNewMessages()
    {
        if (!IsLoaded)
        {
            return;
        }

        var count = _engine.NewMessageCount;
        var text = count == 1 ? "1 new message" : $"{count} new messages";

        var peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
        peer?.RaiseNotificationEvent(
            AutomationNotificationKind.Other,
            AutomationNotificationProcessing.CurrentThenMostRecent,
            text,
            Guid.NewGuid().ToString());
    }
}
