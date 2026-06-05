using System.Collections;
using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Shelved: kept internal so PowerShell module loader does not register the cmdlet.
// See GetDfEntity.cs for the full reasoning. Re-enable by switching `class` to
// `public class` and adding `Invoke-OrchDfQuery` to UiPathOrch.psd1 CmdletsToExport.
[Cmdlet(VerbsLifecycle.Invoke, "OrchDfQuery")]
[OutputType(typeof(PSObject))]
class InvokeDfQueryCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DfEntityNameCompleter))]
    public string Entity { get; set; } = "";

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    public Hashtable? Filter { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string[]? SelectedFields { get; set; }

    // Sort spec: field name for ascending, prefix with '-' for descending (e.g., "-CreateTime").
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string[]? Sort { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int Start { get; set; } = 0;

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int Limit { get; set; } = 100;

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth, includeRoot: true);
        var filterGroup = BuildFilterGroup();

        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var payload = new Dictionary<string, object?>
                {
                    ["filterGroup"] = filterGroup,
                    ["start"] = Start,
                    ["limit"] = Limit,
                };
                if (SelectedFields is { Length: > 0 })
                {
                    payload["selectedFields"] = SelectedFields;
                }
                if (Sort is { Length: > 0 })
                {
                    payload["sortOptions"] = Sort.Select(BuildSortOption).ToArray();
                }

                string body = drive.OrchAPISession.QueryDfEntity(Entity, payload, folder.Id);
                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("value", out var value)) continue;

                foreach (var rec in value.EnumerateArray())
                {
                    WriteObject(DfJsonTools.RecordToPSObject(rec));
                }
            }
            catch (System.Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "InvokeDfQueryError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }

    // Accept the user-supplied hashtable verbatim, but normalize it into types JsonSerializer
    // can handle. If no filter is supplied, emit the empty AND group the API requires.
    private object BuildFilterGroup()
    {
        if (Filter is null)
        {
            return new Dictionary<string, object?>
            {
                ["logicalOperator"] = 0,
                ["queryFilters"] = System.Array.Empty<object>(),
                ["filterGroups"] = System.Array.Empty<object>(),
            };
        }
        return DfJsonTools.ToJsonPayload(Filter)!;
    }

    private static Dictionary<string, object?> BuildSortOption(string spec)
    {
        bool desc = false;
        string field = spec;
        if (field.StartsWith('-'))
        {
            desc = true;
            field = field[1..];
        }
        return new Dictionary<string, object?>
        {
            ["fieldName"] = field,
            ["isDescending"] = desc,
        };
    }
}
