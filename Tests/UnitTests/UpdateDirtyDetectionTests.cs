using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Change-detection tests for the Update-* cmdlets. Every Update-Orch* cmdlet must only call
// the write API when a parameter actually changes a value; a no-op request must be skipped so
// it does not churn the audit log (the customer-reported Update-OrchUser -ES_* defect: an
// unchanged execution-setting value still produced a full-record update).
//
// These exercise the shared diff primitives (OrchExtensions.cs) and the extracted, API-free
// UpdateUserCmdlet.ComputeUserUpdate core directly — no live Orchestrator, no HTTP.

public class UnorderedEqualsTests
{
    [Fact]
    public void NullAndEmpty_AreEqual()
    {
        Assert.True(OrchStringExtensions.UnorderedEquals<string>(null, [], s => s));
        Assert.True(OrchStringExtensions.UnorderedEquals<string>([], null, s => s));
        Assert.True(OrchStringExtensions.UnorderedEquals<string>(null, null, s => s));
    }

    [Fact]
    public void SameItemsDifferentOrder_AreEqual()
    {
        Assert.True(OrchStringExtensions.UnorderedEquals(new[] { "a", "b", "c" }, new[] { "c", "a", "b" }, s => s));
    }

    [Fact]
    public void DifferentItems_AreNotEqual()
    {
        Assert.False(OrchStringExtensions.UnorderedEquals(new[] { "a", "b" }, new[] { "a", "c" }, s => s));
    }

    [Fact]
    public void DifferentMultiplicity_IsNotEqual()
    {
        // Same distinct set but different counts must NOT compare equal (multiset semantics).
        Assert.False(OrchStringExtensions.UnorderedEquals(new[] { "a", "a", "b" }, new[] { "a", "b", "b" }, s => s));
        Assert.False(OrchStringExtensions.UnorderedEquals(new[] { "a", "a" }, new[] { "a" }, s => s));
    }

    [Fact]
    public void KeySelector_ProjectsIdentity()
    {
        var a = new[] { new RobotUser { UserName = "u1", RobotId = 1 }, new RobotUser { UserName = "u2", RobotId = 2 } };
        var b = new[] { new RobotUser { UserName = "u2", RobotId = 2 }, new RobotUser { UserName = "u1", RobotId = 1 } };
        Assert.True(OrchStringExtensions.UnorderedEquals(a, b, r => $"{r.RobotId}|{r.UserName}"));

        var c = new[] { new RobotUser { UserName = "u1", RobotId = 1 }, new RobotUser { UserName = "u3", RobotId = 3 } };
        Assert.False(OrchStringExtensions.UnorderedEquals(a, c, r => $"{r.RobotId}|{r.UserName}"));
    }
}

public class TagsEqualAndAssignTagsDiffTests
{
    private static Tag[] T(params (string n, string? v)[] items) =>
        items.Select(i => new Tag { Name = i.n, Value = i.v }).ToArray();

    [Fact]
    public void TagsEqual_OrderInsensitive_ByNameAndValue()
    {
        Assert.True(OrchStringExtensions.TagsEqual(T(("env", "prod"), ("tier", "1")), T(("tier", "1"), ("env", "prod"))));
        Assert.False(OrchStringExtensions.TagsEqual(T(("env", "prod")), T(("env", "dev"))));
        Assert.True(OrchStringExtensions.TagsEqual(null, System.Array.Empty<Tag>()));
    }

    [Fact]
    public void TagsEqual_DoesNotCollideOnConcatenation()
    {
        // "ab"/"c" and "a"/"bc" must not be treated as the same tag.
        Assert.False(OrchStringExtensions.TagsEqual(T(("ab", "c")), T(("a", "bc"))));
    }

    private sealed class TagHolder { public Tag[]? Tags { get; set; } }

    [Fact]
    public void AssignTagsDiff_UnchangedSet_ReturnsFalse_AndDoesNotAssign()
    {
        var source = new TagHolder { Tags = T(("env", "prod")) };
        var target = new TagHolder { Tags = T(("env", "prod")) };
        bool changed = target.AssignTags(new[] { "env=prod" }, source, h => h.Tags, (h, v) => h.Tags = v);
        Assert.False(changed);
    }

