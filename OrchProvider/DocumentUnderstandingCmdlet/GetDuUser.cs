using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "DuUser")]
    [OutputType(typeof(Entities.DuUser))]
    public class GetDuUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(NameCompleter))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        private class NameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var recurse = GetSwitchParameterValue(commandAst, "Recurse");

                // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
                var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
                var drivesProjects = OrchDuDriveInfo.EnumFolders(paramPath, recurse);

                // パラメータで選択済みの DocumentTypeName は、候補から除外する
                var wpName = CreateWPListFromParameter(commandAst, "Name", Positional.Name.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drivesProjects, dp => {
                    var (drive, project) = dp;
                    var partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
                    var (_, tenantKey) = drive.ParentDrive.GetTenantId();
                    return drive.GetDuUsers(partitionGlobalId, tenantKey, project);
                });

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var user in entities!
                        .Where(e => wp.IsMatch(e?.displayName))
                        .ExcludeByWildcards(e => e?.displayName!, wpName)
                        .OrderBy(e => e?.displayName))
                    {
                        string tiphelp = user.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(user.displayName), user.displayName, CompletionResultType.Text, tiphelp);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drivesProjects = OrchDuDriveInfo.EnumFolders(Path, Recurse.IsPresent);
            var wpName = Name.ConvertToWildcardPatternList();

            using var results = OrchThreadPool.RunForEach(drivesProjects,
                dp => dp.project.GetPSPath(),
                dp => dp.project,
                dp =>
                {
                    var (drive, project) = dp;
                    var partitionGlobalId = drive.ParentDrive.GetPartitionGlobalId();
                    var (_, tenantKey) = drive.ParentDrive.GetTenantId();
                    return drive.GetDuUsers(partitionGlobalId, tenantKey, project);
                });

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    WriteObject(entities
                        .FilterByWildcards(u => u?.displayName, wpName)
                        .OrderBy(e => e.displayName),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetDuUserError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
