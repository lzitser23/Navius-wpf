using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for OneTimePasswordFieldPage.xaml
/// </summary>
public partial class OneTimePasswordFieldPage : UserControl
{
    public OneTimePasswordFieldPage()
    {
        InitializeComponent();

        DefaultOtp.ValueChanged += (_, e) =>
            DefaultOtpValue.Text = string.IsNullOrWhiteSpace(e.Value) ? "Value: (empty)" : $"Value: {e.Value}";

        AutoSubmitOtp.AutoSubmitted += (_, e) =>
            AutoSubmitStatus.Text = $"AutoSubmitted with \"{e.Value}\"";
    }
}
