using System.Collections.Generic;
using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>
/// Interaction logic for FormPage.xaml
/// </summary>
public partial class FormPage : UserControl
{
    public FormPage()
    {
        InitializeComponent();

        DemoForm.Submitted += (_, _) => FormStatus.Text = "Submitted: every field was valid.";

        InjectErrorButton.Click += (_, _) =>
        {
            DemoForm.Errors = new Dictionary<string, string[]>
            {
                ["username"] = new[] { "This username is already taken." },
            };
            UsernameField.Reveal();
            FormStatus.Text = "Server error injected into 'username'.";
        };

        FixButton.Click += (_, _) =>
        {
            PasswordField.Invalid = false;
            DemoForm.Errors = null;
            FormStatus.Text = "Fields marked valid; submit now succeeds.";
        };
    }
}
