using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Locks the surface of Update-OrchTestSetSchedule (added in v1.5.3).
// The cmdlet mirrors New-OrchTestSetSchedule parameter-for-parameter,
// plus -NewName, plus the standard -Recurse / -Depth pair for any cmdlet
// that walks folders. These tests pin the contract.
public class UpdateTestSetScheduleShapeTests
{
    [Fact]
    public void DeclaresUpdateVerb_AndSupportsShouldProcess()
    {
        var attr = typeof(UpdateTestSetScheduleCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Update", attr.VerbName);
        Assert.Equal("OrchTestSetSchedule", attr.NounName);
        Assert.True(attr.SupportsShouldProcess);

        var outputAttr = typeof(UpdateTestSetScheduleCmdlet).GetCustomAttribute<OutputTypeAttribute>();
        Assert.NotNull(outputAttr);
        Assert.Contains(outputAttr.Type, t => t.Type == typeof(TestSetSchedule));
    }

    [Fact]
    public void Name_IsMandatoryAndPipelineBound()
    {
        var prop = typeof(UpdateTestSetScheduleCmdlet).GetProperty("Name");
        Assert.NotNull(prop);
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.True(attrs.Any(a => a.Mandatory),
            "Update-OrchTestSetSchedule.Name must be Mandatory.");
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            "Update-OrchTestSetSchedule.Name must accept ValueFromPipelineByPropertyName.");
    }

    public static System.Collections.Generic.IEnumerable<object[]> MirroredParameters()
    {
        // Every New-OrchTestSetSchedule parameter must also exist on
        // Update-OrchTestSetSchedule (with the same name) so a CSV export
        // round-trips into either cmdlet.
        var names = new[] { "Name", "TestSetName", "CronExpression", "Description", "Enabled", "TimeZoneId", "CalendarName", "Path" };
        foreach (var n in names) yield return new object[] { n };
    }

    [Theory]
    [MemberData(nameof(MirroredParameters))]
    public void EveryNewParam_ExistsOnUpdate(string paramName)
    {
        Assert.True(typeof(NewTestSetScheduleCmdlet).GetProperty(paramName) is not null,
            $"NewTestSetScheduleCmdlet missing -{paramName} — test out of date with cmdlet source.");
        Assert.True(typeof(UpdateTestSetScheduleCmdlet).GetProperty(paramName) is not null,
            $"UpdateTestSetScheduleCmdlet missing -{paramName} — CSV round-trip via Update- would lose this value.");
    }

    [Fact]
    public void Update_HasNewNameParam_ForRenameSupport()
    {
        // -NewName is the rename idiom (mirrors Update-OrchTrigger /
        // Update-OrchApiTrigger). Without it, rename via the cmdlet is
        // impossible — you'd have to Remove + New.
        var prop = typeof(UpdateTestSetScheduleCmdlet).GetProperty("NewName");
        Assert.NotNull(prop);
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            "Update-OrchTestSetSchedule.NewName must accept ValueFromPipelineByPropertyName.");
    }

    [Theory]
    [InlineData("Recurse")]
    [InlineData("Depth")]
    public void Update_HasFolderWalkParams(string paramName)
    {
        // Update- variants on folder-scoped entities standardly accept
        // -Recurse and -Depth for the per-folder fan-out. Without these,
        // bulk update across many folders has to loop manually.
        var prop = typeof(UpdateTestSetScheduleCmdlet).GetProperty(paramName);
        Assert.NotNull(prop);
    }

    [Fact]
    public void TimeZoneId_UsesTimeZoneIdCompleter_NotTimeZoneCompleter()
    {
        // The two completer types differ in what they emit:
        //   TimeZoneCompleter   -> DisplayName ("(UTC+09:00) Osaka, Sapporo, Tokyo")
        //   TimeZoneIdCompleter -> Id          ("Tokyo Standard Time")
        // -TimeZoneId binds to the Id, so only TimeZoneIdCompleter is correct.
        // The matching completer on -TimeZone (the resolved-name variant)
        // is TimeZoneCompleter; that distinction is pinned here so a
        // future drive-by edit doesn't accidentally swap them.
        var prop = typeof(UpdateTestSetScheduleCmdlet).GetProperty("TimeZoneId");
        Assert.NotNull(prop);
        var completer = prop!.GetCustomAttribute<ArgumentCompleterAttribute>();
        Assert.NotNull(completer);
        Assert.Equal(typeof(UiPath.PowerShell.Completer.TimeZoneIdCompleter), completer!.Type);
    }

    [Fact]
    public void IsListedInModuleManifest()
    {
        var psd1 = LocateModuleManifest();
        var data = System.IO.File.ReadAllText(psd1);
        Assert.Contains("'Update-OrchTestSetSchedule'", data);
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
