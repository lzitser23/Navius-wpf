using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.DateInput;

/// <summary>
/// Shared spinbutton cell (contract's NaviusFieldSegment, shared by NaviusDateInput and
/// NaviusTimeInput/NaviusTimePicker; the WPF port keeps it in the DateInput family folder rather
/// than a separate Navius.Primitives.Common-equivalent assembly, since this repo is single-package).
/// A thin, directly themable Control: the owning root builds one per editable unit in its layout
/// (not hand-placed -- same "root iterates its layout, not a user-composed part" precedent as
/// NaviusOneTimePasswordFieldInput) and drives DisplayText/ValueNow/IsPlaceholder from its pure
/// DateTimeSegment model. All keyboard handling lives on the root via PreviewKeyDown, mirroring
/// NaviusOneTimePasswordField's cell wiring; this class owns no logic of its own beyond the
/// AutomationPeer's read/write bridge back to the root (<see cref="ValueRequested"/>).
/// </summary>
public class NaviusFieldSegment : Control
{
    public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
        nameof(Unit), typeof(SegmentUnit), typeof(NaviusFieldSegment), new PropertyMetadata(SegmentUnit.Day));

    public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(
        nameof(DisplayText), typeof(string), typeof(NaviusFieldSegment), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsPlaceholderProperty = DependencyProperty.Register(
        nameof(IsPlaceholder), typeof(bool), typeof(NaviusFieldSegment), new PropertyMetadata(true));

    public static readonly DependencyProperty IsSegmentReadOnlyProperty = DependencyProperty.Register(
        nameof(IsSegmentReadOnly), typeof(bool), typeof(NaviusFieldSegment), new PropertyMetadata(false));

    public static readonly DependencyProperty ValueNowProperty = DependencyProperty.Register(
        nameof(ValueNow), typeof(int?), typeof(NaviusFieldSegment), new PropertyMetadata(null));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(int), typeof(NaviusFieldSegment), new PropertyMetadata(0));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(int), typeof(NaviusFieldSegment), new PropertyMetadata(0));

    static NaviusFieldSegment()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NaviusFieldSegment), new FrameworkPropertyMetadata(typeof(NaviusFieldSegment)));
        FocusableProperty.OverrideMetadata(typeof(NaviusFieldSegment), new FrameworkPropertyMetadata(true));
    }

    /// <summary>Fired when the AutomationPeer's IRangeValueProvider.SetValue is invoked; the owning root applies it to the pure segment model and commits.</summary>
    public event EventHandler<int>? ValueRequested;

    public SegmentUnit Unit
    {
        get => (SegmentUnit)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>Visible + aria-valuetext-equivalent text ("Empty" when unfilled, else the formatted value).</summary>
    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    /// <summary>data-placeholder: true when the segment has no value yet.</summary>
    public bool IsPlaceholder
    {
        get => (bool)GetValue(IsPlaceholderProperty);
        set => SetValue(IsPlaceholderProperty, value);
    }

    public bool IsSegmentReadOnly
    {
        get => (bool)GetValue(IsSegmentReadOnlyProperty);
        set => SetValue(IsSegmentReadOnlyProperty, value);
    }

    /// <summary>aria-valuenow equivalent; null when unfilled.</summary>
    public int? ValueNow
    {
        get => (int?)GetValue(ValueNowProperty);
        set => SetValue(ValueNowProperty, value);
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusFieldSegmentAutomationPeer(this);

    internal void RaiseValueRequested(int value) => ValueRequested?.Invoke(this, value);
}

/// <summary>
/// Maps NaviusFieldSegment to role="spinbutton": AutomationControlType.Spinner + IRangeValueProvider
/// (aria-valuenow/min/max), per date-input.md's WPF strategy. Every segment kind -- including
/// day-period -- uses the same peer (contract delta: the web's AM/PM segment omits aria-valuemin/max
/// and behaves as a toggle; the WPF port models it as Min=0 ("AM")/Max=1 ("PM") over the same
/// IRangeValueProvider instead of a second peer type, since ArrowUp/Down already treat it as a
/// 2-state range and one peer contract is simpler for AT to consume consistently).
/// </summary>
public sealed class NaviusFieldSegmentAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    public NaviusFieldSegmentAutomationPeer(NaviusFieldSegment owner) : base(owner)
    {
    }

    private NaviusFieldSegment Segment => (NaviusFieldSegment)Owner;

    public bool IsReadOnly => !Segment.IsEnabled || Segment.IsSegmentReadOnly;

    public double Value => Segment.ValueNow ?? double.NaN;

    public double Minimum => Segment.Minimum;

    public double Maximum => Segment.Maximum;

    public double SmallChange => 1;

    public double LargeChange => 1;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;

    protected override string GetClassNameCore() => nameof(NaviusFieldSegment);

    public void SetValue(double value)
    {
        if (IsReadOnly)
        {
            throw new ElementNotEnabledException();
        }

        Segment.RaiseValueRequested((int)Math.Round(value));
    }
}
