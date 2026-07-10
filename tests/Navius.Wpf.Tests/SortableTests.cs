using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Sortable;

namespace Navius.Wpf.Tests;

public class SortableTests
{
    static SortableTests()
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

    private static Func<string, bool> Disabled(params string[] keys)
    {
        var set = new HashSet<string>(keys);
        return set.Contains;
    }

    private static NaviusSortable BuildSortable(params (string Value, bool Disabled)[] items)
    {
        var sortable = new NaviusSortable();
        foreach (var (value, disabled) in items)
        {
            sortable.Items.Add(new NaviusSortableItem { Value = value, Disabled = disabled });
        }

        return sortable;
    }

    private static NaviusSortableItem ItemAt(NaviusSortable sortable, int index) =>
        (NaviusSortableItem)sortable.Items[index]!;

    private static IReadOnlyList<string> Order(NaviusSortable sortable) =>
        sortable.Items.Cast<NaviusSortableItem>().Select(i => i.Value).ToList();

    // --- SortableKeyboardReducer: enabled scanning ---

    [StaFact]
    public void NextEnabled_SkipsDisabledRows()
    {
        var order = new[] { "a", "b", "c", "d" };
        Assert.Equal(2, SortableKeyboardReducer.NextEnabled(order, Disabled("b"), 0));
    }

    [StaFact]
    public void NextEnabled_ReturnsMinusOneAtEnd()
    {
        var order = new[] { "a", "b" };
        Assert.Equal(-1, SortableKeyboardReducer.NextEnabled(order, Disabled(), 1));
    }

    [StaFact]
    public void PrevEnabled_SkipsDisabledRows()
    {
        var order = new[] { "a", "b", "c", "d" };
        Assert.Equal(0, SortableKeyboardReducer.PrevEnabled(order, Disabled("b", "c"), 3));
    }

    [StaFact]
    public void PrevEnabled_ReturnsMinusOneAtStart()
    {
        var order = new[] { "a", "b" };
        Assert.Equal(-1, SortableKeyboardReducer.PrevEnabled(order, Disabled(), 0));
    }

    [StaFact]
    public void FirstEnabled_SkipsLeadingDisabled()
    {
        var order = new[] { "a", "b", "c" };
        Assert.Equal(1, SortableKeyboardReducer.FirstEnabled(order, Disabled("a")));
    }

    [StaFact]
    public void LastEnabled_SkipsTrailingDisabled()
    {
        var order = new[] { "a", "b", "c" };
        Assert.Equal(1, SortableKeyboardReducer.LastEnabled(order, Disabled("c")));
    }

    [StaFact]
    public void FirstEnabled_AllDisabled_ReturnsMinusOne()
    {
        var order = new[] { "a", "b" };
        Assert.Equal(-1, SortableKeyboardReducer.FirstEnabled(order, Disabled("a", "b")));
    }

    // --- SortableKeyboardReducer: roving (not grabbing) ---

    [StaFact]
    public void Rove_Forward_LandsOnNextEnabled()
    {
        var order = new[] { "a", "b", "c" };
        Assert.Equal(2, SortableKeyboardReducer.Rove(order, Disabled("b"), 0, SortableMove.Forward));
    }

    [StaFact]
    public void Rove_Backward_LandsOnPrevEnabled()
    {
        var order = new[] { "a", "b", "c" };
        Assert.Equal(0, SortableKeyboardReducer.Rove(order, Disabled("b"), 2, SortableMove.Backward));
    }

    [StaFact]
    public void Rove_First_And_Last()
    {
        var order = new[] { "a", "b", "c" };
        Assert.Equal(0, SortableKeyboardReducer.Rove(order, Disabled(), 2, SortableMove.First));
        Assert.Equal(2, SortableKeyboardReducer.Rove(order, Disabled(), 0, SortableMove.Last));
    }

    [StaFact]
    public void Rove_Forward_AtEnd_DoesNotWrap()
    {
        var order = new[] { "a", "b" };
        Assert.Equal(1, SortableKeyboardReducer.Rove(order, Disabled(), 1, SortableMove.Forward));
    }

    // --- SortableKeyboardReducer: grab-move ---

    [StaFact]
    public void Move_Forward_SwapsPastNextEnabled()
    {
        var result = SortableKeyboardReducer.Move(new[] { "a", "b", "c" }, Disabled(), 0, SortableMove.Forward);

        Assert.True(result.Moved);
        Assert.Equal(new[] { "b", "a", "c" }, result.Order);
        Assert.Equal(1, result.GrabbedIndex);
    }

