using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchPackageVersion")]
[OutputType(typeof(Entities.Package))]
public class GetPackageVersionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageIdCompleter))]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageVersionCompleter))]
    public string[]? Version { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumPackageFeedFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent);
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();

        // A two-phase chain (list packages, then fetch each one's versions) can't show a
        // meaningful percentage while streaming: the package total isn't known until every
        // folder has been listed, so the bar's ceiling would keep growing mid-run. Instead
        // run the phases in sequence so each bar has a known denominator.

        // Phase 1: list matching packages across every folder (parallel, cap=4). The folder
        // count is known up front, so this bar is a real percentage; it also yields the
        // total package count for Phase 2.
        using var packagePool = OrchThreadPool.RunForEach(
            drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.GetPackages(df.folder)
                .FilterByWildcards(p => p?.Id, wpId)
                .OrderBy(p => p.Id!.ToLower())
                .Select(p => (df.drive, df.folder, package: p))
                .ToList());

        var packages = new List<(OrchDriveInfo drive, Folder folder, Package package)>();
        using (var reporter = new ProgressReporter(this, 1, packagePool.Count, "Listing packages"))
        {
            foreach (var task in packagePool)
            {
                try
                {
                    var found = packagePool.GetResultWithProgress(task, reporter, cancelHandler.Token);
                    if (found is not null) packages.AddRange(found);
                }
                catch (OrchException ex)
                {
                    // Distinct ErrorId ("couldn't list packages") vs the version error below.
                    WriteError(new ErrorRecord(ex, "GetPackageError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }

        // Phase 2: fetch versions per package (parallel, cap=4) with a real percentage
        // against the now-known package count.
        using var versionPool = OrchThreadPool.RunForEach(
            packages,
            t => t.package.GetPSPath(),
            t => t.package,
            t => t.drive.GetPackageVersions(t.folder, t.package.Id!));

        using var versionReporter = new ProgressReporter(this, 1, versionPool.Count, "Getting package versions");
        foreach (var task in versionPool)
        {
            try
            {
                var versions = versionPool.GetResultWithProgress(task, versionReporter, cancelHandler.Token);
                WriteObject(versions!.FilterByWildcards(v => v?.Version, wpVersion), true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPackageVersionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
