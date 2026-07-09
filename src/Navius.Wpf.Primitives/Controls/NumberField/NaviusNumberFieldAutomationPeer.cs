using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Maps NaviusNumberField to role="spinbutton": AutomationControlType.Spinner plus
/// IRangeValueProvider (aria-valuenow/min/max, SmallChange=Step, LargeChange=LargeStep), since
/// there is no native WPF NumberBox automation peer to inherit from (see
/// docs/parity/number-field.md "WPF strategy").
/// </summary>
public class NaviusNumberFieldAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    public NaviusNumberFieldAutomationPeer(NaviusNumberField owner) : base(owner)
    {
    }

    private NaviusNumberField Field => (NaviusNumberField)Owner;

    public bool IsReadOnly => !Field.IsEnabled || Field.ReadOnly;

    public double Value => Field.Value ?? double.NaN;

    public double Minimum => Field.Minimum ?? double.NegativeInfinity;

    public double Maximum => Field.Maximum ?? double.PositiveInfinity;

    public double SmallChange => Field.Step;

    public double LargeChange => Field.LargeStep;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;

    protected override string GetClassNameCore() => nameof(NaviusNumberField);

    public void SetValue(double value)
    {
        if (IsReadOnly)
        {
            throw new ElementNotEnabledException();
        }

        Field.Value = value;
    }
}
