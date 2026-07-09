using System.Windows.Controls;

namespace Navius.Wpf.Gallery.Pages;

/// <summary>Demonstrates NaviusTagInput: default chips, transform/validate/max rules, blur commit, disabled.</summary>
public partial class TagInputPage : UserControl
{
    public TagInputPage()
    {
        InitializeComponent();

        // Delegate-typed parameters (Transform/Validate) and seed tags are code-behind concerns.
        RuledTagInput.Transform = s => s.ToLowerInvariant();
        RuledTagInput.Validate = s => s.Length >= 2;
        DefaultTagInput.DefaultValue = new[] { "navius", "wpf" };
        DisabledTagInput.DefaultValue = new[] { "read", "only" };
    }
}
