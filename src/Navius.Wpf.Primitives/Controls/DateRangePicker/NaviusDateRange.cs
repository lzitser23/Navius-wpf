using System;

namespace Navius.Wpf.Primitives.Controls.DateRangePicker;

/// <summary>
/// WPF-idiomatic mirror of the web contract's <c>NaviusDateRange</c> (Common/NaviusDateRange.cs),
/// using <see cref="DateTime"/> instead of <c>DateOnly</c> to match <see cref="System.Windows.Controls.Calendar"/>'s
/// own value type. A plain readonly record struct so it has structural equality out of the box
/// (needed for WPF DependencyProperty change detection) and is trivially unit-testable.
/// </summary>
public readonly record struct NaviusDateRange(DateTime? Start, DateTime? End)
{
    public static readonly NaviusDateRange Empty = new(null, null);

    /// <summary>Neither endpoint is set.</summary>
    public bool IsEmpty => Start is null && End is null;

    /// <summary>Both endpoints are set (mirrors the web contract's IsComplete).</summary>
    public bool IsComplete => Start is not null && End is not null;
}
