using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Navius.Wpf.Primitives.Controls.Select;

/// <summary>
/// The public, strongly-typed Select control (contract's NaviusSelect). Generic over the value
/// type so <see cref="Value"/>/<see cref="SelectedValues"/> and the typed change events are
/// TItem-shaped, while all visual/template state lives on <see cref="NaviusSelectBase"/>.
///
/// The DefaultStyleKey is deliberately pointed at <see cref="NaviusSelectBase"/> (not at the closed
/// generic type): WPF resolves DefaultStyleKeyProperty per closed generic type, so without this
/// override <c>NaviusSelect&lt;string&gt;</c> and <c>NaviusSelect&lt;int&gt;</c> would each need
/// their own <c>Style TargetType</c>; pointing every instantiation at the base lets one style in
/// Themes/Select.xaml serve them all.
/// </summary>
public class NaviusSelect<TItem> : NaviusSelectBase
{
    static NaviusSelect()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusSelect<TItem>),
            new FrameworkPropertyMetadata(typeof(NaviusSelectBase)));
    }

    public NaviusSelect()
    {
        // DefaultStyleKey (above) only routes the *theme* style (Generic.xaml) to the base type.
        // The Select theme ships as an *implicit* style merged into ambient Resources, and WPF
        // resolves implicit styles by the element's runtime type (the closed generic), which never
        // matches the base-typed key. Pointing Style at the base type's resource key makes every
        // closed instantiation pick up the single shared style wherever Themes/Select.xaml is in
        // scope. Harmless (Style stays unresolved) when no such resource is present.
        SetResourceReference(StyleProperty, typeof(NaviusSelectBase));
    }

    /// <summary>Single-select value (contract's Value); wraps the base's object-typed RawValue.</summary>
    public TItem? Value
    {
        get => RawValue is TItem value ? value : default;
        set => RawValue = value;
    }

    /// <summary>Controlled multi-select set (contract's Values); wraps the base's object-typed RawValues.</summary>
    public IReadOnlyList<TItem> Values
    {
        get => SelectedValues;
        set => RawValues = value is null ? Array.Empty<object>() : value.Cast<object>().ToArray();
    }

    /// <summary>The current multi-select set, materialised as TItem (contract's SelectedValues).</summary>
    public IReadOnlyList<TItem> SelectedValues =>
        RawValues.OfType<TItem>().ToArray();

    /// <summary>Fires on every single-select commit (contract's ValueChanged).</summary>
    public event EventHandler<TItem?>? ValueSelected;

    /// <summary>Fires on every multi-select toggle (contract's ValuesChanged).</summary>
    public event EventHandler<IReadOnlyList<TItem>>? ValuesSelected;

    protected override void OnValueCommitted()
    {
        base.OnValueCommitted();
        ValueSelected?.Invoke(this, Value);
    }

    protected override void OnValuesCommitted()
    {
        base.OnValuesCommitted();
        ValuesSelected?.Invoke(this, SelectedValues);
    }
}
