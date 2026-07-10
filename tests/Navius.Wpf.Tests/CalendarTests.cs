using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Calendar;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class CalendarTests
{
    static CalendarTests()
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
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Calendar.xaml"),
        });

        return scope;
    }

    [StaFact]
    public void DerivesFromNativeCalendar_WithSingleDateDefault()
    {
        var calendar = new NaviusCalendar();

        // Tier A: the native Calendar's keyboard model and CalendarAutomationPeer come along.
        Assert.IsAssignableFrom<System.Windows.Controls.Calendar>(calendar);
        Assert.Equal(CalendarSelectionMode.SingleDate, calendar.SelectionMode);
    }

    [StaFact]
    public void ApplyTemplate_WithThemeLoaded_WiresPartStyles()
    {
        var scope = CreateThemedScope();
        var calendar = new NaviusCalendar
        {
            Resources = scope,
            // Element not in a tree: assign the shared style explicitly, same lookup the
            // implicit alias in Themes/Calendar.xaml performs in a page.
            Style = (Style)scope["Navius.Calendar.Style"],
        };

        Assert.True(calendar.ApplyTemplate());

        // The token restyle rides the native CalendarDayButtonStyle/CalendarButtonStyle/
        // CalendarItemStyle properties so Calendar's own population code stamps every cell.
        Assert.NotNull(calendar.CalendarDayButtonStyle);
        Assert.NotNull(calendar.CalendarButtonStyle);
        Assert.NotNull(calendar.CalendarItemStyle);
    }

    [StaFact]
    public void ImplicitStyleAlias_ResolvesToSharedStyle()
    {
        var scope = CreateThemedScope();

        var implicitStyle = Assert.IsType<Style>(scope[typeof(NaviusCalendar)]);
        Assert.Same(scope["Navius.Calendar.Style"], implicitStyle.BasedOn);
    }

    [StaFact]
    public void AutomationPeer_IsTheNativeCalendarPeer()
    {
        var calendar = new NaviusCalendar();

        var peer = typeof(NaviusCalendar)
            .GetMethod("OnCreateAutomationPeer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(calendar, null) as AutomationPeer;

        Assert.NotNull(peer);
        Assert.Equal(AutomationControlType.Calendar, peer!.GetAutomationControlType());
    }
}
