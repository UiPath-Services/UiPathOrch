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
public class GetPmGroupMemberCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

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
        var drives = SessionState.EnumPmDrives(Path);
        var wpGroupName = GroupName.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            var groups = drive.PmGroups.Get()
                .Where(g => g is not null)
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g.name);

            foreach (var group in groups.WithCancellation(cancelHandler.Token))
            {
                try
                {
                    var detailedGroup = drive.PmGroups.Get(group.id);
                    if (detailedGroup is null) continue;

                    if (writer is not null)
                    {
                        WriteCsvContent(writer, detailedGroup, drive.NameColonSeparator);
                    }
                    else
                    {
                        if (detailedGroup.members is null) continue;

                        // PmGroupMember is org-shared (PmGroups cache is keyed by
                        // organization); attach drive-local Path and PathGroupName as
                        // PSObject note properties per emit. PathGroupName drives the
                        // ps1xml GroupBy ("Group: Orch1:\<GroupName>" header).
                        string pathGroupName = System.IO.Path.Combine(drive.NameColonSeparator, detailedGroup.name ?? "");
                        WriteObject(detailedGroup.members
                            .OrderBy(m => m.name)
                            .Select(m => m
                                .WithPath(drive.NameColonSeparator)
                                ?.WithNoteProperty("PathGroupName", pathGroupName)),
                            true);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(group.GetPSPath(drive.NameColonSeparator), ex), "GetPmGroupMemberError", ErrorCategory.InvalidOperation, group));
                }
            }

            WriteCSVExportedMessage(this, providerCsvPath);
        }
    }
}
