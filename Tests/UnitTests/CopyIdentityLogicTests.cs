using System.Collections.Generic;
using System.Linq;
using UiPath.PowerShell.Entities;
using Xunit;
using static UiPath.PowerShell.Core.OrchProvider;

namespace UnitTests;

// Verifies the cross-org copy safeguards extracted from Copy-PmUser and
// Copy-PmRobotAccount (CopyIdentityLogic). These pin the behaviour that was
// added/fixed and live-verified OnPrem 22.10.1 -> Cloud:
//   * no-email source users are skipped (never sent with an empty email);
//   * the destination "already taken" lookup is built without throwing on
//     duplicate/empty-email entries (the old ToDictionary(email) crashed and
//     silently disabled the duplicate check);
//   * a BulkCreate failure is surfaced only on an explicit succeeded == false;
//   * Copy-PmRobotAccount skips an account whose name already exists, without
//     throwing on a null name.
public class CopyIdentityLogicTests
{
    private static PmUser User(string? email, string? userName = null) =>
        new() { email = email, userName = userName };

    private static PmRobotAccount Robot(string? name) => new() { name = name };

    // ---- HasNoEmail ----

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("alice@x.com", false)]
    public void HasNoEmail_DetectsMissingEmail(string? email, bool expected)
    {
        Assert.Equal(expected, HasNoEmail(User(email)));
    }

    [Fact]
    public void HasNoEmail_NullUser_IsTrueNotThrow()
    {
        Assert.True(HasNoEmail(null));
    }

    // ---- BuildDstUserLookup ----

    [Fact]
    public void BuildDstUserLookup_DuplicateEmails_DoNotThrow_AndCollapse()
    {
        var users = new[] { User("dup@x.com", "first"), User("dup@x.com", "second") };
        var lookup = BuildDstUserLookup(users);
        Assert.Single(lookup);
        Assert.True(lookup.ContainsKey("dup@x.com"));
        Assert.Equal("first", lookup["dup@x.com"].userName); // GroupBy keeps the first
    }

    [Fact]
    public void BuildDstUserLookup_EmptyAndNullEmails_AreExcluded_NoThrow()
    {
        // Two empty-email users would have collided on key "" with a plain
        // ToDictionary and thrown "same key has already been added".
        var users = new[] { User(null, "a"), User("", "b"), User("real@x.com", "c") };
        var lookup = BuildDstUserLookup(users);
        Assert.Single(lookup);
        Assert.True(lookup.ContainsKey("real@x.com"));
    }

    [Fact]
    public void BuildDstUserLookup_IsCaseInsensitive()
    {
        var users = new[] { User("Mixed@X.com", "first"), User("mixed@x.com", "second") };
        var lookup = BuildDstUserLookup(users);
        Assert.Single(lookup);
        Assert.True(lookup.ContainsKey("MIXED@X.COM"));
    }

    [Fact]
    public void BuildDstUserLookup_DistinctEmails_AllPresent()
    {
        var users = new[] { User("a@x.com"), User("b@x.com"), User("c@x.com") };
        var lookup = BuildDstUserLookup(users);
        Assert.Equal(3, lookup.Count);
    }

    // ---- IsBulkCreateFailure ----

    [Fact]
    public void IsBulkCreateFailure_NullResponse_IsFalse_NoFalseAlarm()
    {
        Assert.False(IsBulkCreateFailure(null));
    }

    [Fact]
    public void IsBulkCreateFailure_NullResult_IsFalse()
    {
        Assert.False(IsBulkCreateFailure(new BulkCreateResponse { result = null }));
    }

    [Fact]
    public void IsBulkCreateFailure_NullSucceeded_IsFalse()
    {
        var r = new BulkCreateResponse { result = new BulkCreateResult { succeeded = null } };
        Assert.False(IsBulkCreateFailure(r));
    }

    [Fact]
    public void IsBulkCreateFailure_Succeeded_IsFalse()
    {
        var r = new BulkCreateResponse { result = new BulkCreateResult { succeeded = true } };
        Assert.False(IsBulkCreateFailure(r));
    }

    [Fact]
    public void IsBulkCreateFailure_ExplicitFalse_IsTrue()
    {
        var r = new BulkCreateResponse { result = new BulkCreateResult { succeeded = false } };
        Assert.True(IsBulkCreateFailure(r));
    }

    // ---- BuildExistingRobotNameSet ----

    [Fact]
    public void BuildExistingRobotNameSet_DuplicatesAndEmpties_NoThrow()
    {
        var robots = new[] { Robot("bot1"), Robot("bot1"), Robot(null), Robot(""), Robot("bot2") };
        var set = BuildExistingRobotNameSet(robots);
        Assert.Equal(2, set.Count);
        Assert.Contains("bot1", set);
        Assert.Contains("bot2", set);
    }

    [Fact]
    public void BuildExistingRobotNameSet_IsCaseInsensitive()
    {
        var set = BuildExistingRobotNameSet(new[] { Robot("Bot1") });
        Assert.Contains("BOT1", set);
    }

    // ---- RobotNameAlreadyExists ----

    [Theory]
    [InlineData("bot1", true)]
    [InlineData("BOT1", true)]   // case-insensitive
    [InlineData("other", false)]
    [InlineData("", false)]
    [InlineData(null, false)]    // null name must not throw on Contains
    public void RobotNameAlreadyExists_HandlesPresenceAndNulls(string? name, bool expected)
    {
        var existing = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "bot1", "bot2" };
        Assert.Equal(expected, RobotNameAlreadyExists(existing, name));
    }
}
