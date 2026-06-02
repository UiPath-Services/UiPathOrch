using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Copies a user's identity settings from the source organization to the
// same-named user in each destination organization.
[Cmdlet(VerbsCommon.Copy, "PmUserPreference", SupportsShouldProcess = true)]
[OutputType(typeof(PmUserPreference))]
public class CopyPmUserPreferenceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserPreferenceUserNameCompleter))]
    [SupportsWildcards]
    public string[]? UserName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetPmDrive(Path!) ?? throw new InvalidOperationException($"'{Path}' is not a valid UiPathOrch drive.");
        var dstDrives = SessionState.EnumDestinationDrives(Destination!);
        var wpUserName = UserName.ConvertToWildcardPatternList();

        string? srcPartition;
        try
        {
            srcPartition = srcDrive.GetPartitionGlobalId();
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetGlobalPartitionIdError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }
        if (string.IsNullOrEmpty(srcPartition)) return;

        var srcUsers = srcDrive.PmUsers.Get()
            .Where(u => u is not null && !string.IsNullOrEmpty(u.userName))
            .FilterByWildcards(u => u!.userName!, wpUserName)
            .OrderBy(u => u!.userName)
            .ToList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var srcUser in srcUsers.WithCancellation(cancelHandler.Token))
        {
            PmUserSettingDto[]? settings;
            try
            {
                settings = srcDrive.OrchAPISession.GetUserSettings(srcPartition, srcUser!.id!);
            }
            catch (Exception ex)
            {
                string t = System.IO.Path.Combine(srcDrive.NameColonSeparator, srcUser!.userName!);
                WriteError(new ErrorRecord(new OrchException(t, ex), "GetPmUserPreferenceError", ErrorCategory.InvalidOperation, t));
                continue;
            }

            var settingList = (settings ?? [])
                .Where(s => !string.IsNullOrEmpty(s.key))
                .Select(s => new Entities.KeyValuePair(s.key, s.value))
                .ToList();
            if (settingList.Count == 0) continue;

            foreach (var dstDrive in dstDrives.WithCancellation(cancelHandler.Token))
            {
                string? dstPartition;
                try
                {
                    dstPartition = dstDrive.GetPartitionGlobalId();
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(dstDrive.NameColonSeparator, ex), "GetGlobalPartitionIdError", ErrorCategory.InvalidOperation, dstDrive));
                    continue;
                }
                if (string.IsNullOrEmpty(dstPartition) || srcPartition == dstPartition) continue;

                string target = $"Item: {System.IO.Path.Combine(srcDrive.NameColon, srcUser!.userName!)} Destination: {dstDrive.NameColonSeparator}";

                var dstUser = dstDrive.PmUsers.Get()
                    .FirstOrDefault(u => u is not null && string.Equals(u.userName, srcUser.userName, StringComparison.OrdinalIgnoreCase));
                if (dstUser is null)
                {
                    WriteError(new ErrorRecord(new OrchException(target, $"User '{srcUser.userName}' was not found in '{dstDrive.NameColonSeparator}'."), "PmUserNotFound", ErrorCategory.ObjectNotFound, target));
                    continue;
                }

                if (ShouldProcess(target, "Copy PmUserPreference"))
                {
                    var payload = new UpdatePmUserSettingPayload
                    {
                        settings = settingList,
                        partitionGlobalId = dstPartition,
                        userId = dstUser.id,
                    };
                    try
                    {
                        dstDrive.OrchAPISession.PutPmUserSetting(payload);
                        foreach (var s in settingList)
                        {
                            WriteObject(new PmUserPreference { Path = dstDrive.NameColonSeparator, UserName = dstUser.userName, Key = s.key, Value = s.value });
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "CopyPmUserPreferenceError", ErrorCategory.InvalidOperation, target));
                    }
                }
            }
        }
    }
}
