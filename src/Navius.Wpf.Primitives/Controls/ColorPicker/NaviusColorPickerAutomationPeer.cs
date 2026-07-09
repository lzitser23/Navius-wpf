using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Surfaces the current color as a read-only ValuePattern (the hex string), the same
/// template-agnostic pattern used elsewhere in this port for template-only text that would
/// otherwise expose nothing over UIA (see the M3-gate precedent referenced in the task brief).
/// SetValue always throws: the value can only change via the Area/Hue/Alpha tracks, the hex
/// field, or a swatch selection, never by an assistive-technology client writing the value
/// directly.
/// </summary>
public class NaviusColorPickerAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
    public NaviusColorPickerAutomationPeer(NaviusColorPicker owner) : base(owner)
    {
    }

    private NaviusColorPicker Picker => (NaviusColorPicker)Owner;

    public bool IsReadOnly => true;

    public string Value => Picker.HexValue;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusColorPicker);

    public void SetValue(string value) =>
        throw new InvalidOperationException("NaviusColorPicker's UIA Value is read-only; use the Area/Hue/Alpha tracks, the hex field, or a swatch.");
}
