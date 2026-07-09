using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.PasswordToggleField;

/// <summary>
/// Tier C-flavored caveat resolved: WPF's PasswordBox.Password is deliberately not a
/// bindable dependency property (security-by-design), and PasswordBox has no type="text"
/// mode to reveal plaintext in place the way the web contract's &lt;input&gt; just flips its
/// type attribute. This control overlays two real controls -- PART_PasswordBox (authoritative
/// while hidden) and PART_TextBox (authoritative while revealed) -- and copies the value
/// across exactly at the moment Visible flips, rather than mirroring it into a third shared
/// bindable string the whole time. On hide, the TextBox is cleared immediately after the
/// value is copied back into the PasswordBox, so plaintext never lingers in a loaded-but-
/// invisible TextBox.
///
/// The ancestor NaviusPasswordToggleField pushes ApplyVisibility(bool) directly (from
/// OnContentChanged and on every Visible change) rather than this control pulling its
/// ancestor's Visible on Loaded, since Loaded never fires for elements outside a live Window.
/// </summary>
public class NaviusPasswordToggleFieldInput : Control
{
    public static readonly RoutedEvent PasswordChangedEvent = EventManager.RegisterRoutedEvent(
        "PasswordChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusPasswordToggleFieldInput));

    private PasswordBox? _passwordBox;
    private TextBox? _textBox;
    private bool _visible;
    private bool _isSyncing;

    static NaviusPasswordToggleFieldInput()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusPasswordToggleFieldInput),
            new FrameworkPropertyMetadata(typeof(NaviusPasswordToggleFieldInput)));
    }

    /// <summary>Bubbles whenever the authoritative control's value changes, without exposing the plaintext.</summary>
    public event RoutedEventHandler PasswordChanged
    {
        add => AddHandler(PasswordChangedEvent, value);
        remove => RemoveHandler(PasswordChangedEvent, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_passwordBox is not null)
        {
            _passwordBox.PasswordChanged -= OnPasswordBoxChanged;
        }

        if (_textBox is not null)
        {
            _textBox.TextChanged -= OnTextBoxChanged;
        }

        _passwordBox = GetTemplateChild("PART_PasswordBox") as PasswordBox;
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;

        if (_passwordBox is not null)
        {
            _passwordBox.PasswordChanged += OnPasswordBoxChanged;
        }

        if (_textBox is not null)
        {
            _textBox.TextChanged += OnTextBoxChanged;
        }

        ApplyVisibility(_visible);
    }

    /// <summary>Called by the ancestor NaviusPasswordToggleField whenever Visible changes.</summary>
    internal void ApplyVisibility(bool visible)
    {
        _visible = visible;

        if (_passwordBox is null || _textBox is null || _isSyncing)
        {
            return;
        }

        _isSyncing = true;
        try
        {
            if (visible)
            {
                _textBox.Text = _passwordBox.Password;
                _textBox.Visibility = Visibility.Visible;
                _passwordBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                _passwordBox.Password = _textBox.Text;
                _textBox.Clear();
                _passwordBox.Visibility = Visibility.Visible;
                _textBox.Visibility = Visibility.Collapsed;
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>Opt-in plaintext read of whichever control currently holds the authoritative value.</summary>
    internal string GetPassword() => _visible ? _textBox?.Text ?? string.Empty : _passwordBox?.Password ?? string.Empty;

    private void OnPasswordBoxChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncing)
        {
            return;
        }

        RaiseEvent(new RoutedEventArgs(PasswordChangedEvent, this));
    }

    private void OnTextBoxChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSyncing)
        {
            return;
        }

        RaiseEvent(new RoutedEventArgs(PasswordChangedEvent, this));
    }
}
