using System.Windows;
using System.Windows.Controls;
using Navius.Wpf.Primitives.Controls.Drawer;
using Navius.Wpf.Primitives.Theming;

namespace Navius.Wpf.Tests;

public class DrawerTests
{
    static DrawerTests()
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

    [StaFact]
    public void Defaults_AreClosedModalAndDockedBottom()
    {
        var drawer = new NaviusDrawer();

        Assert.False(drawer.IsOpen);
        Assert.True(drawer.Modal);
        Assert.True(drawer.CloseOnOutsideClick);
        Assert.Equal(NaviusDrawerSide.Bottom, drawer.Side);
        Assert.Equal(360, drawer.PanelWidth);
        Assert.Equal(280, drawer.PanelHeight);
        Assert.Equal(Visibility.Collapsed, drawer.Visibility);
    }

    [StaFact]
    public void PanelDimensions_DriveTheDefaultTemplate()
    {
        var scope = new ResourceDictionary();
        ThemeManager.Apply(NaviusTheme.Light, scope);
        scope.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Navius.Wpf.Primitives;component/Themes/Drawer.xaml"),
        });
        var drawer = new NaviusDrawer
        {
            Resources = scope,
            Side = NaviusDrawerSide.Right,
            PanelWidth = 324,
        };

        Assert.True(drawer.ApplyTemplate());
        var panel = Assert.IsType<Border>(drawer.Template.FindName("PART_Panel", drawer));
        Assert.Equal(324, panel.Width);
        Assert.True(double.IsNaN(panel.Height));
    }

    [StaFact]
    public void Side_RoundTrips()
    {
        var drawer = new NaviusDrawer { Side = NaviusDrawerSide.Left };

        Assert.Equal(NaviusDrawerSide.Left, drawer.Side);
    }

    [StaFact]
    public void IsOpen_WithoutAHostWindow_RevertsToFalse()
    {
        var drawer = new NaviusDrawer();

        drawer.IsOpen = true;

        Assert.False(drawer.IsOpen);
    }

    [StaFact]
    public void OnApplyTemplate_WithNoTemplateApplied_DoesNotThrow()
    {
        var drawer = new NaviusDrawer();

        var exception = Record.Exception(() => drawer.ApplyTemplate());

        Assert.Null(exception);
    }

    [StaFact]
    public void ModalEffective_TracksTheModalProperty()
    {
        var probe = new ProbeDrawer { Modal = false };

        Assert.False(probe.ModalEffectivePublic);
    }

    [StaFact]
    public void CloseOnOutsideClickEffective_TracksTheProperty()
    {
        var probe = new ProbeDrawer { CloseOnOutsideClick = false };

        Assert.False(probe.CloseOnOutsideClickEffectivePublic);
    }

    // --- DrawerGeometry: pure per-side offscreen-offset mapping, tested directly ---

    [Theory]
    [InlineData(NaviusDrawerSide.Left, -100, 0)]
    [InlineData(NaviusDrawerSide.Right, 100, 0)]
    [InlineData(NaviusDrawerSide.Top, 0, -50)]
    [InlineData(NaviusDrawerSide.Bottom, 0, 50)]
    public void GetOffscreenOffset_PointsPastTheDockedEdge(NaviusDrawerSide side, double expectedX, double expectedY)
    {
        var offset = DrawerGeometry.GetOffscreenOffset(side, new Size(100, 50));

        Assert.Equal(expectedX, offset.X);
        Assert.Equal(expectedY, offset.Y);
    }

    private sealed class ProbeDrawer : NaviusDrawer
    {
        public bool ModalEffectivePublic => ModalEffective;

        public bool CloseOnOutsideClickEffectivePublic => CloseOnOutsideClickEffective;
    }
}
