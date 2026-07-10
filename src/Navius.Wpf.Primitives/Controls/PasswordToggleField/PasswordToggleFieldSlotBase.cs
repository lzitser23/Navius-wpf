using System.Windows;
using System.Windows.Controls;

namespace Navius.Wpf.Primitives.Controls.PasswordToggleField;

/// <summary>
/// Shared revealed-state content swap for Icon and Slot, both of which render no wrapper
/// element in the web contract and just swap RenderFragment content by revealed state --
/// exactly what a ContentControl swapping its Content already does natively in WPF, without
/// needing a separate DataTemplateSelector. The ancestor NaviusPasswordToggleField pushes the
/// current revealed state via SetRevealed (from OnContentChanged and on every Visible
/// change) rather than this control pulling its ancestor on Loaded, since Loaded never fires
/// for elements outside a live Window.
/// </summary>
public abstract class PasswordToggleFieldSlotBase : ContentControl
{
    public static readonly DependencyProperty VisibleContentProperty = DependencyProperty.Register(
        nameof(VisibleContent), typeof(object), typeof(PasswordToggleFieldSlotBase), new PropertyMetadata(null, OnContentSourceChanged));

    public static readonly DependencyProperty HiddenContentProperty = DependencyProperty.Register(
        nameof(HiddenContent), typeof(object), typeof(PasswordToggleFieldSlotBase), new PropertyMetadata(null, OnContentSourceChanged));

    /// <summary>Content shown while the password is revealed.</summary>
    public object? VisibleContent
    {
        get => GetValue(VisibleContentProperty);
        set => SetValue(VisibleContentProperty, value);
    }

    /// <summary>Content shown while the password is hidden.</summary>
    public object? HiddenContent
    {
        get => GetValue(HiddenContentProperty);
        set => SetValue(HiddenContentProperty, value);
    }

    protected bool IsRevealed { get; private set; }

    /// <summary>Called by the ancestor NaviusPasswordToggleField whenever Visible changes.</summary>
    internal void SetRevealed(bool revealed)
    {
        IsRevealed = revealed;
        ApplyContent();
    }

    protected virtual void ApplyContent() => Content = IsRevealed ? VisibleContent : HiddenContent;

    private static void OnContentSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((PasswordToggleFieldSlotBase)d).ApplyContent();
}
