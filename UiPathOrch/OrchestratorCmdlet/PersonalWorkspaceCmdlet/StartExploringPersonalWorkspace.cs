using System.Management.Automation;

namespace UiPath.PowerShell.Commands;

// Even if personal workspaces could be displayed with dir, it wouldn't be very useful
// because the user lacks the necessary permissions.
// /odata/PersonalWorkspaces({key})/UiPath.Server.Configuration.OData.StartExploring
// does not support OAuth authentication, so this cmdlet does not work. Kept as unpublished for now.
//[Cmdlet(VerbsLifecycle.Start, "OrchExploringPersonalWorkspace", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.QueueDefinition))]
class StartExploringPersonalWorkspaceCmdlet : OrchestratorPSCmdlet
{
    //[Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    //[ArgumentCompleter(typeof(NameCompleter))]
    //[SupportsWildcards]
    //public string[]? Name { get; set; }

    //[Parameter(ValueFromPipelineByPropertyName = true)]
    //[SupportsWildcards]
    //public string[]? Path { get; set; }

    //private class NameCompleter : OrchArgumentCompleter
    //{
    //    public override IEnumerable<CompletionResult> CompleteArgument(
    //        string commandName,
    //        string parameterName,
    //        string wordToComplete,
    //        CommandAst commandAst,
    //        IDictionary fakeBoundParameters)
    //    {
    //        var drives = ResolveDrives(fakeBoundParameters);

    //        // Exclude Names already selected by the parameter from the candidates
    //        var wpName = CreateWPListFromParameter(commandAst, "Name", ["Name"], wordToComplete);

    //        var wp = CreateWPFromWordToComplete(wordToComplete);

    //        var results = ParallelResults3.GroupBy(drives, drive => drive.GetPersonalWorkspaces());

    //        foreach (var result in results)
    //        {
    //            if (!result.TryGetValue(out var entities)) continue;

    //            foreach (var e in entities!
    //                .Where(q => wp.IsMatch(q.Name))
    //                .ExcludeByWildcards(q => q?.Name, wpName)
    //                .OrderBy(q => q.Name))
    //            {
    //                string tiphelp = $"OwnerId: {e.OwnerId}  OwnerName: {e.OwnerName}";
    //                yield return new CompletionResult(PathTools.EscapePSText(e.Name), e.Name, CompletionResultType.ParameterValue, tiphelp);
    //            }
    //        }
    //    }
    //}

    //protected override void ProcessRecord()
    //{
    //    var drives = OrchDriveInfo.EnumOrchDrives(Path);
    //    var wpName = Name.ConvertToWildcardPatternList();

    //    using var results = OrchThreadPool.RunForEach(drives,
    //        drive => drive.NameColonSeparator,
    //        drive => drive,
    //        drive => drive.GetPersonalWorkspaces());

    //    using var cancelHandler = new ConsoleCancelHandler();
    //    foreach (var result in results)
    //    {
    //        //drive.OrchAPISession.StartExploringPersonalWorkspace(280062);
    //        try
    //        {
    //            var entities = result.GetResult(cancelHandler.Token);
    //            if (entities is null) continue;

    //            var drive = result.Source;

    //            bool bDirty = false;
    //            foreach (var ws in entities
    //                .FilterByWildcards(ws => ws?.Name, wpName)
    //                .OrderBy(ws => ws.Name))
    //            {
    //                string target = $"{ws.Path}{ws.Name}";
    //                if (ShouldProcess(target, "Start ExploringPersonalWorkspace"))
    //                {
    //                    drive!._dicPersonalWorkspacesExploringAvailable ??= new();
    //                    drive._dicPersonalWorkspacesExploringAvailable.Add(ws);
    //                    bDirty = true;
    //                }
    //            }
    //            if (bDirty)
    //            {
    //                drive!._dicFolders = null;
    //                drive._dicFoldersForEnumFolders = null;
    //            }
    //        }
    //        catch (OrchException ex)
    //        {
    //            WriteError(new ErrorRecord(ex, "GetPackageUserError", ErrorCategory.InvalidOperation, ex.Target));
    //        }
    //    }
    //}
}
