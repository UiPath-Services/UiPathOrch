using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

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

        // Phase 1 = GetPackages per (drive, folder); fanout to (drive, folder, package).
        // Phase 2 = GetPackageVersions per package. Cap=4 shared across both phases
        // by ChainedThreadPool (avoids the cap-multiplication of nested OrchThreadPool).
        using var pool = OrchThreadPool.RunForEachChained(
            drivesFolders,
            df => df.folder.GetPSPath(),
            df => (object)df.folder,
            df => df.drive.GetPackages(df.folder)
                .FilterByWildcards(p => p?.Id, wpId)
                .OrderBy(p => p.Id!.ToLower())
                .Select(p => (df.drive, df.folder, package: p)),
            t => t.package.GetPSPath(),
            t => (object)t.package,
            t => t.drive.GetPackageVersions(t.folder, t.package.Id!),
            cancelHandler.Token);

        foreach (var task in pool)
        {
            try
            {
                var versions = task.GetResult(cancelHandler.Token);
                WriteObject(versions!
                    .FilterByWildcards(v => v?.Version, wpVersion),
                    //.OrderBy(v => v.Version!, VersionComparer.Instance),
                    true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetPackageVersionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        // Phase 1 errors (GetPackages failures) — distinct ErrorId to
        // preserve the legacy distinction between "couldn't list packages"
        // and "couldn't get versions of a specific package".
        foreach (var (_, ex) in pool.Phase1Errors)
        {
            WriteError(new ErrorRecord(ex, "GetPackageError", ErrorCategory.InvalidOperation, ex.Target));
        }
    }
}
