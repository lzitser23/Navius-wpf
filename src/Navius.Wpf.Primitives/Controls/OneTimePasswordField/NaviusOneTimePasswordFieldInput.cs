using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.OneTimePasswordField;

/// <summary>
/// A single-character cell, created and owned directly by NaviusOneTimePasswordField (not a
/// user-composed part: the root auto-renders Length of these, per the WPF strategy's "custom
/// Control that owns an internal collection of textbox-like parts"). Char is the logical
/// single character; Text is a derived display value so Type="password" masking can show a
/// bullet without corrupting the real character the root's buffer logic operates on. All
/// keyboard/paste handling lives on the root (OnCellPreviewKeyDown/OnCellPreviewTextInput/
/// OnCellPaste) since the root is the one with the buffer and its neighbors; this class stays
/// a thin, directly themable TextBox.
/// </summary>
public class NaviusOneTimePasswordFieldInput : TextBox
{
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(
        nameof(Index), typeof(int), typeof(NaviusOneTimePasswordFieldInput), new PropertyMetadata(0));

    public static readonly DependencyProperty CharProperty = DependencyProperty.Register(
        nameof(Char), typeof(char?), typeof(NaviusOneTimePasswordFieldInput), new PropertyMetadata(null, OnDisplayAffectingChanged));

    public static readonly DependencyProperty IsMaskedProperty = DependencyProperty.Register(
        nameof(IsMasked), typeof(bool), typeof(NaviusOneTimePasswordFieldInput), new PropertyMetadata(false, OnDisplayAffectingChanged));

    static NaviusOneTimePasswordFieldInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusOneTimePasswordFieldInput),
            new FrameworkPropertyMetadata(typeof(NaviusOneTimePasswordFieldInput)));
    }

    public NaviusOneTimePasswordFieldInput()
    {
        MaxLength = 1;
    }

    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public char? Char
    {
        get => (char?)GetValue(CharProperty);
        set => SetValue(CharProperty, value);
    }

    public bool IsMasked
    {
        get => (bool)GetValue(IsMaskedProperty);
        set => SetValue(IsMaskedProperty, value);
    }

    private static void OnDisplayAffectingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((NaviusOneTimePasswordFieldInput)d).UpdateDisplayText();

    private void UpdateDisplayText()
    {
        Text = Char is null ? string.Empty : (IsMasked ? "•" : Char.ToString());
    }
}
