using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.OverlaySurface;

namespace Navius.Wpf.Ui.CommandPalette;

/// <summary>
/// A modal search-and-run surface: a text input filtering a grouped command list, ArrowUp/Down
/// moving a virtual highlight (never real focus, same model as
/// Navius.Wpf.Primitives.Controls.Autocomplete.NaviusAutocompleteBase), Enter executing the
/// highlighted item, Escape closing. Built directly on <see cref="NaviusOverlaySurfaceBase"/> (the
/// same Dialog/AlertDialog/Drawer substrate): that base class already supplies the modal
/// backdrop/focus-trap/Escape/outside-click/enter-exit-fade state machine, so this control only
/// adds the search input, the filtered+grouped row list, and keyboard-driven execution on top of
/// it, consuming the primitive rather than re-deriving any of its plumbing. Filtering/highlight math
/// itself lives in <see cref="CommandPaletteEngine"/>, which in turn consumes
/// Navius.Wpf.Primitives' AutocompleteEngine.
/// </summary>
[TemplatePart(Name = PartInput, Type = typeof(TextBox))]
[TemplatePart(Name = PartList, Type = typeof(ItemsControl))]
public class NaviusCommandPalette : NaviusOverlaySurfaceBase
{
    private const string PartInput = "PART_Input";
    private const string PartList = "PART_List";

    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IReadOnlyList<CommandPaletteItem>), typeof(NaviusCommandPalette),
        new PropertyMetadata(Array.Empty<CommandPaletteItem>(), OnItemsChanged));

    public static readonly DependencyProperty QueryProperty = DependencyProperty.Register(
        nameof(Query), typeof(string), typeof(NaviusCommandPalette),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnQueryChanged));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(NaviusCommandPalette), new PropertyMetadata("Type a command..."));

    public static readonly DependencyProperty HighlightedIndexProperty = DependencyProperty.Register(
        nameof(HighlightedIndex), typeof(int), typeof(NaviusCommandPalette),
        new PropertyMetadata(-1, OnHighlightedIndexChanged));

    private static readonly DependencyPropertyKey FilteredRowsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(FilteredRows), typeof(ObservableCollection<CommandPaletteRow>), typeof(NaviusCommandPalette),
        new PropertyMetadata(null));

    public static readonly DependencyProperty FilteredRowsProperty = FilteredRowsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsEmptyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsEmpty), typeof(bool), typeof(NaviusCommandPalette), new PropertyMetadata(true));

    public static readonly DependencyProperty IsEmptyProperty = IsEmptyPropertyKey.DependencyProperty;

    private TextBox? _inputPart;

    static NaviusCommandPalette()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusCommandPalette),
            new FrameworkPropertyMetadata(typeof(NaviusCommandPalette)));
    }

    public NaviusCommandPalette()
    {
        SetValue(FilteredRowsPropertyKey, new ObservableCollection<CommandPaletteRow>());
    }

    /// <summary>The full command set. Pre-group adjacent items sharing a Group so headers render once per cluster.</summary>
    public IReadOnlyList<CommandPaletteItem> Items
    {
        get => (IReadOnlyList<CommandPaletteItem>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public string Query
    {
        get => (string)GetValue(QueryProperty);
        set => SetValue(QueryProperty, value);
    }

    public string? Placeholder
    {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public int HighlightedIndex
    {
        get => (int)GetValue(HighlightedIndexProperty);
        set => SetValue(HighlightedIndexProperty, value);
    }

    public ObservableCollection<CommandPaletteRow> FilteredRows =>
        (ObservableCollection<CommandPaletteRow>)GetValue(FilteredRowsProperty);

    public bool IsEmpty => (bool)GetValue(IsEmptyProperty);

    protected override bool ModalEffective => true;

    protected override FrameworkElement? ResolveInitialFocusElement() => _inputPart;

    public override void OnApplyTemplate()
    {
        if (_inputPart is not null)
        {
            _inputPart.PreviewKeyDown -= OnInputPreviewKeyDown;
        }

        base.OnApplyTemplate();

        _inputPart = GetTemplateChild(PartInput) as TextBox;
        if (_inputPart is not null)
        {
            _inputPart.PreviewKeyDown += OnInputPreviewKeyDown;
        }

        Recompute();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusCommandPaletteAutomationPeer(this);

    private void Recompute()
    {
        var filtered = CommandPaletteEngine.Filter(Items ?? Array.Empty<CommandPaletteItem>(), Query);

        var rows = new List<CommandPaletteRow>(filtered.Count);
        string? lastGroup = null;
        for (var i = 0; i < filtered.Count; i++)
        {
            var item = filtered[i];
            var showHeader = !string.IsNullOrEmpty(item.Group) && item.Group != lastGroup;
            rows.Add(new CommandPaletteRow(item, i, showHeader));
            lastGroup = item.Group;
        }

        var collection = FilteredRows;
        collection.Clear();
        foreach (var row in rows)
        {
            collection.Add(row);
        }

        SetValue(IsEmptyPropertyKey, rows.Count == 0);
        SetCurrentValue(HighlightedIndexProperty, rows.Count > 0 ? 0 : -1);
    }

    private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusCommandPalette)d).Recompute();

    private static void OnQueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusCommandPalette)d).Recompute();

    private static void OnHighlightedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var palette = (NaviusCommandPalette)d;
        var index = (int)e.NewValue;
        foreach (var row in palette.FilteredRows)
        {
            row.IsHighlighted = row.Index == index;
        }
    }

    private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var count = FilteredRows.Count;

        switch (e.Key)
        {
            case Key.Down:
                SetCurrentValue(HighlightedIndexProperty, CommandPaletteEngine.MoveHighlight(HighlightedIndex, count, +1));
                e.Handled = true;
                break;

            case Key.Up:
                SetCurrentValue(HighlightedIndexProperty, CommandPaletteEngine.MoveHighlight(HighlightedIndex, count, -1));
                e.Handled = true;
                break;

            case Key.Home:
                if (count > 0)
                {
                    SetCurrentValue(HighlightedIndexProperty, 0);
                    e.Handled = true;
                }

                break;

            case Key.End:
                if (count > 0)
                {
                    SetCurrentValue(HighlightedIndexProperty, count - 1);
                    e.Handled = true;
                }

                break;

            case Key.Enter:
                RunHighlighted();
                e.Handled = true;
                break;
        }
    }

    private void RunHighlighted()
    {
        if (HighlightedIndex < 0 || HighlightedIndex >= FilteredRows.Count)
        {
            return;
        }

        var item = FilteredRows[HighlightedIndex].Item;
        CommandPaletteEngine.Execute(item);
        Close();
    }
}

/// <summary>Per-row wrapper the list binds to: the item, its flat filtered-order index (used for keyboard nav), and whether a group header renders above it.</summary>
public sealed class CommandPaletteRow : INotifyPropertyChanged
{
    private bool _isHighlighted;

    public CommandPaletteRow(CommandPaletteItem item, int index, bool showGroupHeader)
    {
        Item = item;
        Index = index;
        ShowGroupHeader = showGroupHeader;
    }

    public CommandPaletteItem Item { get; }

    public int Index { get; }

    public bool ShowGroupHeader { get; }

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (_isHighlighted == value)
            {
                return;
            }

            _isHighlighted = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHighlighted)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

internal sealed class NaviusCommandPaletteAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusCommandPaletteAutomationPeer(NaviusCommandPalette owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

    protected override string GetClassNameCore() => nameof(NaviusCommandPalette);

    protected override bool IsDialogCore() => true;
}
