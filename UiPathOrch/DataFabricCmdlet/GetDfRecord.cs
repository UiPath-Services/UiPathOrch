using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Shelved: kept internal so PowerShell module loader does not register the cmdlet.
// See GetDfEntity.cs for the full reasoning. Re-enable by switching `class` to
// `public class` and adding `Get-OrchDfRecord` to UiPathOrch.psd1 CmdletsToExport.
[Cmdlet(VerbsCommon.Get, "OrchDfRecord", DefaultParameterSetName = "All")]
[OutputType(typeof(PSObject))]
class GetDfRecordCmdlet : OrchestratorPSCmdlet
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

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df =>
            {
                var (drive, folder) = df;
                var records = new List<PSObject>();
                if (Id is not null)
                {
                    string body = drive.OrchAPISession.GetDfRecord(Entity, Id, folder.Id);
                    using var doc = JsonDocument.Parse(body);
                    records.Add(DfJsonTools.RecordToPSObject(doc.RootElement));
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
                    if (doc.RootElement.TryGetProperty("value", out var value))
                    {
                        foreach (var rec in value.EnumerateArray())
                        {
                            records.Add(DfJsonTools.RecordToPSObject(rec));
                        }
                    }
                }
                return records;
            });

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, 1, results.Count, "Getting Data Fabric records");
        foreach (var result in results)
        {
            try
            {
                var records = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (records is null) continue;
                WriteObject(records, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetDfRecordError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
