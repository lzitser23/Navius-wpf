using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

/// <summary>
/// Accessible label part for a NaviusMeter. The web contract has the label register itself with a
/// cascaded context so the root can wire aria-labelledby automatically; WPF has no such push-based
/// registration, so the consumer wires
/// <c>AutomationProperties.LabeledBy="{Binding ElementName=...}"</c> on the NaviusMeter pointing at
/// this element instead, the same idiom NaviusProgressLabel uses.
/// </summary>
public class NaviusMeterLabel : TextBlock
{
}
