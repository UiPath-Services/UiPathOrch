using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Reads what the cmdlets need out of a `.nupkg`: the dependency list from the `.nuspec`, the
/// workflows the package carries, and which of those workflows are entry points.
///
/// Orchestrator has no endpoint for any of it, so the only way to learn them is the one the
/// web UI's "Explore package" takes: download the whole package and look inside. The archive
/// is read once and only these small lists are kept (see OrchDriveInfo.PackageContents) — the
/// bytes themselves are worth nothing to cache, and the two manifests are a kilobyte or two.
///
/// Pure and static so the parsing is unit-testable without a live Orchestrator.
/// </summary>
internal static class PackageContentsReader
{
    /// <summary>
    /// Opens the package and returns its dependencies and workflows. Throws when the bytes are
    /// not a readable package, so the caller (and its exception cache) treats that like any
    /// other fetch failure.
    /// </summary>
    internal static PackageContents ReadContents(byte[] packageBytes, string packageId, string packageVersion)
    {
        using var stream = new MemoryStream(packageBytes, writable: false);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

        var contents = new PackageContents();

        // The manifest is "<id>.nuspec" at the archive root. Match on the extension rather than
        // the id: a package renamed after packing keeps the old manifest name.
        var nuspec = zip.Entries.FirstOrDefault(e =>
            e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) &&
            !e.FullName.Contains('/'));

        if (nuspec is not null)
        {
            using var nuspecStream = nuspec.Open();
            contents.Dependencies = ReadDependencies(XDocument.Load(nuspecStream), packageId, packageVersion);
        }

        // project.json names the main workflow and the published entry points. It sits beside
        // the project, under the same packing prefix as the .xaml files.
        var projectJson = zip.Entries.FirstOrDefault(e =>
            TrimPackagingPrefix(e.FullName).Equals("project.json", StringComparison.OrdinalIgnoreCase));

        (string? main, HashSet<string> entryPoints) project = (null, new(StringComparer.OrdinalIgnoreCase));
        if (projectJson is not null)
        {
            using var projectStream = projectJson.Open();
            project = ReadProject(projectStream);
        }

        contents.Workflows = ReadWorkflows(zip, packageId, packageVersion, project.main, project.entryPoints);
        return contents;
    }

    /// <summary>
    /// Every `.xaml` in the package, with the path it has inside the project, flagged with
    /// whether the project publishes it as an entry point and whether it is the main one.
    /// </summary>
    internal static List<PackageWorkflow> ReadWorkflows(
        ZipArchive zip, string packageId, string packageVersion, string? main, HashSet<string> entryPoints) =>
        zip.Entries
            .Where(e => e.FullName.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .Select(e =>
            {
                string workflow = TrimPackagingPrefix(e.FullName);
                return new PackageWorkflow
                {
                    Package = packageId,
                    Version = packageVersion,
                    Workflow = workflow,
                    Length = e.Length,
                    IsMain = main is not null && SamePath(workflow, main),
                    // A project with no project.json (or none listing entry points) still has a
                    // main; treat that one as the entry point rather than reporting none.
                    IsEntryPoint = entryPoints.Count > 0
                        ? entryPoints.Any(p => SamePath(workflow, p))
                        : main is not null && SamePath(workflow, main),
                };
            })
            .OrderBy(w => w.Workflow, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Reads `main` and the `entryPoints[].filePath` list out of project.json. Returns nulls /
    /// an empty set for a project.json that is missing, unreadable or shaped differently — the
    /// workflow list is still worth returning without the flags.
    /// </summary>
    internal static (string? Main, HashSet<string> EntryPoints) ReadProject(Stream projectJson)
    {
        var entryPoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(projectJson);
            var root = doc.RootElement;

            string? main = root.TryGetProperty("main", out var mainElement) && mainElement.ValueKind == JsonValueKind.String
                ? mainElement.GetString()
                : null;

            if (root.TryGetProperty("entryPoints", out var eps) && eps.ValueKind == JsonValueKind.Array)
            {
                foreach (var ep in eps.EnumerateArray())
                {
                    if (ep.ValueKind == JsonValueKind.Object &&
                        ep.TryGetProperty("filePath", out var filePath) &&
                        filePath.ValueKind == JsonValueKind.String &&
                        filePath.GetString() is string value && value.Length > 0)
                    {
                        entryPoints.Add(value);
                    }
                }
            }

            return (main, entryPoints);
        }
        catch (JsonException)
        {
            return (null, entryPoints);
        }
    }

    /// <summary>Drops the `lib/&lt;framework&gt;/` or `content/` wrapper NuGet packing adds.</summary>
    internal static string TrimPackagingPrefix(string entryPath)
    {
        var parts = entryPath.Split('/');

        if (parts.Length > 2 && parts[0].Equals("lib", StringComparison.OrdinalIgnoreCase))
            return string.Join('/', parts.Skip(2));

        if (parts.Length > 1 && parts[0].Equals("content", StringComparison.OrdinalIgnoreCase))
            return string.Join('/', parts.Skip(1));

        return entryPath;
    }

    /// <summary>
    /// project.json writes a project-relative path with Windows separators ("Sub\Flow.xaml");
    /// the archive uses forward slashes. Compare with both normalised.
    /// </summary>
    private static bool SamePath(string? a, string? b) =>
        a is not null && b is not null &&
        a.Replace('\\', '/').TrimStart('.', '/').Equals(b.Replace('\\', '/').TrimStart('.', '/'), StringComparison.OrdinalIgnoreCase);

    /// <summary>Parses an already-loaded `.nuspec` document. Separated for the unit tests.</summary>
    internal static List<PackageDependency> ReadDependencies(XDocument nuspec, string packageId, string packageVersion)
    {
        // .nuspec documents carry one of several NuGet schema namespaces (and some carry none),
        // so every lookup goes by local name.
        var metadata = nuspec.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "metadata");
        var dependencies = metadata?.Elements().FirstOrDefault(e => e.Name.LocalName == "dependencies");
        if (dependencies is null) return [];

        var result = new List<PackageDependency>();

        // Two shapes are legal: <dependency> directly under <dependencies>, and <group
        // targetFramework="..."> wrapping them. Studio writes the grouped form; older packages
        // and hand-written manifests use the flat one, and a document may mix both.
        foreach (var group in dependencies.Elements().Where(e => e.Name.LocalName == "group"))
        {
            string? framework = group.Attribute("targetFramework")?.Value;
            foreach (var dependency in group.Elements().Where(e => e.Name.LocalName == "dependency"))
            {
                Add(dependency, framework);
            }
        }

        foreach (var dependency in dependencies.Elements().Where(e => e.Name.LocalName == "dependency"))
        {
            Add(dependency, null);
        }

        return result;

        void Add(XElement dependency, string? framework)
        {
            var id = dependency.Attribute("id")?.Value;
            if (string.IsNullOrEmpty(id)) return;

            result.Add(new PackageDependency
            {
                Package = packageId,
                Version = packageVersion,
                Dependency = id,
                // NuGet allows an absent version attribute, meaning "any version".
                Range = dependency.Attribute("version")?.Value,
                TargetFramework = framework,
            });
        }
    }
}
