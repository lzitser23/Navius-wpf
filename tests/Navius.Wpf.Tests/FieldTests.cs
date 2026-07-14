using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Navius.Wpf.Primitives.Controls.Field;
using Navius.Wpf.Primitives.Controls.Select;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class FieldTests
{
    static FieldTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        if (Application.Current is null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Field.xaml"),
        });

        return scope;
    }

    private static (NaviusField Field, NaviusFieldLabel Label, NaviusFieldControl Control, NaviusFieldError Error) CreateField(
        FieldValidationMode mode = FieldValidationMode.OnSubmit)
    {
        var label = new NaviusFieldLabel();
        var fieldControl = new NaviusFieldControl();
        var error = new NaviusFieldError();
        var field = new NaviusField
        {
            ValidationMode = mode,
            Resources = CreateThemedScope(),
            Content = new StackPanel { Children = { label, fieldControl, error } },
        };

        return (field, label, fieldControl, error);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var (field, _, _, _) = CreateField();
        Assert.True(field.ApplyTemplate());
    }

    [StaFact]
    public void InputAndSelect_HaveMatchingCollapsedHeight()
    {
        var resources = CreateThemedScope();
        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Select.xaml"),
        });
        var input = new NaviusInput { Resources = resources, Text = "Name" };
        var select = new NaviusSelect<string> { Resources = resources, Placeholder = "Variant" };
        select.Style = (Style)resources[typeof(NaviusSelectBase)];
        Assert.True(select.ApplyTemplate());

        input.Measure(new Size(300, 100));
        select.Measure(new Size(300, 100));
        select.Arrange(new Rect(select.DesiredSize));
        var trigger = Assert.IsType<ToggleButton>(select.Template.FindName("PART_Trigger", select));

        Assert.Equal(input.DesiredSize.Height, select.DesiredSize.Height);
        Assert.Equal(14, input.FontSize);
        Assert.Equal(14, select.FontSize);
        Assert.Equal(select.ActualHeight, trigger.ActualHeight);
    }

    [StaFact]
    public void FieldControl_DefaultsToNaviusInput_WhenContentNotSupplied()
    {
        var (_, _, fieldControl, _) = CreateField();

        Assert.IsType<NaviusInput>(fieldControl.Content);
    }

    [StaFact]
    public void FieldControl_RendersItsContent()
    {
        var (field, _, fieldControl, _) = CreateField();
        var input = Assert.IsType<NaviusInput>(fieldControl.Content);

        field.Measure(new Size(500, 500));
        field.Arrange(new Rect(0, 0, 500, 500));
        field.UpdateLayout();

        Assert.NotNull(System.Windows.Media.VisualTreeHelper.GetParent(input));
    }

    private sealed class FixedTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate? SelectTemplate(object? item, DependencyObject container) => null;
    }

    [StaFact]
    public void FieldControl_ForwardsTheFullContentContract_ToItsPresenter()
    {
        var (field, _, fieldControl, _) = CreateField();
        var selector = new FixedTemplateSelector();
        fieldControl.Content = 42;
        fieldControl.ContentTemplateSelector = selector;
        fieldControl.ContentStringFormat = "#{0}";

        field.Measure(new Size(500, 500));
        field.Arrange(new Rect(0, 0, 500, 500));
        field.UpdateLayout();

        var presenter = Assert.IsType<ContentPresenter>(
            System.Windows.Media.VisualTreeHelper.GetChild(fieldControl, 0));
        Assert.Same(selector, presenter.ContentTemplateSelector);
        Assert.Equal("#{0}", presenter.ContentStringFormat);
    }

    [StaFact]
    public void Field_RegistersTheDefaultInput_AsItsControl()
    {
        var (field, _, fieldControl, _) = CreateField();

        Assert.Same(fieldControl.Content, field.RegisteredControl);
    }

    [StaFact]
    public void FieldLabel_TargetIsWiredToTheRegisteredControl()
    {
        var (field, label, _, _) = CreateField();

        Assert.Same(field.RegisteredControl, label.Target);
    }

    [StaFact]
    public void Disabled_SetsIsEnabledFalse()
    {
        var (field, _, _, _) = CreateField();

        field.Disabled = true;

        Assert.False(field.IsEnabled);
    }

    [StaFact]
    public void Dirty_Touched_Filled_Focused_UpdateFromBubblingControlEvents()
    {
        var (field, _, fieldControl, _) = CreateField();
        var input = (NaviusInput)fieldControl.Content!;

        Assert.False(field.IsDirty);
        Assert.False(field.IsFieldFocused);

        input.RaiseEvent(new RoutedEventArgs(UIElement.GotFocusEvent, input));
        Assert.True(field.IsFieldFocused);

        input.Text = "abc";
        input.RaiseEvent(new TextChangedEventArgs(TextBoxBase.TextChangedEvent, UndoAction.None));
        Assert.True(field.IsDirty);
        Assert.True(field.IsFilled);

        input.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, input));
        Assert.False(field.IsFieldFocused);
        Assert.True(field.IsTouched);
    }

    [StaFact]
    public void ValidationMode_OnSubmit_StaysNullUntilRevealCalled()
    {
        var (field, _, _, _) = CreateField(FieldValidationMode.OnSubmit);
        field.Invalid = true;

        Assert.Null(field.IsFieldValid);
        Assert.False(field.IsFieldInvalid);

        field.Reveal();

        Assert.False(field.IsFieldValid);
        Assert.True(field.IsFieldInvalid);
    }

    [StaFact]
    public void ValidationMode_OnChange_RevealsOnFirstTextChanged()
    {
        var (field, _, fieldControl, _) = CreateField(FieldValidationMode.OnChange);
        field.Invalid = true;
        var input = (NaviusInput)fieldControl.Content!;

        Assert.Null(field.IsFieldValid);

        input.RaiseEvent(new TextChangedEventArgs(TextBoxBase.TextChangedEvent, UndoAction.None));

        Assert.True(field.IsFieldInvalid);
    }

    [StaFact]
    public void ValidationMode_OnBlur_RevealsOnLostFocus()
    {
        var (field, _, fieldControl, _) = CreateField(FieldValidationMode.OnBlur);
        field.Invalid = true;
        var input = (NaviusInput)fieldControl.Content!;

        Assert.Null(field.IsFieldValid);

        input.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, input));

        Assert.True(field.IsFieldInvalid);
    }

    [StaFact]
    public void ServerInvalid_AutoClearsOnNextEdit()
    {
        var (field, _, fieldControl, _) = CreateField();
        field.ServerInvalid = true;
        field.Reveal();
        Assert.True(field.IsFieldInvalid);

        var input = (NaviusInput)fieldControl.Content!;
        input.RaiseEvent(new TextChangedEventArgs(TextBoxBase.TextChangedEvent, UndoAction.None));

        Assert.False(field.ServerInvalid);
        Assert.False(field.IsFieldInvalid);
    }

    [StaFact]
    public void FieldError_HiddenUntilRevealedInvalid()
    {
        var (field, _, _, error) = CreateField();
        field.Invalid = true;

        Assert.Equal(Visibility.Collapsed, error.Visibility);

        field.Reveal();

        Assert.Equal(Visibility.Visible, error.Visibility);
    }

    [StaFact]
    public void FieldError_ForceMatch_AlwaysShows()
    {
        var (_, _, _, error) = CreateField();

        error.ForceMatch = true;

        Assert.Equal(Visibility.Visible, error.Visibility);
    }

    [StaFact]
    public void FieldError_Match_OnlyShowsWhenThatMessageIsPresent()
    {
        var (field, _, _, error) = CreateField();
        error.Match = "required";
        field.ExternalErrors = new[] { "something else" };
        field.Reveal();

        Assert.Equal(Visibility.Collapsed, error.Visibility);

        field.ExternalErrors = new[] { "required" };

        Assert.Equal(Visibility.Visible, error.Visibility);
    }

    [StaFact]
    public void FieldDescription_IsAssociatedWithControl_ViaAutomationHelpText()
    {
        var label = new NaviusFieldLabel();
        var fieldControl = new NaviusFieldControl();
        var description = new NaviusFieldDescription { Content = "We never share your email." };
        var field = new NaviusField
        {
            Resources = CreateThemedScope(),
            Content = new StackPanel { Children = { label, fieldControl, description } },
        };

        Assert.Equal(
            "We never share your email.",
            System.Windows.Automation.AutomationProperties.GetHelpText(field.RegisteredControl!));
    }

    [StaFact]
    public void FieldsetDisabled_CascadesIntoField_ViaIsEnabledInheritance()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Fieldset.xaml"),
        });

        var (field, _, _, _) = CreateField();
        var fieldset = new Navius.Wpf.Primitives.Controls.Fieldset.NaviusFieldset
        {
            Resources = scope,
            Content = field,
        };

        // IsEnabled propagates down the VISUAL tree, so realize it first (template + layout),
        // exactly as a windowed app would have by the time a user interacts.
        Assert.True(fieldset.ApplyTemplate());
        fieldset.Measure(new Size(500, 500));
        fieldset.Arrange(new Rect(0, 0, 500, 500));
        fieldset.UpdateLayout();

        fieldset.Disabled = true;

        Assert.False(fieldset.IsEnabled);
        Assert.False(field.IsEnabled);
    }
}
