using System.Windows.Input;
using Navius.Wpf.Primitives.Controls.Internal;

namespace Navius.Wpf.Primitives.Controls.DateInput;

/// <summary>
/// Maps <see cref="Key"/> onto the WPF-free <see cref="SegmentKey"/> enum consumed by
/// <see cref="SegmentMath.HandleKey"/>. Shared by NaviusDateInput and NaviusTimeInput/
/// NaviusTimePicker so both roots dispatch through the same PreviewKeyDown table.
/// </summary>
internal static class SegmentKeyMapper
{
    public static (SegmentKey Key, int Digit) Map(Key key) => key switch
    {
        Key.Up => (SegmentKey.ArrowUp, 0),
        Key.Down => (SegmentKey.ArrowDown, 0),
        Key.PageUp => (SegmentKey.PageUp, 0),
        Key.PageDown => (SegmentKey.PageDown, 0),
        Key.Home => (SegmentKey.Home, 0),
        Key.End => (SegmentKey.End, 0),
        Key.Back => (SegmentKey.Backspace, 0),
        Key.Delete => (SegmentKey.Delete, 0),
        Key.Left => (SegmentKey.ArrowLeft, 0),
        Key.Right => (SegmentKey.ArrowRight, 0),
        Key.A => (SegmentKey.LetterA, 0),
        Key.P => (SegmentKey.LetterP, 0),
        >= Key.D0 and <= Key.D9 => (SegmentKey.Digit, key - Key.D0),
        >= Key.NumPad0 and <= Key.NumPad9 => (SegmentKey.Digit, key - Key.NumPad0),
        _ => (SegmentKey.None, 0),
    };
}