    [StaFact]
    public void Move_Forward_JumpsOverDisabledRow()
    {
        // "a" grabbed, "b" disabled: forward should land past the next ENABLED row ("c").
        var result = SortableKeyboardReducer.Move(new[] { "a", "b", "c" }, Disabled("b"), 0, SortableMove.Forward);

        Assert.True(result.Moved);
        Assert.Equal(new[] { "b", "c", "a" }, result.Order);
        Assert.Equal(2, result.GrabbedIndex);
    }

    [StaFact]
    public void Move_Backward_SwapsPastPrevEnabled()
    {
        var result = SortableKeyboardReducer.Move(new[] { "a", "b", "c" }, Disabled(), 2, SortableMove.Backward);

        Assert.True(result.Moved);
        Assert.Equal(new[] { "a", "c", "b" }, result.Order);
        Assert.Equal(1, result.GrabbedIndex);
    }

    [StaFact]
    public void Move_Forward_AtLastEnabled_NoMove()
    {
        var result = SortableKeyboardReducer.Move(new[] { "a", "b" }, Disabled(), 1, SortableMove.Forward);

        Assert.False(result.Moved);
        Assert.Equal(new[] { "a", "b" }, result.Order);
    }

    [StaFact]
    public void Move_Backward_AtFirstEnabled_NoMove()
    {
        var result = SortableKeyboardReducer.Move(new[] { "a", "b" }, Disabled(), 0, SortableMove.Backward);

        Assert.False(result.Moved);
    }

    [StaFact]
    public void Move_First_MovesToFrontEnabledSlot()
    {
        var result = SortableKeyboardReducer.Move(new[] { "a", "b", "c" }, Disabled(), 2, SortableMove.First);

        Assert.True(result.Moved);
        Assert.Equal(new[] { "c", "a", "b" }, result.Order);
        Assert.Equal(0, result.GrabbedIndex);
    }

    [StaFact]
    public void Move_Last_MovesToBackEnabledSlot()
    {
        var result = SortableKeyboardReducer.Move(new[] { "a", "b", "c" }, Disabled(), 0, SortableMove.Last);

        Assert.True(result.Moved);
        Assert.Equal(new[] { "b", "c", "a" }, result.Order);
        Assert.Equal(2, result.GrabbedIndex);
    }

    [StaFact]
    public void Move_First_AlreadyFirst_NoMove()
    {
        var result = SortableKeyboardReducer.Move(new[] { "a", "b", "c" }, Disabled(), 0, SortableMove.First);

        Assert.False(result.Moved);
    }

    [StaFact]
    public void Move_PreservesEveryKey()
    {
        var result = SortableKeyboardReducer.Move(new[] { "a", "b", "c", "d" }, Disabled("c"), 0, SortableMove.Last);

        Assert.Equal(new[] { "a", "b", "c", "d" }.OrderBy(x => x), result.Order.OrderBy(x => x));
    }

    // --- NaviusSortable: defaults ---

    [StaFact]
    public void Defaults_MatchContract()
    {
        var sortable = new NaviusSortable();

        Assert.Null(sortable.Values);
        Assert.Null(sortable.DefaultValues);
        Assert.Equal(NaviusSortableOrientation.Vertical, sortable.Orientation);
        Assert.False(sortable.Disabled);
        Assert.False(sortable.IsDragging);
        Assert.Null(sortable.GrabbedKey);
    }

    [StaFact]
    public void IsItemItsOwnContainer_TrueForSortableItem()
    {
        var sortable = new NaviusSortable();
        sortable.Items.Add(new NaviusSortableItem { Value = "a" });

        // A own-container item is surfaced directly (not wrapped) by ItemsControl.
        Assert.IsType<NaviusSortableItem>(sortable.Items[0]);
    }

    // --- NaviusSortable: keyboard grab / move / drop ---

    [StaFact]
    public void Grab_SetsGrabbedKeyAndIsDragging()
    {
        var sortable = BuildSortable(("a", false), ("b", false));

        var grabbed = sortable.Grab(ItemAt(sortable, 0));

        Assert.True(grabbed);
        Assert.Equal("a", sortable.GrabbedKey);
        Assert.True(sortable.IsDragging);
        Assert.True(ItemAt(sortable, 0).IsKeyboardGrabbed);
    }

    [StaFact]
    public void Grab_DisabledItem_ReturnsFalse()
    {
        var sortable = BuildSortable(("a", true), ("b", false));

        Assert.False(sortable.Grab(ItemAt(sortable, 0)));
        Assert.Null(sortable.GrabbedKey);
    }

