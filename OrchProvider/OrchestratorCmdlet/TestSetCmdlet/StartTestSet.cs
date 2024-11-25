using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Start, "OrchTestSet", SupportsShouldProcess = true)]
    [OutputType(typeof(Int64))]
    public class StartTestSetCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(TestSetNameCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
            var wpName = Name.ConvertToWildcardPatternList();

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.TestSets.Get(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (drive, folder) in drivesFolders)
            {
                try
                {
                    var testSets = drive.TestSets.Get(folder);

                    foreach (var testSet in testSets
                        .FilterByWildcards(ts => ts?.Name, wpName)
                        .OrderBy(ts => ts.Name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        if (ShouldProcess(testSet.GetPSPath(), "Start TestSet"))
                        {
                            try
                            {
                                Int64? ret = drive.OrchAPISession.StartTestSets(folder.Id ?? 0, testSet.Id ?? 0);
                                if (ret.HasValue)
                                {
                                    WriteObject(ret);
                                }
                                drive._dicTestSetExecutions?.TryRemove(folder.Id ?? 0, out _);
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(testSet.GetPSPath(), ex), "StartTestSetError", ErrorCategory.InvalidOperation, testSet));
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestSetError", ErrorCategory.InvalidOperation, folder));
                }
            }
        }

        // マルチスレッド化したバージョン
        // HTTP call を cap した状態では逆に遅くなる場合があるため、シングルスレッドで書き直した
        //protected override void ProcessRecord()
        //{
        //    var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(Path, Recurse.IsPresent, Depth);
        //    var wpName = Name.ConvertToWildcardPatternList();

        //    using var results = OrchThreadPool.RunForEach(drivesFolders,
        //        df => df.folder.GetPSPath(),
        //        df => df.folder,
        //        df => df.drive.GetTestSets(df.folder));

        //    using var cancelHandler = new ConsoleCancelHandler();
        //    foreach (var result in results)
        //    {
        //        try
        //        {
        //            var entities = result.GetResult(cancelHandler.Token);
        //            if (entities == null) continue;

        //            var (drive, folder) = result.Source;

        //            foreach (var testSet in entities
        //                .FilterByWildcards(ts => ts.Name!, wpName)
        //                .OrderBy(ts => ts.Name))
        //            {
        //                if (ShouldProcess(testSet.GetPSPath(), "Start TestSet"))
        //                {
        //                    try
        //                    {
        //                        Int64? ret = drive.OrchAPISession.StartTestSets(folder.Id ?? 0, testSet.Id ?? 0);
        //                        if (ret.HasValue)
        //                        {
        //                            WriteObject(ret);
        //                        }
        //                        drive._dicTestSetExecutions?.TryRemove(folder.Id ?? 0, out _);
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        WriteError(new ErrorRecord(new OrchException(testSet.GetPSPath(), ex), "StartTestSetError", ErrorCategory.InvalidOperation, testSet));
        //                    }
        //                }
        //            }
        //        }
        //        catch (OrchException ex)
        //        {
        //            WriteError(new ErrorRecord(ex, "GetTestSetError", ErrorCategory.InvalidOperation, ex.Target));
        //        }
        //    }
        //}
    }
}
