using System;
using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Theming;
using Navius.Wpf.Ui.SplitButton;
using Xunit;

namespace Navius.Wpf.Tests;

public class UiSplitButtonTests
{
    static UiSplitButtonTests()
    {
        // pack://application URIs only resolve once an Application exists in the process.
        // Guarded try/catch (rather than a bare null-check) because xunit runs test classes in
        // parallel on separate STA threads: another test class's static ctor can win the race.
        if (Application.Current is null)
        {
            try
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            catch (InvalidOperationException)
            {
                // Another test class's static ctor already created the process-wide Application.
            }
        }
    }

    private static ResourceDictionary CreateThemedScope()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Menu.xaml"),
        });
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Ui;component/Themes/SplitButton.xaml"),
        });

        return scope;
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_Succeeds()
    {
        var splitButton = new NaviusSplitButton { Resources = CreateThemedScope() };

        Assert.True(splitButton.ApplyTemplate());
    }

    [StaFact]
    public void ContentAlignment_ExplicitLeft_ForwardsThroughPrimaryButton()
    {
        // Regression: PART_Primary is a plain Button styled by an internal x:Key style
        // (Navius.SplitButton.PrimaryStyle). Its ContentPresenter hardcoded Center, and even after
        // fixing that, PART_Primary's own HorizontalContentAlignment was never wired to the outer
        // NaviusSplitButton's -- a two-hop forwarding gap. Both hops must work for this to pass.
        var content = new Border { Width = 20, Height = 10 };
        var splitButton = new NaviusSplitButton
        {
            Content = content,
            Width = 260,
            Height = 40,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Resources = CreateThemedScope(),
        };

        splitButton.ApplyTemplate();
        splitButton.Measure(new Size(260, 40));
        splitButton.Arrange(new Rect(0, 0, 260, 40));

        var primary = (Button)splitButton.Template.FindName("PART_Primary", splitButton)!;
        // The primary segment's own default Padding ("14,7") offsets the left edge.
        var offset = content.TranslatePoint(new Point(0, 0), primary);
        Assert.Equal(14, offset.X, 3);
    }

    [StaFact]
    public void ContentAlignment_Default_CentersContentInPrimaryButton()
    {
        var content = new Border { Width = 20, Height = 10 };
        var splitButton = new NaviusSplitButton
        {
            Content = content,
            Width = 260,
            Height = 40,
            Resources = CreateThemedScope(),
        };

        splitButton.ApplyTemplate();
        splitButton.Measure(new Size(260, 40));
        splitButton.Arrange(new Rect(0, 0, 260, 40));

        var primary = (Button)splitButton.Template.FindName("PART_Primary", splitButton)!;
        var offset = content.TranslatePoint(new Point(0, 0), primary);

        // Primary column is 260 minus the 1px divider and the chevron segment (padding 8,7 either
        // side of an 8px-wide glyph); content stays centered within that column by default.
        Assert.True(offset.X > 14, "content should not be pinned to the left padding edge by default");
    }
}
