using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// How Remove-OrchAssetUserValue names a user value in its -WhatIf / -Confirm target. The
// separator used to be unconditional, so a value with no machine came out as
// "[me@example.com\]" -- a trailing backslash that reads as an escaping bug rather than as the
// "user\machine" separator it is. Most per-user asset values are not machine-scoped, so that was
// the common case, not the corner one.
public class RemoveAssetUserValueTargetTests
{
    [Fact]
    public void MachineScopedValue_KeepsTheSeparator()
        => Assert.Equal(@"me@example.com\ROBOT01", RemoveAssetUserValueCmdlet.FormatUserValueTarget(
            new AssetUserValue { UserName = "me@example.com", MachineName = "ROBOT01" }));

    [Fact]
    public void UserOnlyValue_HasNoTrailingSeparator()
        => Assert.Equal("me@example.com", RemoveAssetUserValueCmdlet.FormatUserValueTarget(
            new AssetUserValue { UserName = "me@example.com", MachineName = null }));

    [Fact]
    public void EmptyMachineName_IsTreatedAsAbsent()
        => Assert.Equal("me@example.com", RemoveAssetUserValueCmdlet.FormatUserValueTarget(
            new AssetUserValue { UserName = "me@example.com", MachineName = "" }));

    [Fact]
    public void MissingUserName_DoesNotRenderNull()
        => Assert.Equal("", RemoveAssetUserValueCmdlet.FormatUserValueTarget(new AssetUserValue()));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NoMachine_NeverEndsWithABackslash(string? machine)
    {
        var s = RemoveAssetUserValueCmdlet.FormatUserValueTarget(
            new AssetUserValue { UserName = "u", MachineName = machine });
        Assert.False(s.EndsWith('\\'), $"target '{s}' ends with a bare separator");
    }
}
