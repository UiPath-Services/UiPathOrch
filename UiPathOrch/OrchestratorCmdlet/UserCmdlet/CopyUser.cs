using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;
using User = UiPath.PowerShell.Entities.User;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchUser", SupportsShouldProcess = true)]
public class CopyUserCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserFullNameCompleter))]
    public string[]? FullName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<DirectoryTypeItems, int>))]
    [ValidateDictionaryKey<DirectoryTypeItems, int>(AllowWildcard = true)]
    public string[]? Type { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter]
    public string? UserMappingCsv { get; set; }

    internal static void CopyUsers(
        IWritableHost _this,
        OrchDriveInfo srcDrive,
        List<WildcardPattern>? wpUserName,
        List<WildcardPattern>? wpFullName,
        List<WildcardPattern>? wpType,
        IList<OrchDriveInfo> dstDrives,
        bool shouldProcess, CancellationToken cancelToken,
        Dictionary<string, string>? userMapping = null)
    {
        srcDrive.Users.ClearCache();
        srcDrive.UsersDetailed.ClearCache();

        var srcUsers = srcDrive.Users.Get()
            .FilterByWildcardsAny([user => user?.UserName, user => user?.EmailAddress], wpUserName)
            .FilterByWildcards(user => user?.FullName, wpFullName)
            .FilterByWildcards(user => user?.Type, wpType)
            .OrderBy(user => user.UserName)
            .ToList();

        // Fixed label, destination in Context: see CopyCalendar for why the destination may
        // not go in the activity of a bar that is already on screen.
        using var reporter = new ProgressReporter(_this, dstDrives.Count * srcUsers.Count, "Users");

        int index = 0;

        foreach (var dstDrive in dstDrives)
        {
            // Within one tenant only with a mapping CSV: each mapped user gets the source
            // user's tenant roles and robot settings (a domain or identity-provider change).
            bool inPlace = srcDrive == dstDrive && userMapping is not null;
            if (srcDrive == dstDrive && !inPlace) continue;

            foreach (var srcUser in srcUsers)
            {
                cancelToken.ThrowIfCancellationRequested();

                string? inPlaceName = null;
                if (inPlace)
                {
                    if (srcUser.UserName is null || !userMapping!.TryGetValue(srcUser.UserName, out inPlaceName)) continue;
                }
                else if (dstDrive.NameColonSeparator == srcUser.Path) continue;

                var target = inPlace
                    ? $"Item: {srcDrive.NameColonSeparator}{OrchArgumentCompleter.TipHelp(srcUser)} Destination: {dstDrive.NameColonSeparator}{inPlaceName}"
                    : $"Item: {srcDrive.NameColonSeparator}{OrchArgumentCompleter.TipHelp(srcUser)} Destination: {dstDrive.NameColonSeparator}";

                reporter.Context = dstDrive.NameColonSeparator;
                reporter.WriteProgress(++index, srcUser.UserName);

                if (shouldProcess || _this.ShouldProcess(target, "Copy User"))
                {
                    try
                    {
                        // The mapped user may already be in the tenant -- signed in once through a
                        // directory group, or copied on an earlier run. Then only the tenant roles
                        // it lacks are added; its own settings are left alone.
                        if (inPlace && FindTenantUser(dstDrive.Users.Get(), inPlaceName!, srcUser.Type) is User existing)
                        {
                            AddMissingTenantRoles(_this, srcDrive, srcUser, dstDrive, existing, cancelToken);
                            continue;
                        }

                        string srcPartitionGlobalId = srcDrive.GetPartitionGlobalId();
                        string dstPartitionGlobalId = dstDrive.GetPartitionGlobalId();

                        // For the same organization, we could probably reuse DirectoryIdentifier as-is,
                        // but is "autogen" the right value for Domain?
                        //if (newUser.DirectoryIdentifier is null && srcPartitionGlobalId == dstPartitionGlobalId)
                        ////if (false)
                        //{
                        //    newUser.DirectoryIdentifier = newUser.Key;
                        //    newUser.UserName = null;
                        //    newUser.Domain = "autogen";
                        //}
                        //else
                        User detailedUser = srcDrive.UsersDetailed.Get(srcUser.Id!.Value);
                        if (detailedUser is null)
                        {
                            _this.WriteError(new ErrorRecord(new OrchException(target, $"Failed to retrieve {target}."), "GetUserError", ErrorCategory.InvalidOperation, srcUser));
                            continue;
                        }

                        User newUser = OrchCollectionExtensions.DeepCopy(detailedUser);

                        // If srcType has an unexpected value, default to the number corresponding to "DirectoryUser".
                        // However, unexpected values may appear in future versions, so perhaps we should issue a warning.
                        var srcType = DirectoryTypeItems.Items.GetValueOrDefault(detailedUser.Type ?? "DirectoryUser", DirectoryTypeItems.Items["DirectoryUser"]);

                        // Name resolution via UserMappingCsv
                        if (userMapping is not null && userMapping.TryGetValue(newUser.UserName!, out var mappedName)
                            && !string.IsNullOrEmpty(mappedName))
                        {
                            newUser.UserName = mappedName;
                        }

                        // If found among the organization's PmUsers, set the identifier as DirectoryIdentifier
                        var dstPmUsers = dstDrive.SearchDirectory(newUser.UserName!)
                            .Where(u => string.Compare(u.identityName, newUser.UserName, StringComparison.OrdinalIgnoreCase) == 0 && u.type == srcType)
                            .ToList();
                        DirectoryObject dstPmUser = null;
                        if (dstPmUsers.Count == 1) dstPmUser = dstPmUsers.First(); // Only process when exactly one match is found

                        if (inPlace && dstPmUser is null)
                        {
                            // Within one tenant the e-mail fallback below would find the source user itself.
                            _this.WriteError(new ErrorRecord(new OrchException(target, $"'{inPlaceName}' was not found in the directory of {dstDrive.NameColonSeparator}."), "SearchUserError", ErrorCategory.ObjectNotFound, srcUser));
                            continue;
                        }

                        if (dstPmUser is null && !string.IsNullOrEmpty(newUser.EmailAddress) && newUser.UserName != newUser.EmailAddress)
                        {
                            // If not found, also try searching by email address
                            dstPmUser = dstDrive.SearchDirectory(newUser.EmailAddress)
                                .Where(u => string.Compare(u.identityName, newUser.EmailAddress, StringComparison.OrdinalIgnoreCase) == 0 && u.type == srcType)
                                .FirstOrDefault();
                        }

                        if (dstPmUser is not null)
                        {
                            //WriteError(new ErrorRecord(new OrchException(srcUser.GetPSPath(), $"A user with the same name does not exist in the organization of {dstDrive.NameColonSeparator}."), "SearchUserError", ErrorCategory.InvalidOperation, srcUser));
                            newUser.DirectoryIdentifier = dstPmUser.identifier;
                            newUser.Domain = dstPmUser.domain;
                        }
                        else
                        {
                            _this.WriteWarning($"\"{dstDrive.NameColonSeparator}\": Failed to retrieve {newUser.UserName}. Ignoring.");
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
                        // newUser.Path = null; // Not needed since it has the JsonIgnore attribute
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
                        newUser.UserRoles = null; // UserRoles is not needed since role names are stored in RolesList

                        var dstRoles = dstDrive.Roles.Get();
                        List<string> rolesToBeRemoved = null;
                        foreach (var role in newUser.RolesList ?? [])
                        {
                            var destinationRole = dstRoles.FirstOrDefault(r => string.Compare(r.Name, role, StringComparison.OrdinalIgnoreCase) == 0);

                            string? targetUser = null;
                            #region Warn and exclude roles that do not exist at the destination
                            if (destinationRole is null)
                            {
                                rolesToBeRemoved ??= [];
                                rolesToBeRemoved.Add(role);
                                targetUser = System.IO.Path.Combine(dstDrive.NameColonSeparator, srcUser.UserName!);

                                _this.WriteError(new ErrorRecord(
                                    new OrchException(
                                        System.IO.Path.Combine(srcDrive.NameColonSeparator, srcUser.UserName!), $"No role with the same name exists for '{role}' in {dstDrive.NameColonSeparator}. This role will be ignored."),
                                    "NoMatchedRoleError",
                                    ErrorCategory.ObjectNotFound,
                                    dstDrive));
                            }
                            #endregion

                            #region Warn and exclude folder roles
                            else if (destinationRole.Type == "Folder")
                            {
                                rolesToBeRemoved ??= [];
                                rolesToBeRemoved.Add(role);
                                targetUser ??= System.IO.Path.Combine(dstDrive.NameColonSeparator, OrchArgumentCompleter.TipHelp(srcUser));
                                _this.WriteWarning($"\"{targetUser}\": Folder role '{destinationRole.Name}' will be removed.");
                            }
                            #endregion
                        }

                        if (rolesToBeRemoved is not null)
                        {
                            newUser.RolesList = newUser.RolesList?.Except(rolesToBeRemoved).ToArray();
                        }

                        if (newUser.RobotProvision is not null)
                        {
                            // Setting RobotProvision.RobotId to null should be sufficient
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
                                    _this, srcDrive, dstDrive, dstDrive.RootFolder!,
                                    newUser.UnattendedRobot.CredentialStoreId, srcUser.GetPSPath())?.Id;
                            }

                            // Setting UnattendedRobot.RobotId to null should be sufficient
                            newUser.UnattendedRobot.RobotId = null;
                            //newUser.UnattendedRobot.RobotId = OrchFolderProvider.FindDstRobot(
                            //    this, srcDrive, dstDrive, dstDrive.RootFolder!,
                            //    newUser.UnattendedRobot.CredentialStoreId, srcUser.GetPSPath())?.Id;
                            newUser.UnattendedRobot.Password = null;

                            // Within one tenant the source user still holds the account, and
                            // Orchestrator allows one robot per Windows account ("There already
                            // exists a floating robot for CORP\svc_rpa!", 25.10, 2026-10-08), and an
                            // unattended robot without an account is refused too ("Username should
                            // be present in the Provision Settings."). So the user is created
                            // without one, and the robot is moved by hand when the users switch
                            // over -- after a domain change it is often a new account anyway.
                            if (inPlace && !string.IsNullOrEmpty(newUser.UnattendedRobot.UserName))
                            {
                                _this.WriteWarning($"\"{dstDrive.NameColonSeparator}{inPlaceName}\": Created without the unattended robot of '{srcUser.UserName}', which runs as '{newUser.UnattendedRobot.UserName}': Orchestrator allows one robot per Windows account. When switching over, move the triggers that run on that robot (Find-OrchAccountReference lists them), take the robot off '{srcUser.UserName}' (Update-OrchUser -MayHaveUnattendedSession false), and set it here (Update-OrchUser -MayHaveUnattendedSession true -UR_UserName -UR_Password).");
                                newUser.UnattendedRobot = null;
                                newUser.MayHaveUnattendedSession = false;
                            }
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
                            // The API never returns the robot's password, so it was not copied.
                            if (!inPlace && NeedsUnattendedPassword(detailedUser.UnattendedRobot))
                            {
                                string created = System.IO.Path.Combine(dstDrive.NameColonSeparator, OrchArgumentCompleter.TipHelp(srcUser));
                                _this.WriteWarning($"\"{created}\": The unattended robot's password of '{detailedUser.UnattendedRobot!.UserName}' is not copied. Set it with Update-OrchUser -UR_Password.");
                            }
                            dstDrive.Users.ClearCache();
                            dstDrive.UsersDetailed.ClearCache();
                        }
                    }
                    catch (Exception ex)
                    {
                        _this.WriteError(new ErrorRecord(new OrchException(srcUser.GetPSPath(), ex), "CreateUserError", ErrorCategory.InvalidOperation, srcUser));
                    }
                }
            }
        }
    }

    // The tenant user an in-place copy lands on, by user name or e-mail and of the same type.
    internal static User? FindTenantUser(IEnumerable<User> users, string name, string? type) =>
        users.FirstOrDefault(u =>
            string.Equals(u.Type, type, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(u.UserName, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.EmailAddress, name, StringComparison.OrdinalIgnoreCase)));

    // The source's tenant roles the existing user lacks. Folder roles and roles the tenant no
    // longer has are left out, as on a create.
    internal static string[] MissingTenantRoles(IEnumerable<string>? sourceRoles, IEnumerable<string>? existingRoles, IEnumerable<Role> tenantRoles)
    {
        var have = new HashSet<string>(existingRoles ?? [], StringComparer.OrdinalIgnoreCase);
        var tenantRoleNames = new HashSet<string>(
            tenantRoles.Where(r => r.Type != "Folder" && r.Name is not null).Select(r => r.Name!), StringComparer.OrdinalIgnoreCase);
        return (sourceRoles ?? [])
            .Where(r => tenantRoleNames.Contains(r) && !have.Contains(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // A password lives in Orchestrator only for the default credential type with no external
    // name; then the copy, which cannot read it, leaves the robot without one.
    internal static bool NeedsUnattendedPassword(UnattendedRobot? ur) =>
        ur is not null
        && !string.IsNullOrEmpty(ur.UserName)
        && (string.IsNullOrEmpty(ur.CredentialType) || string.Equals(ur.CredentialType, "Default", StringComparison.OrdinalIgnoreCase))
        && string.IsNullOrEmpty(ur.CredentialExternalName);

    private static void AddMissingTenantRoles(IWritableHost _this, OrchDriveInfo srcDrive, User srcUser,
        OrchDriveInfo dstDrive, User existing, CancellationToken cancelToken)
    {
        cancelToken.ThrowIfCancellationRequested();
        string target = existing.GetPSPath();

        var detailedSrc = srcDrive.UsersDetailed.Get(srcUser.Id!.Value);
        var detailedDst = dstDrive.UsersDetailed.Get(existing.Id!.Value);
        if (detailedSrc is null || detailedDst is null)
        {
            _this.WriteError(new ErrorRecord(new OrchException(target, $"Failed to retrieve {target}."), "GetUserError", ErrorCategory.InvalidOperation, existing));
            return;
        }

        if (!string.IsNullOrEmpty(detailedSrc.UnattendedRobot?.UserName) && string.IsNullOrEmpty(detailedDst.UnattendedRobot?.UserName))
        {
            _this.WriteWarning($"\"{target}\": Already in the tenant, so its robot settings are kept. '{srcUser.UserName}' runs its unattended robot as '{detailedSrc.UnattendedRobot!.UserName}'; set it with Update-OrchUser if this user should too.");
        }

        var missing = MissingTenantRoles(detailedSrc.RolesList, detailedDst.RolesList, dstDrive.Roles.Get());
        if (missing.Length == 0)
        {
            _this.WriteVerbose($"\"{target}\": Already has the tenant roles of '{srcUser.UserName}'.");
            return;
        }

        detailedDst.LoginProviders = null;
        detailedDst.CreatorUserId = null;
        detailedDst.UserRoles = null;
        detailedDst.RolesList = (detailedDst.RolesList ?? []).Concat(missing).ToArray();
        dstDrive.OrchAPISession.PutUser(detailedDst);
        _this.WriteVerbose($"\"{target}\": Added the tenant roles {string.Join(", ", missing.Select(r => $"'{r}'"))}.");
        dstDrive.Users.ClearCache();
        dstDrive.UsersDetailed.ClearCache();
    }

    protected override void ProcessRecord()
    {
        if (UserName?.Length == 0 || string.IsNullOrEmpty(UserName?[0])) UserName = null;
        if (FullName?.Length == 0 || string.IsNullOrEmpty(FullName?[0])) FullName = null;

        var wpUserName = UserName.ConvertToWildcardPatternList();
        var wpFullName = FullName.ConvertToWildcardPatternList();
        var wpType = Type.ConvertToWildcardPatternList();

        var srcDrive = SessionState.GetOrchDrive(EffectivePath(Path, LiteralPath)!) ?? throw new InvalidOperationException($"'{Path}' is not a valid UiPathOrch drive.");
        var dstDrives = SessionState.EnumDestinationDrives(Destination!);

        using var cancelHandler = new ConsoleCancelHandler();
        var userMapping = dstDrives.Count == 1
            ? SessionState?.LoadUserMappingCsv(this, srcDrive, dstDrives[0], UserMappingCsv, allowSameDrive: true)
            : null;
        CopyUsers(this, srcDrive, wpUserName, wpFullName, wpType, dstDrives, false, cancelHandler.Token, userMapping);
    }
}
