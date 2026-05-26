using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Locks the surface of Import-OrchTestDataQueueItem. It exists to upload the
// Orchestrator web "Upload Items" CSV format (header row = the queue's
// ContentJsonSchema property names) into a test data queue. The verb is
// Import (file -> bulk add), mirroring Import-OrchQueueItem; -Name and
// -ImportCsv are both mandatory because uploading needs a target queue and a
// source file.
public class ImportTestDataQueueItemShapeTests
{
    private static readonly System.Type Cmdlet = typeof(ImportTestDataQueueItemCmdlet);

    [Fact]
    public void DeclaresImportVerb_AndSupportsShouldProcess()
    {
        var attr = Cmdlet.GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("Import", attr!.VerbName);
        Assert.Equal("OrchTestDataQueueItem", attr.NounName);
        Assert.True(attr.SupportsShouldProcess,
            "Import-OrchTestDataQueueItem mutates the queue, so it must support ShouldProcess.");
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("ImportCsv")]
    public void Cmdlet_HasMandatoryPipelineParameter(string paramName)
    {
        var prop = Cmdlet.GetProperty(paramName);
        Assert.True(prop is not null, $"ImportTestDataQueueItemCmdlet missing -{paramName}.");
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.True(attrs.Any(a => a.Mandatory),
            $"ImportTestDataQueueItemCmdlet.{paramName} must be Mandatory.");
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            $"ImportTestDataQueueItemCmdlet.{paramName} must accept pipeline binding by property name.");
    }

    [Fact]
    public void HasPathParameter_ForFolderTargeting()
    {
        var prop = Cmdlet.GetProperty("Path");
        Assert.True(prop is not null, "ImportTestDataQueueItemCmdlet must expose -Path for folder targeting.");
        Assert.Equal(typeof(string[]), prop!.PropertyType);
    }

    [Fact]
    public void IsListedInModuleManifest()
    {
        Assert.Contains("'Import-OrchTestDataQueueItem'", ManifestText());
    }

    private static string ManifestText()
    {
        var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "Staging", "UiPathOrch.psd1");
            if (System.IO.File.Exists(candidate)) return System.IO.File.ReadAllText(candidate);
            dir = dir.Parent;
        }
        throw new System.IO.FileNotFoundException(
            "Staging/UiPathOrch.psd1 not found above " + System.AppContext.BaseDirectory);
    }
}
