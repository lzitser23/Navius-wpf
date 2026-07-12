using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls;

public enum NaviusButtonVariant
{
    Default,
    Secondary,
    Outline,
    Ghost,
    Destructive,
}

public enum NaviusButtonSize
{
    Default,
    Small,
    Large,
    Icon,
}

/// <summary>
/// Tier A: derives from the native Button (inheriting its AutomationPeer and
/// keyboard behavior) and supplies a token-driven default template.
///
/// Disabled/FocusableWhenDisabled implement the contract's soft-disabled mode
/// (docs/parity/button.md "WPF strategy"): native WPF disabled controls
/// (IsEnabled="False") are unfocusable and skipped in tab order, so
/// "focusable + aria-disabled but inert" requires keeping IsEnabled true,
/// suppressing Click (OnClick is the single funnel for mouse, keyboard, and
/// UIA Invoke activation on ButtonBase), and reporting disabled via the
/// AutomationPeer instead of via native IsEnabled.
/// </summary>
public class NaviusButton : Button
{
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(NaviusButtonVariant), typeof(NaviusButton),
        new FrameworkPropertyMetadata(NaviusButtonVariant.Default));

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(NaviusButtonSize), typeof(NaviusButton),
        new FrameworkPropertyMetadata(NaviusButtonSize.Default));

    public static readonly DependencyProperty DisabledProperty = DependencyProperty.Register(
        nameof(Disabled), typeof(bool), typeof(NaviusButton),
        new PropertyMetadata(false, OnDisabledOrFocusableWhenDisabledChanged));

    public static readonly DependencyProperty FocusableWhenDisabledProperty = DependencyProperty.Register(
        nameof(FocusableWhenDisabled), typeof(bool), typeof(NaviusButton),
        new PropertyMetadata(false, OnDisabledOrFocusableWhenDisabledChanged));

    private static readonly DependencyPropertyKey IsSoftDisabledPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsSoftDisabled), typeof(bool), typeof(NaviusButton), new PropertyMetadata(false));

    public static readonly DependencyProperty IsSoftDisabledProperty = IsSoftDisabledPropertyKey.DependencyProperty;

    static NaviusButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusButton),
            new FrameworkPropertyMetadata(typeof(NaviusButton)));
    }

    /// <summary>Token-backed visual treatment.</summary>
    public NaviusButtonVariant Variant
    {
        get => (NaviusButtonVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>Token-backed control density.</summary>
    public NaviusButtonSize Size
    {
        get => (NaviusButtonSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Logical disabled state. Combined with FocusableWhenDisabled to choose native IsEnabled=False vs. soft-disabled.</summary>
    public bool Disabled
    {
        get => (bool)GetValue(DisabledProperty);
        set => SetValue(DisabledProperty, value);
    }

    /// <summary>When true and Disabled, the button stays natively enabled/focusable but suppresses activation (the aria-disabled-while-focusable mode).</summary>
    public bool FocusableWhenDisabled
    {
        get => (bool)GetValue(FocusableWhenDisabledProperty);
        set => SetValue(FocusableWhenDisabledProperty, value);
    }

    /// <summary>True when Disabled &amp;&amp; FocusableWhenDisabled: natively enabled/focusable but activation is suppressed and the AutomationPeer reports disabled.</summary>
    public bool IsSoftDisabled => (bool)GetValue(IsSoftDisabledProperty);

    protected override AutomationPeer OnCreateAutomationPeer() => new NaviusButtonAutomationPeer(this);

    protected override void OnClick()
    {
        if (IsSoftDisabled)
        {
            return;
        }

        base.OnClick();
    }

    private static void OnDisabledOrFocusableWhenDisabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusButton)d).UpdateDisabledState();

    private void UpdateDisabledState()
    {
        var soft = Disabled && FocusableWhenDisabled;
        SetValue(IsSoftDisabledPropertyKey, soft);

        // Hard-disabled (Disabled && !FocusableWhenDisabled) uses native IsEnabled=False, the WPF
        // analog of the native `disabled` attribute. Every other combination (including plain
        // Disabled=false) stays natively enabled -- soft-disabled relies on OnClick suppression
        // and the AutomationPeer instead of IsEnabled to stay focusable/tabbable.
        IsEnabled = !(Disabled && !FocusableWhenDisabled);
    }
}
