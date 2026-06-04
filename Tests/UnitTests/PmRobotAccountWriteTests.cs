using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// New-/Set-PmRobotAccount take a string[] UserName and share
// PmRobotAccountWriteCmdletBase. The per-name existence check (SelectWriteTargets)
// must evaluate EACH requested name on its own pattern. A regression matched
// every requested name against the union of all requested patterns, so once one
// requested name matched an existing account, every other name in the same call
// looked already-present. Symptoms:
//   New: only the first new name in `-UserName a,b,c` was created; the rest
//        failed with "<first> already exists".
//   Set: new names mixed with existing ones were silently dropped (no error),
//        and each existing match was updated/emitted once per requested name.
public class PmRobotAccountWriteTests
{
    private static List<PmRobotAccount> Existing(params string[] names)
        => names.Select(n => new PmRobotAccount { name = n }).ToList();

    [Fact]
    public void SelectWriteTargets_creates_new_name_even_when_another_requested_name_exists()
    {
        // New symptom: `-UserName alice,bob` with alice existing must still
        // plan to create bob (no matches), not report it as already-present.
        var plans = PmRobotAccountWriteCmdletBase.SelectWriteTargets(
            Existing("alice"), new[] { "alice", "bob" });

        var bob = plans.Single(p => p.createName == "bob");
        Assert.Empty(bob.matches);

        var alice = plans.Single(p => p.createName == "alice");
        Assert.Equal(new[] { "alice" }, alice.matches.Select(r => r.name));
    }

    [Fact]
    public void SelectWriteTargets_does_not_cross_match_across_requested_names()
    {
        // Set symptom: each existing name must match only its own request,
        // not the union — otherwise every robot is updated/emitted N times.
        var plans = PmRobotAccountWriteCmdletBase.SelectWriteTargets(
            Existing("a", "b"), new[] { "a", "b" });

        Assert.Equal(new[] { "a" }, plans.Single(p => p.createName == "a").matches.Select(r => r.name));
        Assert.Equal(new[] { "b" }, plans.Single(p => p.createName == "b").matches.Select(r => r.name));
    }

    [Fact]
    public void SelectWriteTargets_plans_all_when_none_exist()
    {
        var plans = PmRobotAccountWriteCmdletBase.SelectWriteTargets(
            Existing(), new[] { "a", "b", "c" });

        Assert.All(plans, p => Assert.Empty(p.matches));
        Assert.Equal(new[] { "a", "b", "c" }, plans.Select(p => p.createName));
    }

    [Fact]
    public void SelectWriteTargets_supports_wildcard_match()
    {
        // A single wildcard request still selects every matching account
        // (Set bulk-update use case).
        var plans = PmRobotAccountWriteCmdletBase.SelectWriteTargets(
            Existing("svc-1", "svc-2", "other"), new[] { "svc-*" });

        var plan = Assert.Single(plans);
        Assert.Equal(new[] { "svc-1", "svc-2" }, plan.matches.Select(r => r.name).OrderBy(n => n));
    }

    [Fact]
    public void SelectWriteTargets_handles_null_inputs()
    {
        Assert.Empty(PmRobotAccountWriteCmdletBase.SelectWriteTargets(null, null));
        Assert.Empty(PmRobotAccountWriteCmdletBase.SelectWriteTargets(Existing("a"), null));
    }
}
