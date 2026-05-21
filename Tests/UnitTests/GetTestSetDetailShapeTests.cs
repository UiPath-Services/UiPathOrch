using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Locks the surface of Get-OrchTestSetDetail (added in v1.5.3) so the
// "Detail vs LIST" split doesn't silently regress. The cmdlet's whole
// reason to exist is making Get | New round-trip work for TestSets —
// without -Name mandatory + ValueFromPipelineByPropertyName, the
// documented `Get-OrchTestSetDetail X | New-OrchTestSet -Name Y` clone
// path collapses.
public class GetTestSetDetailShapeTests
{
    [Fact]
    public void DeclaresGetVerb_AndTestSetOutput()
    {
        var attr = typeof(GetTestSetDetailCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Get", attr.VerbName);
        Assert.Equal("OrchTestSetDetail", attr.NounName);
        // Detail is read-only; declaring SupportsShouldProcess would imply otherwise.
        Assert.False(attr.SupportsShouldProcess);

        var outputAttr = typeof(GetTestSetDetailCmdlet).GetCustomAttribute<OutputTypeAttribute>();
        Assert.NotNull(outputAttr);
        Assert.Contains(outputAttr.Type, t => t.Type == typeof(TestSet));
    }

    [Fact]
    public void Name_IsMandatoryAndPipelineBound()
    {
        // -Name mandatory by design: the cmdlet fans out one GetForEdit call
        // per matched TestSet, so a default "all" would be expensive. Plus
        // the pipeline-clone use case demands ValueFromPipelineByPropertyName.
        var prop = typeof(GetTestSetDetailCmdlet).GetProperty("Name");
        Assert.NotNull(prop);
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.True(attrs.Any(a => a.Mandatory),
            "Get-OrchTestSetDetail.Name must be Mandatory.");
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            "Get-OrchTestSetDetail.Name must accept ValueFromPipelineByPropertyName.");
    }

    [Fact]
    public void Path_AcceptsPipelineBinding()
    {
        var prop = typeof(GetTestSetDetailCmdlet).GetProperty("Path");
        Assert.NotNull(prop);
        Assert.True(prop!.GetCustomAttributes<ParameterAttribute>().Any(a => a.ValueFromPipelineByPropertyName),
            "Get-OrchTestSetDetail.Path must accept ValueFromPipelineByPropertyName so piping from another Get-Orch* works.");
    }

    [Fact]
    public void IsListedInModuleManifest()
    {
        var psd1 = LocateModuleManifest();
        var data = System.IO.File.ReadAllText(psd1);
        Assert.Contains("'Get-OrchTestSetDetail'", data);
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
