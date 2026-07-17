using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Exhaustive per-field change-detection tests for Update-OrchWebhook: every field that can end
// up in the PATCH payload is asserted in both directions — the current value is a no-op, a
// different value writes. Exercised through the pure, API-free UpdateWebhookCmdlet.ComputeWebhookUpdate.
public class UpdateWebhookDirtyTests
{
    private static WebhookEvent[] Ev(params string[] types) =>
        types.Select(t => new WebhookEvent { EventType = t }).ToArray();

    private static Webhook Baseline() => new()
    {
        Id = 1,
        Description = "desc",
        Url = "https://example.test/hook",
        Secret = "sekret",
        Enabled = true,
        AllowInsecureSsl = false,
        SubscribeToAllEvents = false,
        Events = Ev("job.completed", "task.assigned"),
    };

    private static bool Run(Webhook source, UpdateWebhookCmdlet.WebhookUpdateInputs input)
    {
        var payload = new Webhook { Id = source.Id };
        return UpdateWebhookCmdlet.ComputeWebhookUpdate(payload, source, input);
    }

    private static void AssertField(UpdateWebhookCmdlet.WebhookUpdateInputs unchanged, UpdateWebhookCmdlet.WebhookUpdateInputs changed)
    {
        Assert.False(Run(Baseline(), unchanged), "expected NO write when the value equals the current one");
        Assert.True(Run(Baseline(), changed), "expected a write when the value differs from the current one");
    }

    [Fact] public void Description() => AssertField(new() { Description = "desc" }, new() { Description = "changed" });
    [Fact] public void Url() => AssertField(new() { Url = "https://example.test/hook" }, new() { Url = "https://example.test/other" });
    [Fact] public void Secret() => AssertField(new() { Secret = "sekret" }, new() { Secret = "rotated" });

    [Fact]
    public void Secret_EmptyString_IsNoOp_NeverWritesBlank()
    {
        // -Secret "" means "leave it", not "clear it": a blank credential must never be written.
        var source = Baseline();          // has a secret
        var payload = new Webhook { Id = source.Id };
        bool dirty = UpdateWebhookCmdlet.ComputeWebhookUpdate(
            payload, source, new UpdateWebhookCmdlet.WebhookUpdateInputs { Secret = "" });

        Assert.False(dirty);
        Assert.Null(payload.Secret);      // not set to "" on the payload
    }
    [Fact] public void Enabled() => AssertField(new() { Enabled = "true" }, new() { Enabled = "false" });
    [Fact] public void AllowInsecureSsl() => AssertField(new() { AllowInsecureSsl = "false" }, new() { AllowInsecureSsl = "true" });
    [Fact] public void SubscribeToAllEvents() => AssertField(new() { SubscribeToAllEvents = "false" }, new() { SubscribeToAllEvents = "true" });

    [Fact]
    public void Events_SameSetDifferentOrder_IsNoOp()
    {
        // Same event set, reordered; SubscribeToAllEvents already false so the implied flip is a no-op too.
        bool dirty = Run(Baseline(), new UpdateWebhookCmdlet.WebhookUpdateInputs
        {
            ResolvedEvents = Ev("task.assigned", "job.completed"),
            SubscribeAllBound = false,
        });
        Assert.False(dirty);
    }

    [Fact]
    public void Events_DifferentSet_Writes()
    {
        bool dirty = Run(Baseline(), new UpdateWebhookCmdlet.WebhookUpdateInputs
        {
            ResolvedEvents = Ev("job.completed"),
            SubscribeAllBound = false,
        });
        Assert.True(dirty);
    }

    [Fact]
    public void Events_OnSubscribeToAllWebhook_ImpliesNotSubscribeToAll_Writes()
    {
        // Specifying -Events (even an unchanged set) on a subscribe-to-all webhook turns the flag
        // off, which is a real change.
        var source = Baseline();
        source.SubscribeToAllEvents = true;
        bool dirty = Run(source, new UpdateWebhookCmdlet.WebhookUpdateInputs
        {
            ResolvedEvents = Ev("task.assigned", "job.completed"), // same set as baseline
            SubscribeAllBound = false,
        });
        Assert.True(dirty);
    }

    [Fact]
    public void Events_ExplicitSubscribeAllBound_SuppressesImpliedFlip()
    {
        // When -SubscribeToAllEvents is explicitly bound, the implied flip does not fire; an
        // unchanged set with a matching explicit flag is a no-op.
        var source = Baseline();
        source.SubscribeToAllEvents = true;
        bool dirty = Run(source, new UpdateWebhookCmdlet.WebhookUpdateInputs
        {
            SubscribeToAllEvents = "true", // matches current
            ResolvedEvents = Ev("task.assigned", "job.completed"), // same set
            SubscribeAllBound = true,
        });
        Assert.False(dirty);
    }

    [Fact]
    public void NothingSpecified_IsNoOp()
    {
        Assert.False(Run(Baseline(), new UpdateWebhookCmdlet.WebhookUpdateInputs()));
    }
}
