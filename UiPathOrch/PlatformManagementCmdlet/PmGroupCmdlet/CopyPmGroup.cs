using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "PmGroup", SupportsShouldProcess = true)]
public class CopyPmGroupCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmGroupNameCompleter))]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? GroupName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    // objectType must be set to "user" or "application"
    private List<string> FindIdentifiers(
        OrchDriveInfo drive,
        string srcGroupPath,
        string objectType,
        IEnumerable<PmGroupMember> srcMembers)
    {
        if (!srcMembers.Any()) return [];

        List<string> retIdentifiers = [];
        List<PmGroupMember> unresolvedMembers = [];

        var entriesUsers = drive.PmBulkResolveByName(
            objectType, srcMembers, m => m.name ?? "",
            unresolvedMembers);

        retIdentifiers.AddRange(entriesUsers
            .Where(u => u.Value is not null)
            .Select(u => u.Value!.identifier!));

        // For users not found by name, also search by email
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

            // Output an error for users not found by either name or email
            foreach (var unresolvedEmail in unresolvedEmails)
            {
                // Should not display errors for local users, robot accounts, and apps
                // Local groups are not included within groups
                if (unresolvedEmail.source == "local" || unresolvedEmail.source == "app") continue;

                string userName = unresolvedEmail.name;
                if (!string.IsNullOrEmpty(unresolvedEmail.email))
                    userName += $" ({unresolvedEmail.email})";

                WriteWarning($"{srcGroupPath}: Failed to find {objectType} '{userName}' in '{drive.NameColonSeparator}'. Ignored.");
            }
        }
        return retIdentifiers;
    }

    // Querying the member directory across multiple groups would be overkill.
    // A reasonable implementation is to query the directory in bulk for each group's members.
    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetPmDrive(EffectivePath(Path, LiteralPath));
        var dstDrives = SessionState.EnumPmDrives(Destination);
        var wpGroupName = GroupName.ConvertToWildcardPatternList();

        var srcGroups = srcDrive.PmGroups.Get();
        var targetGroups = srcGroups.FilterByWildcards(g => g?.name, wpGroupName);

        using var cancelHandler = new ConsoleCancelHandler();

        foreach (var srcGroup in targetGroups.OrderBy(g => g.name).WithCancellation(cancelHandler.Token))
        {
            PmGroup srcDetailedGroup = null;
            try
            {
                srcDetailedGroup = srcDrive.PmGroups.Get(srcGroup.id);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetPmGroupError", ErrorCategory.InvalidOperation, srcDrive));
                continue;
            }
            if (srcDetailedGroup is null) continue;

            foreach (var dstDrive in dstDrives.WithCancellation(cancelHandler.Token))
            {
                if (srcDrive.GetPartitionGlobalId() == dstDrive.GetPartitionGlobalId()) continue;

                string target = $"Item: {srcGroup.GetPSPath(srcDrive.NameColonSeparator)} Destination: {dstDrive.NameColonSeparator}";
                if (ShouldProcess(target, "Copy PmGroup"))
                {
                    try
                    {
                        // Query the directory in bulk for all members in the group.
                        // Users, groups, and apps must be queried separately.
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
                                        srcGroup.GetPSPath(srcDrive.NameColonSeparator),
                                        "user",
                                        groupedMembers));
                                    break;
                                case "DirectoryGroup":
                                    directoryUserMemberIDs.AddRange(FindIdentifiers(
                                        dstDrive,
                                        srcGroup.GetPSPath(srcDrive.NameColonSeparator),
                                        "group",
                                        groupedMembers));
                                    break;
                                case "DirectoryApplication":
                                    directoryUserMemberIDs.AddRange(FindIdentifiers(
                                        dstDrive,
                                        srcGroup.GetPSPath(srcDrive.NameColonSeparator),
                                        "application",
                                        groupedMembers));
                                    break;
                                case "DirectoryRobotUser":
                                    foreach (var robot in groupedMembers)
                                    {
                                        var addingMembers = dstDrive.PmRobotAccounts.Get();
                                        var addingMember = addingMembers?
                                            .FirstOrDefault(t => string.Compare(t.name, robot.name, StringComparison.OrdinalIgnoreCase) == 0);
                                        if (addingMember?.id is not null)
                                        {
                                            directoryUserMemberIDs.Add(addingMember.id);
                                        }
                                        // If not found, silently ignore.
                                    }
                                    break;
                            }
                        }

                        // At this point, all members to add to the group have been extracted.

                        // Find a group with the same name; if found, add entries to that group.
                        var dstGroups = dstDrive.PmGroups.Get();
                        var dstGroup = dstGroups.FirstOrDefault(g => string.Compare(srcDetailedGroup.name, g.name, StringComparison.OrdinalIgnoreCase) == 0);

                        PmGroup? newGroup = null;
                        if (dstGroup is null)
                        {
                            newGroup = dstDrive.CreatePmGroup(srcDetailedGroup.name, directoryUserMemberIDs);
                        }
                        else
                        {
                            var existingMemberIds = dstGroup.members?.Select(m => m.identifier) ?? [];
                            var addingMemberIds = directoryUserMemberIDs.Except(existingMemberIds);

                            if (addingMemberIds.Any())
                            {
                                newGroup = dstDrive.AddMemberToPmGroup(dstGroup.id, srcDetailedGroup.name, addingMemberIds);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
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
