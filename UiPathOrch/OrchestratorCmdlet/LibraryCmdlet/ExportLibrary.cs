using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Export, "OrchLibrary", SupportsShouldProcess = true)]
public class ExportLibraryCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(LibraryIdCompleter))]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(LibraryVersionCompleter))]
    public string[]? Version { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    public string? Destination { get; set; }

    //[Parameter]
    //public SwitchParameter HostFeed { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        if (Destination is null)
        {
            Destination = SessionState.Path.CurrentFileSystemLocation.Path;
        }

        // Convert the PSDrive path to the actual file system path
        Destination = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);

        if (!Directory.Exists(Destination))
        {
            throw new DirectoryNotFoundException($"A directory '{Destination}' does not exist.");
        }

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            //drive => HostFeed ? drive.LibrariesInHost.Get() : drive.LibrariesInTenant.Get());
            drive => drive.LibrariesInTenant.Get());

        using var reporter = new ProgressReporter(this, 1, 100, "Export Library");
        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var libraries = result.GetResult(cancelHandler.Token);
                var drive = result.Source; // The Source property must be accessed after GetResult()

                foreach (var library in libraries!
                    .FilterByWildcards(l => l?.Id, wpId)
                    .OrderBy(library => library.Id))
                {
                    try
                    {
                        //var versions = (HostFeed ? drive.GetLibraryVersionsInHostFeed(library.Id!) : drive.GetLibraryVersions(library.Id!))
                        var versions = drive.GetLibraryVersions(library.Id!)
                            .FilterByWildcards(l => l?.Version, wpVersion)
                            //.OrderBy(l => l.Version!, VersionComparer.Instance)
                            .ToList();

                        int index = 0;
                        reporter.TotalNum = versions.Count;
                        foreach (var version in versions.WithCancellation(cancelHandler.Token))
                        {
                            string target = $"{version.Id}.{version.Version}.nupkg";
                            reporter.WriteProgress(++index, target);
                            if (ShouldProcess(target, "Export Library"))
                            {
                                try
                                {
                                    var (fileName, fileContent) = drive.OrchAPISession.DownloadLibrary(version.Id!, version.Version!);
                                    string filePath = System.IO.Path.Combine(Destination, fileName!);
                                    File.WriteAllBytes(filePath, fileContent);
                                }
                                catch (Exception ex)
                                {
                                    WriteError(new ErrorRecord(new OrchException(version.GetPSPath(), ex), "ExportLibraryError", ErrorCategory.InvalidOperation, version));
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
                        WriteError(new ErrorRecord(new OrchException(library.GetPSPath(), ex), "GetLibraryVersionError", ErrorCategory.InvalidOperation, library));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
