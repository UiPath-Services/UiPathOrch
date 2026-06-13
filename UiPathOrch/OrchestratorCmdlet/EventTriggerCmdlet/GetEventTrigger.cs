using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchEventTrigger")]
[OutputType(typeof(Entities.ApiTrigger))]
public class GetEventTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(EventTriggerNameCompleter))]
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

    // Cache (drive, ReleaseKey) -> ReleaseName lookups within one ProcessRecord run so we don't
    // fetch the Releases listing once per trigger row.
    private readonly Dictionary<(OrchDriveInfo, string), string?> _releaseNameCache = new();

    private string? ResolveReleaseName(OrchDriveInfo drive, Folder folder, string? releaseKey)
    {
        if (string.IsNullOrEmpty(releaseKey)) return null;
        if (_releaseNameCache.TryGetValue((drive, releaseKey), out var cached)) return cached;

        string? name = null;
        try
        {
            var releases = drive.Releases.Get(folder);
            name = releases.FirstOrDefault(r => string.Equals(r.Key, releaseKey, StringComparison.OrdinalIgnoreCase))?.Name;
        }
        catch
        {
            // A missing Release is a soft failure -- leave the name unresolved.
        }
        _releaseNameCache[(drive, releaseKey)] = name;
        return name;
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df,
            df => df.drive.EventTriggers.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var triggers = result.GetResult(cancelHandler.Token);
                if (triggers is null) continue;

                var filtered = triggers
                    .FilterByNames(s => s?.Name, Name)
                    .OrderBy(s => s.Name);

                foreach (var t in filtered)
                {
                    // The triggers endpoint rejects $expand=Release, so Release is null and the
                    // default table's Release column would be blank. Fill it from the ReleaseKey
                    // -> name lookup (cached per run).
                    if (t.Release is null && !string.IsNullOrEmpty(t.ReleaseKey))
                    {
                        var releaseName = ResolveReleaseName(result.Source.drive, result.Source.folder, t.ReleaseKey);
                        if (releaseName is not null)
                        {
                            t.Release = new TriggerRelease { Name = releaseName };
                        }
                    }
                    WriteObject(t);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetEventTriggerError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
