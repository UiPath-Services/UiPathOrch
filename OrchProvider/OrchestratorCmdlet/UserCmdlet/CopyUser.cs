using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;
using User = UiPath.PowerShell.Entities.User;

using Positional = UiPath.PowerShell.Positional.UserName_Destination;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchUser", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.User))]
    public class CopyUserCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(UserNameCompleter))]
        public string[]? UserName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(FullNameCompleter))]
        public string[]? FullName { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationCompleter))]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.UserName_Destination>))]
        public string? Path { get; set; }

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

                // パラメータで選択済みのユーザー名は、候補から除外する
                var wpUserName = CreateWPListFromParameter(commandAst, "UserName", Positional.UserName_Destination.Parameters, wordToComplete);

                // パラメータで選択された FullName のみ対象とする
                var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", Positional.UserName_Destination.Parameters);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(u => wp.IsMatch(u.UserName))
                        .ExcludeByWildcards(u => u?.UserName, wpUserName)
                        .FilterByWildcards(u => u?.FullName, wpFullName)
                        .OrderBy(u => u.UserName))
                    {
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.UserName), e.UserName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        private class FullNameCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択された UserName のみ対象とする
                var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", Positional.UserName_Destination.Parameters);

                // パラメータで選択済みのユーザー名は、候補から除外する
                var wpFullName = CreateWPListFromParameter(commandAst, "FullName", Positional.UserName_Destination.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var e in entities!
                        .Where(u => wp.IsMatch(u.FullName))
                        .ExcludeByWildcards(u => u?.FullName, wpFullName)
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .OrderBy(u => u.FullName))
                    {
                        string tiphelp = TipHelp(e);
                        yield return new CompletionResult(PathTools.EscapePSText(e.FullName), e.FullName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        // DriveCompleter と良く似ているのだけど、これはコピー元のドライブを除外する機能がある。
        public class DestinationCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = OrchDriveInfo.EnumAllOrchDrives();

                // パラメータで選択済みのドライブは、候補から除外する
                var paramPath = GetParameterValues(commandAst, "Path", Positional.UserName_Destination.Parameters).Select(p => p.TrimEnd(':'));
                var paramPathDriveNames = OrchDriveInfo.EnumOrchDrives(paramPath).Select(d => d.Name);
                var wpPath = paramPathDriveNames.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みのドライブは、候補から除外する
                var paramDestination = GetParameterValues(commandAst, "Destination", Positional.UserName_Destination.Parameters, wordToComplete).Select(p => p.TrimEnd(':'));
                var wpDestination = paramDestination.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var drive in drives
                    .ExcludeByWildcards(d => d?.Name, wpPath)
                    .ExcludeByWildcards(d => d?.Name, wpDestination)
                    .Where(d => wp.IsMatch(d.NameColon)))
                {
                    string driveName = drive.NameColon;
                    string tiphelp = drive.DisplayRoot;
                    if (!string.IsNullOrEmpty(drive.Description))
                        tiphelp += $" ({drive.Description})";
                    yield return new CompletionResult(PathTools.EscapePSText(driveName), driveName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }

        protected override void ProcessRecord()
        {
            if (UserName?.Length == 0 || string.IsNullOrEmpty(UserName?[0])) UserName = null;
            if (FullName?.Length == 0 || string.IsNullOrEmpty(FullName?[0])) FullName = null;

            var wpUserName = UserName.ConvertToWildcardPatternList();
            var wpFullName = FullName.ConvertToWildcardPatternList();

            var srcDrive = OrchDriveInfo.GetOrchDrive(Path!) ?? throw new Exception("Path is not OrchDrive.");
            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

            srcDrive._dicUsers = null;
            srcDrive._dicUsersDetailed = null;

            srcDrive._dicUsers_Exception.ClearCache();

            var srcUsers = srcDrive.GetUsers()
                .FilterByWildcards(user => user?.UserName, wpUserName)
                .FilterByWildcards(user => user?.FullName, wpFullName)
                .OrderBy(role => role.UserName)
                .ToList();

            string msg = "Copying users";
            using var reporter = new ProgressReporter(this, 1, 100, msg, msg);

            int index = 0;
            reporter.TotalNum = dstDrives.Count * srcUsers.Count;

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var dstDrive in dstDrives)
            {
                if (srcDrive == dstDrive) continue;

                foreach (var srcUser in srcUsers)
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    if (dstDrive.NameColonSeparator == srcUser.Path) continue;

                    var target = $"Item: {srcUser.GetPSPath()} Destination: {dstDrive.NameColonSeparator}";

                    reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {srcUser.GetPSPath()} to {dstDrive.NameColonSeparator}");

                    if (ShouldProcess(target, "Copy User"))
                    {
                        try
                        {
                            string srcPartitionGlobalId = srcDrive.GetPartitionGlobalId();
                            string dstPartitionGlobalId = dstDrive.GetPartitionGlobalId();

                            User detailedUser = srcDrive.GetUser(srcUser);
                            if (detailedUser == null)
                            {
                                WriteError(new ErrorRecord(new OrchException(target, $"Failed to retrieve {target}."), "GetUserError", ErrorCategory.InvalidOperation, srcUser));
                                continue;
                            }

                            User newUser = OrchCollectionExtensions.DeepCopy(detailedUser);

                            if (newUser.DirectoryIdentifier == null && srcPartitionGlobalId == dstPartitionGlobalId)
                            {
                                newUser.DirectoryIdentifier = newUser.Key;
                                newUser.UserName = null;
                                newUser.Domain = "autogen";
                            }
                            else
                            {
                                newUser.DirectoryIdentifier = null;
                                // in this case, respect UserName.
                            }
                            newUser.Key = null;
                            newUser.Id = null;
                            newUser.IsEmailConfirmed = null;
                            // newUser.Path = null; // JsonIgnore 属性がついているので不要
                            newUser.TenantId = null;
                            newUser.TenantKey = null;
                            newUser.TenantDisplayName = null;
                            newUser.LastLoginTime = null;
                            newUser.LastModificationTime = null;
                            newUser.LastModifierUserId = null;
                            newUser.CreationTime = null;
                            newUser.CreatorUserId = null;
                            newUser.IsActive = null;
                            newUser.LoginProviders = null; // not sure it need to be removed
                            newUser.ProvisionType = null; // need to be removed
                            newUser.UserRoles = null; // ロール名の一覧が RolesList に入っているので、UserRoles は不要

                            if (newUser.RobotProvision != null)
                            {
                                // たぶん RobotProvision.RobotId は null にしておけば良い
                                newUser.RobotProvision.RobotId = null;
                                //newUser.RobotProvision.RobotId = OrchFolderProvider.FindDstRobot(
                                //    this, srcDrive, dstDrive, dstDrive.RootFolder!,
                                //    newUser.RobotProvision.RobotId, srcUser.GetPSPath())?.Id;
                            }
                            if (newUser.UnattendedRobot != null)
                            {
                                //if (newUser.UnattendedRobot.CredentialType != "NoCredential")
                                {
                                    newUser.UnattendedRobot.CredentialStoreId = Core.OrchProvider.FindDstCredentialStore(
                                        this, srcDrive, dstDrive, dstDrive.RootFolder!,
                                        newUser.UnattendedRobot.CredentialStoreId, srcUser.GetPSPath())?.Id;
                                }

                                // たぶん UnattendedRobot.RobotId は null にしておけば良い
                                newUser.UnattendedRobot.RobotId = null;
                                //newUser.UnattendedRobot.RobotId = OrchFolderProvider.FindDstRobot(
                                //    this, srcDrive, dstDrive, dstDrive.RootFolder!,
                                //    newUser.UnattendedRobot.CredentialStoreId, srcUser.GetPSPath())?.Id;
                            }

                            // migrating classic folders list. I am not sure this is needed;
                            if (srcUser.OrganizationUnits != null)
                            {
                                newUser.OrganizationUnits = [];
                                var srcFolders = srcDrive.GetFolders();
                                var dstFolders = dstDrive.GetFolders();
                                foreach (var ou in srcUser.OrganizationUnits)
                                {
                                    var srcFolder = srcFolders.FirstOrDefault(f => f.Id == ou.Id);
                                    if (srcFolder != null)
                                    {
                                        // find classic folder
                                        var dstFolder = dstFolders.FirstOrDefault(f =>
                                            f.ParentId == null && f.DisplayName == srcFolder.DisplayName && f.ProvisionType == "Manual");
                                        if (dstFolder != null)
                                        {
                                            var dstOu = new OrganizationUnit
                                            {
                                                Id = dstFolder.Id,
                                                DisplayName = srcFolder.DisplayName
                                            };
                                            newUser.OrganizationUnits.Add(dstOu);
                                        }
                                    }
                                }
                            }
                            //newUser.OrganizationUnits = null;

                            #region copy from OC2010 (APIver 11) to AC (APIver 18)
                            if (dstDrive.OrchAPISession.ApiVersion.HasValue && dstDrive.OrchAPISession.ApiVersion.Value > 11)
                            {
                                newUser.BypassBasicAuthRestriction = null;
                            }
                            #endregion

                            var createdUser = dstDrive.OrchAPISession.CreateUser(newUser);
                            if (createdUser != null)
                            {
                                createdUser.Path = dstDrive.NameColonSeparator;
                                //dstDrive._dicUsers?.Add(createdUser);
                                //WriteObject(createdUser);
                                if (newUser.UnattendedRobot != null && !string.IsNullOrEmpty(newUser.UnattendedRobot.Password))
                                {
                                    WriteWarning($"{createdUser.GetPSPath()}: Please update URPassword manually.");
                                }
                                dstDrive._dicUsers = null;
                                dstDrive._dicUsersDetailed = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(srcUser.GetPSPath(), ex), "CreateUserError", ErrorCategory.InvalidOperation, srcUser));
                        }
                    }
                }
            }
        }
    }
}
