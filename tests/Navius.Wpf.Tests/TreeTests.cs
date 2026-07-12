using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Tree;

namespace Navius.Wpf.Tests;

public class TreeTests
{
    static TreeTests()
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

    // A small fixture: Fruit (Apple[Fuji, Gala], Banana[disabled: Plantain], Cherry (leaf)).
    private static (NaviusTreeNode Root, NaviusTreeNode Apple, NaviusTreeNode Fuji, NaviusTreeNode Gala,
        NaviusTreeNode Banana, NaviusTreeNode Plantain, NaviusTreeNode Cherry) BuildFixture()
    {
        var fuji = new NaviusTreeNode("fuji", "Fuji");
        var gala = new NaviusTreeNode("gala", "Gala");
        var apple = new NaviusTreeNode("apple", "Apple", new[] { fuji, gala }) { IsExpanded = true };

        var plantain = new NaviusTreeNode("plantain", "Plantain", disabled: true);
        var banana = new NaviusTreeNode("banana", "Banana", new[] { plantain }) { IsExpanded = true };

        var cherry = new NaviusTreeNode("cherry", "Cherry");

        var root = new NaviusTreeNode("fruit", "Fruit", new[] { apple, banana, cherry }) { IsExpanded = true };

        return (root, apple, fuji, gala, banana, plantain, cherry);
    }

    private static NaviusTree BuildTree(NaviusTreeSelectionMode mode, IReadOnlyList<NaviusTreeNode> roots)
    {
        var tree = new NaviusTree { SelectionMode = mode, RootNodes = roots };
        return tree;
    }

    // --- NaviusTreeNode: data model defaults + INotifyPropertyChanged ---

    [StaFact]
    public void Node_HasChildren_TrueOnlyWithNonEmptyChildren()
    {
        var leaf = new NaviusTreeNode("a", "A");
        var parent = new NaviusTreeNode("b", "B", new[] { leaf });

        Assert.False(leaf.HasChildren);
        Assert.True(parent.HasChildren);
    }

    [StaFact]
    public void Node_Constructor_WiresParentBackReference()
    {
        var child = new NaviusTreeNode("a", "A");
        var parent = new NaviusTreeNode("b", "B", new[] { child });

        Assert.Same(parent, child.Parent);
        Assert.Null(parent.Parent);
    }

    [StaFact]
    public void Node_IsExpanded_RaisesPropertyChanged()
    {
        var node = new NaviusTreeNode("a", "A");
        string? changed = null;
        node.PropertyChanged += (_, e) => changed = e.PropertyName;

        node.IsExpanded = true;

        Assert.Equal(nameof(NaviusTreeNode.IsExpanded), changed);
    }

    [StaFact]
    public void Node_IsExpanded_NoEventWhenUnchanged()
    {
        var node = new NaviusTreeNode("a", "A");
        var fired = false;
        node.PropertyChanged += (_, _) => fired = true;

        node.IsExpanded = false;

        Assert.False(fired);
    }

    // --- TreeSelectionState: pure visible-order / navigation math ---

    [StaFact]
    public void VisibleOrder_DescendsOnlyIntoExpandedNodes()
    {
        var (root, apple, fuji, gala, banana, _, cherry) = BuildFixture();

        var visible = TreeSelectionState.VisibleOrder(new[] { root });

        Assert.Equal(new[] { root, apple, fuji, gala, banana, banana.Children![0], cherry }, visible);
    }

    [StaFact]
    public void VisibleOrder_CollapsedNodeHidesChildren()
    {
        var (root, apple, _, _, _, _, _) = BuildFixture();
        apple.IsExpanded = false;

        var visible = TreeSelectionState.VisibleOrder(new[] { root });

        Assert.DoesNotContain(apple.Children![0], visible);
    }

    [StaFact]
    public void NextEnabledIndex_SkipsDisabled_NoWrap()
    {
        var (_, _, _, _, banana, plantain, _) = BuildFixture();
        var visible = new List<NaviusTreeNode> { banana, plantain };

        var next = TreeSelectionState.NextEnabledIndex(visible, 0, +1);

        Assert.Equal(-1, next); // plantain is disabled, and Up/Down never wrap
    }

    [StaFact]
    public void FirstEnabledFrom_ReturnsNullWhenAllDisabled()
    {
        var plantain = new NaviusTreeNode("p", "P", disabled: true);
        var visible = new List<NaviusTreeNode> { plantain };

        Assert.Null(TreeSelectionState.FirstEnabledFrom(visible, 0, +1));
    }

