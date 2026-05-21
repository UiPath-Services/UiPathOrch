using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Locks the surface of the ApiTrigger cmdlet quartet:
//   Get-OrchApiTrigger / New-OrchApiTrigger / Update-OrchApiTrigger / Copy-OrchApiTrigger
//
// Three classes of invariant are pinned down here so they fail at CI rather
// than at first live tenant invocation:
//
//   1. Cmdlet-attribute declarations — verb/noun spelling, SupportsShouldProcess
//      on the mutating cmdlets, OutputType referring to HttpTrigger.
//   2. Pipeline plumbing — Path/Name/Release and every HttpTrigger field
//      parameter declare ValueFromPipelineByPropertyName so an Import-Csv
//      result from a Get-OrchApiTrigger -ExportCsv round-trip binds cleanly.
//   3. CSV column ↔ parameter symmetry — every CsvHeader emitted by
//      Get-OrchApiTrigger has a corresponding parameter on New- and Update-
//      (or is one of the explicitly-exempt structural columns).
//   4. HttpTrigger entity fields observed in dev-tools captures
//      (RunAsCaller / Key added 2026-05-21) are present and JSON-serializable.
public class ApiTriggerCmdletShapeTests
{
    [Fact]
    public void NewApiTrigger_DeclaresShouldProcessAndOutputType()
    {
        var cmdletAttr = typeof(NewApiTriggerCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.Equal("New", cmdletAttr.VerbName);
        Assert.Equal("OrchApiTrigger", cmdletAttr.NounName);
        Assert.True(cmdletAttr.SupportsShouldProcess);

        var outputAttr = typeof(NewApiTriggerCmdlet).GetCustomAttribute<OutputTypeAttribute>();
        Assert.NotNull(outputAttr);
        Assert.Contains(outputAttr.Type, t => t.Type == typeof(HttpTrigger));
    }

    [Fact]
    public void UpdateApiTrigger_DeclaresShouldProcessAndOutputType()
    {
        var cmdletAttr = typeof(UpdateApiTriggerCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.Equal("Update", cmdletAttr.VerbName);
        Assert.Equal("OrchApiTrigger", cmdletAttr.NounName);
        Assert.True(cmdletAttr.SupportsShouldProcess);

        var outputAttr = typeof(UpdateApiTriggerCmdlet).GetCustomAttribute<OutputTypeAttribute>();
        Assert.NotNull(outputAttr);
        Assert.Contains(outputAttr.Type, t => t.Type == typeof(HttpTrigger));
    }

    [Fact]
    public void GetApiTrigger_DoesNotDeclareShouldProcess()
    {
        var cmdletAttr = typeof(GetApiTriggerCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(cmdletAttr);
        Assert.False(cmdletAttr.SupportsShouldProcess);
    }

    [Theory]
    [InlineData(typeof(NewApiTriggerCmdlet), "Name")]
    [InlineData(typeof(NewApiTriggerCmdlet), "Release")]
    [InlineData(typeof(UpdateApiTriggerCmdlet), "Name")]
    public void MutatingCmdlet_HasMandatoryRequiredParameters(System.Type cmdletType, string paramName)
    {
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.True(attrs.Any(a => a.Mandatory),
            $"{cmdletType.Name}.{paramName} must be Mandatory.");
    }

    // ----- Pipeline plumbing: every CSV column name parameter must accept
    // ValueFromPipelineByPropertyName so Get | Export-Csv | Import-Csv |
    // New-OrchApiTrigger works without -PipelineVariable plumbing.
    public static System.Collections.Generic.IEnumerable<object[]> PipelineBoundParameters()
    {
        // "Path" is already in CsvHeaders. Skip "Release" — it is exercised
        // by Release_ParameterAcceptsValueFromPipelineByPropertyName_OnNewAndUpdate
        // since on UpdateApiTriggerCmdlet it is non-mandatory and the
        // mandatory/non-mandatory split needs its own assertion.
        var paramNames = GetCsvHeaders()
            .Where(h => h != "Release")
            .ToArray();
        foreach (var p in paramNames)
        {
            yield return new object[] { typeof(NewApiTriggerCmdlet), p };
            yield return new object[] { typeof(UpdateApiTriggerCmdlet), p };
        }
    }

    [Theory]
    [MemberData(nameof(PipelineBoundParameters))]
    public void Parameter_AcceptsValueFromPipelineByPropertyName(System.Type cmdletType, string paramName)
    {
        var prop = cmdletType.GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            $"{cmdletType.Name}.{paramName} must accept ValueFromPipelineByPropertyName for CSV round-trip.");
    }

    [Fact]
    public void Release_ParameterAcceptsValueFromPipelineByPropertyName_OnNewAndUpdate()
    {
        // Release is mandatory on New, optional on Update, but must accept
        // pipeline binding on both so Get | New (re-create elsewhere) works.
        foreach (var t in new[] { typeof(NewApiTriggerCmdlet), typeof(UpdateApiTriggerCmdlet) })
        {
            var prop = t.GetProperty("Release");
            Assert.NotNull(prop);
            Assert.True(prop.GetCustomAttributes<ParameterAttribute>().Any(a => a.ValueFromPipelineByPropertyName),
                $"{t.Name}.Release must accept ValueFromPipelineByPropertyName.");
        }
    }

    // ----- CSV header ↔ parameter symmetry. Every CSV column emitted by
    // Get-OrchApiTrigger -ExportCsv must map to a parameter on New- and
    // Update- so Import-Csv | New-/Update- round-trips. If a future change
    // adds a column without a parameter, this test fires.
    [Theory]
    [MemberData(nameof(CsvHeadersForBothCmdlets))]
    public void EveryCsvHeader_HasMatchingParameter_OnNewAndUpdate(string header)
    {
        Assert.True(typeof(NewApiTriggerCmdlet).GetProperty(header) is not null,
            $"NewApiTriggerCmdlet missing parameter '{header}' — Get-OrchApiTrigger -ExportCsv would not round-trip into New-OrchApiTrigger.");
        Assert.True(typeof(UpdateApiTriggerCmdlet).GetProperty(header) is not null,
            $"UpdateApiTriggerCmdlet missing parameter '{header}' — Get-OrchApiTrigger -ExportCsv would not round-trip into Update-OrchApiTrigger.");
    }

    public static System.Collections.Generic.IEnumerable<object[]> CsvHeadersForBothCmdlets()
    {
        foreach (var h in GetCsvHeaders())
        {
            yield return new object[] { h };
        }
    }

    // Reflect the static CsvHeaders array off GetApiTriggerCmdlet so the
    // source-of-truth stays the cmdlet itself rather than a shadow copy.
    private static string[] GetCsvHeaders()
    {
        var field = typeof(GetApiTriggerCmdlet).GetField("CsvHeaders",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var value = (string[]?)field!.GetValue(null);
        Assert.NotNull(value);
        return value!;
    }

    [Fact]
    public void CsvHeaders_IncludeRunAsCallerColumn()
    {
        // Regression guard: RunAsCaller was added 2026-05-21 after a dev-tools
        // capture revealed the field on POST/PUT payloads. Dropping it from
        // CSV silently loses the value on Get|Export|Import|Update.
        var headers = GetCsvHeaders();
        Assert.Contains("RunAsCaller", headers);
    }

    // ----- HttpTrigger entity-shape checks.
    [Fact]
    public void HttpTrigger_HasRunAsCallerProperty()
    {
        var prop = typeof(HttpTrigger).GetProperty("RunAsCaller");
        Assert.NotNull(prop);
        Assert.Equal(typeof(bool?), prop!.PropertyType);
    }

    [Fact]
    public void HttpTrigger_HasKeyProperty()
    {
        var prop = typeof(HttpTrigger).GetProperty("Key");
        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    [Fact]
    public void HttpTrigger_JsonRoundTrip_PreservesAllObservedFields()
    {
        // Construct an HttpTrigger with every field a real POST/PUT capture
        // is known to carry (yotsuda tenant 2026-05-21 trace), serialize, and
        // deserialize. Every field must come back identical — that's what the
        // server actually sees on PUT.
        var src = new HttpTrigger
        {
            Name = "api trigger",
            Enabled = true,
            RuntimeType = "Unattended",
            InputArguments = "{}",
            ResumeOnSameContext = false,
            Description = "",
            RemoteControlAccess = "None",
            ReleaseKey = "E3BA4507-54E7-43BE-ABC3-F544A821B665",
            CallingMode = "AsyncRequestReply",
            Method = "Get",
            Slug = "myslug",
            RunAsCaller = false,
            AllowInsecureSsl = true,
            Key = null,
        };

        var json = JsonSerializer.Serialize(src);
        var rt = JsonSerializer.Deserialize<HttpTrigger>(json);

        Assert.NotNull(rt);
        Assert.Equal(src.Name, rt!.Name);
        Assert.Equal(src.Enabled, rt.Enabled);
        Assert.Equal(src.RuntimeType, rt.RuntimeType);
        Assert.Equal(src.InputArguments, rt.InputArguments);
        Assert.Equal(src.ResumeOnSameContext, rt.ResumeOnSameContext);
        Assert.Equal(src.Description, rt.Description);
        Assert.Equal(src.RemoteControlAccess, rt.RemoteControlAccess);
        Assert.Equal(src.ReleaseKey, rt.ReleaseKey);
        Assert.Equal(src.CallingMode, rt.CallingMode);
        Assert.Equal(src.Method, rt.Method);
        Assert.Equal(src.Slug, rt.Slug);
        Assert.Equal(src.RunAsCaller, rt.RunAsCaller);
        Assert.Equal(src.AllowInsecureSsl, rt.AllowInsecureSsl);
        Assert.Equal(src.Key, rt.Key);
    }

    [Fact]
    public void NewApiTrigger_IsListedInModuleManifest()
    {
        var psd1 = LocateModuleManifest();
        var data = System.IO.File.ReadAllText(psd1);
        Assert.Contains("'New-OrchApiTrigger'", data);
        Assert.Contains("'Update-OrchApiTrigger'", data);
    }

    private static string LocateModuleManifest()
    {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "Staging", "UiPathOrch.psd1");
            if (System.IO.File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new System.IO.FileNotFoundException(
            "Staging/UiPathOrch.psd1 not found above " + System.AppContext.BaseDirectory);
    }
}
