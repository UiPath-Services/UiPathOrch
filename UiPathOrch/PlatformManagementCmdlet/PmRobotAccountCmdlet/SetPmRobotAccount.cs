using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Set, "PmRobotAccount", DefaultParameterSetName = psDefault, SupportsShouldProcess = true)]
[OutputType(typeof(PmRobotAccount))]
public class SetPmRobotAccountCommand : OrchestratorPSCmdlet
{
    private const string psDefault = "ConsoleInput";
    private const string psByCsv = "CsvInput";

    [Parameter(ParameterSetName = psDefault, Position = 0, Mandatory = true)]
    [Parameter(ParameterSetName = psByCsv, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(ParameterSetName = psDefault, Position = 1)]
    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(GroupNameCompleter))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    #region GroupName\d
    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName0 { get; set; }

    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName1 { get; set; }

    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName2 { get; set; }

    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName3 { get; set; }

    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName4 { get; set; }

    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName5 { get; set; }

    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName6 { get; set; }

    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName7 { get; set; }

    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName8 { get; set; }

    [Parameter(ParameterSetName = psByCsv, ValueFromPipelineByPropertyName = true, DontShow = true)]
    [SupportsWildcards]
    public string? GroupName9 { get; set; }
    #endregion

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private static readonly char[] separator = [','];

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            // Exclude names already selected via parameters from candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmRobotAccounts.Get());

            foreach (var result in results)
            {
                foreach (var robotAccount in result
                    .Where(r => r is not null)
                    .Where(r => wp.IsMatch(r!.name!))
                    .ExcludeByWildcards(r => r!.name!, wpName)
                    .OrderBy(r => r!.name))
                {
                    string tiphelp = robotAccount.GetPSPath(result.Source.NameColonSeparator);
                    yield return new CompletionResult(PathTools.EscapePSText(robotAccount.name), robotAccount.name, CompletionResultType.ParameterValue, tiphelp);
                }
            }

