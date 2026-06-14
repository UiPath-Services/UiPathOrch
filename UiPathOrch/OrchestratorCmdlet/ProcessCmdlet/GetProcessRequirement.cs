using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchProcessRequirement")]
[OutputType(typeof(SubtypedPackageResource))]
public class GetProcessRequirementCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
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

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders.WithCancellation(cancelHandler.Token))
        {
            IEnumerable<Release> targetReleases;
            try
            {
                var releases = drive.Releases.Get(folder);
                targetReleases = releases
                    .FilterByWildcards(r => r?.Name, wpName)
                    .OrderBy(r => r.Name);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetProcessError", ErrorCategory.InvalidOperation, folder));
                continue;
            }

            using var results = OrchThreadPool.RunForEach(targetReleases,
                release => release.GetPSPath(),
                release => release,
                release => drive.ReleaseRequirements.Get(folder, release));

            foreach (var result in results.WithCancellation(cancelHandler.Token))
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities is null) continue;

                    var targetEntities = entities
                        .OrderBy(s => s.ResourceType)
                        .ThenBy(s => s.ResourceName);

                    WriteObject(targetEntities, true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetProcessRequirementError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
