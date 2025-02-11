using System.Data;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;
using User = UiPath.PowerShell.Entities.User;
using TPositional = UiPath.PowerShell.Positional.UserName_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.User))]
public class CopyUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter<TPositional>))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserFullNameCompleter<TPositional>))]
    public string[]? FullName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    public string[]? Type { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string? Path { get; set; }

    protected override void ProcessRecord()
    {
        if (UserName?.Length == 0 || string.IsNullOrEmpty(UserName?[0])) UserName = null;
        if (FullName?.Length == 0 || string.IsNullOrEmpty(FullName?[0])) FullName = null;

        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        var srcDrive = OrchDriveInfo.GetOrchDrive(Path!) ?? throw new Exception("Path is not OrchDrive.");
        var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

        srcDrive._dicUsers = null;
        srcDrive._dicUsersDetailed = null;

        srcDrive._dicUsers_Exception.ClearCache();

        var srcUsers = srcDrive.GetUsers()
            .FilterByWildcards(user => user?.UserName, wpUserName)
            .FilterByWildcards(user => user?.FullName, wpFullName)
            .FilterByWildcards(user => user?.Type, wpType)
            .OrderBy(user => user.UserName)
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

                var target = $"Item: {srcDrive.NameColonSeparator}{OrchArgumentCompleter.TipHelp(srcUser)} Destination: {dstDrive.NameColonSeparator}";

                reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {srcUser.GetPSPath()} to {dstDrive.NameColonSeparator}");

                if (ShouldProcess(target, "Copy User"))
                {
                    try
                    {
                        string srcPartitionGlobalId = srcDrive.GetPartitionGlobalId();
                        string dstPartitionGlobalId = dstDrive.GetPartitionGlobalId();

                        // 同じ組織なら DirectoryIdentifier をそのまま使っても良さそうだけど
                        // Domain は "autogen" で良いのだろうか。
                        //if (newUser.DirectoryIdentifier is null && srcPartitionGlobalId == dstPartitionGlobalId)
                        ////if (false)
                        //{
                        //    newUser.DirectoryIdentifier = newUser.Key;
                        //    newUser.UserName = null;
                        //    newUser.Domain = "autogen";
                        //}
                        //else
                        User detailedUser = srcDrive.GetUser(srcUser);
                        if (detailedUser is null)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, $"Failed to retrieve {target}."), "GetUserError", ErrorCategory.InvalidOperation, srcUser));
                            continue;
                        }

                        User newUser = OrchCollectionExtensions.DeepCopy(detailedUser);

                        // 組織の PmUser の中に見つかれば、その identifier を DirectoryIdentifier を設定しておく
                        if (DirectoryTypeItems.Items.TryGetValue(detailedUser.Type ?? "DirectoryUser", out var srcType))
                        {
                            var dstPmUser = dstDrive.SearchDirectory(newUser.UserName!)
                                .Where(u => string.Compare(u.identityName, newUser.UserName, true) == 0 && u.type == srcType)
                                .FirstOrDefault();

                            if (dstPmUser is null && !string.IsNullOrEmpty(newUser.EmailAddress) && newUser.UserName != newUser.EmailAddress)
                            {
                                // 見つからない場合は、メールアドレスでも探してみる
                                dstPmUser = dstDrive.SearchDirectory(newUser.EmailAddress)
                                    .Where(u => string.Compare(u.identityName, newUser.EmailAddress, true) == 0 && u.type == srcType)
                                    .FirstOrDefault();
                            }

                            if (dstPmUser is not null)
                            {
                                //WriteError(new ErrorRecord(new OrchException(srcUser.GetPSPath(), $"A user with the same name does not exist in the organization of {dstDrive.NameColonSeparator}."), "SearchUserError", ErrorCategory.InvalidOperation, srcUser));
                                newUser.DirectoryIdentifier = dstPmUser?.identifier;
                                newUser.Domain = dstPmUser?.domain;
                            }
                        }

                        if (!string.IsNullOrEmpty(newUser.DirectoryIdentifier))
                        {
                            newUser.UserName = null;
                            newUser.FullName = null;
                            newUser.Name = null;
                            newUser.Surname = null;
                            newUser.EmailAddress = null;
                        }
                        newUser.Domain ??= "autogen";

                        newUser.AccountId = null;
                        newUser.AuthenticationSource = null;
                        newUser.Key = null;
                        newUser.Id = null;
                        newUser.IsEmailConfirmed = null;
                        // newUser.Path = null; // JsonIgnore 属性がついているので不要
                        newUser.TenantId = null;
                        newUser.TenantKey = null;
                        newUser.TenancyName = null;
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

                        var dstRoles = dstDrive.Roles.Get();
                        List<string> rolesToBeRemoved = null;
                        foreach (var role in newUser.RolesList ?? [])
                        {
                            var destinationRole = dstRoles.FirstOrDefault(r => string.Compare(r.Name, role, StringComparison.OrdinalIgnoreCase) == 0);

                            string? targetUser = null;
                            #region コピー先に存在しないロールは警告して除外する
                            if (destinationRole is null)
                            {
                                rolesToBeRemoved ??= [];
                                rolesToBeRemoved.Add(role);
                                targetUser = System.IO.Path.Combine(dstDrive.NameColonSeparator, srcUser.UserName!);

                                WriteError(new ErrorRecord(
                                    new OrchException(
                                        System.IO.Path.Combine(srcDrive.NameColonSeparator, srcUser.UserName!), $"No role with the same name exists for '{role}' in {dstDrive.NameColonSeparator}. This role will be ignored."),
                                    "NoMatchedRoleError",
                                    ErrorCategory.ObjectNotFound,
                                    dstDrive));
                            }
                            #endregion

                            #region フォルダロールは警告して除外する
                            else if (destinationRole.Type == "Folder")
                            {
                                rolesToBeRemoved ??= [];
                                rolesToBeRemoved.Add(role);
                                targetUser ??= System.IO.Path.Combine(dstDrive.NameColonSeparator, OrchArgumentCompleter.TipHelp(srcUser));
                                WriteWarning($"{targetUser}: Folder role '{destinationRole.Name}' will be removed.");
                            }
                            #endregion
                        }

                        if (rolesToBeRemoved is not null)
                        {
                            newUser.RolesList = newUser.RolesList?.Except(rolesToBeRemoved).ToArray();
                        }

                        if (newUser.RobotProvision is not null)
                        {
                            // たぶん RobotProvision.RobotId は null にしておけば良い
                            newUser.RobotProvision.RobotId = null;
                            //newUser.RobotProvision.RobotId = OrchFolderProvider.FindDstRobot(
                            //    this, srcDrive, dstDrive, dstDrive.RootFolder!,
                            //    newUser.RobotProvision.RobotId, srcUser.GetPSPath())?.Id;
                        }
                        if (newUser.UnattendedRobot is not null)
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
                            newUser.UnattendedRobot.Password = null;

                            // TODO: UR のパスワードを更新してくれ、という警告出した方がいいのでは。
                        }

                        // migrating classic folders list. I am not sure this is needed;
                        if (srcUser.OrganizationUnits is not null)
                        {
                            newUser.OrganizationUnits = [];
                            var srcFolders = srcDrive.GetFolders();
                            var dstFolders = dstDrive.GetFolders();
                            foreach (var ou in srcUser.OrganizationUnits)
                            {
                                var srcFolder = srcFolders.FirstOrDefault(f => f.Id == ou.Id);
                                if (srcFolder is not null)
                                {
                                    // find classic folder
                                    var dstFolder = dstFolders.FirstOrDefault(f =>
                                        f.ParentId is null && f.DisplayName == srcFolder.DisplayName && f.ProvisionType == "Manual");
                                    if (dstFolder is not null)
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

                        var createdUser = dstDrive.OrchAPISession.PostUser(newUser);
                        if (createdUser is not null)
                        {
                            createdUser.Path = dstDrive.NameColonSeparator;
                            //dstDrive._dicUsers?.Add(createdUser);
                            //WriteObject(createdUser);
                            if (newUser.UnattendedRobot is not null && !string.IsNullOrEmpty(newUser.UnattendedRobot.Password))
                            {
                                WriteWarning($"{System.IO.Path.Combine(dstDrive.NameColonSeparator, OrchArgumentCompleter.TipHelp(srcUser))}: Please update -UR_Password with Update-OrchUser cmdlet.");
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