            string newRobotName = "New robot name here";
            string tiphelp2 = "Specify a non-existent name to add a new PmRobotAccount.";
            yield return new CompletionResult(PathTools.EscapePSText(newRobotName), newRobotName, CompletionResultType.ParameterValue, tiphelp2);
        }
    }

    private class GroupNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            // Exclude names already selected via parameters from candidates
            var wpGroupName = CreateSelfExclusionList(commandAst, "GroupName", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmGroups.Get());

            foreach (var result in results)
            {
                foreach (var group in result
                    .Where(e => e is not null)
                    .Where(e => wp.IsMatch(e?.name))
                    .ExcludeByWildcards(e => e?.name!, wpGroupName)
                    .OrderBy(e => e?.name))
                {
                    string tiphelp = group!.GetPSPath(result.Source.NameColonSeparator);
                    yield return new CompletionResult(PathTools.EscapePSText(group!.name), group.name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    // This approach cannot properly clear the cache. It can only process the Path from the first row.

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        // GroupName must not be modified to ensure proper handling during CSV import
        string?[] groupNames = GroupName ?? [];

        // Split GroupName specified in CSV by commas
        groupNames = groupNames
             .SelectMany(name => (name ?? "").Split(separator, StringSplitOptions.RemoveEmptyEntries))
             .Select(name => name.Trim())
             .ToArray();

        if (ParameterSetName == psByCsv)
        {
            groupNames =
            [
                ..groupNames,
                ..new[]
                {
                    GroupName0,
                    GroupName1,
                    GroupName2,
                    GroupName3,
                    GroupName4,
                    GroupName5,
                    GroupName6,
                    GroupName7,
                    GroupName8,
                    GroupName9
                }.Where(r => !string.IsNullOrEmpty(r))
            ];
        }

        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpGroupName = groupNames.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            var existingRobots = drive.PmRobotAccounts.Get();
            string partitionGlobalId = null;
            try
            {
                partitionGlobalId = drive.GetPartitionGlobalId();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetGlobalPartitionIdError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
            if (string.IsNullOrEmpty(partitionGlobalId))
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, "Failed to obtain global partition id."), "GetGlobalPartitionIdError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            var existingGroups = drive.PmGroups.Get();

            List<string> groupIdsToSet = null;
            if (wpGroupName is not null)
            {
                groupIdsToSet = existingGroups
                    .SelectByWildcards(g => g?.name!, wpGroupName)
                    .Select(g => g!.id!)
                    .ToList();
            }

            #region Add the Everyone group id
            var everyoneGroup = existingGroups
                .FirstOrDefault(g => string.Compare(g!.name, "Everyone", StringComparison.OrdinalIgnoreCase) == 0);
            if (everyoneGroup is not null)
            {
                groupIdsToSet ??= [];
                groupIdsToSet!.Add(everyoneGroup.id!);
            }
            groupIdsToSet = groupIdsToSet?.Distinct().ToList();
            #endregion

            if (groupIdsToSet?.Count == 0) groupIdsToSet = null;

            foreach (var userNames in UserName!.WithCancellation(cancelHandler.Token))
            {
                var targetRobots = existingRobots.SelectByWildcards(r => r?.name, wpUserName);

                if (!targetRobots.Any())
                {
                    var userName = WildcardPattern.Unescape(userNames);
                    string target = System.IO.Path.Combine(drive.NameColonSeparator, userName);
                    if (ShouldProcess(target, "Create PmRobotAccount"))
                    {
                        var cmd = new CreateRobotAccountCommand()
                        {
                            partitionGlobalId = partitionGlobalId,
                            name = userName,
                            displayName = userName,
                            groupIDsToAdd = groupIdsToSet
                        };
                        try
                        {
                            var newRobot = drive.CreatePmRobot(cmd);
                            if (newRobot is not null)
                            {
                                WriteObject(newRobot.WithPath(drive.NameColonSeparator));
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "CreateIdRobotError", ErrorCategory.InvalidOperation, target));
                            continue;
                        }
                    }
                }
                else
                {
                    foreach (var robot in targetRobots
                        .Where(r => r is not null)
                        .OrderBy(r => r.name).WithCancellation(cancelHandler.Token))
                    {
                        // Check if there are any updates; skip if none
                        bool areEqual = robot.groupIds!.Length == groupIdsToSet!.Count &&
                                robot.groupIds!.Order().Zip(groupIdsToSet!.ToList().Order(), (a, b) => a == b).All(equal => equal);
                        if (areEqual) continue;

                        string target = System.IO.Path.Combine(drive.NameColonSeparator, robot.name ?? "");

                        if (ShouldProcess(target, "Update PmRobotAccount"))
                        {
                            // Update the robot with this name

                            #region Enumerate group ids that were not specified
                            List<string> groupIDsToRemove = existingGroups
                                .Where(g => !(groupIdsToSet?.Contains(g.id!) ?? false))
                                .Select(g => g.id!)
                                .ToList();
                            #endregion

                            var cmd = new UpdateRobotAccountCommand()
                            {
                                partitionGlobalId = partitionGlobalId,
                                displayName = robot.displayName,
                                groupIDsToAdd = groupIdsToSet,
                                groupIDsToRemove = groupIDsToRemove
                            };
                            try
                            {
                                var updatedRobot = drive.OrchAPISession.UpdatePmRobot(robot.id!, cmd);
                                drive.PmRobotAccounts.ClearCache();
                                drive.SearchPmDirectoryCache.ClearCache();
                                drive.SearchDirectoryCache.ClearCache();

                                if (updatedRobot is not null)
                                {
                                    WriteObject(updatedRobot.WithPath(drive.NameColonSeparator));
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(target, ex), "UpdatePmRobotAccountError", ErrorCategory.InvalidOperation, target));
                                continue;
                            }
                        }
                    }
                }
            }
        }
    }
}
