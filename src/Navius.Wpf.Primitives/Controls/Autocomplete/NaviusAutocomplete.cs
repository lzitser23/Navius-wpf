using System;
using System.Collections.Generic;
using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Autocomplete;

/// <summary>
/// Generic autocomplete root. All template-bound state lives on <see cref="NaviusAutocompleteBase"/>;
/// this subclass adds the <typeparamref name="TItem"/>-typed inputs (Items, ItemToString, Filter) and
/// implements <see cref="NaviusAutocompleteBase.Recompute"/>. It overrides <c>DefaultStyleKey</c> back
/// to the base type so every closed instantiation shares the one style in Themes/Autocomplete.xaml
/// (WPF resolves default styles per closed generic, which a single open-generic style cannot target).
/// </summary>
/// <typeparam name="TItem">The item type being filtered.</typeparam>
public class NaviusAutocomplete<TItem> : NaviusAutocompleteBase
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items), typeof(IReadOnlyList<TItem>), typeof(NaviusAutocomplete<TItem>),
        new PropertyMetadata(null, OnItemsChanged));

    private Func<TItem, string>? _itemToString;
    private Func<TItem, string, bool>? _filter;

    static NaviusAutocomplete()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAutocomplete<TItem>),
            new FrameworkPropertyMetadata(typeof(NaviusAutocompleteBase)));
    }

    /// <summary>The full item set to filter.</summary>
    public IReadOnlyList<TItem>? Items
    {
        get => (IReadOnlyList<TItem>?)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>Item to display/match text. Defaults to <c>x?.ToString()</c>.</summary>
    public Func<TItem, string>? ItemToString
    {
        get => _itemToString;
        set
        {
            _itemToString = value;
            Recompute();
        }
    }

    /// <summary>Custom <c>(item, query) => keep</c> predicate. Defaults to a case-insensitive substring match.</summary>
    public Func<TItem, string, bool>? Filter
    {
        get => _filter;
        set
        {
            _filter = value;
            Recompute();
        }
    }

    protected override void Recompute()
    {
        var items = Items ?? Array.Empty<TItem>();
        var toString = _itemToString ?? (item => item?.ToString() ?? string.Empty);

        var filtered = AutocompleteEngine.Filter(items, Value, toString, _filter);

        var rows = new List<AutocompleteRow>(filtered.Count);
        for (var i = 0; i < filtered.Count; i++)
        {
            var item = filtered[i];
            rows.Add(new AutocompleteRow(item, toString(item), i));
        }

        SetRows(rows);
    }

    private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusAutocomplete<TItem>)d).Recompute();
}
