using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Note: deliberately does NOT derive from RemoveFolderEntityCmdletBase. That base
// issues one delete per matched entity; test cases have a folder-scoped BulkDelete
// endpoint, so this cmdlet confirms per test case but accumulates the approved ids
// per (drive, folder) and flushes one BulkDelete per folder in EndProcessing -- so
// `Remove-OrchTestCase *` is one request per folder instead of one per test case.
[Cmdlet(VerbsCommon.Remove, "OrchTestCase", SupportsShouldProcess = true)]
public class RemoveTestCaseCmdlet : OrchestratorPSCmdlet
{
    private const int ChunkSize = 1000;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // Keyed by (drive, folderId) -- folder Id is stable across pipeline rows, so a
    // piped/CSV set targeting one folder collapses to a single BulkDelete.
    private Dictionary<(OrchDriveInfo drive, long folderId), Dictionary<long, TestCaseDefinition>>? _pending;
    private Dictionary<(OrchDriveInfo drive, long folderId), Folder>? _folders;

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        // -Name is taken literally (no comma-split): one CSV cell / quoted string is one
        // test case name. Select multiple via a native -Name a,b,c array or one per row.
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<TestCaseDefinition> matches;
            try
            {
                matches = drive.TestCases.Get(folder).FilterByWildcards(t => t?.Name, wpName).OrderBy(t => t?.Name);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestCaseError", ErrorCategory.InvalidOperation, folder));
                continue;
            }

            var key = (drive, folder.Id ?? 0);
            foreach (var tc in matches.WithCancellation(cancelHandler.Token))
            {
                if (tc?.Id is not long id) continue;
                if (ShouldProcess(tc.GetPSPath(), "Remove TestCase"))
                {
                    _pending ??= [];
                    _folders ??= [];
                    if (!_pending.TryGetValue(key, out var approved))
                    {
                        approved = [];
                        _pending[key] = approved;
                        _folders[key] = folder;
                    }
                    approved[id] = tc; // dedupe across pipeline rows
                }
            }
        }
    }

    protected override void EndProcessing()
    {
        if (_pending is null || _folders is null) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (key, approved) in _pending
            .WithProgressBar(this, "Removing test cases", kv => _folders[kv.Key].GetPSPath()))
        {
            if (approved.Count == 0) continue;
            var (drive, _) = key;
            var folder = _folders[key];

            bool anyDeleted = false;
            foreach (var chunk in approved.Keys.Chunk(ChunkSize))
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                try
                {
                    drive.OrchAPISession.RemoveTestCases(folder.Id ?? 0, chunk);
                    anyDeleted = true;
                }
                catch (Exception ex)
                {
                    // BulkDelete is best-effort (204 even with unknown ids), so a throw
                    // here is a transport/auth-level failure for the whole chunk.
                    WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "RemoveTestCaseError", ErrorCategory.InvalidOperation, folder));
                }
            }

            if (!anyDeleted) continue;

            drive.TestCases.ClearCache(folder);

            // BulkDelete returns 204 with no per-item result, so a test case that was
            // not deleted is silently skipped. Re-fetch and surface any approved target
            // that still exists, matching the per-item visibility before batching.
            HashSet<long> survivors;
            try
            {
                survivors = drive.TestCases.Get(folder)
                    .Where(t => t?.Id is long sid && approved.ContainsKey(sid))
                    .Select(t => t!.Id!.Value)
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "VerifyRemoveTestCaseError", ErrorCategory.InvalidOperation, folder));
                continue;
            }

            foreach (var sid in survivors)
            {
                var tc = approved[sid];
                WriteError(new ErrorRecord(
                    new OrchException(tc.GetPSPath(), "Test case was not deleted."),
                    "RemoveTestCaseNotDeleted", ErrorCategory.InvalidOperation, tc));
            }
        }
    }
}
