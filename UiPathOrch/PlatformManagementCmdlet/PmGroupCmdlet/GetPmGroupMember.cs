using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmGroupMember")]
[OutputType(typeof(Entities.DirectoryUser))]
[OutputType(typeof(Entities.DirectoryGroup))]
[OutputType(typeof(Entities.DirectoryRobotUser))]
[OutputType(typeof(Entities.DirectoryApplication))]
public class GetPmGroupMemberCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private static readonly string DefaultCsvName = "ExportedPmGroups.csv";
    private static readonly string[] CsvHeaders = ["Path", "GroupName", "Type", "UserName", "Email", "Source"];

    private static void WriteCsvContent(StreamWriter writer, PmGroup group, string drivePath)
    {
        // Write data rows for each group
        if (group?.members is null) return;

        foreach (var member in group.members
            .OrderBy(m => m.groupName)
            .ThenBy(m => m.objectType)
            .ThenBy(m => m.name))
        {
            string[] line = [
                EscapeCsvValue(drivePath, true),
                EscapeCsvValue(member.groupName, true),
                EscapeCsvValue(member.objectType), ////////// TODO: Does this need conversion?
                EscapeCsvValue(member.name),
                member.email ?? "",
                member.source ?? ""
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath));
        var wpGroupName = GroupName.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        // Two-phase parallel fetch (see Get-OrchUserDetail for the canonical
        // shape). Phase 1: per-drive group list + name filter. Phase 2: the
        // per-group detail fetch (PmGroups.Get(id)). Both phases share a
        // single cap=4 semaphore so total in-flight API calls stay bounded;
        // per-org caches still serialize same-partition fetches internally.
        // Emission stays on the pipeline thread.
        using var cancelHandler = new ConsoleCancelHandler();

        using var pool = OrchThreadPool.RunForEachChained(
            drives,
            drive => drive.NameColonSeparator,
            drive => (object)drive,
            drive => drive.PmGroups.Get()
                .Where(g => g is not null)
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g.name)
                .Select(group => (drive, group)),
            t => t.group.GetPSPath(t.drive.NameColonSeparator),
            t => (object)t.group,
            t => t.drive.PmGroups.Get(t.group.id),
            cancelHandler.Token);

        foreach (var task in pool)
        {
            try
            {
                var detailedGroup = task.GetResult(cancelHandler.Token);
                if (detailedGroup is null) continue;

                var (drive, _) = task.Source;
                if (writer is not null)
                {
                    WriteCsvContent(writer, detailedGroup, drive.NameColonSeparator);
                }
                else
                {
                    if (detailedGroup.members is null) continue;

                    // PmGroupMember is org-shared (PmGroups cache is keyed by
                    // organization); emit a per-emit shallow copy carrying the
                    // drive-local Path / PathGroupName. PathGroupName drives the
                    // ps1xml GroupBy ("Group: Orch1:\<GroupName>" header).
                    string pathGroupName = System.IO.Path.Combine(drive.NameColonSeparator, detailedGroup.name ?? "");
                    WriteObject(detailedGroup.members
                        .OrderBy(m => m.name)
                        .Select(m => { var c = m.ShallowClone(); c.Path = drive.NameColonSeparator; c.PathGroupName = pathGroupName; return c; }),
                        true);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPmGroupMemberError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        // Phase 1 (per-drive group-list) failures, same ErrorId as Phase 2.
        foreach (var (_, ex) in pool.Phase1Errors)
        {
            WriteError(new ErrorRecord(ex, "GetPmGroupMemberError", ErrorCategory.InvalidOperation, ex.Target));
        }

        if (!string.IsNullOrEmpty(ExportCsv))
        {
            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
