using System.Runtime.InteropServices;

namespace Navius.Wpf.Tests;

/// <summary>
/// Shared helper for tests that drive WPF's NATIVE input paths (ButtonBase Space handling,
/// ListBox arrow navigation, NumberField modifier-scaled stepping). Those paths read live
/// Win32 per-thread keyboard state via GetKeyState: Keyboard.Modifiers and
/// Mouse.PrimaryDevice.LeftButton both resolve to the calling thread's 256-entry virtual-key
/// table. That table only re-syncs when the thread retrieves real input messages, so on a dev
/// machine a test window that momentarily receives real input (mouse capture taken by
/// ButtonBase's Space handler, a shown window under the cursor) can snapshot a pressed
/// modifier or mouse button and keep it STALE indefinitely -- no amount of waiting recovers,
/// and the test flakes. Headless CI never receives input, so it never sees this.
/// SetKeyboardState zeroes the calling thread's table only (each Xunit.StaFact test thread has
/// its own input queue), restoring the neutral state those tests assume without touching real
/// devices or other threads.
/// </summary>
internal static class InputState
{
    [DllImport("user32.dll")]
    private static extern bool SetKeyboardState(byte[] lpKeyState);

    public static void Neutralize() => SetKeyboardState(new byte[256]);
}
