using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// The "Get-Orch* -ExportCsv columns must match New-/Update-Orch*
// parameter names exactly" invariant pinned down for every cmdlet that
// has had -ExportCsv added since v1.5.1. A failure here means a CSV
// emitted by the Get- side won't bind cleanly back into the New-/Update-
// side, which silently breaks the round-trip workflow:
//
//     Get-OrchXxx -ExportCsv exported.csv
//     Import-Csv exported.csv | Update-OrchXxx     # one of these
//     Import-Csv exported.csv | New-OrchXxx        # would silently
//                                                   # ignore mismatched
//                                                   # columns
//
// "Path" is whitelisted as a CSV header column even when not always a
// New-/Update- parameter — the cmdlet receives it via positional binding
// or via -Path enumeration when re-importing.
public class ExportCsvHeaderParityTests
{
    public static System.Collections.Generic.IEnumerable<object[]> CsvHeaderParityCases()
    {
        // Each case: (GetCmdlet type, NewCmdlet type, UpdateCmdlet type-or-null).
        // Get-OrchApiTrigger has both a New- (v1.5.1) and an Update- (v1.5.1).
        yield return new object[] { typeof(GetApiTriggerCmdlet), typeof(NewApiTriggerCmdlet), typeof(UpdateApiTriggerCmdlet) };
        // Get-OrchTestSetSchedule has both a New- (v1.5.1) and Update- (v1.5.3).
        yield return new object[] { typeof(GetTestSetScheduleCmdlet), typeof(NewTestSetScheduleCmdlet), typeof(UpdateTestSetScheduleCmdlet) };
        // Get-OrchWebhook has New- (v1.5.3) and Update- (pre-existing).
        yield return new object[] { typeof(GetWebhookCmdlet), typeof(NewWebhookCmdlet), typeof(UpdateWebhookCmdlet) };
        // Get-OrchTestDataQueue + Get-OrchActionCatalog have New- but no Update- yet.
        yield return new object[] { typeof(GetTestDataQueueCmdlet), typeof(NewTestDataQueueCmdlet), null! };
        yield return new object[] { typeof(GetActionCatalogCmdlet), typeof(NewActionCatalogCmdlet), null! };
    }

    [Theory]
    [MemberData(nameof(CsvHeaderParityCases))]
    public void EveryCsvHeaderColumn_MatchesAParameterName(
        System.Type getCmdlet, System.Type newCmdlet, System.Type? updateCmdlet)
    {
        var csvHeaders = GetCsvHeaders(getCmdlet);
        Assert.NotEmpty(csvHeaders);

        var newParams = ParamNames(newCmdlet);
        var updateParams = updateCmdlet is not null ? ParamNames(updateCmdlet) : System.Array.Empty<string>().ToHashSet();

        // "Path" is always present on Get-Orch* output and is positional /
        // pipeline-bound on New-/Update- — whitelisted regardless of
        // explicit parameter mention.
        var allowed = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "Path" };
        allowed.UnionWith(newParams);
        allowed.UnionWith(updateParams);

        foreach (var col in csvHeaders)
        {
            Assert.True(allowed.Contains(col),
                $"{getCmdlet.Name}.CsvHeaders contains '{col}' which is not a parameter on {newCmdlet.Name}" +
                (updateCmdlet is not null ? $" / {updateCmdlet.Name}" : "") +
                $" — Import-Csv | {VerbNounOf(newCmdlet)} would silently drop the value.");
        }
    }

    [Theory]
    [MemberData(nameof(CsvHeaderParityCases))]
    public void EveryNewCmdletParameter_IsCoveredByCsvHeaderColumn(
        System.Type getCmdlet, System.Type newCmdlet, System.Type? _)
    {
        // The reverse direction: every New-/Update- parameter that the
        // user might want to round-trip should have a CSV column.
        // Common parameter set (-WhatIf, -Confirm, -Verbose ...) and a
        // small whitelist of cmdlet-internal switches are exempt.
        var csvHeaders = GetCsvHeaders(getCmdlet).ToHashSet(System.StringComparer.OrdinalIgnoreCase);
        var newParams = ParamNames(newCmdlet);

        var exempt = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            // Folder enumeration: Path is in CSV; Recurse/Depth are runtime,
            // not entity-shape data so they don't belong in a row.
            "Recurse", "Depth",
            // Update-only rename ergonomic; the new name is the row's Name on
            // re-import, never a separate round-tripped column.
            "NewName",
            // NOTE: New-OrchApiTrigger now exports its full writable surface
            // (callbacks, SSL flag, job-control timeouts, alert thresholds,
            // JobPriority, RuntimeType, TargetFramework, MachineRobots, ...) —
            // every one is live-verified to round-trip (Orch1 2026-05-22), so
            // none are exempt: the test enforces a real column for each.
            // CallbackMode was removed entirely (read-only; the server rejects
            // any non-default value), so it is neither a param nor a column.
        };

        foreach (var p in newParams)
        {
            if (exempt.Contains(p)) continue;
            Assert.True(csvHeaders.Contains(p),
                $"{newCmdlet.Name} has parameter -{p} but {getCmdlet.Name}.CsvHeaders is missing it; " +
                "either add the column or add the param to the test's exempt set with a comment explaining why.");
        }
    }

    // Reflect the static CsvHeaders array off the Get-Orch* cmdlet.
    private static string[] GetCsvHeaders(System.Type cmdletType)
    {
        var field = cmdletType.GetField("CsvHeaders",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var value = (string[]?)field!.GetValue(null);
        Assert.NotNull(value);
        return value!;
    }

    private static System.Collections.Generic.HashSet<string> ParamNames(System.Type cmdletType)
    {
        var names = cmdletType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
            .Select(p => p.Name);
        return new System.Collections.Generic.HashSet<string>(names, System.StringComparer.OrdinalIgnoreCase);
    }

    private static string VerbNounOf(System.Type cmdletType)
    {
        var attr = cmdletType.GetCustomAttribute<CmdletAttribute>();
        return attr is null ? cmdletType.Name : $"{attr.VerbName}-{attr.NounName}";
    }
}
