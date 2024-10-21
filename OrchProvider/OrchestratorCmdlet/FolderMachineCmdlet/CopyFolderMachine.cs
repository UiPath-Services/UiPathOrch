using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchFolderMachine", SupportsShouldProcess = true)]
    public class CopyFolderMachineCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(NameCompleter))]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        private class NameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

                // パラメータで選択済みの Name は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name_Destination.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetMachinesAssignedToFolder(df.folder));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var machine in entities!
                        .Where(m => wp.IsMatch(m.Name))
                        .ExcludeByWildcards(m => m?.Name, wpName)
                        .OrderBy(m => m.Name))
                    {
                        yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, TipHelp(machine));
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Path);
            var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

            var (dstDrive, dstRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Destination);

            // コピー元とコピー先が同じなら、何もしない
            if (srcDrive == dstDrive && srcRootFolder.FullyQualifiedName == dstRootFolder.FullyQualifiedName) return;

            var wpName = Name.ConvertToWildcardPatternList();

            //using var results = OrchThreadPool.RunForEach(srcDrivesFolders,
            //    df => df.folder.GetPSPath(),
            //    df => df.folder,
            //    df => df.drive.GetMachinesAssignedToFolder(df.folder));

            string msg = "Copying assigned machines...";
            using var reporter = new ProgressReporter(this, 200, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (_, srcFolder) in srcDrivesFolders)
            {
                // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                srcDrive._dicMachinesAssigned?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.GetMachinesAssignedToFolder(srcFolder)
                    .Where(e => e.IsAssignedToFolder.GetValueOrDefault())
                    .FilterByWildcards(e => e?.Name, wpName);
                if (!srcEntities.Any()) continue;

                Folder? dstFolder = GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
                if (dstFolder == null) continue;

                try
                {
                    Core.OrchProvider.CopyFolderMachines(this,
                        srcDrive, srcFolder, wpName,
                        dstDrive, dstFolder, reporter,
                        cancelHandler.Token, false);
                    dstDrive._dicMachinesAssigned?.TryRemove(dstFolder.Id!.Value, out _);
                    dstDrive._dicAssignedMachines?.TryRemove(dstFolder.Id!.Value, out _);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string target = dstFolder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(target, ex), "CopyFolderMachineError", ErrorCategory.InvalidOperation, dstFolder));
                }
            }
        }

        //protected override void ProcessRecord()
        //{
        //    var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Path);
        //    var srcDrivesFolders = OrchDriveInfo.EnumFoldersForRecursiveCopy(Path, Recurse.IsPresent, Depth);

        //    var dstDrivesFolders = OrchDriveInfo.EnumFolders(Destination);
        //    var wpName = Name.ConvertToWildcardPatternList();

        //    using var results = OrchThreadPool.RunForEach(srcDrivesFolders,
        //        df => df.folder.GetPSPath(),
        //        df => df.folder,
        //        df => df.drive.GetMachinesAssignedToFolder(df.folder));

        //    string msg = "Copying assigned machines...";
        //    using var reporter = new ProgressReporter(this, 200, Int32.MaxValue, msg, msg);
        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var dstDriveFolder in dstDrivesFolders)
        //    {
        //        var (dstDrive, dstFolder) = dstDriveFolder;
        //        foreach (var result in results)
        //        {
        //            cancelHandler.Token.ThrowIfCancellationRequested();

        //            try
        //            {
        //                var entities = result.GetResult(cancelHandler.Token);
        //                if (entities == null) continue;

        //                var (srcDrive, srcFolder) = result.Source;

        //                try
        //                {
        //                    Core.OrchProvider.CopyFolderMachines(this,
        //                    srcDrive, srcFolder, wpName,
        //                    dstDrive, dstFolder, reporter, false);
        //                    dstDrive._dicMachinesAssigned?.TryRemove(dstFolder.Id ?? 0, out _);
        //                }
        //                catch (Exception ex)
        //                {
        //                    string target = dstFolder.GetPSPath();
        //                    WriteError(new ErrorRecord(new OrchException(target, ex), "CreateFolderMachineError", ErrorCategory.InvalidOperation, dstFolder));
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                string target = dstFolder.GetPSPath();
        //                WriteError(new ErrorRecord(new OrchException(target, ex), "GetFolderMachineError", ErrorCategory.InvalidOperation, dstFolder));
        //            }
        //        }
        //    }
        //}
    }
}
