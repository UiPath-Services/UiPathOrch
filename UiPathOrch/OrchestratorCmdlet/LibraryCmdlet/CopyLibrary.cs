using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchLibrary", SupportsShouldProcess = true)]
public class CopyLibraryCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(LibraryVersionCompleter))]
    public string[]? Version { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    //[Parameter]
    //public SwitchParameter HostFeed { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string? Path { get; set; }

    // Does a library with the same name and version already exist in the destination feed?
    // HostFeed pertains to the source, so it does not need to be considered in this method.
    private static bool LibraryExists(OrchDriveInfo drive, LibraryVersion version)
    {
        try
        {
            // We could also search by Key, but that would corrupt the cache.
            var dstExistingVersions = drive.GetLibraryVersions(version.Id!);
            if (dstExistingVersions is not null)
            {
                return dstExistingVersions.Any(v => v.Version == version.Version);
            }
        }
        catch { } // Swallow this exception

        return false;
    }

    private static bool DownloadLibrary(IWritableHost _this, OrchDriveInfo srcDrive, LibraryVersion version, out string? fileName, out byte[]? fileContent, string target)
    {
        try
        {
            (fileName, fileContent) = srcDrive.OrchAPISession.DownloadLibrary(version.Id!, version.Version!);
            return true;
        }
        catch (Exception ex)
        {
            fileName = null;
            fileContent = null;
            _this.WriteError(new ErrorRecord(new OrchException(target, ex), "DownloadLibraryError", ErrorCategory.InvalidOperation, target));
        }
        return false;
    }

    private static bool UploadLibrary(IWritableHost _this, LibraryVersion version, string? fileName, byte[]? fileContent, OrchDriveInfo dstDrive)
    {
        if (string.IsNullOrEmpty(fileName) || fileContent is null) return false;
        try
        {
            var uploadedLibrary = dstDrive.OrchAPISession.UploadLibrary(fileName!, fileContent!);
            if (uploadedLibrary is not null)
            {
                //uploadedLibrary.Path = dstDrive.NameColonSeparator;
                //WriteObject(result);
                dstDrive.LibrariesInTenant.ClearCache();
            }
            return true;
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException($"{version.Id}:{version.Version}", ex), "CopyLibraryError", ErrorCategory.InvalidOperation, version));
        }
        return false;
    }

    internal static void CopyLibraries(
        IWritableHost _this,
        IEnumerable<OrchDriveInfo> srcDrives, List<WildcardPattern>? wpId, List<WildcardPattern>? wpVersion,
        IList<OrchDriveInfo> dstDrives,
        bool shouldProcess, CancellationToken cancelToken)
    {
        foreach (var srcDrive in srcDrives)
        {
            var srcLibraries = srcDrive.LibrariesInTenant.Get()
            .Where(l => l is not null)
                .FilterByWildcards(l => l!.Id, wpId)
                .OrderBy(l => l.Id);

            int index1 = 0;
            using var reporter1 = new ProgressReporter(_this, 1, srcLibraries.Count(), "Processing libraries...");
            foreach (var library in srcLibraries)
            {
                cancelToken.ThrowIfCancellationRequested();

                reporter1.WriteProgress(++index1);
                try
                {
                    srcDrive.LibraryVersions.ClearCache();
                    var versions = srcDrive.GetLibraryVersions(library.Id!)
                        .FilterByWildcards(l => l?.Version, wpVersion)
                        //.OrderBy(version => version.Version!, VersionComparer.Instance)
                        .ToList();

                    using var reporter2 = new ProgressReporter(_this, 2, dstDrives.Count * versions.Count, "Copying versions...    ");
                    int index2 = 0;
                    foreach (var version in versions)
                    {
                        string? fileName = null;
                        byte[]? fileContent = null;
                        foreach (var dstDrive in dstDrives)
                        {
                            cancelToken.ThrowIfCancellationRequested();

                            var dstSettings = dstDrive.Settings.Get();
                            if (dstSettings is not null)
                            {
                                var feedScope = dstSettings.FirstOrDefault(s => s.Id == "Deployment.Libraries.FeedScope");
                                if (feedScope is not null && feedScope.Value == "Host")
                                {
                                    string errmsg = $"\"{dstDrive.NameColonSeparator}\": Library copying is disabled because the library feed is set to 'Only host feed'. " +
                                                    $"To enable copying, please go to the tenant settings page and change the Library feeds setting to 'Only tenant feed' " +
                                                    $"or 'Both host and tenant feeds'.";
                                    // We can skip all remaining processing.
                                    _this.WriteError(new ErrorRecord(new InvalidOperationException(errmsg), "CopyLibraryError", ErrorCategory.WriteError, dstDrive));
                                    return;
                                }
                            }

                            string key = $"{version.GetPSPath()}:{version.Version}";
                            string target = $"Item: {key} Destination: {dstDrive.NameColonSeparator}";

                            if (srcDrive == dstDrive) continue;

                            // If a library with the same name already exists in dstDrive, show a warning and skip the copy
                            if (LibraryExists(dstDrive, version))
                            {
                                _this.WriteError(new ErrorRecord(new InvalidOperationException($"\"{version.GetPSPath()}:{version.Version}\": Library already exists in {dstDrive.NameColonSeparator}. Skipping the copy."), "CopyLibraryError", ErrorCategory.WriteError, dstDrive));
                                continue;
                            }

                            if (shouldProcess || _this.ShouldProcess(target, $"Copy Library"))
                            {
                                // Progress should only be displayed when actually copying
                                reporter2.WriteProgress(++index2, $"{key}.nupkg to {dstDrive.NameColonSeparator}");

                                if (fileName is null)
                                {
                                    if (!DownloadLibrary(_this, srcDrive, version, out fileName, out fileContent, target)) break;
                                }
                                UploadLibrary(_this, version, fileName, fileContent, dstDrive);
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
                    _this.WriteError(new ErrorRecord(new OrchException(library.GetPSPath(), ex), "GetLibraryVersionError", ErrorCategory.InvalidOperation, library));
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var srcDrives = SessionState.EnumOrchDrives([Path]);
        var dstDrives = SessionState.EnumDestinationDrives(Destination!);

        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version
            .Split1stValueByUnescapedCommas() // May come from CSV input, so split by commas
            .ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        CopyLibraries(this, srcDrives, wpId, wpVersion, dstDrives, false, cancelHandler.Token);
    }
}
