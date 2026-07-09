using System.Windows;
using Navius.Wpf.Primitives.Positioning;

namespace Navius.Wpf.Tests;

public class MonitorWorkAreaTests
{
    private static readonly Rect Fallback = new(0, 0, 1000, 800);

    // ---- ResolveWorkArea: pure DPI-conversion + fallback (the testable seam) ----------------

    [Fact]
    public void ResolveWorkArea_NullMonitorWorkArea_FallsBackGracefully()
    {
        // The P/Invoke lookup failed (no monitor handle, or GetMonitorInfo returned false);
        // callers must land on SystemParameters.WorkArea rather than crash or place off-screen.
        var result = MonitorWorkArea.ResolveWorkArea(null, dpiScaleX: 1.5, dpiScaleY: 1.5, Fallback);

        Assert.Equal(Fallback, result);
    }

    [Fact]
    public void ResolveWorkArea_ResolvedMonitorWorkArea_ConvertsDeviceUnitsToDips_AtStandardDpi()
    {
        var deviceWorkArea = new Rect(0, 0, 1920, 1040);

        var result = MonitorWorkArea.ResolveWorkArea(deviceWorkArea, dpiScaleX: 1.0, dpiScaleY: 1.0, Fallback);

        Assert.Equal(deviceWorkArea, result);
    }

    [Fact]
    public void ResolveWorkArea_ResolvedMonitorWorkArea_ConvertsDeviceUnitsToDips_AtHigherDpi()
    {
        // 150% scaling (144 DPI): 1.5x device pixels per DIP on both axes.
        var deviceWorkArea = new Rect(1920, 0, 1920, 1040);
        const double scale = 1.5;

        var result = MonitorWorkArea.ResolveWorkArea(deviceWorkArea, dpiScaleX: scale, dpiScaleY: scale, Fallback);

        Assert.Equal(new Rect(1920 / scale, 0, 1920 / scale, 1040 / scale), result);
    }

    [Fact]
    public void ResolveWorkArea_DifferentXAndYScale_ConvertsEachAxisIndependently()
    {
        var deviceWorkArea = new Rect(0, 0, 200, 100);

        var result = MonitorWorkArea.ResolveWorkArea(deviceWorkArea, dpiScaleX: 2.0, dpiScaleY: 1.0, Fallback);

        Assert.Equal(new Rect(0, 0, 100, 100), result);
    }

    // ---- TryGetWorkAreaDeviceUnits: real Win32 lookup (runs on the Windows test agent) -------

    [Fact]
    public void TryGetWorkAreaDeviceUnits_OriginPoint_ResolvesANonEmptyWorkArea()
    {
        // (0,0) always lands on some monitor in a real Windows session (MONITOR_DEFAULTTONEAREST
        // guarantees a handle even for points outside every monitor).
        var result = MonitorWorkArea.TryGetWorkAreaDeviceUnits(new Point(0, 0));

        Assert.True(result.HasValue);
        Assert.True(result!.Value.Width > 0);
        Assert.True(result.Value.Height > 0);
    }
}
