using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.PasswordToggleField;

/// <summary>
/// Tier B with a Tier C-flavored caveat on the Input part (see
/// NaviusPasswordToggleFieldInput). Root owns the revealed/hidden Visible state and pushes it
/// into descendant parts (Input/Toggle/Icon/Slot) from OnContentChanged and on every Visible
/// change, since WPF has no cascading-parameter mechanism and FrameworkElement.Loaded never
/// fires for elements outside a live Window (true of every headless unit test in this suite),
/// so a Loaded-based pull from the parts would silently never run there.
///
/// PasswordBox strategy: the password stays INSIDE the real PasswordBox/TextBox controls
/// (see Input) at all times; this root deliberately does not mirror it into a bindable
/// string DP by default, preserving WPF's PasswordBox security-by-design intent. GetPassword()
/// is the opt-in plaintext read the task calls for, and PasswordChanged bubbles from the
/// registered Input so a consumer can react without needing the plaintext.
/// </summary>
public class NaviusPasswordToggleField : ContentControl
{
    public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register(
        nameof(Visible),
        typeof(bool),
        typeof(NaviusPasswordToggleField),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVisibleChanged));

    public static readonly RoutedEvent VisibleChangedEvent = EventManager.RegisterRoutedEvent(
        "VisibleChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NaviusPasswordToggleField));

    static NaviusPasswordToggleField()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NaviusPasswordToggleField),
            new FrameworkPropertyMetadata(typeof(NaviusPasswordToggleField)));
    }

    public event RoutedEventHandler VisibleChanged
    {
        add => AddHandler(VisibleChangedEvent, value);
        remove => RemoveHandler(VisibleChangedEvent, value);
    }

    public bool Visible
    {
        get => (bool)GetValue(VisibleProperty);
        set => SetValue(VisibleProperty, value);
    }

    /// <summary>The descendant Input part registered from OnContentChanged, if any.</summary>
    internal NaviusPasswordToggleFieldInput? RegisteredInput { get; private set; }

    internal void RegisterInput(NaviusPasswordToggleFieldInput input) => RegisteredInput = input;

    /// <summary>
    /// Opt-in plaintext read. The password is never mirrored to a bindable DP; call this only
    /// when the consumer genuinely needs the value (e.g. to submit it), same trust boundary as
    /// calling PasswordBox.Password directly.
    /// </summary>
    public string GetPassword() => RegisteredInput?.GetPassword() ?? string.Empty;

    public void ToggleVisible() => Visible = !Visible;

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        var input = LogicalTreeWalker.Descendants<NaviusPasswordToggleFieldInput>(this).FirstOrDefault();
        if (input is not null)
        {
            RegisterInput(input);
        }

        PushVisibleToDescendants();
    }

    private static void OnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var field = (NaviusPasswordToggleField)d;
        field.RaiseEvent(new RoutedEventArgs(VisibleChangedEvent, field));
        field.PushVisibleToDescendants();
    }

    private void PushVisibleToDescendants()
    {
        RegisteredInput?.ApplyVisibility(Visible);

        foreach (var toggle in LogicalTreeWalker.Descendants<NaviusPasswordToggleFieldToggle>(this))
        {
            toggle.UpdateAccessibleName(Visible);
        }

        foreach (var slot in LogicalTreeWalker.Descendants<PasswordToggleFieldSlotBase>(this))
        {
            slot.SetRevealed(Visible);
        }
    }
}
