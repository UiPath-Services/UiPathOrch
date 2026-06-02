using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Writes a user's identity settings. Each pipeline row is one key/value; rows for
// the same (drive, user) are accumulated and sent as a single PUT per user, so an
// Import-Csv of multiple keys per user (e.g. UserLanguage.Language + .Date) is one
// request. Columns/parameters line up with Get-PmUserPreference -ExportCsv.
[Cmdlet(VerbsCommon.Set, "PmUserPreference", SupportsShouldProcess = true)]
[OutputType(typeof(PmUserPreference))]
public class SetPmUserPreferenceCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmUserPreferenceUserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmPreferenceKeyCompleter))]
    public string? Key { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PmPreferenceValueCompleter))]
    public string? Value { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private sealed class UserKeyComparer : IEqualityComparer<(OrchDriveInfo drive, string userName)>
    {
        public bool Equals((OrchDriveInfo drive, string userName) x, (OrchDriveInfo drive, string userName) y)
            => EqualityComparer<OrchDriveInfo>.Default.Equals(x.drive, y.drive)
               && string.Equals(x.userName, y.userName, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((OrchDriveInfo drive, string userName) obj)
            => HashCode.Combine(EqualityComparer<OrchDriveInfo>.Default.GetHashCode(obj.drive),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.userName));
    }

    // (drive, userName) -> accumulated key/value settings, in input order.
    private Dictionary<(OrchDriveInfo drive, string userName), List<Entities.KeyValuePair>>? _pending;

    protected override void ProcessRecord()
    {
        if (string.IsNullOrEmpty(Key)) return;

        _pending ??= new(new UserKeyComparer());

        var drives = SessionState.EnumPmDrives(Path);
        foreach (var drive in drives)
        {
            foreach (var userName in UserName!)
            {
                var k = (drive, userName);
                if (!_pending.TryGetValue(k, out var list))
                {
                    list = [];
                    _pending[k] = list;
                }
                list.Add(new Entities.KeyValuePair(Key, Value));
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_pending is null) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var byDrive in _pending.GroupBy(kv => kv.Key.drive))
        {
            var drive = byDrive.Key;

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

            var users = drive.PmUsers.Get().Where(u => u is not null && !string.IsNullOrEmpty(u.userName)).ToList();

            foreach (var entry in byDrive)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                var userName = entry.Key.userName;
                var settings = entry.Value;
                string target = System.IO.Path.Combine(drive.NameColonSeparator, userName);

                var user = users.FirstOrDefault(u => string.Equals(u!.userName, userName, StringComparison.OrdinalIgnoreCase));
                if (user is null)
                {
                    WriteError(new ErrorRecord(new OrchException(target, $"PmUser '{userName}' was not found in '{drive.NameColonSeparator}'."), "PmUserNotFound", ErrorCategory.ObjectNotFound, target));
                    continue;
                }

                if (ShouldProcess(target, "Set PmUserPreference"))
                {
                    var payload = new UpdatePmUserSettingPayload
                    {
                        settings = settings,
                        partitionGlobalId = partitionGlobalId,
                        userId = user.id,
                    };
                    try
                    {
                        drive.OrchAPISession.PutPmUserSetting(payload);
                        foreach (var s in settings)
                        {
                            WriteObject(new PmUserPreference { Path = drive.NameColonSeparator, UserName = userName, Key = s.key, Value = s.value });
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "SetPmUserPreferenceError", ErrorCategory.InvalidOperation, target));
                    }
                }
            }
        }
    }
}
