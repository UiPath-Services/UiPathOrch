using System.Linq;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Locks the surface of New-OrchWebhook (added in v1.5.3). The cmdlet's
// reason to exist is closing the gap left by Update-OrchWebhook existing
// without a New- counterpart; the parameter list mirrors Update- so the
// CSV emitted by Get-OrchWebhook -ExportCsv re-imports into either
// cmdlet. These tests pin that 1:1 contract.
public class NewWebhookShapeTests
{
    [Fact]
    public void DeclaresNewVerb_AndWebhookOutput_AndSupportsShouldProcess()
    {
        var attr = typeof(NewWebhookCmdlet).GetCustomAttribute<CmdletAttribute>();
        Assert.NotNull(attr);
        Assert.Equal("New", attr.VerbName);
        Assert.Equal("OrchWebhook", attr.NounName);
        Assert.True(attr.SupportsShouldProcess);

        var outputAttr = typeof(NewWebhookCmdlet).GetCustomAttribute<OutputTypeAttribute>();
        Assert.NotNull(outputAttr);
        Assert.Contains(outputAttr.Type, t => t.Type == typeof(Webhook));
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("Url")]
    public void Cmdlet_HasMandatoryNameAndUrl(string paramName)
    {
        // Without -Url, the webhook is useless (the server would fire it
        // against an empty endpoint). -Name is the identifier.
        var prop = typeof(NewWebhookCmdlet).GetProperty(paramName);
        Assert.NotNull(prop);
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.NotEmpty(attrs);
        Assert.True(attrs.Any(a => a.Mandatory),
            $"NewWebhookCmdlet.{paramName} must be Mandatory.");
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            $"NewWebhookCmdlet.{paramName} must accept pipeline binding so CSV import works.");
    }

    public static System.Collections.Generic.IEnumerable<object[]> EveryUpdateParamMustExistOnNew()
    {
        // Update-OrchWebhook parameters that aren't structural cmdlet metadata
        // (Confirm, WhatIf, Verbose, Debug, etc.) — every one must be present
        // on New-OrchWebhook so a CSV exported from one can re-import into
        // either cmdlet.
        var names = new[] { "Name", "Url", "Description", "Secret", "Enabled", "AllowInsecureSsl", "SubscribeToAllEvents", "Events", "Path" };
        foreach (var n in names) yield return new object[] { n };
    }

    [Theory]
    [InlineData(typeof(NewWebhookCmdlet))]
    [InlineData(typeof(UpdateWebhookCmdlet))]
    public void Events_SupportsWildcards_AndHasEventTypeCompleter(System.Type cmdletType)
    {
        // -Events is string[] with wildcard expansion against the live
        // event-type list (verified on Orch1 2026-05-22); the completer
        // surfaces valid values. Both New- and Update- expose it identically.
        var prop = cmdletType.GetProperty("Events",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.True(prop is not null, $"{cmdletType.Name} must expose -Events.");
        Assert.Equal(typeof(string[]), prop!.PropertyType);
        Assert.NotNull(prop.GetCustomAttribute<SupportsWildcardsAttribute>());

        var completer = prop.GetCustomAttribute<ArgumentCompleterAttribute>();
        Assert.NotNull(completer);
        Assert.Equal("WebhookEventTypeNameCompleter", completer!.Type?.Name);
    }

    [Theory]
    [MemberData(nameof(EveryUpdateParamMustExistOnNew))]
    public void Parameter_ExistsOnNew_AndAcceptsPipelineBinding(string paramName)
    {
        // DeclaredOnly: -Events shares its name with a non-parameter member
        // on a base class, which makes the simple GetProperty(name) overload
        // throw AmbiguousMatchException. We only care about the cmdlet's own
        // declared parameter here.
        var prop = typeof(NewWebhookCmdlet).GetProperty(paramName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.True(prop is not null,
            $"NewWebhookCmdlet missing -{paramName} — round-trip from `Get-OrchWebhook -ExportCsv | Import-Csv | New-OrchWebhook` would silently lose the value.");
        var attrs = prop!.GetCustomAttributes<ParameterAttribute>().ToList();
        Assert.True(attrs.Any(a => a.ValueFromPipelineByPropertyName),
            $"NewWebhookCmdlet.{paramName} must accept ValueFromPipelineByPropertyName.");
    }

    [Fact]
    public void IsListedInModuleManifest()
    {
        var psd1 = LocateModuleManifest();
        var data = System.IO.File.ReadAllText(psd1);
        Assert.Contains("'New-OrchWebhook'", data);
    }

    [Fact]
    public void Url_HasNoCompleter_NotEnumerableValue()
    {
        // -Url is a free-form HTTP URL; an ArgumentCompleter that emits
        // candidates would be misleading (every value the completer
        // suggests would mean "this URL is suggested" which it isn't).
        // No completer is the correct shape. If a future change attaches
        // one, this test fires and demands a justification.
        var prop = typeof(NewWebhookCmdlet).GetProperty("Url");
        var completer = prop?.GetCustomAttribute<ArgumentCompleterAttribute>();
        Assert.Null(completer);
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
