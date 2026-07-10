using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Field;
using Navius.Wpf.Primitives.Controls.Form;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class FormTests
{
    static FormTests()
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
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Form.xaml"),
        });
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Field.xaml"),
        });

        return scope;
    }

    private static NaviusField MakeField(string name) =>
        new()
        {
            FieldName = name,
            Content = new StackPanel { Children = { new NaviusFieldControl() } },
        };

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var form = new NaviusForm { Resources = CreateThemedScope() };
        var submit = new NaviusFormSubmit { Resources = CreateThemedScope() };

        Assert.True(form.ApplyTemplate());
        Assert.True(submit.ApplyTemplate());
    }

    [StaFact]
    public void Submit_FiresWhenEveryFieldIsValid()
    {
        var field = MakeField("email");
        var form = new NaviusForm { Content = field };
        var submitted = 0;
        form.Submitted += (_, _) => submitted++;

        form.SubmitCommand.Execute(null);

        Assert.Equal(1, submitted);
    }

    [StaFact]
    public void Submit_DoesNotFire_WhenAFieldIsInvalid()
    {
        var validField = MakeField("a");
        var invalidField = MakeField("b");
        invalidField.Invalid = true;
        var form = new NaviusForm { Content = new StackPanel { Children = { validField, invalidField } } };
        var submitted = 0;
        form.Submitted += (_, _) => submitted++;

        form.SubmitCommand.Execute(null);

        Assert.Equal(0, submitted);
        Assert.True(invalidField.IsFieldInvalid);
    }

    [StaFact]
    public void Submit_RevealsOnSubmitFields_EvenWhenTheyWereNeverTouched()
    {
        var field = MakeField("a");
        field.Invalid = true;
        var form = new NaviusForm { Content = field };

        Assert.Null(field.IsFieldValid);

        form.SubmitCommand.Execute(null);

        Assert.True(field.IsFieldInvalid);
    }

    [StaFact]
    public void ErrorsByName_InjectsIntoMatchingField_AndClearsFromNonMatching()
    {
        var fieldA = MakeField("a");
        var fieldB = MakeField("b");
        var form = new NaviusForm { Content = new StackPanel { Children = { fieldA, fieldB } } };

        form.Errors = new Dictionary<string, string[]> { ["a"] = new[] { "Required" } };

        Assert.Equal(new[] { "Required" }, fieldA.ExternalErrors);
        Assert.Null(fieldB.ExternalErrors);
    }

    [StaFact]
    public void InjectedErrors_MakeTheFieldInvalid_AfterSubmitReveal()
    {
        var field = MakeField("a");
        var form = new NaviusForm { Content = field };
        form.Errors = new Dictionary<string, string[]> { ["a"] = new[] { "Taken" } };
        var submitted = 0;
        form.Submitted += (_, _) => submitted++;

        form.SubmitCommand.Execute(null);

        Assert.Equal(0, submitted);
        Assert.True(field.IsFieldInvalid);
        Assert.Contains("Taken", field.ExternalErrors!);
    }

    [StaFact]
    public void Reset_ClearsInjectedErrorsFromEveryField()
    {
        var field = MakeField("a");
        var form = new NaviusForm { Content = field };
        field.ExternalErrors = new[] { "Required" };

        form.Reset();

        Assert.Null(field.ExternalErrors);
    }

    [StaFact]
    public void ClearErrorsRequested_FiresOnSubmitAttempt_AndOnReset()
    {
        var form = new NaviusForm { Content = MakeField("a") };
        var cleared = 0;
        form.ClearErrorsRequested += (_, _) => cleared++;

        form.SubmitCommand.Execute(null);
        Assert.Equal(1, cleared);

        form.Reset();
        Assert.Equal(2, cleared);
    }
}
