using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Navius.Wpf.Primitives.Positioning;

/// <summary>
/// Resolves the work area of the monitor containing a given screen point, via the Win32
/// MonitorFromPoint/GetMonitorInfo APIs, so multi-monitor anchored placement (see
/// <see cref="Navius.Wpf.Primitives.Controls.NaviusAnchoredPopup"/>) does not approximate every
/// monitor's work area with the primary monitor's (<see cref="SystemParameters.WorkArea"/>).
///
/// Split into a P/Invoke-dependent lookup (<see cref="TryGetWorkAreaDeviceUnits"/>) and a pure,
/// unit-testable DPI-conversion-plus-fallback step (<see cref="ResolveWorkArea"/>), mirroring how
/// <see cref="PlacementMath"/> stays pure by taking its work area as a parameter.
/// </summary>
public static class MonitorWorkArea
{
    private const uint MonitorDefaultToNearest = 2;

    /// <summary>
    /// The monitor work area (in device pixels, not DIPs) containing <paramref name="screenPointDeviceUnits"/>,
    /// or null when the platform APIs could not resolve one (no monitor handle, or
    /// <c>GetMonitorInfo</c> failed) -- callers should fall back to
    /// <see cref="SystemParameters.WorkArea"/> via <see cref="ResolveWorkArea"/> in that case.
    /// </summary>
    public static Rect? TryGetWorkAreaDeviceUnits(Point screenPointDeviceUnits)
    {
        var point = new NativePoint
        {
            X = (int)screenPointDeviceUnits.X,
            Y = (int)screenPointDeviceUnits.Y,
        };

        var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return null;
        }

        var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info))
        {
            return null;
        }

        var wa = info.rcWorkArea;
        return new Rect(wa.Left, wa.Top, wa.Right - wa.Left, wa.Bottom - wa.Top);
    }

    /// <summary>
    /// Pure: converts a device-pixel monitor work area to DIPs using the anchor's own DPI scale,
    /// or returns <paramref name="fallbackWorkAreaDips"/> (the primary-monitor approximation)
    /// when <paramref name="monitorWorkAreaDeviceUnits"/> is null (the P/Invoke lookup failed).
    /// </summary>
    public static Rect ResolveWorkArea(Rect? monitorWorkAreaDeviceUnits, double dpiScaleX, double dpiScaleY, Rect fallbackWorkAreaDips)
    {
        if (monitorWorkAreaDeviceUnits is not { } device)
        {
            return fallbackWorkAreaDips;
        }

        return new Rect(
            device.Left / dpiScaleX,
            device.Top / dpiScaleY,
            device.Width / dpiScaleX,
            device.Height / dpiScaleY);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWorkArea;
        public uint dwFlags;
    }
}
