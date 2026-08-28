namespace UiPath.OrchAPI;

/// <summary>
/// The advisories a drive has queued but not yet said: each with the text to show, and — when a
/// richer surface has already shown it — the shorter thing still worth keeping on the console.
/// </summary>
/// <remarks>
/// Extracted from OrchAPISession so this can be tested without a live drive. It was a plain
/// string, and one producer appended to it by assignment
/// (<c>PendingWarning = PendingWarning + "\n\n" + warning</c>). When the store became a list of
/// notices, that assignment collapsed every queued notice into a single entry and discarded the
/// console summaries attached to them — which silently lost the Entra advisory whenever logging
/// happened to be enabled on the same drive. There is no setter here, and no way to append except
/// through <see cref="Append"/>.
/// </remarks>
internal sealed class PendingNoticeQueue
{
    private readonly record struct Notice(string Full, string? ConsoleSummary);

    private readonly List<Notice> _notices = [];

    /// <summary>The queued text as the console drain wants it, or null when there is nothing.</summary>
    internal string? Text => _notices.Count == 0 ? null : string.Join("\n\n", _notices.Select(n => n.Full));

    internal int Count => _notices.Count;

    /// <summary>
    /// Queues one advisory. Appending rather than overwriting is what lets independent advisories
    /// — the IgnoreSslErrors notice, the logging notice, the Entra-ID one — coexist on the same
    /// drive instead of clobbering each other.
    /// </summary>
    /// <param name="consoleSummary">
    /// The one-line form to keep once a richer surface has shown the full text. Notices without
    /// one are dropped at that point, having been fully said.
    /// </param>
    internal void Append(string full, string? consoleSummary = null) => _notices.Add(new Notice(full, consoleSummary));

    internal void Clear() => _notices.Clear();

    /// <summary>
    /// Called once a surface that can show the full text has done so: keeps only what is still
    /// worth repeating, in its short form.
    /// </summary>
    /// <remarks>
    /// The sign-in page is better than the console for a human — it is where they are looking and
    /// its links are clickable — but it is invisible to everything else: a scheduled run, a
    /// transcript, a session driven through an MCP server by an agent. So a notice carrying real
    /// content leaves a one-line trace behind rather than vanishing, while the purely advisory
    /// ones are not said twice.
    /// </remarks>
    internal void DowngradeAfterDisplay()
    {
        var kept = _notices
            .Where(n => !string.IsNullOrEmpty(n.ConsoleSummary))
            .Select(n => new Notice(n.ConsoleSummary!, null))
            .ToList();

        _notices.Clear();
        _notices.AddRange(kept);
    }
}
