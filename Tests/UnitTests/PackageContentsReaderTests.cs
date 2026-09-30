using System.Text;
using System.Xml.Linq;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Pins PackageContentsReader, the parsing behind Get-Orch{Package,Process}{Dependency,Workflow}.
// Orchestrator has no endpoint for a package's dependencies or its workflows, so both are read
// out of the downloaded .nupkg: dependencies from the .nuspec, workflows from the archive
// entries, entry points from project.json. These tests pin the shapes met in real packages --
// namespaced and bare manifests, grouped and flat dependency lists, the lib/<framework>/
// packing prefix, and project.json's Windows-separator paths.
public class PackageContentsReaderTests
{
    private static XDocument Nuspec(string inner) => XDocument.Parse(
        $"<package xmlns=\"http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd\"><metadata>{inner}</metadata></package>");

    [Fact]
    public void GroupedDependencies_AreReadWithTheirTargetFramework()
    {
        var doc = Nuspec("""
            <dependencies>
              <group targetFramework=".NETFramework4.6.1">
                <dependency id="UiPath.System.Activities" version="[23.10.3]" />
                <dependency id="UiPath.UIAutomation.Activities" version="[23.10.4]" />
              </group>
            </dependencies>
            """);

        var deps = PackageContentsReader.ReadDependencies(doc, "Pkg", "1.0.0");

        Assert.Equal(2, deps.Count);
        Assert.Equal("UiPath.System.Activities", deps[0].Dependency);
        Assert.Equal("[23.10.3]", deps[0].Range);
        Assert.Equal(".NETFramework4.6.1", deps[0].TargetFramework);
        Assert.Equal("Pkg", deps[0].Package);
        Assert.Equal("1.0.0", deps[0].Version);
    }

    [Fact]
    public void FlatDependencies_AreReadToo()
    {
        // Older packages and hand-written manifests do not group.
        var doc = Nuspec("""<dependencies><dependency id="UiPath.Excel.Activities" version="2.20.2" /></dependencies>""");

        var dep = Assert.Single(PackageContentsReader.ReadDependencies(doc, "Pkg", "1.0.0"));
        Assert.Equal("UiPath.Excel.Activities", dep.Dependency);
        Assert.Null(dep.TargetFramework);
    }

    [Fact]
    public void ManifestWithoutANamespace_IsReadTheSame()
    {
        var doc = XDocument.Parse("""
            <package><metadata><dependencies><dependency id="A" version="1.0.0" /></dependencies></metadata></package>
            """);

        Assert.Single(PackageContentsReader.ReadDependencies(doc, "Pkg", "1.0.0"));
    }

    [Fact]
    public void DependencyWithoutAVersion_KeepsANullRange()
    {
        // NuGet reads an absent version attribute as "any version".
        var doc = Nuspec("""<dependencies><dependency id="A" /></dependencies>""");

        Assert.Null(Assert.Single(PackageContentsReader.ReadDependencies(doc, "Pkg", "1.0.0")).Range);
    }

    [Fact]
    public void NoDependenciesElement_YieldsNoRows()
    {
        Assert.Empty(PackageContentsReader.ReadDependencies(Nuspec("<id>Pkg</id>"), "Pkg", "1.0.0"));
    }

    [Theory]
    [InlineData("lib/net45/Main.xaml", "Main.xaml")]
    [InlineData("lib/net461/Sub/Flow.xaml", "Sub/Flow.xaml")]
    [InlineData("content/Main.xaml", "Main.xaml")]
    [InlineData("Main.xaml", "Main.xaml")]
    public void PackagingPrefix_IsTrimmedToTheProjectPath(string entry, string expected)
    {
        Assert.Equal(expected, PackageContentsReader.TrimPackagingPrefix(entry));
    }

