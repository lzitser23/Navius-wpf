using System.Windows;

namespace Navius.Wpf.Primitives.Controls.OneTimePasswordField;

/// <summary>Carries the aggregate Value for OneTimePasswordField's ValueChanged/Complete/AutoSubmitted events.</summary>
public sealed class OtpRoutedEventArgs : RoutedEventArgs
{
    public OtpRoutedEventArgs(RoutedEvent routedEvent, object source, string value) : base(routedEvent, source)
    {
        Value = value;
    }

    public string Value { get; }
}
