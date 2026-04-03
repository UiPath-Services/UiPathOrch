using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchLibrary", SupportsShouldProcess = true)]
public class RemoveLibraryCommand : OrchestratorPSCmdlet//, IDynamicParameters
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

    // This was never multi-threaded to begin with
    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var libraries = drive.LibrariesInTenant.Get()
                    .FilterByWildcards(l => l?.Id, wpId!)
                    .OrderBy(l => l.Id!.ToLower());

                foreach (var library in libraries)
                {
                    try
                    {
                        var matchingVersions = drive.GetLibraryVersions(library.Id!)
                            .FilterByWildcards(v => v?.Version, wpVersion);
                        //.OrderBy(v => v.Version!, VersionComparer.Instance);

                        foreach (var matchingVersion in matchingVersions)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

                            string target = $"{drive.NameColonSeparator}{matchingVersion.Id}:{matchingVersion.Version}";
                            if (ShouldProcess(target, "Remove Library"))
                            {
                                try
                                {
                                    drive.OrchAPISession.RemoveLibrary(matchingVersion.Id!, matchingVersion.Version!);
                                    drive.LibrariesInTenant.ClearCache();
                                    drive._dicLibraryVersions?.TryRemove(matchingVersion.Id!, out List<LibraryVersion>? _);
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
