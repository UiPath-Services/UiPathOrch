using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Entities;
using System.Management.Automation.Language;
using System.Xml.Linq;

namespace UiPath.PowerShell.Commands
{
    // WIP
    [Cmdlet(VerbsCommon.Copy, "OrchPmGroup", SupportsShouldProcess = true)]
    class CopyPmGroupCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PmGroupNameCompleter<GroupName_Destination>))]
        [SupportsWildcards]
        public string[]? GroupName { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationDriveCompleter<GroupName_Destination>))]
        public string[]? Destination { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<GroupName_Destination>))]
        public string? Path { get; set; }

        // objectType には "user" もしくは "application" を指定する必要がある
        private List<string> FindIdentifiers(
            OrchDriveInfo drive,
            string srcGroupPath,
            string objectType,
            IEnumerable<PmGroupMember> list)
        {
            int listCount = list.Count();
            if (listCount == 0) return [];

            List<string> identifiers = [];

            var entriesUsers = drive.PmBulkResolveByName(objectType, list.Select(m => m.name!));
            identifiers.AddRange(entriesUsers.Select(u => u.identifier!));

            // 見つからなかったユーザーを確認する
            if (listCount != identifiers.Count)
            {
                var dicEntriesUsers = entriesUsers.ToDictionary(m => m.name!, m => m, StringComparer.OrdinalIgnoreCase);
                List<PmGroupMember> notFoundUsers = [];
                foreach (var memberUser in list)
                {
                    if (!dicEntriesUsers.ContainsKey(memberUser.name!))
                    {
                        notFoundUsers.Add(memberUser);
                    }
                }
                if (notFoundUsers.Count != 0)
                {
                    var entriesUsersByEmail = drive.PmBulkResolveByName(
                        objectType,
                        notFoundUsers
                            .Where(u => !string.IsNullOrEmpty(u.email))
                            .Select(u => u.email!));
                    identifiers.AddRange(entriesUsersByEmail.Select(u => u.identifier!));

                    if (notFoundUsers.Count != entriesUsersByEmail.Count)
                    {
                        // それでも見つからなかったユーザーについては警告を表示
                        var dicEntriesUsersByEmail = entriesUsersByEmail.ToDictionary(m => m.name!, m => m, StringComparer.OrdinalIgnoreCase);
                        foreach (var memberUser in notFoundUsers)
                        {
                            if (!dicEntriesUsersByEmail.ContainsKey(memberUser.name!))
                            {
                                string userName = memberUser.name;
                                if (!string.IsNullOrEmpty(memberUser.email))
                                    userName += $" {(memberUser.email)}";

                                WriteError(new ErrorRecord(
                                    new OrchException(
                                        drive.NameColonSeparator,
                                        $"Copying '{srcGroupPath}': Failed to find '{userName}' ({memberUser.email}) in '{drive.NameColonSeparator}'. Ignored."),
                                    "DirectorySearchFailed",
                                    ErrorCategory.InvalidOperation,
                                    drive
                                ));
                            }
                        }
                    }
                }
            }

            return identifiers;
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

                // 結果は srcGroups にキャッシュされる
                var results = ParallelResults.ForEach(srcGroups, g => srcDrive.GetPmGroup(g.id));
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, srcDrive));
                return;
            }

            if (srcGroups == null) return;

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

                        PmGroupMember[]? members = srcGroup.members;
                        List<DirectoryUser> membersUsers = (members?.Where(m => m.objectType == "DirectoryUser") ?? []).Cast<DirectoryUser>().ToList();
                        List<DirectoryApplication> membersApps = (members?.Where(m => m.objectType == "DirectoryApplication") ?? []).Cast<DirectoryApplication>().ToList();
                        List<PmGroupMember> membersOthers = (members?.Where(m => m.objectType != "DirectoryUser" && m.objectType != "DirectoryApplication") ?? []).ToList();

                        // users と apps については、BulkResolveByName() を呼び出す必要がある
                        directoryUserMemberIDs.AddRange(FindIdentifiers(
                            dstDrive,
                            srcGroup.GetPSPath(),
                            "user",
                            membersUsers));

                        directoryUserMemberIDs.AddRange(FindIdentifiers(
                            dstDrive,
                            srcGroup.GetPSPath(),
                            "application",
                            membersApps));

                        // ユーザーとアプリ以外でも、もしかしてこれで動く？？
                        directoryUserMemberIDs.AddRange(FindIdentifiers(
                            dstDrive,
                            srcGroup.GetPSPath(),
                            "robot",
                            membersApps));



                        #region この実装で問題ないが、バルクで処理できたい。
#if false
                        foreach (var srcMember in srcGroup.members ?? [])
                        {
                            try
                            {

                                // この try の中の処理は、ほぼ Add-OrchPmGroupMember cmdlet と同じだ。。
                                var addingMember = dstDrive.SearchPmDirectoryUsers(srcMember.name!)?
                                    .FirstOrDefault(t => string.Compare(t.identityName, srcMember.name, true) == 0);

                                if (addingMember == null)
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
                                    if (entry?.identifier != null)
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
                            if (newGroup != null)
                            {
                                newGroup.Path = dstDrive.NameColonSeparator;
                                WriteObject(newGroup);
                                dstDrive._dicPmDirectoryUsers = null;
                                dstDrive._dicSearchForUsersAndGroups = null;
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
}
