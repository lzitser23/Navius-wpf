using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Accessible label part for a <see cref="NaviusProgress"/>. The web contract has the label
/// register itself with a cascaded context so the root can wire aria-labelledby automatically;
/// WPF has no such push-based registration, so the consumer wires
/// <c>AutomationProperties.LabeledBy="{Binding ElementName=...}"</c> on the NaviusProgress pointing
/// at this element instead (the same idiom WPF's own Label uses).
/// </summary>
public class NaviusProgressLabel : TextBlock
{
}
