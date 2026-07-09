using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.Accordion;

/// <summary>
/// Tier B. The contract renders a real dynamic heading element (`&lt;h1&gt;`-`&lt;h6&gt;`,
/// default `&lt;h3&gt;`) so the document outline stays correct; WPF has no native "heading"
/// element/semantics comparable to HTML's outline, so this is a plain lookless wrapper
/// around the Trigger that instead publishes the level via
/// <see cref="AutomationProperties.HeadingLevelProperty"/> (UIA HeadingLevel, the closest
/// available accessibility signal) rather than changing its rendered element per Level.
/// </summary>
public class NaviusAccordionHeader : ContentControl
{
    public static readonly DependencyProperty LevelProperty = DependencyProperty.Register(
        nameof(Level),
        typeof(int),
        typeof(NaviusAccordionHeader),
        new PropertyMetadata(3, OnLevelChanged));

    static NaviusAccordionHeader()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusAccordionHeader),
            new FrameworkPropertyMetadata(typeof(NaviusAccordionHeader)));
    }

    public NaviusAccordionHeader()
    {
        ApplyHeadingLevel(Level);
    }

    /// <summary>Heading level 1-6, clamped; mirrors the contract's Level (h1-h6).</summary>
    public int Level
    {
        get => (int)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new NaviusAccordionHeaderAutomationPeer(this);

    private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusAccordionHeader)d).ApplyHeadingLevel((int)e.NewValue);

    private void ApplyHeadingLevel(int level)
    {
        var clamped = Math.Clamp(level, 1, 6);
        AutomationProperties.SetHeadingLevel(this, clamped switch
        {
            1 => AutomationHeadingLevel.Level1,
            2 => AutomationHeadingLevel.Level2,
            3 => AutomationHeadingLevel.Level3,
            4 => AutomationHeadingLevel.Level4,
            5 => AutomationHeadingLevel.Level5,
            _ => AutomationHeadingLevel.Level6,
        });
    }
}

internal sealed class NaviusAccordionHeaderAutomationPeer : FrameworkElementAutomationPeer
{
    public NaviusAccordionHeaderAutomationPeer(NaviusAccordionHeader owner) : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override string GetClassNameCore() => nameof(NaviusAccordionHeader);
}
