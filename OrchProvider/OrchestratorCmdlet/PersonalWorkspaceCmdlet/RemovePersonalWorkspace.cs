using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name_OwnerName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchPersonalWorkspace", SupportsShouldProcess = true)]
public class RemovePersonalWorkspaceCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(OwnerNameCompleter))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? OwnerName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);
            var wpOwnerName = CreateWPListFromOtherParameters(commandAst, "OwnerName", TPositional.Parameters);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.PersonalWorkspaces.Get());

            foreach (var result in results)
            {
                foreach (var personalWorkspace in result
                    .Where(q => wp.IsMatch(q.Name))
                    .ExcludeByWildcards(q => q?.Name, wpName)
                    .FilterByWildcards(q => q?.OwnerName, wpOwnerName)
                    .OrderBy(q => q.Name))
                {
                    string tiphelp = TipHelp(personalWorkspace);
                    yield return new CompletionResult(PathTools.EscapePSText(personalWorkspace.Name), personalWorkspace.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    private class OwnerNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);
            var wpOwnerName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.PersonalWorkspaces.Get());

            foreach (var result in results)
            {
                foreach (var personalWorkspace in result
                    .Where(q => wp.IsMatch(q.OwnerName))
                    .FilterByWildcards(q => q?.Name!, wpName)
                    .ExcludeByWildcards(q => q?.OwnerName, wpOwnerName)
                    .OrderBy(q => q.OwnerName))
                {
                    string tiphelp = TipHelp(personalWorkspace);
                    yield return new CompletionResult(PathTools.EscapePSText(personalWorkspace.OwnerName), personalWorkspace.OwnerName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);

        if (Name?.Length == 0      || string.IsNullOrEmpty(Name?[0])) Name = null;
        if (OwnerName?.Length == 0 || string.IsNullOrEmpty(OwnerName?[0])) OwnerName = null;

        if (Name is null && OwnerName is null)
        {
            WriteError(new ErrorRecord(new ArgumentException("Please make sure to specify either -Name or -OwnerName."), "RemovePersonalWorkspaceError", ErrorCategory.InvalidOperation, this));
            return;
        }

        var wpName = Name.ConvertToWildcardPatternList();
        var wpOwnerName = OwnerName.ConvertToWildcardPatternList();

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.PersonalWorkspaces.Get());

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var wss = result.GetResult(cancelHandler.Token);
                if (wss is null) continue;

                var drive = result.Source;

                foreach (var ws in wss
                    .Where(ws => ws is not null && ws.Id is not null)
                    .FilterByWildcards(ws => ws!.Name, wpName)
                    .FilterByWildcards(ws => ws!.OwnerName, wpOwnerName)
                    .OrderBy(ws => ws.OwnerName))
                {
                    if (ShouldProcess(ws.GetPSPath(), "Remove PersonalWorkspace"))
                    {
                        try
                        {
                            drive.DisablePersonalWorkspace(ws.OwnerId);
                            drive!.OrchAPISession.RemoveFolder(ws.Id ?? 0);
                            drive._dicFolders = null;
                            drive._dicFoldersForEnumFolders = null;
                            drive.PersonalWorkspaces.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(ws.GetPSPath(), ex), "RemovePersonalWorkspaceError", ErrorCategory.InvalidOperation, ws));
                        }
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPersonalWorkspaceError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
