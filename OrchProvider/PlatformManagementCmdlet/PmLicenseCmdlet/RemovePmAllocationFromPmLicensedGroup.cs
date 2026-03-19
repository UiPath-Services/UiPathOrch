using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.GroupName_UserName;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "PmAllocationFromPmLicensedGroup", SupportsShouldProcess = true)]
public class RemoveAllocationFromUserLicenseGroup: OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmLicensedGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter]
    public SwitchParameter NoMatchWarning { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
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
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", TPositional.Parameters);
            var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults3.GroupBy(drives, drive => drive.PmLicensedGroups.Get());

            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var group in result
                    .FilterByWildcards(g => g?.name!, wpGroupName)
                    .OrderBy(g => g?.name))
                {
                    var users = drive.GetPmLicensedGroupAllocations(group);
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
        var drives = SessionState.EnumPmDrives(Path);

        var wpGroupName = GroupName.ConvertToWildcardPatternList();
        var wpUserName = UserName.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            var groups = drive.PmLicensedGroups.Get();

            var targetGroups = groups.FilterByWildcards(g => g?.name, wpGroupName);

            if (NoMatchWarning.IsPresent && !targetGroups.Any())
            {
                // This implementation is a bit rough, but it works properly during CSV import, so it should be sufficient.
                // A proper implementation would need to process the GroupName array one element at a time from the beginning.
                WriteWarning($"No match found for GroupName '{GroupName![0]}'.");
                continue;
            }

            foreach (var group in targetGroups.OrderBy(g => g?.name))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                var users = drive.GetPmLicensedGroupAllocations(group);

                var targetUsers = users.FilterByWildcards(u => u?.name, wpUserName);

                if (NoMatchWarning.IsPresent && !targetUsers.Any())
                {
                    // This implementation is a bit rough, but it works properly during CSV import, so it should be sufficient.
                    // A proper implementation would need to process the UserName array one element at a time from the beginning.
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
