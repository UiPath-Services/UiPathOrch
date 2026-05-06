using System.IO;
using System.Linq;
using System.Text.Json;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Guards the per-locale UiPathOrchConfig.json templates that ship as embedded
// resources. Build alone does NOT validate them — the .csproj declares them as
// EmbeddedResource (raw bytes), so JSON syntax errors only surface when a user
// hits "Mount" and the OrchProvider extracts the template for their locale.
//
// This test parses every template with the same JsonSerializerOptions the
// runtime uses (OrchJsonConverter.jsonAllowComments) and additionally
// deserializes into UiPathOrchConfig to catch shape issues like an Edition
// value that doesn't match the OrchEdition enum.
public class EmbeddedConfigTemplateTests
{
    private static readonly JsonSerializerOptions RuntimeOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static string FindResourcesRoot()
    {
        // Walk up from the test assembly until we find the project's Resources dir.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "UiPathOrch", "Resources");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not locate UiPathOrch/Resources from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void EveryLocaleTemplateParsesAndDeserializes()
    {
        var resourcesRoot = FindResourcesRoot();
        var templates = Directory.EnumerateFiles(
            resourcesRoot, "UiPathOrchConfig.json", SearchOption.AllDirectories).ToList();

        Assert.NotEmpty(templates);  // Guard against the discovery silently matching nothing.

        var failures = new List<string>();
        foreach (var path in templates)
        {
            var locale = Path.GetFileName(Path.GetDirectoryName(path)!);
            var text = File.ReadAllText(path);

            // 1. Syntax check — surface a JSON-shaped error for malformed JSONC
            try
            {
                using var _ = JsonDocument.Parse(text, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                });
            }
            catch (JsonException ex)
            {
                failures.Add($"  {locale}: JSON parse failed at line {ex.LineNumber}, byte {ex.BytePositionInLine}: {ex.Message}");
                continue;
            }

            // 2. Schema check — the runtime deserializes into UiPathOrchConfig.
            // Catches things like Edition: "Bogus" (the Edition setter throws),
            // or a typed property receiving the wrong JSON kind.
            try
            {
                var cfg = JsonSerializer.Deserialize<UiPathOrchConfig>(text, RuntimeOptions);
                Assert.NotNull(cfg);
            }
            catch (Exception ex)
            {
                failures.Add($"  {locale}: deserialize to UiPathOrchConfig failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Assert.True(failures.Count == 0,
            "One or more embedded UiPathOrchConfig.json templates failed validation. " +
            "These ship as EmbeddedResource and are extracted at first Mount, so a " +
            "broken template only fails for the affected locale's users at runtime.\n\n" +
            string.Join("\n", failures));
    }
}
