using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchDfRecord", DefaultParameterSetName = "All")]
[OutputType(typeof(PSObject))]
public class GetDfRecordCommand : OrchestratorPSCmdlet
{
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DfEntityNameCompleter))]
    public string Entity { get; set; } = "";

    [Parameter(ParameterSetName = "ById", Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string? Id { get; set; }

    [Parameter(ParameterSetName = "All", ValueFromPipelineByPropertyName = true)]
    public string[]? SelectedFields { get; set; }

    [Parameter(ParameterSetName = "All", ValueFromPipelineByPropertyName = true)]
    public int Start { get; set; } = 0;

    [Parameter(ParameterSetName = "All", ValueFromPipelineByPropertyName = true)]
    public int Limit { get; set; } = 100;

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth, includeRoot: true);

        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                if (Id is not null)
                {
                    string body = drive.OrchAPISession.GetDfRecord(Entity, Id, folder.Id);
                    using var doc = JsonDocument.Parse(body);
                    WriteObject(DfJsonTools.RecordToPSObject(doc.RootElement));
                }
                else
                {
                    var payload = new Dictionary<string, object?>
                    {
                        ["filterGroup"] = new Dictionary<string, object?>
                        {
                            ["logicalOperator"] = 0,
                            ["queryFilters"] = System.Array.Empty<object>(),
                            ["filterGroups"] = System.Array.Empty<object>(),
                        },
                        ["start"] = Start,
                        ["limit"] = Limit,
                    };
                    if (SelectedFields is { Length: > 0 })
                    {
                        payload["selectedFields"] = SelectedFields;
                    }

                    string body = drive.OrchAPISession.QueryDfEntity(Entity, payload, folder.Id);
                    using var doc = JsonDocument.Parse(body);
                    if (!doc.RootElement.TryGetProperty("value", out var value)) continue;

                    foreach (var rec in value.EnumerateArray())
                    {
                        WriteObject(DfJsonTools.RecordToPSObject(rec));
                    }
                }
            }
            catch (System.Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetDfRecordError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