    [StaFact]
    public void Grab_WhenRootDisabled_ReturnsFalse()
    {
        var sortable = BuildSortable(("a", false), ("b", false));
        sortable.Disabled = true;

        Assert.False(sortable.Grab(ItemAt(sortable, 0)));
    }

    [StaFact]
    public void MoveGrabbed_MutatesOrderAndValues_ButNotOnReorder()
    {
        var sortable = BuildSortable(("a", false), ("b", false), ("c", false));
        var reorderCount = 0;
        sortable.OnReorder += (_, _) => reorderCount++;

        sortable.Grab(ItemAt(sortable, 0));
        var moved = sortable.MoveGrabbed(SortableMove.Forward);

        Assert.True(moved);
        Assert.Equal(new[] { "b", "a", "c" }, Order(sortable));
        Assert.Equal(new[] { "b", "a", "c" }, sortable.Values);
        Assert.Equal(0, reorderCount); // OnReorder is deferred to the drop
    }

    [StaFact]
    public void ValuesChanged_FiresOnKeyboardMove()
    {
        var sortable = BuildSortable(("a", false), ("b", false));
        var valuesChanged = 0;
        sortable.ValuesChanged += (_, _) => valuesChanged++;

        sortable.Grab(ItemAt(sortable, 0));
        sortable.MoveGrabbed(SortableMove.Forward);

        Assert.True(valuesChanged >= 1);
    }

    [StaFact]
    public void DropGrabbed_AfterMove_FiresOnReorderOnceWithIndices()
    {
        var sortable = BuildSortable(("a", false), ("b", false), ("c", false));
        SortableReorderEventArgs? captured = null;
        var count = 0;
        sortable.OnReorder += (_, e) =>
        {
            captured = e;
            count++;
        };

        sortable.Grab(ItemAt(sortable, 0));
        sortable.MoveGrabbed(SortableMove.Forward);
        sortable.DropGrabbed();

        Assert.Equal(1, count);
        Assert.NotNull(captured);
        Assert.Equal(0, captured!.OldIndex);
        Assert.Equal(1, captured.NewIndex);
        Assert.Null(sortable.GrabbedKey);
        Assert.False(sortable.IsDragging);
    }

    [StaFact]
    public void DropGrabbed_WithoutMove_DoesNotFireOnReorder()
    {
        var sortable = BuildSortable(("a", false), ("b", false));
        var count = 0;
        sortable.OnReorder += (_, _) => count++;

        sortable.Grab(ItemAt(sortable, 0));
        sortable.DropGrabbed();

        Assert.Equal(0, count);
    }

    [StaFact]
    public void Escape_RestoresOriginalOrder_WithoutFiringOnReorder()
    {
        var sortable = BuildSortable(("a", false), ("b", false), ("c", false));
        var reorderCount = 0;
        sortable.OnReorder += (_, _) => reorderCount++;

        sortable.Grab(ItemAt(sortable, 0));
        sortable.MoveGrabbed(SortableMove.Forward);
        sortable.MoveGrabbed(SortableMove.Forward);
        Assert.Equal(new[] { "b", "c", "a" }, Order(sortable));

        var cancelled = sortable.CancelGrab();

        Assert.True(cancelled);
        Assert.Equal(new[] { "a", "b", "c" }, Order(sortable));
        Assert.Equal(new[] { "a", "b", "c" }, sortable.Values);
        Assert.Equal(0, reorderCount);
        Assert.Null(sortable.GrabbedKey);
    }

    [StaFact]
    public void HandleItemKey_SpaceGrabsThenSpaceDrops()
    {
        var sortable = BuildSortable(("a", false), ("b", false));

        var grab = sortable.HandleItemKey(ItemAt(sortable, 0), Key.Space);
        Assert.True(grab);
        Assert.Equal("a", sortable.GrabbedKey);

        var drop = sortable.HandleItemKey(ItemAt(sortable, 0), Key.Space);
        Assert.True(drop);
        Assert.Null(sortable.GrabbedKey);
    }

    [StaFact]
    public void HandleItemKey_ArrowMoves_GrabbedItemForward()
    {
        var sortable = BuildSortable(("a", false), ("b", false), ("c", false));

        sortable.HandleItemKey(ItemAt(sortable, 0), Key.Enter);
        sortable.HandleItemKey(ItemAt(sortable, 0), Key.Down);

        Assert.Equal(new[] { "b", "a", "c" }, Order(sortable));
    }

    [StaFact]
    public void HandleItemKey_Escape_WhenNotGrabbing_IsNotHandled()
    {
        var sortable = BuildSortable(("a", false), ("b", false));

        Assert.False(sortable.HandleItemKey(ItemAt(sortable, 0), Key.Escape));
    }

