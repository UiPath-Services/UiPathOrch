using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLibraryVersion")]
[OutputType(typeof(Entities.Library))]
public class GetLibraryVersionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryVersionCompleter))]
    [SupportsWildcards]
    public string[]? Version { get; set; }

    [Parameter]
    public SwitchParameter HostFeed { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // Phase 1 = list libraries per drive (host- or tenant-feed depending
        // on -HostFeed); fanout to (drive, lib). Phase 2 = list versions per
        // library. Cap=4 shared across both phases via ChainedThreadPool —
        // the previous nested OrchThreadPool stacked to cap=4×4=16 against
        // a single Orchestrator.
        using var pool = OrchThreadPool.RunForEachChained(
            drives,
            drive => drive.NameColonSeparator,
            drive => (object)drive,
            drive =>
            {
                var libs = HostFeed
                    ? drive.LibrariesInHost.Get()
                    : drive.LibrariesInTenant.Get();
                return libs
                    .FilterByWildcards(l => l?.Id, wpId)
                    .Select(lib => (drive, lib));
            },
            t => t.lib.GetPSPath(),
            t => (object)t.lib,
            t => (HostFeed
                ? t.drive.LibraryVersionsInHostFeed.Get(t.lib.Id!)
                : t.drive.LibraryVersions.Get(t.lib.Id!))
                .FilterByWildcards(l => l?.Version, wpVersion),
            cancelHandler.Token);

        foreach (var task in pool)
        {
            try
            {
                var versions = task.GetResult(cancelHandler.Token);
                WriteObject(versions, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetLibraryVersionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        // Phase 1 errors (per-drive library list failures) — distinct ErrorId
        // to preserve the legacy split between "couldn't list libraries" and
        // "couldn't get versions of a specific library".
        foreach (var (_, ex) in pool.Phase1Errors)
        {
            WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
        }
    }
}
