using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Migrates the connected user's own portal preferences from the source organization
// to the same person in each destination organization. "Same person" is resolved per
// org from that drive's token (the identity user id differs across organizations), so
// every drive involved must be connected with a non-confidential app or PAT.
[Cmdlet(VerbsCommon.Copy, "PmUserPreference", SupportsShouldProcess = true)]
[OutputType(typeof(PmUserPreference))]
public class CopyPmUserPreferenceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetPmDrive(EffectivePath(Path, LiteralPath)!) ?? throw new InvalidOperationException($"'{Path}' is not a valid UiPathOrch drive.");
        var dstDrives = SessionState.EnumDestinationDrives(Destination!);

        string? srcUserId = PmUserPreferenceCurrentUser.Resolve(this, srcDrive);
        if (srcUserId is null) return;

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

        PmUserSettingDto[]? settings;
        try
        {
            settings = srcDrive.OrchAPISession.GetUserSettings(srcPartition, srcUserId, PmUserPreferenceKeys.ReadDefaults);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetPmUserPreferenceError", ErrorCategory.InvalidOperation, srcDrive));
            return;
        }

        var settingList = (settings ?? [])
            .Where(s => !string.IsNullOrEmpty(s.key))
            .Select(s => new Entities.KeyValuePair(s.key, s.value))
            .ToList();
        if (settingList.Count == 0) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var dstDrive in dstDrives.WithCancellation(cancelHandler.Token))
        {
            string? dstUserId = PmUserPreferenceCurrentUser.Resolve(this, dstDrive);
            if (dstUserId is null) continue;

            // "<Path><destination user>" for the grouped view header (display only).
            var dstPathUserName = dstDrive.NameColonSeparator + dstDrive.CurrentUser.Get()?.UserName;

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

            string target = $"Source: {srcDrive.NameColon} Destination: {dstDrive.NameColonSeparator}";

            if (ShouldProcess(target, "Copy PmUserPreference"))
            {
                var payload = new UpdatePmUserSettingPayload
                {
                    settings = settingList,
                    partitionGlobalId = dstPartition,
                    userId = dstUserId,
                };
                try
                {
                    dstDrive.OrchAPISession.PutPmUserSetting(payload);
                    foreach (var s in settingList)
                    {
                        // The destination's cached value for this key is now stale.
                        if (!string.IsNullOrEmpty(s.key)) dstDrive.PmUserPreferences.ClearCache(s.key);
                        WriteObject(new PmUserPreference { Path = dstDrive.NameColonSeparator, PathUserName = dstPathUserName, Key = s.key, Value = s.value });
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
