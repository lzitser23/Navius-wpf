using System;

namespace Navius.Wpf.Primitives.Controls.MessageScroller;

/// <summary>
/// Pure, WPF-free state machine for the "never move the reader" contract behind
/// <see cref="NaviusMessageScroller"/>. Ported from the web engine's live-edge tracking
/// (createMessageScroller, Navius/src/Navius.Primitives/wwwroot/navius-interop.js): follow the
/// live edge only while already there, release follow on any reader-driven scroll away from it,
/// re-engage when the reader lands back at the edge, keep the reader's anchor row fixed when
/// older history is prepended above it, and support a jump-to-bottom request queued until content
/// exists to jump to.
///
/// Deliberately narrower than the full JS engine: no anchored-turn positioning, spacer sizing, or
/// intersection-based visibility tracking (out of scope for this port; see
/// docs/parity/message-scroller.md's "WPF implementation notes"). This engine only tracks
/// scrollable geometry (offset / viewport height / extent height) plus the follow/disengage state
/// and the unseen-message count it implies.
/// </summary>
public sealed class MessageScrollerEngine
{
    private bool _autoScroll;

    public MessageScrollerEngine(bool autoScroll = false, double scrollEdgeThreshold = 8)
    {
        ScrollEdgeThreshold = scrollEdgeThreshold;
        AutoScroll = autoScroll;
    }

    /// <summary>Follow new content only while already at the live edge. Mirrors the web contract's
    /// MessageScrollerProvider.AutoScroll (default false). Setting it recomputes
    /// <see cref="IsFollowing"/> from the current position, matching the web engine's update():
    /// following = autoScroll &amp;&amp; atEnd().</summary>
    public bool AutoScroll
    {
        get => _autoScroll;
        set
        {
            _autoScroll = value;
            SetFollowing(value && IsAtEnd);
        }
    }

    /// <summary>Distance from the bottom edge, in the same units as <see cref="Offset"/>, that
    /// still counts as being at it. Mirrors ScrollEdgeThreshold (default 8).</summary>
    public double ScrollEdgeThreshold { get; set; }

    public double ViewportHeight { get; private set; }

    public double ExtentHeight { get; private set; }

    public double Offset { get; private set; }

    /// <summary>True while the reader is at (or being kept at) the live edge and new content should
    /// pull the view down with it.</summary>
    public bool IsFollowing { get; private set; }

    /// <summary>True once a jump-to-bottom has been requested but there was no content yet to jump
    /// to; resolved (and cleared) by the next geometry/content update that has content.</summary>
    public bool HasPendingJump { get; private set; }

    /// <summary>Messages appended while disengaged (not following) that the reader has not yet
    /// seen. Reset to 0 whenever the reader returns to the live edge, by scrolling or by jumping.</summary>
    public int NewMessageCount { get; private set; }

    public double MaxOffset => Math.Max(0, ExtentHeight - ViewportHeight);

    public bool IsAtEnd => MaxOffset - Offset <= ScrollEdgeThreshold;

    private double Clamp(double offset) => Math.Max(0, Math.Min(offset, MaxOffset));

    private bool HasContent => ExtentHeight > 0;

    private void SetFollowing(bool following)
    {
        IsFollowing = following;
        if (following)
        {
            NewMessageCount = 0;
        }
    }

    /// <summary>
    /// Applies a geometry snapshot that is neither reader- nor content-driven (e.g. a pure
    /// viewport resize). Does not itself change <see cref="IsFollowing"/>: if already following,
    /// re-sticks to the new bottom so the live edge is not lost across a resize; otherwise the
    /// offset is only clamped to the new scrollable range. Resolves a queued jump if content now
    /// exists. Returns the offset the caller should apply.
    /// </summary>
    public double SyncGeometry(double viewportHeight, double extentHeight, double offset)
    {
        ViewportHeight = viewportHeight;
        ExtentHeight = extentHeight;
        Offset = Clamp(offset);

        if (HasPendingJump && HasContent)
        {
            return ResolvePendingJump();
        }

        if (IsFollowing)
        {
            Offset = MaxOffset;
        }

        return Offset;
    }