    [Fact]
    public void AssignTagsDiff_ChangedSet_ReturnsTrue_AndAssigns()
    {
        var source = new TagHolder { Tags = T(("env", "prod")) };
        var target = new TagHolder { Tags = T(("env", "prod")) };
        bool changed = target.AssignTags(new[] { "env=dev" }, source, h => h.Tags, (h, v) => h.Tags = v);
        Assert.True(changed);
        Assert.Equal("dev", target.Tags![0].Value);
    }

    [Fact]
    public void AssignTagsDiff_EmptyInput_IsNoOp()
    {
        var source = new TagHolder { Tags = T(("env", "prod")) };
        var target = new TagHolder { Tags = T(("env", "prod")) };
        Assert.False(target.AssignTags(null, source, h => h.Tags, (h, v) => h.Tags = v));
        Assert.False(target.AssignTags(System.Array.Empty<string>(), source, h => h.Tags, (h, v) => h.Tags = v));
    }
}

public class UpdatePolicyEqualsTests
{
    [Fact]
    public void EqualByTypeAndVersion()
    {
        Assert.True(OrchStringExtensions.UpdatePolicyEquals(
            new UpdatePolicy { Type = "Specific", SpecificVersion = "1.0.0" },
            new UpdatePolicy { Type = "Specific", SpecificVersion = "1.0.0" }));
        Assert.False(OrchStringExtensions.UpdatePolicyEquals(
            new UpdatePolicy { Type = "Specific", SpecificVersion = "1.0.0" },
            new UpdatePolicy { Type = "Specific", SpecificVersion = "2.0.0" }));
    }

    [Fact]
    public void NullPolicy_ComparesAsEmpty()
    {
        Assert.True(OrchStringExtensions.UpdatePolicyEquals(null, null));
        Assert.False(OrchStringExtensions.UpdatePolicyEquals(null, new UpdatePolicy { Type = "None" }));
    }
}

public class MaintenanceWindowEqualsTests
{
    [Fact]
    public void EqualOverSettableFields()
    {
        var a = new MaintenanceWindow { Enabled = true, CronExpression = "0 0 * * 0", TimezoneId = "UTC", Duration = 60 };
        var b = new MaintenanceWindow { Enabled = true, CronExpression = "0 0 * * 0", TimezoneId = "UTC", Duration = 60, NextExecutionTime = System.DateTime.UnixEpoch };
        Assert.True(OrchStringExtensions.MaintenanceWindowEquals(a, b)); // NextExecutionTime ignored
    }

    [Fact]
    public void DifferentCron_NotEqual()
    {
        var a = new MaintenanceWindow { CronExpression = "0 0 * * 0" };
        var b = new MaintenanceWindow { CronExpression = "0 1 * * 0" };
        Assert.False(OrchStringExtensions.MaintenanceWindowEquals(a, b));
    }

    [Fact]
    public void NullWindow_ComparesAsEmpty()
    {
        Assert.True(OrchStringExtensions.MaintenanceWindowEquals(null, new MaintenanceWindow()));
        Assert.False(OrchStringExtensions.MaintenanceWindowEquals(null, new MaintenanceWindow { Enabled = true }));
    }
}

public class AssignNumberIfNotNullStringSourceTests
{
    private sealed class Holder { public int? Num { get; set; } }

    [Fact]
    public void SameAsSource_ReturnsFalse()
    {
        var src = new Holder { Num = 1920 };
        var tgt = new Holder { Num = 1920 };
        Assert.False(tgt.AssignNumberIfNotNull("1920", src, h => h.Num, (h, v) => h.Num = v));
    }

    [Fact]
    public void DifferentFromSource_AssignsAndReturnsTrue()
    {
        var src = new Holder { Num = 1920 };
        var tgt = new Holder { Num = 1920 };
        Assert.True(tgt.AssignNumberIfNotNull("1280", src, h => h.Num, (h, v) => h.Num = v));
        Assert.Equal(1280, tgt.Num);
    }

    [Fact]
    public void EmptyString_ClearsWhenSourceHadValue()
    {
        var src = new Holder { Num = 1920 };
        var tgt = new Holder { Num = 1920 };
        Assert.True(tgt.AssignNumberIfNotNull("", src, h => h.Num, (h, v) => h.Num = v));
        Assert.Null(tgt.Num);
    }

    [Fact]
    public void NullValue_IsNoOp()
    {
        var src = new Holder { Num = 1920 };
        var tgt = new Holder { Num = 1920 };
        Assert.False(tgt.AssignNumberIfNotNull((string?)null, src, h => h.Num, (h, v) => h.Num = v));
    }

