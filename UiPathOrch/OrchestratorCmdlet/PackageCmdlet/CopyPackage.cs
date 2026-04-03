using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchPackage", SupportsShouldProcess = true)]
public class CopyPackageCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageIdCompleter))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageVersionCompleter))]
    [SupportsWildcards]
    public string[]? Version { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PackageFeedFolderCompleter))]
    [SupportsWildcards]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(PathCompleter))]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    internal class PackageFeedFolderCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters, true);

            // Exclude already-selected Destination values from the candidates
            var selectedDestination = GetSelfExclusionValues(commandAst, parameterName, wordToComplete)
                .SelectMany(p => SessionState!.Path.GetResolvedPSPathFromPSPath(p))
                .Select(p => WildcardPattern.Unescape(p.Path.TrimEnd(System.IO.Path.DirectorySeparatorChar)))
                .ToList();

            #region Resolve the specified path // TODO: Something seems off with this condition?
            if (wordToComplete != System.IO.Path.DirectorySeparatorChar.ToString() && wordToComplete != "/" && !wordToComplete.EndsWith(':') &&
                (!string.IsNullOrEmpty(wordToComplete) || wordToComplete.EndsWith(System.IO.Path.DirectorySeparatorChar) || wordToComplete.EndsWith('/')))
            {
                wordToComplete += '*';
            }
            var paths = SessionState?.Path.GetResolvedPSPathFromPSPath(wordToComplete);
            #endregion

            foreach (var p in paths ?? [])
            {
                var drive = p.Drive as OrchDriveInfo;
                if (drive is null) continue;

                string p2 = OrchDriveInfo.PSPathToOrchPath(p.Path);
                if (string.IsNullOrEmpty(p2))
                {
                    yield return new CompletionResult(drive.NameColonSeparator);
                    continue;
                }

                var folder = drive.GetFolder(p2);

                // Exclude the source folder, but keep the root folder as a candidate
                if (folder!.Id is not null && drivesFolders.Contains((drive!, folder!))) continue;

                // Exclude already-selected folders.
                if (selectedDestination.Contains(p.Path)) continue;

                if (folder?.ParentId is null && folder?.FeedType == "FolderHierarchy")
                {
                    string candidate = p.Path
                        .Replace("`", "``")
                        .Replace("'", "''")
                        .Replace("*", "`*")
                        .Replace("?", "`?")
                        .Replace("[", "`[")
                        .Replace("]", "`]");
                    if (candidate != p.Path || candidate.Contains(' '))
                        yield return new CompletionResult($"'{candidate}'", p.Path, CompletionResultType.ProviderContainer, p.Path);
                    else
                        yield return new CompletionResult(p.Path);
                }
            }
        }
    }

    private class PathCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // Something seems off with this condition?
            if (wordToComplete != System.IO.Path.DirectorySeparatorChar.ToString() && wordToComplete != "/" && !wordToComplete.EndsWith(':') &&
                (!string.IsNullOrEmpty(wordToComplete) || wordToComplete.EndsWith(System.IO.Path.DirectorySeparatorChar) || wordToComplete.EndsWith('/')))
            {
                wordToComplete += '*';
            }
            var paths = SessionState?.Path.GetResolvedPSPathFromPSPath(wordToComplete);

            foreach (var p in paths ?? [])
            {
                var drive = p.Drive as OrchDriveInfo;
                if (drive is null) continue;

                string p2 = OrchDriveInfo.PSPathToOrchPath(p.Path);
                if (string.IsNullOrEmpty(p2))
                {
                    yield return new CompletionResult(drive.NameColonSeparator);
                    continue;
                }

                var folder = drive.GetFolder(p2);

                if (folder?.ParentId is null && folder?.FeedType == "FolderHierarchy")
                {
                    string candidate = p.Path
                        .Replace("`", "``")
                        .Replace("'", "''")
                        .Replace("*", "`*")
                        .Replace("?", "`?")
                        .Replace("[", "`[")
                        .Replace("]", "`]");
                    if (candidate != p.Path || candidate.Contains(' '))
                        yield return new CompletionResult($"'{candidate}'", p.Path, CompletionResultType.ProviderContainer, p.Path);
                    else
                        yield return new CompletionResult(p.Path);
                }
            }
        }
    }

    private class NoCorrespondDestinatoinFolderException : Exception { }

    private static bool PackageExists(OrchDriveInfo drive, Folder folder, Package version)
    {
        try
        {
            // We could also search by Key, but that would corrupt the cache.
            var dstExistingVersions = drive.GetPackageVersions(folder, version.Id!);
            if (dstExistingVersions is not null)
            {
                return dstExistingVersions.Any(v => v.Version == version.Version);
            }
        }
        catch { } // Swallow this exception

        return false;
    }

    // TODO: Should this be an extension method on IWritableHost?
    private static bool DownloadPackage(IWritableHost _this, OrchDriveInfo srcDrive, string? srcFeedId, Package srcVersion, out string? fileName, out byte[]? fileContent)
    {
        try
        {
            (fileName, fileContent) = srcDrive.OrchAPISession.DownloadPackage(srcFeedId!, srcVersion.Id!, srcVersion.Version!);
        }
        catch (Exception ex)
        {
            _this.WriteError(new ErrorRecord(new OrchException(srcVersion.GetPSPath(), ex), "DownloadPackageError", ErrorCategory.InvalidOperation, srcVersion));
            fileName = null;
            fileContent = null;
            return false;
        }
        return true;
    }

    private static bool UploadPackage(IWritableHost _this, Package srcVersion, OrchDriveInfo dstDrive, Folder dstFolder, string? fileName, byte[]? fileContent)
    {
        if (string.IsNullOrEmpty(fileName) || fileContent is null) return false;
        try
        {
            string dstFeedId = dstDrive.FolderFeedId.Get(dstFolder);
            var copiedPackage = dstDrive.OrchAPISession.UploadPackage(dstFeedId, fileName!, fileContent!);
            if (copiedPackage is not null)
            {
                dstDrive._dicPackages?.TryRemove(dstFeedId ?? "", out var _);
                dstDrive._dicPackageVersions?.TryRemove(dstFeedId ?? "", out var _);

                // If the destination feed is a personal workspace, also clear the process cache
                if (dstFolder.FolderType == "Personal")
                {
                    dstDrive._dicReleases?.TryRemove(dstFolder.Id ?? 0, out var _);
                    //dstDrive._dicReleaseList?.TryRemove(dstFolder.Id ?? 0, out var _);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            string target = $"{srcVersion.GetPSPath()}:{srcVersion.Version}";
            _this.WriteError(new ErrorRecord(new OrchException(target, ex), "UploadPackageError", ErrorCategory.InvalidOperation, target));
        }

        return false;
    }

    internal static void CopyPackages(
        IWritableHost _this,
        List<(OrchDriveInfo, Folder)> srcDrivesFolders, Folder srcRootFolder,
        List<WildcardPattern>? wpId, List<WildcardPattern>? wpVersion,
        List<(OrchDriveInfo, Folder)> dstDrivesFolders,
        bool shouldProcess, CancellationToken cancelToken)
    {
        int index1 = 0;
        using var reporterMain = new ProgressReporter(_this, 1, srcDrivesFolders.Count, "Processing folders...");
        foreach (var (srcDrive, srcFolder) in srcDrivesFolders)
        {
            cancelToken.ThrowIfCancellationRequested();

            //reporterMain.WriteProgress(++indexMain, $"{indexMain:D}/{srcDrivesFolders.Count} {srcFolder.GetPSPath()}");
            if (srcDrivesFolders.Count > 1) reporterMain.WriteProgress(++index1);
            try
            {
                var srcPackages = srcDrive.GetPackages(srcFolder)
                    .FilterByWildcards(p => p?.Id, wpId)
                    .OrderBy(p => p.Id)
                    .ToList();

                var srcFeedId = srcDrive.FolderFeedId.Get(srcFolder);

                int index2 = 0;
                using var reporter2 = new ProgressReporter(_this, 2, srcPackages.Count, "Processing packages...");
                foreach (var srcPackage in srcPackages)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    if (reporter2.TotalNum > 1) reporter2.WriteProgress(++index2);

                    var srcVersions = srcDrive.GetPackageVersions(srcFolder, srcPackage.Id!)
                        .FilterByWildcards(p => p?.Version, wpVersion)
                        //.OrderBy(p => p.Version!, VersionComparer.Instance)
                        .ToList();

                    int index3 = 0;
                    using var reporter3 = new ProgressReporter(_this, 3, srcVersions.Count * dstDrivesFolders.Count, "Copying versions...   ");
                    foreach (var srcVersion in srcVersions)
                    {
                        string fileName = null;
                        byte[] fileContent = null;

                        foreach (var (dstDrive, dstRootFolder) in dstDrivesFolders)
                        {
                            cancelToken.ThrowIfCancellationRequested();

                            Folder? dstFolder = _this.GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder, true);
                            // Skip the copy if no folder with the same name exists
                            if (dstFolder is null) throw new NoCorrespondDestinatoinFolderException();

                            if (srcFolder == dstFolder) continue;

                            // Skip the copy if the folder exists but has no folder feed
                            // (we do not copy to the tenant feed)
                            if (dstFolder != dstDrive.RootFolder && dstFolder.FeedType != "FolderHierarchy")
                            {
                                _this.WriteError(new ErrorRecord(
                                    new OrchException(srcFolder.GetPSPath(), $"Skipping folder '{dstFolder.GetPSPath()}' as its FeedType is not 'FolderHierarchy'."),
                                    "CopyFolderEntityToRootFolderError",
                                    ErrorCategory.InvalidOperation,
                                    dstDrive));
                                throw new NoCorrespondDestinatoinFolderException();
                            }

                            // If a package with the same name already exists in dstFolder, show a warning and skip the copy
                            if (PackageExists(dstDrive, dstFolder, srcVersion))
                            {
                                _this.WriteError(new ErrorRecord(new InvalidOperationException($"\"{srcVersion.GetPSPath()}:{srcVersion.Version}\": Package already exists in {dstFolder.GetPSPath()}. Skipping the copy."), "CopyPackageError", ErrorCategory.WriteError, dstFolder));
                                continue;
                            }

                            string key = $"{srcVersion.GetPSPath()}:{srcVersion.Version}";
                            string target = $"Item: {key} Destination: {dstFolder.GetPSPath()}";

                            if (shouldProcess || _this.ShouldProcess(target, $"Copy Package"))
                            {
                                // Progress should only be displayed when actually copying
                                reporter3.WriteProgress(++index3, $"{key}.nupkg to {dstDrive.NameColonSeparator}");

                                if (fileName is null)
                                {
                                    if (!DownloadPackage(_this, srcDrive, srcFeedId, srcVersion, out fileName, out fileContent)) break;
                                }
                                UploadPackage(_this, srcVersion, dstDrive, dstFolder, fileName, fileContent);
                            }
                        }
                    }
                }
            }
            catch (NoCorrespondDestinatoinFolderException)
            {
                // This exception is thrown to skip processing when the destination folder does not exist.
                // The warning has already been written to the console, so nothing to do here
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // This is pretty rough.. but good enough for now..
                _this.WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "CopyFolderMachineError", ErrorCategory.InvalidOperation, srcDrive));
            }
        }
    }

    protected override void ProcessRecord()
    {
        // The first element may come from CSV input, so split the first element by commas
        var processedId = Id.Split1stValueByUnescapedCommas();
        var processedVersion = Version.Split1stValueByUnescapedCommas();
        var processedDestination = Destination.Split1stValueByUnescapedCommas();

        var wpId = processedId.ConvertToWildcardPatternList();
        var wpVersion = processedVersion.ConvertToWildcardPatternList();

        var (srcDrive, srcRootFolder) = SessionState.ResolveToSingleFeedFolder(Path);
        var srcDrivesFolders = SessionState.EnumPackageFeedFolders([srcRootFolder.GetPSPath()], Recurse.IsPresent);
        var dstDrivesFolders = SessionState.EnumPackageFeedFolders(processedDestination);

        if (srcRootFolder != srcDrive.RootFolder && Recurse.IsPresent)
        {
            throw new ArgumentException("The -Recurse parameter can only be used when the source folder is the root folder.");
        }

        using var cancelHandler = new ConsoleCancelHandler();
        CopyPackages(this, srcDrivesFolders, srcRootFolder, wpId, wpVersion, dstDrivesFolders, false, cancelHandler.Token);
    }
}
