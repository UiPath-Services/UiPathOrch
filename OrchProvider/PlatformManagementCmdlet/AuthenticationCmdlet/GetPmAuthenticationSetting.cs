using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchPmAuthenticationSetting")]
    [OutputType(typeof(Entities.PmAuthenticationSetting))]
    public class GetPmAuthenticationSettingCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        [SupportsWildcards]
        public string[]? UserName { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.UserName>))]
        public string[]? Path { get; set; }

        // TODO: これは共通化すべき
        private class UserNameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの Name は、候補から除外する
                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", Positional.UserName.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetPmUsers());

                foreach (var drive in drives)
                {
                    foreach (var result in results)
                    {
                        if (!result.TryGetValue(out var entities)) continue;

                        foreach (var e in entities!.Values
                            .Where(g => wp.IsMatch(g?.userName))
                            .ExcludeByWildcards(u => u?.userName!, wpUserName)
                            .OrderBy(u => u?.userName))
                        {
                            string tooltip = e.GetPSPath();
                            yield return new CompletionResult(PathTools.EscapePSText(e.userName), e.userName, CompletionResultType.Text, tooltip);
                        }
                    }
                }
            }
        }

        // TODO: まじめに実装しないと。キャッシュも作らないと。
        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpUserName = UserName.ConvertToWildcardPatternList();

            foreach (var drive in drives)
            {
                string partitionGlobalId = drive.GetPartitionGlobalId();
                WriteObject(drive.OrchAPISession.GetPmAuthenticationSettings(partitionGlobalId!));
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
            //        if (entities == null) continue;

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
}
