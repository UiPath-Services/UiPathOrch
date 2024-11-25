using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.GroupName_UserName;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Remove, "OrchPmAllocationFromPmLicensedGroup", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.PmGroupMember))]
    public class RemoveAllocationFromUserLicenseGroup: OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter<TPositional>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        [SupportsWildcards]
        public string[]? UserName { get; set; }

        [Parameter]
        public SwitchParameter WarnOnNoMatch { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
        public string[]? Path { get; set; }

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

                var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", TPositional.Parameters);
                var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.PmLicensedGroups.Get());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    var drive = result.Source;

                    foreach (var e in entities!
                        .FilterByWildcards(g => g?.name!, wpGroupName)
                        .OrderBy(g => g?.name))
                    {
                        var users = drive.GetPmLicensedGroupAllocations(e);
                        foreach (var user in users
                            .Where(u => wp.IsMatch(u?.name))
                            .ExcludeByWildcards(u => u?.name!, wpUserName)
                            .OrderBy(u => u?.name))
                        {
                            string tiphelp = TipHelp(user);
                            yield return new CompletionResult(PathTools.EscapePSText(user?.name), user?.name, CompletionResultType.Text, tiphelp);
                        }
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            var wpGroupName = GroupName.ConvertToWildcardPatternList();
            var wpUserName = UserName.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                var groups = drive.PmLicensedGroups.Get();

                var targetGroups = groups.FilterByWildcards(g => g?.name, wpGroupName);

                if (WarnOnNoMatch.IsPresent && !targetGroups.Any())
                {
                    // ちょっと適当な実装だけど、これでも CSV インポート時にちゃんと動くから十分か。。
                    // ちゃんと実装するには、GroupName の配列を先頭から順にひとつずつ処理しないといけない。
                    WriteWarning($"No match found for GroupName '{GroupName![0]}'.");
                    continue;
                }

                foreach (var group in targetGroups.OrderBy(g => g?.name))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();
                    var users = drive.GetPmLicensedGroupAllocations(group);

                    var targetUsers = users.FilterByWildcards(u => u?.name, wpUserName);

                    if (WarnOnNoMatch.IsPresent && !targetUsers.Any())
                    {
                        // ちょっと適当な実装だけど、これでも CSV インポート時にちゃんと動くから十分か。。
                        // ちゃんと実装するには、UserName の配列を先頭から順にひとつずつ処理しないといけない。
                        WriteWarning($"No match found for UserName '{UserName![0]}'.");
                        continue;
                    }

                    foreach (var user in targetUsers.OrderBy(u => u?.name))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        string target = user.name;
                        if (!string.IsNullOrEmpty(user.displayName))
                        {
                            target += $" ({user.displayName})";
                        }
                        target += $" from {group.GetPSPath()}";
                        if (ShouldProcess(target, "Remove Allocation from NamedUserLicenseGroup"))
                        {
                            try
                            {
                                drive.OrchAPISession.DeletePmLicenseGroupAllocations(group.id, user.id!);
                                drive._dicPmUserLicenseGroupAllocations = null;
                                drive._dicPmUserLicenseGroupAllocations_Exceptions.ClearCache();
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "RemoveAllocationFromNamedUserLicenseGroup", ErrorCategory.InvalidOperation, drive));
                            }
                        }
                    }
                }
            }
        }
    }
}
