using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.GroupName_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchPmGroup", SupportsShouldProcess = true)]
class CopyPmGroupCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
    public string[]? Destination { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string? Path { get; set; }

    // objectType には "user" もしくは "application" を指定する必要がある
    private List<string> FindIdentifiers(
        OrchDriveInfo drive,
        string srcGroupPath,
        string objectType,
        IEnumerable<PmGroupMember> srcMembers)
    {
        int listCount = srcMembers.Count();
        if (listCount == 0) return [];

        List<string> retIdentifiers = [];
        List<PmGroupMember> unresolvedMembers = [];

        var entriesUsers = drive.PmBulkResolveByName(
            objectType, srcMembers, m => m.name!,
            unresolvedMembers);

        retIdentifiers.AddRange(entriesUsers
            .Where(u => u.Value is not null)
            .Select(u => u.Value!.identifier!));

        // name で見つからなかったユーザーは、email でも検索する
        if (unresolvedMembers.Count > 0)
        {
            List<PmGroupMember> unresolvedEmails = [];
            var entriesEmails = drive.PmBulkResolveByName(
                objectType,
                unresolvedMembers,
                m => m.email!,
                unresolvedEmails);

            retIdentifiers.AddRange(entriesEmails
                .Where(u => u.Value is not null)
                .Select(u => u.Value!.identifier!));

            // name でも email でも見つからなかったユーザーについては、エラーを出力
            foreach (var unresolvedEmail in unresolvedEmails)
            {
                string userName = unresolvedEmail.name;
                if (!string.IsNullOrEmpty(unresolvedEmail.email))
                    userName += $" ({unresolvedEmail.email})";

                WriteWarning($"{srcGroupPath}: Failed to find {objectType} '{userName}' in '{drive.NameColonSeparator}'. Ignored.");

                //WriteError(new ErrorRecord(
                //    new OrchException(
                //        drive.NameColonSeparator,
                //        $"Copying '{srcGroupPath}': Failed to find '{userName}' in '{drive.NameColonSeparator}'. Ignored."),
                //    "DirectorySearchFailed",
                //    ErrorCategory.InvalidOperation,
                //    drive
                //));
            }
        }
        return retIdentifiers;
    }

    protected override void ProcessRecord()
    {
        GroupName = GroupName.Split1stValueByUnescapedCommas()?.ToArray();
        Destination = Destination.Split1stValueByUnescapedCommas()?.ToArray();

        var srcDrive = OrchDriveInfo.GetOrchDrive(Path);
        var dstDrives = OrchDriveInfo.EnumOrchDrives(Destination);
        var wpGroupName = GroupName.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        IEnumerable<Entities.PmGroup> srcGroups;
        try
        {
            srcGroups = srcDrive.GetPmGroups().Values
                .FilterByWildcards(g => g?.name, wpGroupName)
                .OrderBy(g => g.name);

            // GetPmGroup() の結果は srcGroups にキャッシュされる
            var results = ParallelResults.ForEach(srcGroups, g => srcDrive.GetPmGroup(g.id));
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }

        if (srcGroups is null) return;

        string srcPartitionGlobalId = null;
        try
        {
            srcPartitionGlobalId = srcDrive.GetPartitionGlobalId();
        }
        catch { } // この例外は握りつぶして良い

        foreach (var dstDrive in dstDrives)
        {
            string dstPartitionGlobalId;
            try
            {
                dstPartitionGlobalId = dstDrive.GetPartitionGlobalId();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    new OrchException(dstDrive.NameColonSeparator, ex), "GetPartitionGlobalIdError", ErrorCategory.InvalidOperation, dstDrive));
                continue;
            }

            if (string.IsNullOrEmpty(dstPartitionGlobalId)) continue;

            if (srcPartitionGlobalId == dstPartitionGlobalId) continue;

            foreach (var srcGroup in srcGroups)
            {
                string target = $"Item: {srcGroup.GetPSPath()} Destination: {dstDrive.NameColonSeparator}";
                if (ShouldProcess(target, "Copy PmGroup"))
                {
                    List<string> directoryUserMemberIDs = [];

                    foreach (var groupedMembers in srcGroup.members?
                        .GroupBy(m => m.objectType) ?? [])
                    {
                        //Dictionary<string, PmGroupMember?>? entries = null;
                        switch (groupedMembers.Key)
                        {
                            case "DirectoryUser":
                                //entries = drive.PmBulkResolveByName("user", drive_type_userNames, m => m.UserName);
                                directoryUserMemberIDs.AddRange(FindIdentifiers(
                                    dstDrive,
                                    srcGroup.GetPSPath(),
                                    "user",
                                    groupedMembers));
                                break;
                            case "DirectoryGroup":
                                //entries = drive.PmBulkResolveByName("group", drive_type_userNames, m => m.UserName);
                                directoryUserMemberIDs.AddRange(FindIdentifiers(
                                    dstDrive,
                                    srcGroup.GetPSPath(),
                                    "group",
                                    groupedMembers));
                                break;
                            case "DirectoryApplication":
                                //entries = drive.PmBulkResolveByName("application", drive_type_userNames, m => m.UserName);
                                directoryUserMemberIDs.AddRange(FindIdentifiers(
                                    dstDrive,
                                    srcGroup.GetPSPath(),
                                    "application",
                                    groupedMembers));
                                break;
                            case "DirectoryRobot":
                                foreach (var robot in groupedMembers)
                                {
                                    var addingMember = dstDrive.SearchPmDirectory(robot.name!)?
                                        .Where(t => t.objectType == "DirectoryRobot")
                                        .FirstOrDefault(t => string.Compare(t.identityName, robot.name, true) == 0);

                                    // 当たらなかったら警告を表示
                                    if (addingMember is null)
                                    {
                                        WriteWarning($"\"{dstDrive.NameColonSeparator}\": robot \"{robot.name}\" not found. Ignoring.");
                                    }
                                }
                                break;
                        }

                            //foreach (var entry in entries ?? [])
                            //{
                            //    if (entry.Value is null)
                            //    {
                            //        WriteWarning($"\"{drive.NameColonSeparator}\": \"{entry.Key}\" ({type}) not found. Ignoring.");
                            //    }
                            //    else if (entry.Value.objectType == "DirectoryGroup" && entry.Value.source == "local")
                            //    {
                            //        WriteWarning($"\"{drive.NameColonSeparator}\": \"{entry.Key}\" ({type}) cannot be added because it is a local group. Ignoring.");
                            //    }
                            //}
                        }


                    //    PmGroupMember[]? members = srcGroup.members;
                    //List<DirectoryUser> membersUsers = (members?.Where(m => m.objectType == "DirectoryUser") ?? []).Cast<DirectoryUser>().ToList();
                    //List<DirectoryApplication> membersApps = (members?.Where(m => m.objectType == "DirectoryApplication") ?? []).Cast<DirectoryApplication>().ToList();
                    //List<PmGroupMember> membersGroups = (members?.Where(m => m.objectType == "DirectoryGroup") ?? []).ToList();
                    //List<PmGroupMember> membersOthers = (members?.Where(m => m.objectType != "DirectoryUser" && m.objectType != "DirectoryApplication") ?? []).ToList();

                    // user と application と group については、BulkResolveByName() を呼び出す必要がある



                    // それ以外のやつは、ひとつずつ SearchPmDirectoryUsers() で探す。。
//                        foreach (var member in membersOthers)
                    {
//                            var addingMember = dstDrive.SearchPmDirectoryUsers(member.name!)?
//                              .FirstOrDefault(t => string.Compare(t.identityName, member.name, true) == 0);
                        //if (member is not null && !string.IsNullOrEmpty(member.identifier))
                        //{
                        //    directoryUserMemberIDs.Add(member.identifier);
                        //}
                    }

                    // ユーザーとアプリ以外でも、もしかしてこれで動く？？
                    //directoryUserMemberIDs.AddRange(FindIdentifiers(
                    //    dstDrive,
                    //    srcGroup.GetPSPath(),
                    //    "robot",
                    //    membersRobots));



                    #region この実装で問題ないが、バルクで処理できたい。
#if false
                    foreach (var srcMember in srcGroup.members ?? [])
                    {
                        try
                        {

                            // この try の中の処理は、ほぼ Add-OrchPmGroupMember cmdlet と同じだ。。
                            var addingMember = dstDrive.SearchPmDirectoryUsers(srcMember.name!)?
                                .FirstOrDefault(t => string.Compare(t.identityName, srcMember.name, true) == 0);

                            if (addingMember is null)
                            {
                                WriteError(new ErrorRecord(
                                    new OrchException(
                                        dstDrive.NameColonSeparator,
                                        $"Copying '{srcGroup.GetPSPath()}': Failed to find '{srcMember.name}' in '{dstDrive.NameColonSeparator}'. Ignored."),
                                    "DirectorySearchFailed",
                                    ErrorCategory.InvalidOperation,
                                    dstDrive
                                ));
                                continue;
                            }

                            // ユーザーもしくはアプリの場合に限って、BulkResolveByName() を呼び出す必要がある
                            if (addingMember.objectType == "DirectoryUser" || addingMember.objectType == "Application")
                            {
                                string searchKey = addingMember.objectType == "DirectoryUser" ? "user" : "application";

                                var entries = dstDrive.PmBulkResolveByName(searchKey, [addingMember.identityName!]);
                                var entry = entries.FirstOrDefault(e => string.Compare(e.name, srcMember.name, true) == 0);
                                if (entry?.identifier is not null)
                                {
                                    directoryUserMemberIDs.Add(entry.identifier);
                                }
                                else
                                {
                                    WriteWarning($"\"{srcGroup.name}\": No match found for '{srcMember.name}' ({addingMember.identityName}).");
                                    continue;
                                }
                            }
                            else
                            {
                                directoryUserMemberIDs.Add(addingMember.identifier!);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(
                                new OrchException(
                                    dstDrive.NameColonSeparator,
                                    $"Failed to find '{srcMember.name}' in target directory.",
                                    ex
                                ),
                                "DirectorySearchFailed",
                                ErrorCategory.InvalidOperation,
                                dstDrive
                            ));
                        }
                    }
#endif
                    #endregion

                    CreateGroupCommand createGroupCommand = new()
                    {
                        partitionGlobalId = dstPartitionGlobalId,
                        id = Guid.NewGuid().ToString(),
                        name = srcGroup.name,
                        directoryUserMemberIDs = directoryUserMemberIDs.ToArray()
                    };

                    try
                    {
                        var newGroup = dstDrive.OrchAPISession.CreatePmGroup(createGroupCommand);
                        if (newGroup is not null)
                        {
                            newGroup.Path = dstDrive.NameColonSeparator;
                            WriteObject(newGroup);
                            dstDrive._dicSearchPmDirectory = null;
                            dstDrive._dicSearchDirectory = null;
                            dstDrive._dicPmGroups = null;
                            dstDrive._dicPmGroups_Exception.ClearCache();
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "CopyPmGroupError", ErrorCategory.InvalidOperation, createGroupCommand));
                    }
                }
            }
        }
    }
}
