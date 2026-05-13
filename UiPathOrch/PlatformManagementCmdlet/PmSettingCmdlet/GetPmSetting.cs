using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Host admin only

//[Cmdlet(VerbsCommon.Get, "PmSetting")]
//[OutputType(typeof(Entities.PmUser))]
class GetPmSettingCmdlet : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0)]
    //[ArgumentCompleter(typeof(UserNameCompleter))]
    //[SupportsWildcards]
    //public string[]? UserName { get; set; }

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    //private class UserNameCompleter : OrchArgumentCompleter
    //{
    //    public override IEnumerable<CompletionResult> CompleteArgument(
    //        string commandName,
    //        string parameterName,
    //        string wordToComplete,
    //        CommandAst commandAst,
    //        IDictionary fakeBoundParameters)
    //    {
    //        // Extract the path from parameters. If not specified, target the current directory.
    //        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
    //        var drives = OrchDriveInfo.EnumOrchDrives(paramPath);

    //        // Exclude Names already selected via parameters from candidates
    //        var wpUserName = CreateWPListFromParameter(commandAst, "UserName", positionalParams, wordToComplete);

    //        var wp = CreateWPFromWordToComplete(wordToComplete);

    //        var results = ParallelResults3.GroupBy(drives, drive => drive.GetIdentityUsers());

    //        foreach (var drive in drives)
    //        {
    //            foreach (var result in results)
    //            {
    //                if (!result.TryGetValue(out var entities)) continue;

    //                foreach (var e in entities!.Values
    //                    .Where(g => wp.IsMatch(g?.userName))
    //                    .ExcludeByWildcards(u => u?.userName!, wpUserName)
    //                    .OrderBy(u => u?.userName))
    //                {
    //                    string tooltip = e.GetPSPath();
    //                    yield return new CompletionResult(PathTools.EscapePSText(e.userName), e.userName, CompletionResultType.Text, tooltip);
    //                }
    //            }
    //        }
    //    }
    //}

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        //var wpUserName = UserName.ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            var partitionGlobalId = drive.GetPartitionGlobalId();

            try
            {
                drive.OrchAPISession.GetIdentitySetting(partitionGlobalId!, "f7911d4a-bd8e-4e74-b9da-f772ce730b9e");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetIdUserError", ErrorCategory.InvalidOperation, drive));
            }
        }

        //using var results = OrchThreadPool.RunForEach(drives,
        //    drive => drive.NameColonSeparator,
        //    drive => drive,
        //    drive => drive.GetIdentityUsers().Values);

        //using var cancelHandler = new ConsoleCancelHandler();
        //foreach (var result in results)
        //{
        //    try
        //    {
        //        var entities = result.GetResult(cancelHandler.Token);
        //        if (entities is null) continue;

        //        WriteObject(entities
        //            .FilterByWildcards(u => u.userName!, wpUserName)
        //            .OrderBy(u => u.userName),
        //            true);
        //    }
        //    catch (OrchException ex)
        //    {
        //        WriteError(new ErrorRecord(ex, "GetIdUserError", ErrorCategory.InvalidOperation, ex.Target));
        //    }
        //}
    }
}