    [StaFact]
    public void ReplaceSelection_DisabledNodeYieldsEmptySet()
    {
        var disabled = new NaviusTreeNode("d", "D", disabled: true);

        Assert.Empty(TreeSelectionState.ReplaceSelection(disabled));
    }

    [StaFact]
    public void ToggleSelection_AddsThenRemoves()
    {
        var node = new NaviusTreeNode("a", "A");
        var once = TreeSelectionState.ToggleSelection(new HashSet<object>(), node);
        var twice = TreeSelectionState.ToggleSelection(once, node);

        Assert.Contains("a", once);
        Assert.Empty(twice);
    }

    [StaFact]
    public void ToggleSelection_DisabledNodeIsNoOp()
    {
        var disabled = new NaviusTreeNode("d", "D", disabled: true);

        var result = TreeSelectionState.ToggleSelection(new HashSet<object>(), disabled);

        Assert.Empty(result);
    }

    [StaFact]
    public void SelectSpan_SkipsDisabledInRange()
    {
        var (root, apple, fuji, gala, banana, plantain, cherry) = BuildFixture();
        var visible = TreeSelectionState.VisibleOrder(new[] { root });

        var span = TreeSelectionState.SelectSpan(new HashSet<object>(), visible, banana, cherry);

        Assert.Contains("banana", span);
        Assert.DoesNotContain("plantain", span); // disabled, skipped
        Assert.Contains("cherry", span);
    }

    [StaFact]
    public void SelectSpan_WorksRegardlessOfFromToOrder()
    {
        var (root, apple, fuji, gala, _, _, _) = BuildFixture();
        var visible = TreeSelectionState.VisibleOrder(new[] { root });

        var forward = TreeSelectionState.SelectSpan(new HashSet<object>(), visible, fuji, gala);
        var backward = TreeSelectionState.SelectSpan(new HashSet<object>(), visible, gala, fuji);

        Assert.Equal(forward, backward);
    }

    [StaFact]
    public void ToggleSelectAll_SelectsAllEnabledWhenNoneSelected()
    {
        var (root, apple, fuji, gala, banana, plantain, cherry) = BuildFixture();
        var visible = TreeSelectionState.VisibleOrder(new[] { root });

        var all = TreeSelectionState.ToggleSelectAll(new HashSet<object>(), visible);

        Assert.DoesNotContain("plantain", all); // disabled excluded
        Assert.Contains("cherry", all);
    }

    [StaFact]
    public void ToggleSelectAll_DeselectsWhenAllEnabledAlreadySelected()
    {
        var (root, apple, fuji, gala, banana, plantain, cherry) = BuildFixture();
        var visible = TreeSelectionState.VisibleOrder(new[] { root });
        var all = TreeSelectionState.ToggleSelectAll(new HashSet<object>(), visible);

        var none = TreeSelectionState.ToggleSelectAll(all, visible);

        Assert.Empty(none);
    }

    [StaFact]
    public void Typeahead_MatchesLabelPrefix()
    {
        var (root, apple, fuji, gala, banana, _, cherry) = BuildFixture();
        var visible = TreeSelectionState.VisibleOrder(new[] { root });

        var match = TreeSelectionState.Typeahead("ch", root, visible);

        Assert.Same(cherry, match);
    }

    [StaFact]
    public void Typeahead_RepeatedSameCharCyclesFirstLetterMatches()
    {
        var a1 = new NaviusTreeNode("a1", "Apple");
        var a2 = new NaviusTreeNode("a2", "Avocado");
        var visible = new List<NaviusTreeNode> { a1, a2 };

        // Starting from a1, "aa" (repeated 'a') should cycle to the NEXT 'a' match, i.e. a2.
        var match = TreeSelectionState.Typeahead("aa", a1, visible);

        Assert.Same(a2, match);
    }

    [StaFact]
    public void Typeahead_SkipsDisabledCandidates()
    {
        var disabled = new NaviusTreeNode("d", "Delta", disabled: true);
        var enabled = new NaviusTreeNode("e", "Delta2");
        var visible = new List<NaviusTreeNode> { disabled, enabled };

        var match = TreeSelectionState.Typeahead("d", disabled, visible);

        Assert.Same(enabled, match);
    }

