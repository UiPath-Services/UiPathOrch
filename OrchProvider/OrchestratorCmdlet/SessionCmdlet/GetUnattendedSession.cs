using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Status;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchUnattendedSession")]
[OutputType(typeof(Entities.MachineSessionRuntime))]
public class GetUnattendedSessionCommand : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0)]
    //[ArgumentCompleter(typeof(LastCompleter))]
    //public string? Last { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(TimeAfterCompleter))]
    //public DateTime? ReportingTimeAfter { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(TimeBeforeCompleter))]
    //public DateTime? ReportingTimeBefore { get; set; }

    static readonly string[] StatusList = [
        "Available", "Busy", "Disconnected", "Unknown"
    ];

    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(StatusCompleter))]
    [SupportsWildcards]
    public string[]? Status { get; set; }

    //[Parameter]
    //public ulong? Skip { get; set; }

    //[Parameter]
    //[ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    //public ulong? First { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(DriveCompleter<Last>))]
    public string[]? Path { get; set; }

    // TODO: 存在する Status だけを候補に出す方が良い
    // TODO: これのままにするなら、StaticTextCompleter で書き直さないと。
    private class StatusCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var wpStatus = CreateWPListFromParameter(commandAst, "Status", TPositional.Parameters, wordToComplete);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var status in StatusList
                .Where(s => wp.IsMatch(s))
                .ExcludeByWildcards(s => s, wpStatus))
            {
                yield return new CompletionResult(status);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
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
