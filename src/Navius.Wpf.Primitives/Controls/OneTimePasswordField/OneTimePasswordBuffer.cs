namespace Navius.Wpf.Primitives.Controls.OneTimePasswordField;

/// <summary>
/// Pure, UI-free buffer mechanics for NaviusOneTimePasswordField's advance/retreat/paste
/// keyboard table (one-time-password-field.md's Keyboard section), factored out of the
/// control so the logic is directly unit-testable without a WPF Application/STA thread.
/// The internal buffer keeps a positional char?[] (gaps as null); ToValue/FromValue
/// round-trip that through a space-padded string, exactly matching the contract's own
/// "interior gaps are represented as spaces in the aggregate string round-trip" note.
/// </summary>
public static class OneTimePasswordBuffer
{
    public static char?[] FromValue(string? value, int length)
    {
        var buffer = new char?[length];
        if (value is null)
        {
            return buffer;
        }

        for (var i = 0; i < length && i < value.Length; i++)
        {
            buffer[i] = value[i] == ' ' ? null : value[i];
        }

        return buffer;
    }

    public static string ToValue(char?[] buffer) => new(buffer.Select(c => c ?? ' ').ToArray());

    public static bool IsComplete(char?[] buffer) => buffer.All(c => c.HasValue);

    /// <summary>Overwrites the slot and advances focus by one (contract: "last char of raw input wins").</summary>
    public static (char?[] Buffer, int FocusIndex) SetChar(char?[] buffer, int index, char ch)
    {
        var next = (char?[])buffer.Clone();
        next[index] = ch;
        return (next, Math.Min(index + 1, buffer.Length - 1));
    }

    /// <summary>
    /// Non-empty focused cell: clears it, shifts the remainder back, focus retreats one cell.
    /// Empty focused cell: clears the previous cell instead, focus retreats one cell.
    /// </summary>
    public static (char?[] Buffer, int FocusIndex) Backspace(char?[] buffer, int index)
    {
        var next = (char?[])buffer.Clone();

        if (next[index] is not null)
        {
            ShiftLeftFrom(next, index);
            return (next, Math.Max(index - 1, 0));
        }

        if (index > 0)
        {
            ShiftLeftFrom(next, index - 1);
            return (next, Math.Max(index - 1, 0));
        }

        return (next, index);
    }

    /// <summary>Clears the focused character and shifts the remaining characters back one slot; focus stays put.</summary>
    public static (char?[] Buffer, int FocusIndex) Delete(char?[] buffer, int index)
    {
        var next = (char?[])buffer.Clone();
        ShiftLeftFrom(next, index);
        return (next, index);
    }

    public static (char?[] Buffer, int FocusIndex) ClearAll(int length) => (new char?[length], 0);

    /// <summary>Replaces the entire field from slot 0, regardless of which cell was focused.</summary>
    public static (char?[] Buffer, int FocusIndex) Paste(string sanitizedText, int length)
    {
        var next = new char?[length];
        var count = Math.Min(sanitizedText.Length, length);
        for (var i = 0; i < count; i++)
        {
            next[i] = sanitizedText[i];
        }

        return (next, Math.Max(count - 1, 0));
    }

    /// <summary>Culture-aware char.IsDigit/IsLetter classification, matching the web contract's semantics.</summary>
    public static bool IsAllowedChar(char ch, string validationType) => validationType switch
    {
        "numeric" => char.IsDigit(ch),
        "alpha" => char.IsLetter(ch),
        "alphanumeric" => char.IsLetterOrDigit(ch),
        _ => true,
    };

    private static void ShiftLeftFrom(char?[] buffer, int index)
    {
        for (var i = index; i < buffer.Length - 1; i++)
        {
            buffer[i] = buffer[i + 1];
        }

        buffer[^1] = null;
    }
}
