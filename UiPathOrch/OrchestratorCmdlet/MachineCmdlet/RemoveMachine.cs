using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchMachine", SupportsShouldProcess = true)]
public class RemoveMachineCmdlet : OrchestratorPSCmdlet
{
    private const int ChunkSize = 1000;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    // drive -> approved machines (deduped by id) confirmed via ShouldProcess.
    private Dictionary<OrchDriveInfo, Dictionary<long, ExtendedMachine>>? _pending;

    // Confirm per machine here, but defer the API call: a wildcard or piped set is
    // accumulated and flushed as one bulk DeleteBulk per drive in EndProcessing,
    // so `Remove-OrchMachine *` is a single request instead of one DELETE per match.
    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        // Split a single comma-joined value (e.g. one CSV cell "a,b,c") into separate
        // patterns; a normal -Name a,b,c array already arrives split. Backtick-escaped
        // commas are preserved.
        var wpName = Name.Split1stValueByUnescapedCommasPreservingEscapes()?.Select(name => new WildcardPattern(PathTools.UnescapePSText(name), WildcardOptions.IgnoreCase)).ToList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<ExtendedMachine> matches;
            try
            {
                matches = drive.Machines.Get().FilterByWildcards(m => m?.Name, wpName).OrderBy(m => m.Name);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetMachineError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            foreach (var machine in matches.WithCancellation(cancelHandler.Token))
            {
                if (machine?.Id is not long id) continue;
                if (ShouldProcess(machine.GetPSPath(), "Remove Machine"))
                {
                    _pending ??= [];
                    if (!_pending.TryGetValue(drive, out var approved))
                    {
                        approved = [];
                        _pending[drive] = approved;
                    }
                    approved[id] = machine; // dedupe across pipeline rows
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_pending is null) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, approved) in _pending)
        {
            if (approved.Count == 0) continue;

            bool anyDeleted = false;
            foreach (var chunk in approved.Keys.Chunk(ChunkSize))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                try
                {
                    drive.OrchAPISession.RemoveMachines(chunk);
                    anyDeleted = true;
                }
                catch (Exception ex)
                {
                    // DeleteBulk is best-effort (204 even with unknown ids), so a throw
                    // here is a transport/auth-level failure for the whole chunk.
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "RemoveMachineError", ErrorCategory.InvalidOperation, drive));
                }
            }

            if (!anyDeleted) continue;

            drive.Machines.ClearCache();
            drive.FolderMachinesAssignable.ClearCache();

            // DeleteBulk returns 204 with no per-item result, so a machine that could
            // not be deleted (e.g. it is in use) is silently skipped. Re-fetch and
            // surface any approved target that still exists, matching the per-item
            // visibility the caller had before batching.
            HashSet<long> survivors;
            try
            {
                survivors = drive.Machines.Get()
                    .Where(m => m?.Id is long sid && approved.ContainsKey(sid))
                    .Select(m => m!.Id!.Value)
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "VerifyRemoveMachineError", ErrorCategory.InvalidOperation, drive));
                continue;
            }

            foreach (var sid in survivors)
            {
                var machine = approved[sid];
                WriteError(new ErrorRecord(
                    new OrchException(machine.GetPSPath(), "Machine was not deleted; it may be in use (active sessions or assigned robots)."),
                    "RemoveMachineNotDeleted", ErrorCategory.ResourceBusy, machine));
            }
        }
    }
}