    [Fact]
    public void UnparseableNonEmpty_IsNoOp()
    {
        var src = new Holder { Num = 1920 };
        var tgt = new Holder { Num = 1920 };
        Assert.False(tgt.AssignNumberIfNotNull("abc", src, h => h.Num, (h, v) => h.Num = v));
        Assert.Equal(1920, tgt.Num);
    }
}

// The customer-reported defect, pinned end-to-end against the pure decision core:
// Update-OrchUser * -ES_StudioNotifyServer false must NOT update a user whose
// StudioNotifyServer is already false.
public class ComputeUserUpdateTests
{
    // A baseline unattended-capable user with a robot-provision execution-settings block.
    private static User BaselineUser() => new()
    {
        Id = 42,
        Type = "DirectoryUser",
        Name = "First",
        Surname = "Last",
        RolesList = new[] { "Administrator", "Robot" },
        RobotProvision = new AttendedRobot
        {
            ExecutionSettings = new ExecutionSettings { StudioNotifyServer = false, TracingLevel = "Off" }
        },
        UnattendedRobot = new UnattendedRobot
        {
            UserName = @"domain\svc",
            CredentialStoreId = 7,
            ExecutionSettings = new ExecutionSettings { StudioNotifyServer = false }
        },
        UpdatePolicy = new UpdatePolicy { Type = "None" }
    };

    private static bool Run(User detailed, UpdateUserCmdlet.UserUpdateInputs input)
    {
        var posting = OrchCollectionExtensions.DeepCopy(detailed);
        return UpdateUserCmdlet.ComputeUserUpdate(posting, detailed, input);
    }

    [Fact]
    public void EsStudioNotifyServer_AlreadyFalse_IsNoOp()
    {
        // THE reported bug: value unchanged -> must not be dirty -> no PUT.
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { ES_StudioNotifyServer = "false" });
        Assert.False(dirty);
    }

    [Fact]
    public void EsStudioNotifyServer_ChangedToTrue_IsDirty()
    {
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { ES_StudioNotifyServer = "true" });
        Assert.True(dirty);
    }

    [Fact]
    public void EsResolutionWidth_Unchanged_IsNoOp()
    {
        var detailed = BaselineUser();
        detailed.RobotProvision!.ExecutionSettings!.ResolutionWidth = 1920;
        detailed.UnattendedRobot!.ExecutionSettings!.ResolutionWidth = 1920;
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { ES_ResolutionWidth = "1920" });
        Assert.False(dirty);
    }

    [Fact]
    public void NothingSpecified_IsNoOp()
    {
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs());
        Assert.False(dirty);
    }

    [Fact]
    public void Name_Unchanged_IsNoOp_Changed_IsDirty()
    {
        var detailed = BaselineUser();
        Assert.False(Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { Name = "First" }));
        Assert.True(Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { Name = "Renamed" }));
    }

    [Fact]
    public void Roles_SameSetDifferentOrder_IsNoOp()
    {
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs
        {
            RolesSpecified = true,
            ResolvedRoleNames = new[] { "Robot", "Administrator" } // same set, different order
        });
        Assert.False(dirty);
    }

    [Fact]
    public void Roles_DifferentSet_IsDirty()
    {
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs
        {
            RolesSpecified = true,
            ResolvedRoleNames = new[] { "Administrator" }
        });
        Assert.True(dirty);
    }

    [Fact]
    public void UnattendedRobot_UserNameUnchanged_IsNoOp()
    {
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { UR_UserName = @"domain\svc" });
        Assert.False(dirty);
    }

    [Fact]
    public void UnattendedRobot_UserNameChanged_IsDirty()
    {
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { UR_UserName = @"domain\other" });
        Assert.True(dirty);
    }

    [Fact]
    public void UnattendedRobot_CredentialStoreSameId_IsNoOp()
    {
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs
        {
            UR_CredentialStoreSpecified = true,
            UR_ResolvedCredentialStoreId = 7 // same as baseline
        });
        Assert.False(dirty);
    }

    [Fact]
    public void UnattendedRobot_PasswordSupplied_AlwaysDirty()
    {
        // Password can't be diffed (server returns it masked), so supplying one always writes.
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { UR_Password = "s3cr3t" });
        Assert.True(dirty);
    }

    [Fact]
    public void UpdatePolicy_Unchanged_IsNoOp()
    {
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { UpdatePolicyType = "None" });
        Assert.False(dirty);
    }

    [Fact]
    public void UpdatePolicy_Changed_IsDirty()
    {
        var detailed = BaselineUser();
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { UpdatePolicyType = "LatestVersion" });
        Assert.True(dirty);
    }

    [Fact]
    public void DirectoryExternalApplication_SkipsExecutionSettings()
    {
        // ES_* is not applicable to external applications; even a "changed" value must be ignored.
        var detailed = BaselineUser();
        detailed.Type = "DirectoryExternalApplication";
        bool dirty = Run(detailed, new UpdateUserCmdlet.UserUpdateInputs { ES_StudioNotifyServer = "true" });
        Assert.False(dirty);
    }
}

