using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Backward-compatibility guard for the cmdlets that GAINED -ExportCsv in
// v1.5.1 / v1.5.3. Adding a parameter must not change behaviour for
// callers who don't pass it: -ExportCsv and -CsvEncoding must be
// non-mandatory (the object-output path stays the default), and -ExportCsv
// must not be positional (so existing positional `Get-OrchX SomeName`
// invocations still bind SomeName to -Name, not -ExportCsv).
public class ExportCsvOptionalityTests
{
    public static System.Collections.Generic.IEnumerable<object[]> CmdletsWithExportCsv()
    {
        yield return new object[] { typeof(GetApiTriggerCmdlet) };
        yield return new object[] { typeof(GetTestDataQueueCmdlet) };
        yield return new object[] { typeof(GetActionCatalogCmdlet) };
        yield return new object[] { typeof(GetTestSetScheduleCmdlet) };
        yield return new object[] { typeof(GetWebhookCmdlet) };
        yield return new object[] { typeof(GetAssetLinkCmdlet) };
        yield return new object[] { typeof(GetBucketLinkCmdlet) };
        yield return new object[] { typeof(GetQueueLinkCmdlet) };
        // -ExportCsv added in 1.6.2; single [OutputType(NuLicensedUser)].
        // (GetPmGroupLicenseCmdlet is intentionally absent: it declares two
        // [OutputType] attributes, which would make the single-attribute
        // OutputTypeUnchanged check below throw AmbiguousMatchException.)
        yield return new object[] { typeof(GetPmUserLicenseCmdlet) };
        // The whole Compare-Orch* family gained -ExportCsv in 1.16.0, inherited
        // from CompareOrchCmdlet. Same guarantee: the object-output path stays
        // the default, and -Name / -DifferencePath / -DifferenceName keep their
        // positions.
        foreach (var t in CompareCmdletTypes()) yield return new object[] { t };
    }

    // Every Compare-Orch* cmdlet, found through the shared base rather than listed
    // by hand, so a new noun added to the family is covered the day it appears.
    private static System.Collections.Generic.IEnumerable<System.Type> CompareCmdletTypes()
        => typeof(CompareOrchCmdlet).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(CompareOrchCmdlet).IsAssignableFrom(t))
            .OrderBy(t => t.Name, System.StringComparer.Ordinal);

    [Fact]
    public void EveryCompareCmdlet_InheritsTheCsvBase()
    {
        // The family is discovered by base type above; if a cmdlet were added with
        // OrchestratorPSCmdlet as its base it would silently escape every guard here.
        var strays = typeof(CompareOrchCmdlet).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.Name.StartsWith("Compare", System.StringComparison.Ordinal))
            .Where(t => typeof(OrchestratorPSCmdlet).IsAssignableFrom(t))
            .Where(t => !typeof(CompareOrchCmdlet).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        Assert.True(strays.Count == 0,
            "These Compare-Orch* cmdlets do not derive from CompareOrchCmdlet, so they have no " +
            "-ExportCsv and are not covered by the guards in this file:\n  " + string.Join("\n  ", strays));
    }

    [Fact]
    public void EveryCompareCmdlet_HasADistinctDefaultCsvName()
    {
        // The default file name is what -ExportCsv <directory> lands on; two nouns sharing one
        // would have the second silently overwrite the first in a scripted verification pass.
        var names = CompareCmdletTypes()
            .Select(t => (Type: t, Name: (string)t.GetProperty("DefaultCsvName",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
                .GetValue(System.Activator.CreateInstance(t))!))
            .ToList();

        Assert.All(names, n => Assert.EndsWith(".csv", n.Name, System.StringComparison.Ordinal));

        var duplicates = names.GroupBy(n => n.Name, System.StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(x => x.Type.Name))}")
            .ToList();
        Assert.True(duplicates.Count == 0,
            "Compare-Orch* cmdlets sharing a default CSV file name:\n  " + string.Join("\n  ", duplicates));
    }

    [Theory]
    [MemberData(nameof(CmdletsWithExportCsv))]
    public void ExportCsv_IsOptionalAndNotPositional(System.Type cmdletType)
    {
        var prop = cmdletType.GetProperty("ExportCsv");
        Assert.True(prop is not null, $"{cmdletType.Name} lost its -ExportCsv parameter.");
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.All(attrs, a => Assert.False(a.Mandatory,
            $"{cmdletType.Name}.-ExportCsv must NOT be Mandatory — existing callers omit it."));
        Assert.All(attrs, a => Assert.True(a.Position == int.MinValue,
            $"{cmdletType.Name}.-ExportCsv must be a Named parameter (not positional) so " +
            "positional `Get-OrchX SomeName` still binds to -Name."));
    }

    [Theory]
    [MemberData(nameof(CmdletsWithExportCsv))]
    public void CsvEncoding_IsOptional(System.Type cmdletType)
    {
        var prop = cmdletType.GetProperty("CsvEncoding");
        Assert.True(prop is not null, $"{cmdletType.Name} lost its -CsvEncoding parameter.");
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.All(attrs, a => Assert.False(a.Mandatory,
            $"{cmdletType.Name}.-CsvEncoding must NOT be Mandatory."));
    }

    [Theory]
    [MemberData(nameof(CmdletsWithExportCsv))]
    public void OutputTypeUnchanged_StillDeclaresEntityOutput(System.Type cmdletType)
    {
        // The object-output path (no -ExportCsv) must still advertise the
        // entity OutputType so Get-Help / pipelines keep working.
        var outputAttr = cmdletType.GetCustomAttribute<OutputTypeAttribute>();
        Assert.True(outputAttr is not null,
            $"{cmdletType.Name} must still declare [OutputType] for the object-output path.");
        Assert.NotEmpty(outputAttr!.Type);
    }
}
