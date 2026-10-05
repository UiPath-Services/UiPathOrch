using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchLibrary", SupportsShouldProcess = true)]
public class RemoveLibraryCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryVersionCompleter))]
    [SupportsWildcards]
    public string[]? Version { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // This was never multi-threaded to begin with
    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives.WithCancellation(cancelHandler.Token))
        {
            try
            {
                // Two bars, shaped like Copy-OrchLibrary's pair: the top one names the
                // library, the one nested under it the version being removed. This was a
                // single bar carrying both in its ACTIVITY -- "Removing versions of <id> in
                // <drive>" -- so the label was a different width for every library and the
                // bar walked left and right as the run went on. Labels are short and
                // hardcoded, and unpadded: each is the only bar at its indent.
                var libraries = drive.LibrariesInTenant.Get()
                    .FilterByWildcards(l => l?.Id, wpId!)
                    .OrderBy(l => l.Id!.ToLower())
                    .ToList();

                int libraryIndex = 0;
                using var libraryReporter = new ProgressReporter(this, libraries.Count, "Libraries");

                foreach (var library in libraries.WithCancellation(cancelHandler.Token))
                {
                    libraryReporter.WriteProgress(++libraryIndex, library.GetPSPath());
                    try
                    {
                        var matchingVersions = drive.LibraryVersions.Get(library.Id!)
                            .FilterByWildcards(v => v?.Version, wpVersion)
                            .ToList();
                        //.OrderBy(v => v.Version!, VersionComparer.Instance);

                        // Disposed at the end of each library, so the bar belongs to the
                        // library above it rather than accumulating across the run.
                        int versionIndex = 0;
                        using var versionReporter = new ProgressReporter(this, matchingVersions.Count, "Versions", libraryReporter);

                        foreach (var matchingVersion in matchingVersions.WithCancellation(cancelHandler.Token))
                        {
                            versionReporter.WriteProgress(++versionIndex, matchingVersion.Version);
                            string target = $"{drive.NameColonSeparator}{matchingVersion.Id}:{matchingVersion.Version}";
                            if (ShouldProcess(target, "Remove Library"))
                            {
                                try
                                {
                                    drive.OrchAPISession.RemoveLibrary(matchingVersion.Id!, matchingVersion.Version!);
                                    drive.LibrariesInTenant.ClearCache();
                                    drive.LibraryVersions.ClearCache(matchingVersion.Id!);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveLibraryError", ErrorCategory.InvalidOperation, matchingVersion));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetLibraryVersionError", ErrorCategory.InvalidOperation, drive));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetLibraryError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