    [Fact]
    public void Project_ReadsMainAndEntryPoints()
    {
        var json = """
            {
              "name": "Pkg",
              "main": "Main.xaml",
              "entryPoints": [
                { "filePath": "Main.xaml", "uniqueId": "1" },
                { "filePath": "Sub\\Flow.xaml", "uniqueId": "2" }
              ]
            }
            """;

        var (main, entryPoints) = PackageContentsReader.ReadProject(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        Assert.Equal("Main.xaml", main);
        Assert.Equal(2, entryPoints.Count);
        Assert.Contains("Sub\\Flow.xaml", entryPoints);
    }

    [Fact]
    public void Project_ThatIsNotJson_YieldsNothingRatherThanThrowing()
    {
        // A workflow list without the flags is still worth returning.
        var (main, entryPoints) = PackageContentsReader.ReadProject(new MemoryStream(Encoding.UTF8.GetBytes("not json")));

        Assert.Null(main);
        Assert.Empty(entryPoints);
    }

    [Fact]
    public void Project_WithoutEntryPoints_YieldsOnlyMain()
    {
        var (main, entryPoints) = PackageContentsReader.ReadProject(
            new MemoryStream(Encoding.UTF8.GetBytes("""{ "main": "Main.xaml" }""")));

        Assert.Equal("Main.xaml", main);
        Assert.Empty(entryPoints);
    }

    private static byte[] BuildPackage(params (string Path, string Content)[] entries)
    {
        using var memory = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(memory, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(path).Open());
                writer.Write(content);
            }
        }
        return memory.ToArray();
    }

    [Fact]
    public void ReadContents_ReadsBothListsFromOneArchive()
    {
        var bytes = BuildPackage(
            ("Pkg.nuspec", """
                <package><metadata><dependencies><group targetFramework="net461">
                  <dependency id="UiPath.System.Activities" version="[23.10.3]" />
                </group></dependencies></metadata></package>
                """),
            ("lib/net45/project.json", """
                { "main": "Main.xaml", "entryPoints": [ { "filePath": "Main.xaml" }, { "filePath": "Sub\\Entry.xaml" } ] }
                """),
            ("lib/net45/Main.xaml", "<Activity />"),
            ("lib/net45/Sub/Entry.xaml", "<Activity />"),
            ("lib/net45/Sub/Helper.xaml", "<Activity />"),
            ("lib/net45/project.notes", "not a workflow"));

        var contents = PackageContentsReader.ReadContents(bytes, "Pkg", "1.0.0");

        Assert.Equal("UiPath.System.Activities", Assert.Single(contents.Dependencies).Dependency);

        Assert.Equal(["Main.xaml", "Sub/Entry.xaml", "Sub/Helper.xaml"], contents.Workflows.Select(w => w.Workflow));
        var main = contents.Workflows.Single(w => w.Workflow == "Main.xaml");
        Assert.True(main.IsMain);
        Assert.True(main.IsEntryPoint);
        // project.json writes "Sub\Entry.xaml"; the archive says "Sub/Entry.xaml".
        var entry = contents.Workflows.Single(w => w.Workflow == "Sub/Entry.xaml");
        Assert.True(entry.IsEntryPoint);
        Assert.False(entry.IsMain);
        var helper = contents.Workflows.Single(w => w.Workflow == "Sub/Helper.xaml");
        Assert.False(helper.IsEntryPoint);
    }

    [Fact]
    public void ReadContents_WithoutProjectJson_TreatsNothingAsAnEntryPoint()
    {
        var bytes = BuildPackage(
            ("Pkg.nuspec", "<package><metadata /></package>"),
            ("lib/net45/Main.xaml", "<Activity />"));

        var workflow = Assert.Single(PackageContentsReader.ReadContents(bytes, "Pkg", "1.0.0").Workflows);

        Assert.Equal("Main.xaml", workflow.Workflow);
        Assert.False(workflow.IsEntryPoint);
        Assert.False(workflow.IsMain);
        Assert.True(workflow.Length > 0);
    }
}
