using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchUnattendedSession")]
[OutputType(typeof(Entities.MachineSessionRuntime))]
public class GetUnattendedSessionCmdlet : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0)]
    //[ArgumentCompleter(typeof(StaticTextsCompleter<Hour_Day_Week_Month_3Month_6Month_Year_3Year>))]
    //[ValidateStaticCandidate<Hour_Day_Week_Month_3Month_6Month_Year_3Year>]
    //public string? Last { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(TimeAfterCompleter))]
    //public DateTime? ReportingTimeAfter { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(TimeBeforeCompleter))]
    //public DateTime? ReportingTimeBefore { get; set; }

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<MachineSessionStatusItems>))]
    [SupportsWildcards]
    public string[]? Status { get; set; }

    //[Parameter]
    //public ulong? Skip { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    //public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpStatus = Status.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.MachineSessionRuntimes.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var entities = result.GetResult(cancelHandler.Token);
                if (entities is null) continue;

                WriteObject(entities
                    .FilterByWildcards(s => s?.Status, wpStatus), true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetMachineSessionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        //foreach (var (drive, folder) in drivesFolders)
        //{
        //    try
        //    {
        //        var sessions = drive.OrchAPISession.GetMachineSessionRuntimesByFolderId(folder.Id ?? 0);
        //        foreach (var session in sessions)
        //        {
        //            session.Path = folder.GetPSPath();
        //            WriteObject(session);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "UpdateUserError", ErrorCategory.InvalidOperation, folder));
        //    }
        //}



    }
}