// Exhaustive per-parameter coverage: for EVERY Update-OrchUser parameter that can flip the
// dirty flag (i.e. every field that can end up in the PUT payload's decision), assert both
// directions — the current value is a no-op, a different value writes. This is the guarantee
// the customer asked for: nothing writes unless it actually changes, field by field.
public class ComputeUserUpdate_EveryParameterTests
{
    // Baseline with a concrete, known value for every diffable field so "same" and "different"
    // are both expressible per parameter. Both execution-settings blocks (RobotProvision and
    // UnattendedRobot) carry identical values so an ES_* diff is exercised on both.
    private static ExecutionSettings Es() => new()
    {
        TracingLevel = "Off",
        StudioNotifyServer = false,
        LoginToConsole = false,
        ResolutionWidth = 1920,
        ResolutionHeight = 1080,
        ResolutionDepth = 32,
        FontSmoothing = false,
        AutoDownloadProcess = false,
    };

    private static User Baseline() => new()
    {
        Id = 1,
        Type = "DirectoryUser",
        Name = "First",
        Surname = "Last",
        IsExternalLicensed = false,
        MayHaveUserSession = true,
        MayHaveRobotSession = true,
        MayHaveUnattendedSession = false,
        MayHavePersonalWorkspace = true,
        RestrictToPersonalWorkspace = false,
        RolesList = new[] { "Administrator", "Robot" },
        RobotProvision = new AttendedRobot { ExecutionSettings = Es() },
        UnattendedRobot = new UnattendedRobot
        {
            UserName = @"domain\svc",
            CredentialStoreId = 7,
            CredentialExternalName = "ext",
            CredentialType = "UsernamePassword",
            LimitConcurrentExecution = false,
            ExecutionSettings = Es(),
        },
        UpdatePolicy = new UpdatePolicy { Type = "None" },
    };

    // Assert both directions for one parameter in a single case.
    private static void AssertField(UpdateUserCmdlet.UserUpdateInputs unchanged, UpdateUserCmdlet.UserUpdateInputs changed)
    {
        var d1 = Baseline();
        Assert.False(UpdateUserCmdlet.ComputeUserUpdate(OrchCollectionExtensions.DeepCopy(d1), d1, unchanged),
            "expected NO write when the value equals the current one");
        var d2 = Baseline();
        Assert.True(UpdateUserCmdlet.ComputeUserUpdate(OrchCollectionExtensions.DeepCopy(d2), d2, changed),
            "expected a write when the value differs from the current one");
    }

    [Fact]
    public void Name() => AssertField(
        new() { Name = "First" }, new() { Name = "Changed" });

    [Fact]
    public void Surname() => AssertField(
        new() { Surname = "Last" }, new() { Surname = "Changed" });

    [Fact]
    public void IsExternalLicensed() => AssertField(
        new() { IsExternalLicensed = "false" }, new() { IsExternalLicensed = "true" });

    [Fact]
    public void MayHaveUserSession() => AssertField(
        new() { MayHaveUserSession = "true" }, new() { MayHaveUserSession = "false" });

    [Fact]
    public void MayHaveRobotSession() => AssertField(
        new() { MayHaveRobotSession = "true" }, new() { MayHaveRobotSession = "false" });

    [Fact]
    public void MayHaveUnattendedSession() => AssertField(
        new() { MayHaveUnattendedSession = "false" }, new() { MayHaveUnattendedSession = "true" });

    [Fact]
    public void MayHavePersonalWorkspace() => AssertField(
        new() { MayHavePersonalWorkspace = "true" }, new() { MayHavePersonalWorkspace = "false" });

