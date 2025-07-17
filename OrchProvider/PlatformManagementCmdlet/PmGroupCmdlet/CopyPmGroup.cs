using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.GroupName_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "PmGroup", SupportsShouldProcess = true)]
public class CopyPmGroupCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter<TPositional>))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
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
            objectType, srcMembers, m => m.name ?? "",
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
                // ローカルユーザー、ロボットアカウント、アプリについてはエラーを表示すべきでない
                // グループの中にローカルグループは入っていない
                if (unresolvedEmail.source == "local" || unresolvedEmail.source == "app") continue;

                string userName = unresolvedEmail.name;
                if (!string.IsNullOrEmpty(unresolvedEmail.email))
                    userName += $" ({unresolvedEmail.email})";

                WriteWarning($"{srcGroupPath}: Failed to find {objectType} '{userName}' in '{drive.NameColonSeparator}'. Ignored.");
            }
        }
        return retIdentifiers;
    }

    // 複数のグループにまたがって、メンバーディレクトリに問い合わせるのはやりすぎだと思う。
    // 各グループごとに、そのメンバーをバルクでディレクトリに問い合わせるのが妥当な実装であろう。。
    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetPmDrive(Path);
        var dstDrives = SessionState.EnumPmDrives(Destination.Split1stValueByUnescapedCommas());
        var wpGroupName = GroupName.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

        var srcGroups = srcDrive.PmGroups.Get();
        var targetGroups = srcGroups.FilterByWildcards(g => g?.name, wpGroupName);

        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var srcGroup in targetGroups.OrderBy(g => g.name))
        {
            PmGroup srcDetailedGroup = null;
            try
            {
                srcDetailedGroup = srcDrive.PmGroups.Get(srcGroup.id);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, srcDrive));
                continue;
            }
            if (srcDetailedGroup is null) continue;

            foreach (var dstDrive in dstDrives)
            {
                if (srcDrive.GetPartitionGlobalId() == dstDrive.GetPartitionGlobalId()) continue;

                string target = $"Item: {srcGroup.GetPSPath()} Destination: {dstDrive.NameColonSeparator}";
                if (ShouldProcess(target, "Copy PmGroup"))
                {
                    try
                    {
                        // グループに含まれるメンバーを一括してディレクトリに問い合わせる
                        // ユーザー・グループ・アプリを別にして問い合わせないと。
                        List<string> directoryUserMemberIDs = [];

                        foreach (var groupedMembers in srcDetailedGroup.members?
                            .GroupBy(m => m.objectType) ?? [])
                        {
                            //Dictionary<string, PmGroupMember?>? entries = null;
                            switch (groupedMembers.Key)
                            {
                                case "DirectoryUser":
                                    directoryUserMemberIDs.AddRange(FindIdentifiers(
                                        dstDrive,
                                        srcGroup.GetPSPath(),
                                        "user",
                                        groupedMembers));
                                    break;
                                case "DirectoryGroup":
                                    directoryUserMemberIDs.AddRange(FindIdentifiers(
                                        dstDrive,
                                        srcGroup.GetPSPath(),
                                        "group",
                                        groupedMembers));
                                    break;
                                case "DirectoryApplication":
                                    directoryUserMemberIDs.AddRange(FindIdentifiers(
                                        dstDrive,
                                        srcGroup.GetPSPath(),
                                        "application",
                                        groupedMembers));
                                    break;
                                case "DirectoryRobotUser":
                                    foreach (var robot in groupedMembers)
                                    {
                                        var addingMembers = dstDrive.PmRobotAccounts.Get();
                                        var addingMember = addingMembers?
                                            .FirstOrDefault(t => string.Compare(t.name, robot.name, true) == 0);
                                        if (addingMember?.id is not null)
                                        {
                                            directoryUserMemberIDs.Add(addingMember.id);
                                        }
                                        // 当たらなかったら、黙って無視。
                                    }
                                    break;
                            }
                        }

                        // ここまでで、グループに追加するメンバーをすべて抽出済み

                        // 同名のグループを探して、あればそのグループに entries を追加する
                        var dstGroups = dstDrive.PmGroups.Get();
                        var dstGroup = dstGroups.FirstOrDefault(g => string.Compare(srcDetailedGroup.name, g.name, true) == 0);

                        PmGroup? newGroup = null;
                        if (dstGroup is null)
                        {
                            newGroup = dstDrive.CreatePmGroup(srcDetailedGroup.name, directoryUserMemberIDs);
                        }
                        else
                        {
                            var existingMemberIds = dstGroup.members?.Select(m => m.identifier) ?? [];
                            var addingMemberIds = directoryUserMemberIDs.Except(existingMemberIds) ?? [];

                            if (addingMemberIds?.Any() ?? false)
                            {
                                newGroup = dstDrive.AddMemberToPmGroup(dstGroup.id, srcDetailedGroup.name, addingMemberIds);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "CopyPmGroupError", ErrorCategory.InvalidOperation, dstDrive));
                    }
                }
            }
        }
    }
}
