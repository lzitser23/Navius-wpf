using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls.PasswordToggleField;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class PasswordToggleFieldTests
{
    static PasswordToggleFieldTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static readonly MethodInfo OnClickMethod =
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>Invokes the protected, most-derived OnClick(), just like a real click.</summary>
    private static void SimulateClick(ButtonBase button) => OnClickMethod.Invoke(button, null);

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/PasswordToggleField.xaml"),
        });

        return scope;
    }

    private static (NaviusPasswordToggleField Field, NaviusPasswordToggleFieldInput Input, NaviusPasswordToggleFieldToggle Toggle, PasswordBox PasswordBox, TextBox TextBox) CreateField()
    {
        var scope = CreateThemedScope();
        var input = new NaviusPasswordToggleFieldInput { Resources = scope };
        var toggle = new NaviusPasswordToggleFieldToggle { Resources = scope };
        var field = new NaviusPasswordToggleField
        {
            Resources = scope,
            Content = new StackPanel { Children = { input, toggle } },
        };

        Assert.True(input.ApplyTemplate());

        var passwordBox = (PasswordBox)input.Template.FindName("PART_PasswordBox", input)!;
        var textBox = (TextBox)input.Template.FindName("PART_TextBox", input)!;

        return (field, input, toggle, passwordBox, textBox);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var scope = CreateThemedScope();
        var field = new NaviusPasswordToggleField { Resources = scope };
        var input = new NaviusPasswordToggleFieldInput { Resources = scope };
        var toggle = new NaviusPasswordToggleFieldToggle { Resources = scope };
        var icon = new NaviusPasswordToggleFieldIcon { Resources = scope };
        var slot = new NaviusPasswordToggleFieldSlot { Resources = scope };

        Assert.True(field.ApplyTemplate());
        Assert.True(input.ApplyTemplate());
        Assert.True(toggle.ApplyTemplate());
        Assert.True(icon.ApplyTemplate());
        Assert.True(slot.ApplyTemplate());
    }

    [StaFact]
    public void Toggle_ContentAlignment_ExplicitLeft_ForwardsToContentPresenter()
    {
        // Regression: the ContentPresenter hardcoded Center and ignored HorizontalContentAlignment.
        var content = new Border { Width = 10, Height = 10 };
        var toggle = new NaviusPasswordToggleFieldToggle
        {
            Content = content,
            Width = 100,
            Height = 40,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Resources = CreateThemedScope(),
        };

        toggle.ApplyTemplate();
        toggle.Measure(new Size(100, 40));
        toggle.Arrange(new Rect(0, 0, 100, 40));

        // The template's fixed Margin="6" on the ContentPresenter offsets the left edge.
        var offset = content.TranslatePoint(new Point(0, 0), toggle);
        Assert.Equal(6, offset.X, 3);
    }

    [StaFact]
    public void HiddenByDefault_PasswordBoxIsTheVisibleControl()
    {
        var (_, _, _, passwordBox, textBox) = CreateField();

        Assert.Equal(Visibility.Visible, passwordBox.Visibility);
        Assert.Equal(Visibility.Collapsed, textBox.Visibility);
    }

    [StaFact]
    public void Reveal_SwapsToTextBox_AndPreservesTheValue()
    {
        var (field, _, _, passwordBox, textBox) = CreateField();
        passwordBox.Password = "hunter2";

        field.Visible = true;

        Assert.Equal(Visibility.Collapsed, passwordBox.Visibility);
        Assert.Equal(Visibility.Visible, textBox.Visibility);
        Assert.Equal("hunter2", textBox.Text);
    }

    [StaFact]
    public void Hide_CopiesValueBackToPasswordBox_AndClearsThePlaintextTextBox()
    {
        var (field, _, _, passwordBox, textBox) = CreateField();
        passwordBox.Password = "hunter2";
        field.Visible = true;
        textBox.Text = "hunter2-edited";

        field.Visible = false;

        Assert.Equal("hunter2-edited", passwordBox.Password);
        Assert.Equal(string.Empty, textBox.Text);
        Assert.Equal(Visibility.Visible, passwordBox.Visibility);
        Assert.Equal(Visibility.Collapsed, textBox.Visibility);
    }

    [StaFact]
    public void GetPassword_ReadsTheAuthoritativeControl_InBothStates()
    {
        var (field, _, _, passwordBox, textBox) = CreateField();
        passwordBox.Password = "abc";

        Assert.Equal("abc", field.GetPassword());

        field.Visible = true;
        textBox.Text = "abcdef";

        Assert.Equal("abcdef", field.GetPassword());
    }

    [StaFact]
    public void ToggleClick_FlipsVisible_AndRaisesVisibleChanged()
    {
        var (field, _, toggle, _, _) = CreateField();
        var raised = 0;
        field.VisibleChanged += (_, _) => raised++;

        SimulateClick(toggle);

        Assert.True(field.Visible);
        Assert.Equal(1, raised);

        SimulateClick(toggle);

        Assert.False(field.Visible);
        Assert.Equal(2, raised);
    }

    [StaFact]
    public void ToggleAccessibleName_FlipsBetweenShowAndHidePassword()
    {
        var (field, _, toggle, _, _) = CreateField();

        Assert.Equal("Show password", AutomationProperties.GetName(toggle));

        field.Visible = true;

        Assert.Equal("Hide password", AutomationProperties.GetName(toggle));
    }

    [StaFact]
    public void PasswordChanged_BubblesWithoutExposingPlaintext()
    {
        var (field, input, _, passwordBox, _) = CreateField();
        var raised = 0;
        input.PasswordChanged += (_, _) => raised++;

        passwordBox.Password = "x";

        Assert.Equal(1, raised);
        Assert.Equal("x", field.GetPassword());
    }

    // --- security: masked plaintext must never reach the UI Automation surface ------------------

    [StaFact]
    public void HiddenState_DoesNotLeakPlaintextThroughTheAutomationSurface()
    {
        // The single highest-severity check for this family: while hidden (the default, masked
        // state) an assistive-technology / UIA client must not be able to read the actual
        // password characters. The value lives only inside the native PasswordBox (never a
        // bindable/gettable DP on the Navius controls), and the plaintext TextBox is cleared and
        // collapsed while hidden.
        const string secret = "hunter2-super-secret";
        var (_, _, _, passwordBox, textBox) = CreateField();
        passwordBox.Password = secret;

        // The plaintext overlay is empty and out of the tree while hidden.
        Assert.Equal(string.Empty, textBox.Text);
        Assert.Equal(Visibility.Collapsed, textBox.Visibility);

        // The authoritative PasswordBox marks itself IsPassword (UIA masks it) and does not
        // surface the plaintext through the Value pattern.
        var peer = (PasswordBoxAutomationPeer)UIElementAutomationPeer.CreatePeerForElement(passwordBox);
        Assert.True(peer.IsPassword());
        if (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
        {
            Assert.NotEqual(secret, valueProvider.Value);
        }

        // The plaintext TextBox peer (if reachable) also does not carry the secret while hidden.
        var textPeer = (TextBoxAutomationPeer)UIElementAutomationPeer.CreatePeerForElement(textBox);
        if (textPeer.GetPattern(PatternInterface.Value) is IValueProvider textValueProvider)
        {
            Assert.NotEqual(secret, textValueProvider.Value);
        }
    }

    [StaFact]
    public void RevealThenHide_LeavesNoPlaintextInTheAutomationSurface()
    {
        const string secret = "corner-case-secret";
        var (field, _, _, passwordBox, textBox) = CreateField();
        passwordBox.Password = secret;

        field.Visible = true;   // reveal: plaintext is intentionally in the TextBox now
        Assert.Equal(secret, textBox.Text);

        field.Visible = false;  // hide again

        // After hiding, no plaintext lingers in the collapsed TextBox or its automation surface.
        Assert.Equal(string.Empty, textBox.Text);
        Assert.Equal(Visibility.Collapsed, textBox.Visibility);
        var textPeer = (TextBoxAutomationPeer)UIElementAutomationPeer.CreatePeerForElement(textBox);
        if (textPeer.GetPattern(PatternInterface.Value) is IValueProvider textValueProvider)
        {
            Assert.NotEqual(secret, textValueProvider.Value);
        }

        // And the value survived the round trip inside the secure PasswordBox.
        Assert.Equal(secret, passwordBox.Password);
    }

    [StaFact]
    public void Icon_SwapsContentByRevealedState()
    {
        var scope = CreateThemedScope();
        var icon = new NaviusPasswordToggleFieldIcon
        {
            Resources = scope,
            VisibleContent = "eye-off",
            HiddenContent = "eye",
        };
        var field = new NaviusPasswordToggleField { Resources = scope, Content = icon };

        Assert.Equal("eye", icon.Content);

        field.Visible = true;

        Assert.Equal("eye-off", icon.Content);
    }

    [StaFact]
    public void Slot_ContentFactory_TakesPrecedenceAndReceivesRevealedState()
    {
        var scope = CreateThemedScope();
        var slot = new NaviusPasswordToggleFieldSlot
        {
            Resources = scope,
            VisibleContent = "ignored",
            HiddenContent = "ignored",
            ContentFactory = revealed => revealed ? "custom-visible" : "custom-hidden",
        };
        var field = new NaviusPasswordToggleField { Resources = scope, Content = slot };

        Assert.Equal("custom-hidden", slot.Content);

        field.Visible = true;

        Assert.Equal("custom-visible", slot.Content);
    }
}