    [StaFact]
    public void ExpandableSiblings_ReturnsOnlyNodesWithChildren()
    {
        var (root, apple, fuji, _, banana, _, cherry) = BuildFixture();

        var siblings = TreeSelectionState.ExpandableSiblings(apple, new[] { root });

        Assert.Contains(apple, siblings);
        Assert.Contains(banana, siblings);
        Assert.DoesNotContain(cherry, siblings); // leaf, no children
    }

    // --- NaviusTree: defaults ---

    [StaFact]
    public void Tree_DefaultSelectionModeIsSingle()
    {
        Assert.Equal(NaviusTreeSelectionMode.Single, new NaviusTree().SelectionMode);
    }

    [StaFact]
    public void Tree_DefaultSelectionIsEmpty()
    {
        Assert.Empty(new NaviusTree().SelectedValues);
        Assert.Null(new NaviusTree().SelectedValue);
    }

    [StaFact]
    public void Tree_SettingRootNodes_WiresItemsSource()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = new NaviusTree { RootNodes = new[] { root } };

        Assert.Same(tree.RootNodes, tree.ItemsSource);
    }

    // --- NaviusTree: single-select keyboard model ---

    [StaFact]
    public void HandleKey_Down_FromNoActiveNode_ResolvesDefaultThenAdvances()
    {
        // Mirrors the web contract's MoveAsync 1:1: ActiveOrDefault resolves the first node as
        // "current" when nothing is focused yet, then Down advances from there. In the real
        // running app this never actually surfaces because native Tab-focus already lands on the
        // first tabbable node before any keydown can fire; Home/End are the entry points that seed
        // a known active node for the rest of these tests.
        var (root, apple, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });

        var handled = tree.HandleKey(Key.Down, ModifierKeys.None);

        Assert.True(handled);
        Assert.Equal(apple.Value, tree.ActiveValue);
    }

    [StaFact]
    public void HandleKey_Down_Repeated_AdvancesThroughVisibleOrder()
    {
        var (root, apple, fuji, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = root

        tree.HandleKey(Key.Down, ModifierKeys.None); // apple
        tree.HandleKey(Key.Down, ModifierKeys.None); // fuji

        Assert.Equal(fuji.Value, tree.ActiveValue);
    }

    [StaFact]
    public void HandleKey_Down_DoesNotWrapPastLastVisible()
    {
        var cherry = new NaviusTreeNode("cherry", "Cherry");
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { cherry });
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = cherry (the only node)

        tree.HandleKey(Key.Down, ModifierKeys.None); // no more nodes, must stay put

        Assert.Equal(cherry.Value, tree.ActiveValue);
    }

    [StaFact]
    public void HandleKey_End_JumpsToLastVisibleNode()
    {
        var (root, _, _, _, _, _, cherry) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });

        tree.HandleKey(Key.End, ModifierKeys.None);

        Assert.Equal(cherry.Value, tree.ActiveValue);
    }

    [StaFact]
    public void HandleKey_Home_JumpsToFirstVisibleNode()
    {
        var (root, _, _, _, _, _, cherry) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.HandleKey(Key.End, ModifierKeys.None);

        tree.HandleKey(Key.Home, ModifierKeys.None);

        Assert.Equal(root.Value, tree.ActiveValue);
    }

    [StaFact]
    public void HandleKey_Space_SingleMode_ReplacesSelection()
    {
        var (root, apple, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = root
        tree.HandleKey(Key.Space, ModifierKeys.None);
        tree.HandleKey(Key.Down, ModifierKeys.None); // active = apple
        tree.HandleKey(Key.Space, ModifierKeys.None);

        Assert.Equal(new object[] { apple.Value }, tree.SelectedValues);
    }

    [StaFact]
    public void HandleKey_ArrowRight_Ltr_ExpandsCollapsedNode()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        root.IsExpanded = false;
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.HandleKey(Key.Down, ModifierKeys.None); // active = root

        tree.HandleKey(Key.Right, ModifierKeys.None);

        Assert.True(root.IsExpanded);
    }

    [StaFact]
    public void HandleKey_ArrowLeft_Ltr_CollapsesExpandedNode()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = root (already expanded)

        tree.HandleKey(Key.Left, ModifierKeys.None);

        Assert.False(root.IsExpanded);
    }

    [StaFact]
    public void HandleKey_ArrowKeys_AreMirroredUnderRtl()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.FlowDirection = FlowDirection.RightToLeft;
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = root (expanded)

        // Under rtl, ArrowLeft is the "expand" key and ArrowRight is "collapse" (mirrored).
        tree.HandleKey(Key.Right, ModifierKeys.None);

        Assert.False(root.IsExpanded);
    }

    [StaFact]
    public void HandleKey_Asterisk_ExpandsAllSiblingsAtLevel()
    {
        var (root, apple, _, _, banana, _, cherry) = BuildFixture();
        apple.IsExpanded = false;
        banana.IsExpanded = false;
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.HandleKey(Key.Down, ModifierKeys.None); // active = root
        tree.HandleKey(Key.Down, ModifierKeys.None); // active = apple

        tree.HandleKey(Key.Multiply, ModifierKeys.None);

        Assert.True(apple.IsExpanded);
        Assert.True(banana.IsExpanded);
    }

    [StaFact]
    public void HandleKey_Enter_TogglesExpansionThenSelectsInSingleMode()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = root (expanded)

        tree.HandleKey(Key.Enter, ModifierKeys.None);

        Assert.False(root.IsExpanded); // toggled
        Assert.Equal(new object[] { root.Value }, tree.SelectedValues);
    }

    [StaFact]
    public void HandleKey_WhenDisabled_ReturnsFalse()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.IsEnabled = false;

        Assert.False(tree.HandleKey(Key.Down, ModifierKeys.None));
    }

    [StaFact]
    public void HandleKey_Typeahead_MovesActiveToMatch()
    {
        var (root, _, _, _, _, _, cherry) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = root

        tree.HandleKey(Key.C, ModifierKeys.None);

        Assert.Equal(cherry.Value, tree.ActiveValue);
    }

    // --- NaviusTree: multi-select keyboard model ---

    [StaFact]
    public void HandleKey_CtrlA_MultiMode_SelectsAllEnabled()
    {
        var (root, apple, fuji, gala, banana, plantain, cherry) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });

        tree.HandleKey(Key.A, ModifierKeys.Control);

        Assert.DoesNotContain(plantain.Value, tree.SelectedValues);
        Assert.Contains(cherry.Value, tree.SelectedValues);
    }

    [StaFact]
    public void HandleKey_CtrlA_Twice_DeselectsAll()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.HandleKey(Key.A, ModifierKeys.Control);

        tree.HandleKey(Key.A, ModifierKeys.Control);

        Assert.Empty(tree.SelectedValues);
    }

    [StaFact]
    public void HandleKey_ShiftDown_ExtendsSelectionToNewNode()
    {
        var (root, apple, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = root
        tree.HandleKey(Key.Space, ModifierKeys.None); // select root

        tree.HandleKey(Key.Down, ModifierKeys.Shift); // extend to apple

        Assert.Contains(root.Value, tree.SelectedValues);
        Assert.Contains(apple.Value, tree.SelectedValues);
    }

    [StaFact]
    public void HandleKey_ShiftSpace_SelectsContiguousSpanFromAnchor()
    {
        var (root, apple, fuji, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = root
        tree.HandleKey(Key.Space, ModifierKeys.None); // anchor = root
        tree.HandleKey(Key.Down, ModifierKeys.None); // active = apple
        tree.HandleKey(Key.Down, ModifierKeys.None); // active = fuji

        tree.HandleKey(Key.Space, ModifierKeys.Shift);

        Assert.Contains(root.Value, tree.SelectedValues);
        Assert.Contains(apple.Value, tree.SelectedValues);
        Assert.Contains(fuji.Value, tree.SelectedValues);
    }

    [StaFact]
    public void HandleKey_CtrlShiftEnd_SelectsRangeToLastNode()
    {
        var (root, _, _, _, _, _, cherry) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.HandleKey(Key.Home, ModifierKeys.None); // active = root

        tree.HandleKey(Key.End, ModifierKeys.Control | ModifierKeys.Shift);

        Assert.Contains(root.Value, tree.SelectedValues);
        Assert.Contains(cherry.Value, tree.SelectedValues);
        Assert.Equal(cherry.Value, tree.ActiveValue);
    }

    [StaFact]
    public void HandleKey_CtrlShiftHome_SelectsRangeToFirstNode()
    {
        var (root, _, _, _, _, _, cherry) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.HandleKey(Key.End, ModifierKeys.None); // active = cherry

        tree.HandleKey(Key.Home, ModifierKeys.Control | ModifierKeys.Shift);

        Assert.Contains(root.Value, tree.SelectedValues);
        Assert.Contains(cherry.Value, tree.SelectedValues);
    }

    [StaFact]
    public void HandleKey_Space_MultiMode_Toggles()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.HandleKey(Key.Down, ModifierKeys.None);

        tree.HandleKey(Key.Space, ModifierKeys.None);
        tree.HandleKey(Key.Space, ModifierKeys.None);

        Assert.Empty(tree.SelectedValues);
    }

    [StaFact]
    public void HandleKey_NoneMode_SpaceDoesNotSelect()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.None, new[] { root });
        tree.HandleKey(Key.Down, ModifierKeys.None);

        tree.HandleKey(Key.Space, ModifierKeys.None);

        Assert.Empty(tree.SelectedValues);
    }

    // --- NaviusTree: click model ---

    [StaFact]
    public void HandleItemClicked_SingleMode_PlainClickReplacesSelection()
    {
        var (root, apple, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });

        tree.HandleItemClicked(root, new NaviusTreeItem(), ModifierKeys.None);
        tree.HandleItemClicked(apple, new NaviusTreeItem(), ModifierKeys.None);

        Assert.Equal(new object[] { apple.Value }, tree.SelectedValues);
    }

    [StaFact]
    public void HandleItemClicked_PlainClick_TogglesExpansionOfExpandableNode()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });

        tree.HandleItemClicked(root, new NaviusTreeItem(), ModifierKeys.None);

        Assert.False(root.IsExpanded);
    }

    [StaFact]
    public void HandleItemClicked_MultiMode_CtrlClickToggles()
    {
        var (root, apple, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.HandleItemClicked(root, new NaviusTreeItem(), ModifierKeys.None);

        tree.HandleItemClicked(apple, new NaviusTreeItem(), ModifierKeys.Control);

        Assert.Contains(root.Value, tree.SelectedValues);
        Assert.Contains(apple.Value, tree.SelectedValues);
    }

    [StaFact]
    public void HandleItemClicked_MultiMode_ShiftClickSelectsRange()
    {
        var (root, apple, fuji, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        // Seed the anchor via Space (not a plain click on root, which would toggle-collapse root
        // per the contract's Activate semantics and hide apple/fuji from the visible order).
        tree.HandleKey(Key.Home, ModifierKeys.None);
        tree.HandleKey(Key.Space, ModifierKeys.None);

        tree.HandleItemClicked(fuji, new NaviusTreeItem(), ModifierKeys.Shift);

        Assert.Contains(root.Value, tree.SelectedValues);
        Assert.Contains(apple.Value, tree.SelectedValues);
        Assert.Contains(fuji.Value, tree.SelectedValues);
    }

    [StaFact]
    public void HandleItemClicked_DisabledNode_IsNoOp()
    {
        var (root, _, _, _, _, plantain, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });

        tree.HandleItemClicked(plantain, new NaviusTreeItem(), ModifierKeys.None);

        Assert.Empty(tree.SelectedValues);
    }

    // --- NaviusTree: selection painting (NaviusTreeNode.IsSelected) ---

    [StaFact]
    public void ApplySelection_SetsIsSelectedOnNode()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });

        tree.SetSelectedValues(new object[] { root.Value });

        Assert.True(root.IsSelected);
    }

    [StaFact]
    public void ApplySelection_ClearsIsSelectedOnPreviouslySelectedNode()
    {
        var (root, apple, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.SetSelectedValues(new object[] { root.Value });

        tree.SetSelectedValues(new object[] { apple.Value });

        Assert.False(root.IsSelected);
        Assert.True(apple.IsSelected);
    }

    [StaFact]
    public void SelectedValuesChanged_FiresWithFullSelection()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        IReadOnlyList<object>? observed = null;
        tree.SelectedValuesChanged += (_, e) => observed = e.SelectedValues;

        tree.SetSelectedValues(new object[] { root.Value });

        Assert.Equal(new object[] { root.Value }, observed);
    }

    [StaFact]
    public void SelectedValuesChanged_DoesNotFireForNoOpSet()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.SetSelectedValues(new object[] { root.Value });
        var fired = false;
        tree.SelectedValuesChanged += (_, _) => fired = true;

        tree.SetSelectedValues(new object[] { root.Value });

        Assert.False(fired);
    }

    // --- Automation peers ---

    [StaFact]
    public void AutomationPeer_CanSelectMultiple_ReflectsSelectionMode()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        var peer = (ISelectionProvider)new NaviusTreeAutomationPeer(tree);

        Assert.True(peer.CanSelectMultiple);
    }

    [StaFact]
    public void AutomationPeer_CanSelectMultiple_FalseForSingleMode()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        var peer = (ISelectionProvider)new NaviusTreeAutomationPeer(tree);

        Assert.False(peer.CanSelectMultiple);
    }

    [StaFact]
    public void AutomationPeer_IsSelectionRequired_IsFalse()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        var peer = (ISelectionProvider)new NaviusTreeAutomationPeer(tree);

        Assert.False(peer.IsSelectionRequired);
    }

    [StaFact]
    public void ItemAutomationPeer_IsCustomPeer_StillDerivesTreeViewItemPeer()
    {
        // NaviusTreeItem now overrides OnCreateAutomationPeer to fix the SelectionItemPattern gap
        // this test used to document (see NaviusTreeItemAutomationPeer's doc comment /
        // docs/parity/tree.md "WPF implementation notes"), but it still derives
        // TreeViewItemAutomationPeer so the native ExpandCollapsePattern mapping is unaffected.
        var item = new NaviusTreeItem();

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(item);

        var custom = Assert.IsType<NaviusTreeItemAutomationPeer>(peer);
        Assert.IsAssignableFrom<TreeViewItemAutomationPeer>(custom);
    }

    [StaFact]
    public void ItemAutomationPeer_IsSelected_ReflectsNodeSelection_NotNativeIsSelected()
    {
        var (root, _, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        var item = new NaviusTreeItem { DataContext = root };
        var peer = (ISelectionItemProvider)new NaviusTreeItemAutomationPeer(item);

        Assert.False(peer.IsSelected);

        tree.SetSelectedValues(new object[] { root.Value });

        Assert.True(peer.IsSelected);

        tree.SetSelectedValues(Array.Empty<object>());

        Assert.False(peer.IsSelected);
    }

    [StaFact]
    public void ItemAutomationPeer_WithoutTreeAncestor_RoutedMembersNoOpInsteadOfThrowing()
    {
        var item = new NaviusTreeItem { DataContext = new NaviusTreeNode("a", "A") };
        var peer = (ISelectionItemProvider)new NaviusTreeItemAutomationPeer(item);

        Assert.Null(peer.SelectionContainer);
        peer.AddToSelection();
        peer.RemoveFromSelection();
        peer.Select();
    }

    [StaFact]
    public void ItemAutomationPeer_DisabledItemRefusesSelectionActions()
    {
        var item = new NaviusTreeItem
        {
            DataContext = new NaviusTreeNode("a", "A"),
            IsEnabled = false,
        };
        var peer = (ISelectionItemProvider)new NaviusTreeItemAutomationPeer(item);

        Assert.Throws<ElementNotEnabledException>(peer.AddToSelection);
        Assert.Throws<ElementNotEnabledException>(peer.RemoveFromSelection);
        Assert.Throws<ElementNotEnabledException>(peer.Select);
    }

    [StaFact]
    public void ItemAutomationPeer_RaiseSelectionEvents_DoesNotThrowWithoutListener()
    {
        var item = new NaviusTreeItem();
        var peer = new NaviusTreeItemAutomationPeer(item);

        peer.RaiseSelectionEvents(true);
        peer.RaiseSelectionEvents(false);
    }

    // --- NaviusTree: ISelectionItemProvider routing (AddToSelection/RemoveFromSelection/Select) ---

    [StaFact]
    public void SelectNodeExclusive_ReplacesSelectionWithJustThatNode_EvenInMultipleMode()
    {
        var (root, apple, fuji, gala, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.SetSelectedValues(new object[] { fuji.Value, gala.Value });

        tree.SelectNodeExclusive(apple);

        Assert.Equal(new object[] { apple.Value }, tree.SelectedValues);
    }

    [StaFact]
    public void SelectNodeExclusive_DisabledNode_NoOp()
    {
        var (root, _, _, _, _, plantain, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });

        tree.SelectNodeExclusive(plantain);

        Assert.Empty(tree.SelectedValues);
    }

    [StaFact]
    public void AddNodeToSelection_Multiple_AddsWithoutClearingExisting()
    {
        var (root, _, fuji, gala, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.SetSelectedValues(new object[] { fuji.Value });

        tree.AddNodeToSelection(gala);

        Assert.Equal(new object[] { fuji.Value, gala.Value }, tree.SelectedValues.OrderBy(v => v));
    }

    [StaFact]
    public void AddNodeToSelection_Single_NoExistingSelection_Selects()
    {
        var (root, apple, _, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });

        tree.AddNodeToSelection(apple);

        Assert.Equal(new object[] { apple.Value }, tree.SelectedValues);
    }

    [StaFact]
    public void AddNodeToSelection_Single_DifferentExistingSelection_Throws()
    {
        var (root, apple, fuji, _, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, new[] { root });
        tree.SetSelectedValues(new object[] { apple.Value });

        Assert.Throws<InvalidOperationException>(() => tree.AddNodeToSelection(fuji));
    }

    [StaFact]
    public void RemoveNodeFromSelection_RemovesJustThatNode()
    {
        var (root, _, fuji, gala, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.SetSelectedValues(new object[] { fuji.Value, gala.Value });

        tree.RemoveNodeFromSelection(fuji);

        Assert.Equal(new object[] { gala.Value }, tree.SelectedValues);
    }

    [StaFact]
    public void RemoveNodeFromSelection_NotSelected_NoOpDoesNotFireEvent()
    {
        var (root, _, fuji, gala, _, _, _) = BuildFixture();
        var tree = BuildTree(NaviusTreeSelectionMode.Multiple, new[] { root });
        tree.SetSelectedValues(new object[] { fuji.Value });
        var fired = false;
        tree.SelectedValuesChanged += (_, _) => fired = true;

        tree.RemoveNodeFromSelection(gala);

        Assert.False(fired);
    }

    // --- NaviusTreeItem: container plumbing ---

    [StaFact]
    public void TreeItem_Node_ReflectsDataContext()
    {
        var node = new NaviusTreeNode("a", "A");
        var item = new NaviusTreeItem { DataContext = node };

        Assert.Same(node, item.Node);
    }

    // --- Perf guard: the retemplate must not silently regress virtualization (10k-node gate) ---

    [StaFact]
    public void StyleApplication_PreservesVirtualization_Tree()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tree.xaml", UriKind.Absolute),
        };

        var style = (Style)dictionary[typeof(NaviusTree)];
        Assert.NotNull(style);

        var tree = new NaviusTree { Style = style };

        Assert.True((bool)tree.GetValue(VirtualizingPanel.IsVirtualizingProperty));
        Assert.Equal(VirtualizationMode.Recycling, (VirtualizationMode)tree.GetValue(VirtualizingPanel.VirtualizationModeProperty));
    }

    [StaFact]
    public void StyleApplication_PreservesVirtualization_TreeItem()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tree.xaml", UriKind.Absolute),
        };

        var style = (Style)dictionary[typeof(NaviusTreeItem)];
        Assert.NotNull(style);

        var item = new NaviusTreeItem { Style = style };

        Assert.True((bool)item.GetValue(VirtualizingPanel.IsVirtualizingProperty));
        Assert.Equal(VirtualizationMode.Recycling, (VirtualizationMode)item.GetValue(VirtualizingPanel.VirtualizationModeProperty));
    }

    [StaFact]
    public void Style_TargetsNaviusTree()
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Tree.xaml", UriKind.Absolute),
        };

        var style = (Style)dictionary[typeof(NaviusTree)];

        Assert.Equal(typeof(NaviusTree), style.TargetType);
    }

    // --- 10k-node scale: keyboard nav math stays correct and fast at scale ---

    [StaFact]
    public void VisibleOrder_10kFlatNodes_CountsCorrectly()
    {
        var nodes = Enumerable.Range(0, 10_000).Select(i => new NaviusTreeNode(i, $"Node {i}")).ToList();

        var visible = TreeSelectionState.VisibleOrder(nodes);

        Assert.Equal(10_000, visible.Count);
    }

    [StaFact]
    public void HandleKey_End_On10kNodes_ReachesLastNode()
    {
        var nodes = Enumerable.Range(0, 10_000).Select(i => new NaviusTreeNode(i, $"Node {i}")).ToList();
        var tree = BuildTree(NaviusTreeSelectionMode.Single, nodes);

        tree.HandleKey(Key.End, ModifierKeys.None);

        Assert.Equal(9_999, tree.ActiveValue);
    }
}
