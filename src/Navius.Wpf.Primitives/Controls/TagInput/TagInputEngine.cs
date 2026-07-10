namespace Navius.Wpf.Primitives.Controls.TagInput;

/// <summary>A key/char that commits the current field text into a chip (mirrors the web enum).</summary>
public enum TagDelimiter
{
    /// <summary>The Enter key.</summary>
    Enter,

    /// <summary>A comma (also splits typed/pasted text).</summary>
    Comma,

    /// <summary>The Tab key.</summary>
    Tab,

    /// <summary>A space (also splits typed/pasted text).</summary>
    Space,
}

/// <summary>Why <see cref="TagInputEngine.TryCommit"/> refused a candidate (or committed it).</summary>
public enum TagCommitStatus
{
    /// <summary>The candidate was appended.</summary>
    Committed,

    /// <summary>The candidate was empty after trim/transform; a silent no-op (no OnInvalid).</summary>
    Empty,

    /// <summary>The candidate already exists and AllowDuplicates is false (raises the invalid event).</summary>
    Duplicate,

    /// <summary>The list already holds MaxTags entries (raises the invalid event).</summary>
    TooMany,

    /// <summary>The consumer's Validate callback returned false (raises the invalid event).</summary>
    Rejected,
}

/// <summary>
/// Pure, STA-free commit/split/remove/highlight math for <see cref="NaviusTagInput"/>, mirroring
/// the ComboboxEngine factoring so the whole state machine is unit-testable without a UI thread.
/// The commit rule order is ported verbatim from the web root's <c>HandleAddAsync</c>:
/// trim -> transform -> empty (silent) -> duplicate -> max -> validate.
/// </summary>
public static class TagInputEngine
{
    /// <summary>
    /// Applies the web commit pipeline to <paramref name="raw"/>. Returns the outcome, the
    /// normalized candidate text, and the (new or unchanged) tag list. The input list is never
    /// mutated.
    /// </summary>
    public static (TagCommitStatus Status, string Text, IReadOnlyList<string> Tags) TryCommit(
        IReadOnlyList<string> tags,
        string raw,
        Func<string, string>? transform,
        Func<string, bool>? validate,
        bool allowDuplicates,
        int? maxTags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var text = transform is not null ? transform(raw.Trim()) : raw.Trim();
        if (text.Length == 0)
        {
            return (TagCommitStatus.Empty, text, tags);
        }

        if (!allowDuplicates && tags.Contains(text))
        {
            return (TagCommitStatus.Duplicate, text, tags);
        }

        if (maxTags is int max && tags.Count >= max)
        {
            return (TagCommitStatus.TooMany, text, tags);
        }

        if (validate is not null && !validate(text))
        {
            return (TagCommitStatus.Rejected, text, tags);
        }

        var list = new List<string>(tags) { text };
        return (TagCommitStatus.Committed, text, list);
    }

    /// <summary>
    /// The web field's <c>FindCharDelimiter</c>: the first char delimiter present in
    /// <paramref name="text"/>, comma checked before space. Null when none applies.
    /// </summary>
    public static char? FindCharDelimiter(string text, IReadOnlyList<TagDelimiter> delimiters)
    {
        if (delimiters.Contains(TagDelimiter.Comma) && text.Contains(','))
        {
            return ',';
        }

        if (delimiters.Contains(TagDelimiter.Space) && text.Contains(' '))
        {
            return ' ';
        }

        return null;
    }

    /// <summary>
    /// Splits <paramref name="text"/> at its active char delimiter: every completed segment is a
    /// commit candidate; the remainder (after the last delimiter) stays in the field. When no char
    /// delimiter is present the whole text is the remainder.
    /// </summary>
    public static (IReadOnlyList<string> Candidates, string Remainder) Split(
        string text, IReadOnlyList<TagDelimiter> delimiters)
    {
        if (FindCharDelimiter(text, delimiters) is not char d)
        {
            return (Array.Empty<string>(), text);
        }

        var parts = text.Split(d);
        return (parts[..^1], parts[^1]);
    }

    /// <summary>Removes the tag at <paramref name="index"/>; out-of-range is a no-op returning the input list.</summary>
    public static IReadOnlyList<string> RemoveAt(IReadOnlyList<string> tags, int index)
    {
        ArgumentNullException.ThrowIfNull(tags);
        if (index < 0 || index >= tags.Count)
        {
            return tags;
        }

        var list = new List<string>(tags);
        list.RemoveAt(index);
        return list;
    }

    /// <summary>
    /// Where the highlight lands after removing the chip at <paramref name="removedIndex"/>:
    /// the next adjacent chip (clamped to the new last), or -1 (back to the field) when the list
    /// is now empty.
    /// </summary>
    public static int HighlightAfterRemove(int removedIndex, int newCount) =>
        newCount == 0 ? -1 : Math.Min(removedIndex, newCount - 1);
}
