using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Writes the connected user's own portal preferences. Each pipeline row is one
// key/value; rows for the same drive are accumulated and sent as a single PUT, so an
// Import-Csv of multiple keys (e.g. UserLanguage.Language + .Date) is one request.
// Columns/parameters line up with Get-PmUserPreference -ExportCsv. The cmdlet always
// acts on the user behind the drive's token (non-confidential app or PAT required).
[Cmdlet(VerbsCommon.Set, "PmUserPreference", SupportsShouldProcess = true)]
[OutputType(typeof(PmUserPreference))]
public class SetPmUserPreferenceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmPreferenceKeyCompleter))]
    public string? Key { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmPreferenceValueCompleter))]
    public string? Value { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    // drive -> accumulated key/value settings, in input order.
    private Dictionary<OrchDriveInfo, List<Entities.KeyValuePair>>? _pending;

    protected override void ProcessRecord()
    {
        if (string.IsNullOrEmpty(Key)) return;

        _pending ??= [];

        var drives = SessionState.EnumPmDrives(Path);
        foreach (var drive in drives)
        {
            if (!_pending.TryGetValue(drive, out var list))
            {
                list = [];
                _pending[drive] = list;
            }
            list.Add(new Entities.KeyValuePair(Key, Value));
        }
    }

    protected override void EndProcessing()
    {
        if (_pending is null) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, settings) in _pending)
        {
            cancelHandler.Token.ThrowIfCancellationRequested();

            string? userId = PmUserPreferenceCurrentUser.Resolve(this, drive);
            if (userId is null) continue;

            string? partitionGlobalId;
            try
            {
                partitionGlobalId = drive.GetPartitionGlobalId();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetGlobalPartitionIdError", ErrorCategory.InvalidOperation, drive));
                continue;
            }
            if (string.IsNullOrEmpty(partitionGlobalId)) continue;

            if (ShouldProcess(drive.NameColonSeparator, "Set PmUserPreference"))
            {
                var payload = new UpdatePmUserSettingPayload
                {
                    settings = settings,
                    partitionGlobalId = partitionGlobalId,
                    userId = userId,
                };
                try
                {
                    drive.OrchAPISession.PutPmUserSetting(payload);
                    foreach (var s in settings)
                    {
                        WriteObject(new PmUserPreference { Path = drive.NameColonSeparator, Key = s.key, Value = s.value });
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "SetPmUserPreferenceError", ErrorCategory.InvalidOperation, drive));
                }
            }
        }
    }
}