    /// <summary>
    /// A scroll event attributable to the reader (wheel, keys, drag, scrollbar), not a
    /// programmatic offset the engine itself set. Takes the full geometry snapshot because a real
    /// scroll event can carry a simultaneous unclassified extent/viewport change. Recomputes
    /// <see cref="IsFollowing"/> from the resulting position: this single rule implements both
    /// "intent disengage" (moving away from the edge stops following) and "re-engage at edge"
    /// (landing back within <see cref="ScrollEdgeThreshold"/> resumes following), matching the web
    /// engine's onUserIntent.
    /// </summary>
    public double OnUserScrolled(double viewportHeight, double extentHeight, double offset)
    {
        ViewportHeight = viewportHeight;
        ExtentHeight = extentHeight;
        Offset = Clamp(offset);
        SetFollowing(AutoScroll && IsAtEnd);
        if (IsAtEnd)
        {
            // Reaching the live edge means the reader has seen everything, even when AutoScroll
            // is off (no standing follow to re-engage, but nothing is unseen anymore either).
            NewMessageCount = 0;
        }

        return Offset;
    }

    /// <summary>
    /// New items appended at the end of the collection. Extent grows below the current view, so
    /// existing rows above the offset don't move: only <see cref="Offset"/> changes, and only when
    /// following (or a queued jump resolves now that content exists): a reader scrolled up
    /// into history is left exactly where they are, with <paramref name="appendedItemCount"/>
    /// counted toward <see cref="NewMessageCount"/>.
    /// </summary>
    public double OnItemsAppended(double viewportHeight, double extentHeight, int appendedItemCount = 1)
    {
        ViewportHeight = viewportHeight;
        ExtentHeight = extentHeight;

        if (HasPendingJump && HasContent)
        {
            return ResolvePendingJump();
        }

        if (IsFollowing)
        {
            Offset = MaxOffset;
        }
        else
        {
            Offset = Clamp(Offset);
            if (appendedItemCount > 0)
            {
                NewMessageCount += appendedItemCount;
            }
        }

        return Offset;
    }

    /// <summary>
    /// Items inserted above the current items (e.g. loading earlier history). Preserves the
    /// reader's anchor line by shifting <see cref="Offset"/> by exactly the height the extent grew,
    /// so the row under the reader's eye does not move. Does not affect
    /// <see cref="NewMessageCount"/> (prepended rows are older history, not new messages). A
    /// pending jump still takes priority: if one was queued, it resolves instead of preserving the
    /// anchor, since the reader explicitly asked to go to the live edge.
    /// </summary>
    public double OnItemsPrepended(double viewportHeight, double extentHeight)
    {
        var previousExtent = ExtentHeight;
        ViewportHeight = viewportHeight;
        ExtentHeight = extentHeight;

        if (HasPendingJump && HasContent)
        {
            return ResolvePendingJump();
        }

        var grew = extentHeight - previousExtent;
        Offset = Clamp(Offset + grew);
        return Offset;
    }

    /// <summary>
    /// Explicit request to jump to the live edge (the JumpToLatest button, or an app-level
    /// "scroll to end" call). If content exists, jumps immediately: this also clears
    /// <see cref="NewMessageCount"/>, and re-engages <see cref="IsFollowing"/> only when
    /// <see cref="AutoScroll"/> is on (matching the web engine's scrollToEnd: an explicit jump
    /// while AutoScroll is off is a one-time scroll, not a standing follow). If there is no
    /// content yet, the jump is queued (<see cref="HasPendingJump"/>) and resolved by the next
    /// geometry/content update that has content.
    /// </summary>
    public double RequestJumpToBottom()
    {
        if (!HasContent)
        {
            HasPendingJump = true;
            return Offset;
        }

        return ResolvePendingJump();
    }

    private double ResolvePendingJump()
    {
        HasPendingJump = false;
        Offset = MaxOffset;
        NewMessageCount = 0;
        IsFollowing = AutoScroll;
        return Offset;
    }

    /// <summary>Clears all tracked geometry and follow/unseen state back to the constructor
    /// defaults (AutoScroll/ScrollEdgeThreshold are left as configured). For a collection Reset
    /// (all items cleared): there is no content left to follow, preserve, or count as unseen.</summary>
    public void Reset()
    {
        ViewportHeight = 0;
        ExtentHeight = 0;
        Offset = 0;
        HasPendingJump = false;
        NewMessageCount = 0;
        IsFollowing = AutoScroll;
    }
}
