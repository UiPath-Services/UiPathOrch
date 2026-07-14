using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace UnitTests;

// Guards the [ArgumentCompleter([...])] type literals in the SHIPPED PowerShell function cmdlets
// (Staging\Functions\*.ps1).
//
// Unlike a C# cmdlet's [ArgumentCompleter(typeof(X))], which the compiler checks, a PowerShell type
// literal is resolved at RUNTIME. When the completer classes stopped being generic and the
// UiPath.PowerShell.Positional.UserName type went away, four function cmdlets kept naming the old
// shape —
//
//     [ArgumentCompleter([UiPath.PowerShell.Completer.DriveCompleter[UiPath.PowerShell.Positional.UserName]])]
//
// — and nothing said a word. PowerShell cannot resolve that literal, so it cannot pick between
// ArgumentCompleterAttribute's one-argument constructors (Type / ScriptBlock /
// IArgumentCompleterFactory) and throws "Multiple ambiguous overloads found for .ctor". The effect
// was not a degraded completion but four cmdlets that could not be INVOKED at all
// (Enable/Disable-OrchUserAttended, Enable/Disable-OrchPersonalWorkspace) — shipped that way,
// because no test touched them.
//
// So: every completer named by a shipped function must resolve to a real, constructible completer
// type in the module assembly.
public class FunctionCompleterAttributeTests
{
    // Captures the type literal inside [ArgumentCompleter([...])].
    private static readonly Regex CompleterAttribute =
        new(@"\[ArgumentCompleter\(\s*\[(?<type>[^\]]*(?:\[[^\]]*\][^\]]*)*)\]\s*\)\]", RegexOptions.Compiled);

    private static string FunctionsDir
    {
        get
        {
            string root = typeof(FunctionCompleterAttributeTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .First(a => a.Key == "RepoRoot").Value!;
            return Path.GetFullPath(Path.Combine(root, "Staging", "Functions"));
        }
    }

    public static TheoryData<string, string> CompleterTypeLiterals()
    {
        var data = new TheoryData<string, string>();

        foreach (string file in Directory.GetFiles(FunctionsDir, "*.ps1"))
        {
            string text = File.ReadAllText(file);
            foreach (Match m in CompleterAttribute.Matches(text))
            {
                data.Add(Path.GetFileName(file), m.Groups["type"].Value.Trim());
            }
        }

        return data;
    }

    [Fact]
    public void The_shipped_functions_are_where_we_think_they_are()
    {
        Assert.True(Directory.Exists(FunctionsDir), $"not found: {FunctionsDir}");
        Assert.NotEmpty(Directory.GetFiles(FunctionsDir, "*.ps1"));

        // The regex has to actually find something, or this whole test file passes vacuously.
        Assert.NotEmpty(CompleterTypeLiterals());
    }

    [Theory]
    [MemberData(nameof(CompleterTypeLiterals))]
    public void Every_completer_a_function_names_is_a_real_completer_type(string file, string typeLiteral)
    {
        // No completer in this assembly is generic, so a type literal carrying type arguments names
        // a shape that no longer exists. This is the exact break that shipped: DriveCompleter[UserName].
        Assert.False(typeLiteral.Contains('['),
            $"{file}: [ArgumentCompleter([{typeLiteral}])] passes type arguments, but no completer is " +
            "generic. PowerShell cannot resolve this literal and the cmdlet becomes uncallable.");

        var type = typeof(UiPath.PowerShell.Completer.DriveCompleter).Assembly.GetType(typeLiteral);

        Assert.True(type is not null, $"{file}: [ArgumentCompleter([{typeLiteral}])] names a type that does not exist.");

        // PowerShell will construct it through the parameterless ctor and call it as a completer;
        // a type that is abstract, or isn't one, fails only at <Tab> time — catch it here.
        Assert.True(typeof(System.Management.Automation.IArgumentCompleter).IsAssignableFrom(type),
            $"{file}: {typeLiteral} is not an IArgumentCompleter.");
        Assert.False(type!.IsAbstract, $"{file}: {typeLiteral} is abstract and cannot be constructed.");
        Assert.True(type.GetConstructor(Type.EmptyTypes) is not null,
            $"{file}: {typeLiteral} has no parameterless constructor.");
    }
}
