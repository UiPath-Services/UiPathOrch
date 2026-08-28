using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// The sign-in page shows the full advisories and then downgrades the queue, so a notice with real
// content leaves a one-line trace on the console for anything that cannot see a browser.
// These pin the behaviour, and in particular the defect that cost an afternoon: a second notice
// arriving after the first must not take the first one's summary with it.
public class PendingNoticeQueueTests
{
    [Fact]
    public void An_empty_queue_has_no_text()
    {
        var q = new PendingNoticeQueue();

        Assert.Null(q.Text);
    }

    [Fact]
    public void Notices_are_joined_the_way_the_console_drain_splits_them()
    {
        var q = new PendingNoticeQueue();
        q.Append("First.");
        q.Append("Second.");

        Assert.Equal("First.\n\nSecond.", q.Text);
    }

    [Fact]
    public void Downgrade_keeps_only_what_has_a_summary_and_keeps_it_short()
    {
        var q = new PendingNoticeQueue();
        q.Append("Long advisory with something to do about it.", "Short form.");
        q.Append("Purely advisory, fully said on the page.");

        q.DowngradeAfterDisplay();

        Assert.Equal("Short form.", q.Text);
    }

    [Fact]
    public void A_later_notice_without_a_summary_does_not_discard_an_earlier_summary()
    {
        // The actual defect: the logging advisory is queued by the first HTTP call, which happens
        // AFTER the Entra advisory is queued during the sign-in callback. While one producer
        // appended by assigning the joined text back, that second notice replaced the list and
        // the Entra summary vanished -- so the console said nothing at all.
        var q = new PendingNoticeQueue();
        q.Append("Entra advisory, full text.", "Entra summary.");
        q.Append("Logging is enabled for 'Orch1:\\'.");

        Assert.Equal(2, q.Count);

        q.DowngradeAfterDisplay();

        Assert.Equal("Entra summary.", q.Text);
    }

    [Fact]
    public void Downgrade_with_nothing_worth_keeping_empties_the_queue()
    {
        var q = new PendingNoticeQueue();
        q.Append("Shown on the page, nothing to repeat.");

        q.DowngradeAfterDisplay();

        Assert.Null(q.Text);
    }

    [Fact]
    public void Downgrade_twice_does_not_re_downgrade_the_summaries_away()
    {
        // The page path calls this once, but a second call must be harmless: a summary that has
        // already replaced its full text has no summary of its own to fall back to.
        var q = new PendingNoticeQueue();
        q.Append("Full.", "Summary.");

        q.DowngradeAfterDisplay();
        q.DowngradeAfterDisplay();

        Assert.Null(q.Text);
    }
}
