using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;

using Positional = UiPath.PowerShell.Positional.Last_Component_UserName_Action;
using LastItems = UiPath.PowerShell.Positional.Hour_Day_Week_Month_3Month_6Month_Year_3Year;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchAuditLog", DefaultParameterSetName = "Filter")] //, SupportsPaging = true)]
    [OutputType(typeof(AuditLog))]
    public class GetAuditLogCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, ParameterSetName = "Filter")]
        [ArgumentCompleter(typeof(StaticTextsCompleter<LastItems>))]
        public string? Last { get; set; }

        [Parameter(Position = 1, ParameterSetName = "Filter")]
        [ArgumentCompleter(typeof(ComponentCompleter))]
        public string[]? Component;

        [Parameter(Position = 2, ParameterSetName = "Filter")]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName;

        [Parameter(Position = 3, ParameterSetName = "Filter")]
        [ArgumentCompleter(typeof(ActionCompleter))]
        public string[]? Action;

        [Parameter(ParameterSetName = "Filter")]
        [ArgumentCompleter(typeof(TimeAfterCompleter))]
        public DateTime? ExecutionTimeAfter { get; set; }

        [Parameter(ParameterSetName = "Filter")]
        [ArgumentCompleter(typeof(TimeBeforeCompleter))]
        public DateTime? ExecutionTimeBefore { get; set; }

        [Parameter(ParameterSetName = "Id")]
        [ArgumentCompleter(typeof(IdCompleter))]
        [SupportsWildcards]
        public string[]? Id;

        static readonly string[] ComponentsList = [
            "Assets",
            "Buckets",
            "CloudSnapshots",
            "CloudSubscriptions",
            "Comments",
            "CredentialStores",
            "CredentialsProxies",
            "DirectoryService",
            "Environments",
            "ExecutionMedia",
            "Folders",
            "Jobs",
            "Libraries",
            "Licenses",
            "Machines",
            "Maintenance",
            "Monitoring",
            "Packages",
            "PersonalWorkspaces",
            "Processes",
            "Queues",
            "RemoteControl",
            "Robots",
            "Roles",
            "Triggers",
            "Sessions",
            "Settings",
            //"Actions",
            "Units",
            "Users",
            "Webhooks",
        ];

        static readonly string[] ActionsList =
        [
            "Acknowledge",
            "Activate",
            "Assign",
            "Associate",
            "AutomaticallyExploreEnd",
            "BulkComplete",
            "BulkSave",
            "BulkUpload",
            "ChangePassword",
            "ChangeStatus",
            "Convert",
            "Create",
            "CreateBlobFileSas",
            "Deactivate",
            "Delete",
            "DeleteBlobFile",
            "Download",
            "End",
            "ExploreEnd",
            "ExploreStart",
            "Forward",
            "lmport",
            "MigrateFolder",
            "Move",
            "PasswordResetAttempt",
            "ResetPassword",
            "Save",
            "Skip",
            "Start",
            "StartDelete",
            "StartJob",
            "StartMigrateFolders",
            "StopJob",
            "Toggle",
            "ToggleUserFolderSubscription",
            "Unassign",
            "Update",
            "Upload",
            "VideoAccess"
        ];

        [Parameter]
        public SwitchParameter ExpandEntity { get; set; }

        [Parameter]
        public ulong? Skip { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
        public ulong? First { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Last_Component_UserName_Action>))]
        public string[]? Path { get; set; }

        private class IdCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの Id は、候補から除外する
                var paramId = GetParameterValues(commandAst, "Id", null, wordToComplete);
                var wpId = paramId.ConvertToWildcardPatternList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                // キャッシュを使うので、マルチスレッド化する必要はない
                var results = new List<AuditLog>();
                foreach (var drive in drives)
                {
                    if (drive._dicAuditLogs == null)
                        continue;
                    results.AddRange(drive._dicAuditLogs
                        .Where(log => wp.IsMatch(log.Id.ToString()))
                        .ExcludeByWildcards(log => log?.Id.ToString(), wpId));
                }

                foreach (var log in results.OrderBy(log => log.Id))
                {
                    string logId = log.Id.ToString();
                    string tiphelp = $"{logId} {log.ExecutionTime!.Value.ToLocalTime():yyyy/MM/dd} {log.OperationText}";
                    yield return new CompletionResult(logId, logId, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }

        // TODO: StaticTextCompleter で書き直す
        private class ComponentCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                // パラメータで選択済みの Component は、候補から除外する
                var paramComponent = GetParameterValues(commandAst, "Component", null, wordToComplete);
                var wpComponent = paramComponent.ConvertToWildcardPatternList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var c in ComponentsList
                    .Where(c => wp.IsMatch(c))
                    .ExcludeByWildcards(c => c, wpComponent))
                {
                    yield return new CompletionResult(c);
                }
            }
        }

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

                // パラメータで選択済みの User は、候補から除外する
                var paramUserName = GetParameterValues(commandAst, "UserName", null, wordToComplete);
                var wpUserName = paramUserName.ConvertToWildcardPatternList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(user => wp.IsMatch(user.UserName))
                        .ExcludeByWildcards(user => user?.UserName, wpUserName)
                        .OrderBy(user => user.UserName))
                    {
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.UserName), e.UserName, CompletionResultType.Text, tiphelp);
                    }
                }
            }
        }

        // TODO: StaticTextCompleter で書き直す
        private class ActionCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                // パラメータで選択済みの Action は、候補から除外する
                var paramAction = GetParameterValues(commandAst, "Action", null, wordToComplete);
                var wpAction = paramAction.ConvertToWildcardPatternList();

                var results = ActionsList.ExcludeByWildcards(a => a, wpAction);
                foreach (var a in results)
                {
                    yield return new CompletionResult(a);
                }
            }
        }

        private string? MakeFilter()
        {
            var filter = new List<string>();

            #region Last
            if (Last != null)
            {
                var last = Last.ToLower() switch
                {
                    "hour" => DateTime.UtcNow.AddHours(-1),
                    "day" => DateTime.UtcNow.AddDays(-1),
                    "week" => DateTime.UtcNow.AddDays(-7),
                    "month" => DateTime.UtcNow.AddMonths(-1),
                    "3months" => DateTime.UtcNow.AddMonths(-3),
                    "6months" => DateTime.UtcNow.AddMonths(-6),
                    "year" => DateTime.UtcNow.AddYears(-1),
                    "3years" => DateTime.UtcNow.AddYears(-3),
                    _ => throw new ArgumentException("Invalid Last parameter. Valid values are 'Hour', 'Day', 'Week', 'Month', '3Months', '6Months', 'Year', '3Years'.")
                };
                filter.Add($"(ExecutionTime%20ge%20{last:yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region ExecutionTimeAfter
            if (ExecutionTimeAfter != null)
            {
                filter.Add($"(ExecutionTime%20ge%20{ExecutionTimeAfter.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region ExecutionTimeBefore
            if (ExecutionTimeBefore != null)
            {
                filter.Add($"(ExecutionTime%20lt%20{ExecutionTimeBefore.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ})");
            }
            #endregion

            #region Component
            if (Component != null && Component.Length > 0)
            {
                var components = new List<string>();
                var wpComponent = Component.Select(st => new WildcardPattern(st, WildcardOptions.IgnoreCase)).ToList();
                foreach (var component in ComponentsList.FilterByWildcards(st => st, wpComponent))
                {
                    components.Add($"(Component%20eq%20%27{component}%27)");
                }
                if (components.Count != 0)
                    filter.Add("(" + string.Join("%20or%20", components) + ")");
            }
            #endregion

            #region UserName
            if (UserName != null && UserName.Length > 0)
            {
                try
                {
                    var drives = OrchDriveInfo.EnumOrchDrives(Path);
                    var wpUserName = UserName.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();
                    var userIds = new HashSet<Int64>();
                    foreach (var drive in drives)
                    {
                        userIds.UnionWith(drive.GetUsers()
                            .FilterByWildcards(u => u?.UserName, wpUserName)
                            .Select(u => u.Id ?? 0));
                    }
                    if (userIds.Count != 0)
                        filter.Add("(" + string.Join("%20or%20", userIds.Select(id => $"(UserId%20eq%20{id})")) + ")");
                }
                catch { }
            }
            #endregion

            #region Action
            if (Action != null && Action.Length > 0)
            {
                var actions = new List<string>();
                var wpAction = Action.Select(ac => new WildcardPattern(ac, WildcardOptions.IgnoreCase)).ToList();
                foreach (var action in ActionsList.FilterByWildcards(st => st, wpAction))
                {
                    actions.Add($"(Action%20eq%20%27{action}%27)");
                }
                if (actions.Count != 0)
                    filter.Add("(" + string.Join("%20or%20", actions) + ")");
            }
            #endregion

            string ret = string.Join("%20and%20", filter);
            if (!string.IsNullOrEmpty(ret))
                return "&$filter=(" + ret + ")";
            else
                return null;
        }

        protected void WriteLog(IEnumerable<AuditLog> logs)
        {
            if (ExpandEntity)
            {
                foreach (var log in logs)
                {
                    if (log.Entities != null && log.Entities.Length > 0)
                    {
                        WriteObject(log.Entities, true);
                    }
                }
            }
            else
            {
                WriteObject(logs, true);
            }
        }

        protected override void ProcessRecord()
        {
            //ulong skip = PagingParameters.Skip;
            //ulong first = PagingParameters.First;

            ulong skip = Skip ?? 0;
            ulong first = First ?? ulong.MaxValue;

            string filter = MakeFilter();

            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            foreach (var drive in drives)
            {
                if (ParameterSetName == "Filter")
                {
                    try
                    {
                        WriteLog(drive.GetAuditLogs(filter, skip, first));
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetAuditLogError", ErrorCategory.InvalidOperation, drive));
                    }
                }
                else // ParameterSetName == "Id"
                {
                    if (drive._dicAuditLogs == null)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, "The -Id parameter searches only within the local cache. Please execute Get-OrchAuditLog without the -Id parameter first."), "GetAuditLogError", ErrorCategory.InvalidOperation, drive));
                        continue;
                    }
                    var wpId = Id?.Select(id => new WildcardPattern(id)).ToList();
                    WriteLog(drive._dicAuditLogs.FilterByWildcards(log => (log?.Id ?? 0).ToString(), wpId));
                }
            }
        }
    }
}
