using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using Xunit;

namespace UnitTests;

// Validates the reflection-based switch-parameter detection that replaced the
// hand-maintained knownSwitchParameters whitelist. The semantics under test:
//
// 1. A SwitchParameter declared on a cmdlet is detected as a switch when used
//    against that cmdlet's name.
// 2. A non-switch (value-bound) parameter is NOT detected as a switch — even if
//    the user typed `-Path` without yet typing a value.
// 3. PowerShell common parameters (Verbose / Debug / WhatIf / Confirm) are
//    detected as switches on any cmdlet.
// 4. An unknown parameter name (typo, future param) returns false.
public class OrchCompleterSwitchDetectionTests
{
    private static CommandAst ParseFirstCommand(string input)
    {
        var sba = Parser.ParseInput(input, out _, out _);
        return sba.FindAll(a => a is CommandAst, true).Cast<CommandAst>().First();
    }

    [Theory]
    [InlineData("Get-OrchAsset -Recurse", "Recurse", true)]
    [InlineData("Remove-OrchAsset -Recurse", "Recurse", true)]
    [InlineData("Get-OrchPackageVersion -Recurse", "Recurse", true)]
    public void Recurse_DetectedOnCmdletsThatDeclareIt(string input, string param, bool expected)
    {
        var ast = ParseFirstCommand(input);
        Assert.Equal(expected, OrchArgumentCompleter.GetSwitchParameterValue(ast, param));
    }

    [Theory]
    [InlineData("Get-OrchAsset", "Recurse")]      // switch declared, but not present in input
    [InlineData("Get-OrchAsset -Path foo", "Path")] // Path is value-bound, not a switch
    public void NotPresent_OrNotSwitch_ReturnsFalse(string input, string param)
    {
        var ast = ParseFirstCommand(input);
        Assert.False(OrchArgumentCompleter.GetSwitchParameterValue(ast, param));
    }

    [Theory]
    [InlineData("Get-OrchAsset -Verbose", "Verbose")]
    [InlineData("Get-OrchAsset -Debug", "Debug")]
    [InlineData("Remove-OrchAsset -WhatIf", "WhatIf")]
    [InlineData("Remove-OrchAsset -Confirm", "Confirm")]
    public void CommonSwitches_DetectedOnAnyCmdlet(string input, string param)
    {
        var ast = ParseFirstCommand(input);
        Assert.True(OrchArgumentCompleter.GetSwitchParameterValue(ast, param));
    }

    [Fact]
    public void UnknownParameterName_ReturnsFalse()
    {
        var ast = ParseFirstCommand("Get-OrchAsset -ThisDoesNotExist");
        Assert.False(OrchArgumentCompleter.GetSwitchParameterValue(ast, "ThisDoesNotExist"));
    }

    // GetParameterValue must not let a switch swallow the following positional argument.
    // Remove-OrchAsset: Name=pos0, ValueType=pos1, Recurse=switch.
    private static readonly string[] AssetPositionals = { "Name", "ValueType" };

    [Fact]
    public void GetParameterValue_ColonSwitchBeforePositional_PositionalResolved()
    {
        // The bug: "-Recurse:$true" has a non-null AST Argument, so GetSwitchParameterValue
        // reported it as a value-bound param and the positional "MyAsset" was eaten as its value.
        var ast = ParseFirstCommand("Remove-OrchAsset -Recurse:$true MyAsset");
        Assert.Equal("MyAsset", OrchArgumentCompleter.GetParameterValue(ast, "Name", AssetPositionals));
    }

    [Fact]
    public void GetParameterValue_BareSwitchBeforePositional_PositionalResolved()
    {
        var ast = ParseFirstCommand("Remove-OrchAsset -Recurse MyAsset");
        Assert.Equal("MyAsset", OrchArgumentCompleter.GetParameterValue(ast, "Name", AssetPositionals));
    }

    [Fact]
    public void GetParameterValue_NamedValue_SpaceAndColonForms()
    {
        Assert.Equal("Text", OrchArgumentCompleter.GetParameterValue(
            ParseFirstCommand("Remove-OrchAsset -ValueType Text"), "ValueType"));
        Assert.Equal("Text", OrchArgumentCompleter.GetParameterValue(
            ParseFirstCommand("Remove-OrchAsset -ValueType:Text"), "ValueType"));
    }

    [Fact]
    public void SwitchWithExplicitValue_ReturnsFalse()
    {
        // -Recurse:false — colon syntax means Argument is non-null in the AST,
        // so the walker treats it as a value-bound use, not a "switch is on" use.
        var ast = ParseFirstCommand("Get-OrchAsset -Recurse:$false");
        Assert.False(OrchArgumentCompleter.GetSwitchParameterValue(ast, "Recurse"));
    }

    // Sanity check: every name from the legacy hardcoded knownSwitchParameters list
    // resolves to true for at least one cmdlet that actually declares it. If any of
    // these regress, a real switch parameter is being mis-detected as non-switch.
    [Theory]
    [InlineData("Get-OrchAsset -Recurse", "Recurse")]
    [InlineData("Get-OrchLibrary -HostFeed", "HostFeed")]
    [InlineData("Get-OrchFolderUser -IncludeInherited", "IncludeInherited")]
    [InlineData("Get-OrchTrigger -Recurse", "Recurse")]
    [InlineData("Remove-OrchUser -NoMatchWarning", "NoMatchWarning")]
    public void LegacyHardcodedNames_StillDetected(string input, string param)
    {
        var ast = ParseFirstCommand(input);
        Assert.True(OrchArgumentCompleter.GetSwitchParameterValue(ast, param));
    }
}