    [StaFact]
    public void HandleItemKey_NoOpWhenRootDisabled()
    {
        var sortable = BuildSortable(("a", false), ("b", false));
        sortable.Disabled = true;

        Assert.False(sortable.HandleItemKey(ItemAt(sortable, 0), Key.Space));
    }

    // --- NaviusSortable: roving tabindex + UIA position ---

    [StaFact]
    public void RovingTabStop_ExactlyOneEnabledItem_DisabledSkipped()
    {
        var sortable = BuildSortable(("a", false), ("b", true), ("c", false));

        // Rove to the last enabled item; RefreshItemStates then assigns the single tab stop.
        sortable.HandleItemKey(ItemAt(sortable, 0), Key.End);

        Assert.False(ItemAt(sortable, 0).IsTabStop);
        Assert.False(ItemAt(sortable, 1).IsTabStop); // disabled is never a tab stop
        Assert.True(ItemAt(sortable, 2).IsTabStop);
    }

    [StaFact]
    public void RovingFocus_ArrowDown_SkipsDisabledRow()
    {
        var sortable = BuildSortable(("a", false), ("b", true), ("c", false));

        sortable.HandleItemKey(ItemAt(sortable, 0), Key.Down);

        Assert.True(ItemAt(sortable, 2).IsTabStop);
        Assert.False(ItemAt(sortable, 1).IsTabStop);
    }

    [StaFact]
    public void PositionInSet_And_SizeOfSet_PushedOntoItems()
    {
        var sortable = BuildSortable(("a", false), ("b", false), ("c", false));

        // Any state refresh (a grab here) populates the UIA position attributes.
        sortable.Grab(ItemAt(sortable, 1));

        Assert.Equal(2, AutomationProperties.GetPositionInSet(ItemAt(sortable, 1)));
        Assert.Equal(3, AutomationProperties.GetSizeOfSet(ItemAt(sortable, 1)));
        Assert.Equal(1, AutomationProperties.GetPositionInSet(ItemAt(sortable, 0)));
    }

    [StaFact]
    public void IsDragging_ClearsAfterDrop()
    {
        var sortable = BuildSortable(("a", false), ("b", false));

        sortable.Grab(ItemAt(sortable, 0));
        Assert.True(sortable.IsDragging);

        sortable.DropGrabbed();
        Assert.False(sortable.IsDragging);
    }

    // --- NaviusSortableItem ---

    [StaFact]
    public void Item_Defaults()
    {
        var item = new NaviusSortableItem { Value = "x" };

        Assert.Equal("x", item.Value);
        Assert.False(item.Disabled);
        Assert.False(item.IsKeyboardGrabbed);
        Assert.False(item.IsDragging);
        Assert.False(item.IsDropTarget);
    }

    [StaFact]
    public void Item_AccessibleLabel_FallsBackToValue()
    {
        Assert.Equal("x", new NaviusSortableItem { Value = "x" }.AccessibleLabel);
        Assert.Equal("Explicit", new NaviusSortableItem { Value = "x", Label = "Explicit" }.AccessibleLabel);
    }

    // --- Automation peers ---

    [StaFact]
    public void SortableAutomationPeer_ReportsListControlType()
    {
        var sortable = new NaviusSortable();
        var peer = new NaviusSortableAutomationPeer(sortable);

        Assert.Equal(AutomationControlType.List, peer.GetAutomationControlType());
        Assert.Equal(nameof(NaviusSortable), peer.GetClassName());
    }

    [StaFact]
    public void SortableItemAutomationPeer_ReportsListItemControlType()
    {
        var item = new NaviusSortableItem { Value = "a", Label = "Alpha" };
        var peer = new NaviusSortableItemAutomationPeer(item);

        Assert.Equal(AutomationControlType.ListItem, peer.GetAutomationControlType());
        Assert.Equal("Alpha", peer.GetName());
    }

    [StaFact]
    public void SortableItemAutomationPeer_LocalizedControlType_IsSortableItem()
    {
        var item = new NaviusSortableItem { Value = "a" };
        var peer = new NaviusSortableItemAutomationPeer(item);

        Assert.Equal("sortable item", peer.GetLocalizedControlType());
    }

    [StaFact]
    public void HandleAutomationPeer_IsHiddenFromControlAndContentViews()
    {
        var handle = new NaviusSortableItemHandle();
        var peer = new NaviusSortableItemHandleAutomationPeer(handle);

        Assert.False(peer.IsControlElement());
        Assert.False(peer.IsContentElement());
    }
}