    [Fact]
    public void RestrictToPersonalWorkspace() => AssertField(
        new() { RestrictToPersonalWorkspace = "false" }, new() { RestrictToPersonalWorkspace = "true" });

    [Fact]
    public void Roles() => AssertField(
        new() { RolesSpecified = true, ResolvedRoleNames = new[] { "Robot", "Administrator" } },
        new() { RolesSpecified = true, ResolvedRoleNames = new[] { "Administrator" } });

    [Fact]
    public void UR_UserName() => AssertField(
        new() { UR_UserName = @"domain\svc" }, new() { UR_UserName = @"domain\other" });

    [Fact]
    public void UR_CredentialExternalName() => AssertField(
        new() { UR_CredentialExternalName = "ext" }, new() { UR_CredentialExternalName = "ext2" });

    [Fact]
    public void UR_CredentialType() => AssertField(
        new() { UR_CredentialType = "UsernamePassword" }, new() { UR_CredentialType = "WindowsCredentials" });

    [Fact]
    public void UR_LimitConcurrentExecution() => AssertField(
        new() { UR_LimitConcurrentExecution = "false" }, new() { UR_LimitConcurrentExecution = "true" });

    [Fact]
    public void UR_CredentialStore() => AssertField(
        new() { UR_CredentialStoreSpecified = true, UR_ResolvedCredentialStoreId = 7 },
        new() { UR_CredentialStoreSpecified = true, UR_ResolvedCredentialStoreId = 8 });

    [Fact]
    public void UR_Password_AlwaysWrites()
    {
        // Can't be diffed (returned masked), so any supplied password writes.
        var d = Baseline();
        Assert.True(UpdateUserCmdlet.ComputeUserUpdate(OrchCollectionExtensions.DeepCopy(d), d,
            new UpdateUserCmdlet.UserUpdateInputs { UR_Password = "s3cr3t" }));
    }

    [Fact]
    public void UpdatePolicyType() => AssertField(
        new() { UpdatePolicyType = "None" }, new() { UpdatePolicyType = "LatestVersion" });

    [Fact]
    public void UpdatePolicyVersion()
    {
        // Needs a pinned-version baseline (the shared baseline is Type=None / no version).
        static User Pinned() { var u = Baseline(); u.UpdatePolicy = new UpdatePolicy { Type = "Specific", SpecificVersion = "1.0.0" }; return u; }
        var d1 = Pinned();
        Assert.False(UpdateUserCmdlet.ComputeUserUpdate(OrchCollectionExtensions.DeepCopy(d1), d1,
            new UpdateUserCmdlet.UserUpdateInputs { UpdatePolicyType = "Specific", UpdatePolicyVersion = "1.0.0" }));
        var d2 = Pinned();
        Assert.True(UpdateUserCmdlet.ComputeUserUpdate(OrchCollectionExtensions.DeepCopy(d2), d2,
            new UpdateUserCmdlet.UserUpdateInputs { UpdatePolicyType = "Specific", UpdatePolicyVersion = "9.9.9" }));
    }

    [Fact]
    public void ES_TracingLevel() => AssertField(
        new() { ES_TracingLevel = "Off" }, new() { ES_TracingLevel = "Verbose" });

    [Fact]
    public void ES_StudioNotifyServer() => AssertField(
        new() { ES_StudioNotifyServer = "false" }, new() { ES_StudioNotifyServer = "true" });

    [Fact]
    public void ES_LoginToConsole() => AssertField(
        new() { ES_LoginToConsole = "false" }, new() { ES_LoginToConsole = "true" });

    [Fact]
    public void ES_ResolutionWidth() => AssertField(
        new() { ES_ResolutionWidth = "1920" }, new() { ES_ResolutionWidth = "1280" });

    [Fact]
    public void ES_ResolutionHeight() => AssertField(
        new() { ES_ResolutionHeight = "1080" }, new() { ES_ResolutionHeight = "720" });

    [Fact]
    public void ES_ResolutionDepth() => AssertField(
        new() { ES_ResolutionDepth = "32" }, new() { ES_ResolutionDepth = "16" });

    [Fact]
    public void ES_FontSmoothing() => AssertField(
        new() { ES_FontSmoothing = "false" }, new() { ES_FontSmoothing = "true" });

    [Fact]
    public void ES_AutoDownloadProcess() => AssertField(
        new() { ES_AutoDownloadProcess = "false" }, new() { ES_AutoDownloadProcess = "true" });
}
