using System.Reflection;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// The views in Staging/UiPathOrch.Format.ps1xml are selected by type name, which nothing checks
// at build time: a renamed or removed class leaves its view silently unused. Every UiPathOrch
// type a view names must exist, and an added view type name ("Base#Purpose") must be one the code
// adds (ViewTypeNames) and have a view, on a base type that exists.
public class FormatViewTypeNameTests
{
    private static string FormatFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Staging", "UiPathOrch.Format.ps1xml");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Staging/UiPathOrch.Format.ps1xml not found above " + AppContext.BaseDirectory);
    }

    private static List<string> ViewTypeNamesInFormat()
        => Regex.Matches(File.ReadAllText(FormatFile()), @"<TypeName>\s*([^<]+?)\s*</TypeName>")
            .Select(m => m.Groups[1].Value)
            .Where(n => n.StartsWith("UiPath.PowerShell.", StringComparison.Ordinal))
            .Distinct()
            .ToList();

    private static readonly Assembly Module = typeof(OrchTask).Assembly;

    private static HashSet<string> AddedNames()
        => typeof(ViewTypeNames).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // PowerShell selects a view by type name ignoring case, so this does too ("PMGroup" selected
    // PmGroup's view); a name no type answers to under any casing is a view nothing uses.
    private static bool TypeExists(string name) => Module.GetType(name, throwOnError: false, ignoreCase: true) is not null;

    [Fact]
    public void Every_type_a_view_names_exists()
    {
        var added = AddedNames();
        var missing = ViewTypeNamesInFormat()
            .Where(n => n.Contains('#') ? !added.Contains(n) || !TypeExists(n.Split('#')[0])
                                        : !TypeExists(n))
            .ToList();
        Assert.True(missing.Count == 0, "Views name types that do not exist: " + string.Join(", ", missing));
    }

    [Fact]
    public void Every_added_type_name_has_a_view()
    {
        var inFormat = ViewTypeNamesInFormat().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unused = AddedNames().Where(n => !inFormat.Contains(n)).ToList();
        Assert.True(unused.Count == 0, "Added type names without a view: " + string.Join(", ", unused));
    }
}
