using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Id_Version_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchLibrary", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.BulkItemDtoOfString))]
public class CopyLibraryCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LibraryIdCompleter<TPositional>))]
    [SupportsWildcards]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(LibraryVersionCompleter<TPositional>))]
    public string[]? Version { get; set; }

    [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
    public string[]? Destination { get; set; }

    //[Parameter]
    //public SwitchParameter HostFeed { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string? Path { get; set; }

    // 宛先のフィードに、同名で同バージョンのライブラリが存在するか？
    // HostFeed はコピー元についてであるので、このメソッドにおいては HostFeed は考慮する必要はない。
    private static bool LibraryExists(OrchDriveInfo drive, LibraryVersion version)
    {
        try
        {
            // Key でも検索できるんだけど、キャッシュが壊れちゃう。
            var dstExistingVersions = drive.GetLibraryVersions(version.Id!);
            if (dstExistingVersions is not null)
            {
                return dstExistingVersions.Any(v => v.Version == version.Version);
            }
        }
        catch { } // この例外は握りつぶす

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
        IEnumerable<OrchDriveInfo> dstDrives,
        bool shouldProcess, CancellationToken cancelToken)
    {
        foreach (var srcDrive in srcDrives)
        {
            var srcLibraries = srcDrive.LibrariesInTenant.Get()
            .Where(l => l is not null)
                .FilterByWildcards(l => l!.Id, wpId)
                .OrderBy(l => l.Id);

            string msg1 = "Processing libraries...";
            int index1 = 0;
            using var reporter1 = new ProgressReporter(_this, 1, srcLibraries.Count(), msg1, msg1);
            foreach (var library in srcLibraries)
            {
                cancelToken.ThrowIfCancellationRequested();

                reporter1.WriteProgress(++index1, $"{index1:D}/{reporter1.TotalNum}");
                try
                {
                    srcDrive._dicLibraryVersions = null;
                    var versions = srcDrive.GetLibraryVersions(library.Id!)
                        .FilterByWildcards(l => l?.Version, wpVersion)
                        //.OrderBy(version => version.Version!, VersionComparer.Instance)
                        .ToList();

                    string msg2 = "Copying versions...    ";
                    using var reporter2 = new ProgressReporter(_this, 2, dstDrives.Count() * versions.Count, msg2, msg2);
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
                                    // 残りの処理は全部スキップで良いな。
                                    _this.WriteError(new ErrorRecord(new InvalidOperationException(errmsg), "CopyLibraryError", ErrorCategory.WriteError, dstDrive));
                                    return;
                                }
                            }

                            string key = $"{version.GetPSPath()}:{version.Version}";
                            string target = $"Item: {key} Destination: {dstDrive.NameColonSeparator}";

                            if (srcDrive == dstDrive) continue;

                            // dstDrive に同名のパッケージがあれば、警告を表示してコピーをスキップする
                            if (LibraryExists(dstDrive, version))
                            {
                                _this.WriteError(new ErrorRecord(new InvalidOperationException($"\"{version.GetPSPath()}:{version.Version}\": Library already exists in {dstDrive.NameColonSeparator}. Skipping the copy."), "CopyLibraryError", ErrorCategory.WriteError, dstDrive));
                                continue;
                            }

                            if (shouldProcess || _this.ShouldProcess(target, $"Copy Library"))
                            {
                                // 進捗は、実際にコピーするときにだけ表示された方が良い
                                reporter2.WriteProgress(++index2, $"{index2:D}/{reporter2.TotalNum} {key}.nupkg to {dstDrive.NameColonSeparator}");

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
        var srcDrives = OrchDriveInfo.EnumOrchDrives([Path]);
        var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version
            .Split1stValueByUnescapedCommas() // CSV から入力されている可能性があるので、カンマで区切る
            .ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        CopyLibraries(this, srcDrives, wpId, wpVersion, dstDrives, false, cancelHandler.Token);



        // マルチスレッド版。ちゃんと動作しているが、シングルスレッドで書き直すことにした。
        // コピー元ドライブをマルチスレッドにすることは意味がなかった。。
        // GetLibraries() はコストが安いし、各ドライブに対して一度しか実行しない
        // そもそも、コピー元はひとつしか指定できないようにしてあるしな。。
        //using var results = OrchThreadPool.RunForEach(srcDrives,
        //    drive => drive.NameColonSeparator,
        //    drive => drive,
        //    drive => drive.GetLibraries()
        //        .FilterByWildcards(l => l?.Id, wpId)
        //        .OrderBy(version => version.Id)
        //        .ToList());

        //using var cancelHandler = new ConsoleCancelHandler();
        //foreach (var result in results)
        //{
        //    try
        //    {
        //        var entities = result.GetResult(cancelHandler.Token);
        //        var srcDrive = result.Source;

        //        foreach (var library in entities!)
        //        {
        //            try
        //            {
        //                srcDrive._dicLibraryVersions = null;
        //                var versions = srcDrive.GetLibraryVersions(library.Id!)
        //                    .FilterByWildcards(l => l?.Version, wpVersion)
        //                    //.OrderBy(version => version.Version!, VersionComparer.Instance)
        //                    .ToList();

        //                int index = 0;
        //                reporter.TotalNum = dstDrives.Count * versions.Count;
        //                foreach (var version in versions)
        //                {
        //                    string? fileName = null;
        //                    byte[]? fileContent = null;
        //                    foreach (var dstDrive in dstDrives)
        //                    {
        //                        cancelHandler.Token.ThrowIfCancellationRequested();

        //                        string key = $"{version.GetPSPath()}:{version.Version}";
        //                        string target = $"Item: {key} Destination: {dstDrive.NameColonSeparator}";

        //                        if (srcDrive == dstDrive) continue;

        //                        // dstDrive に同名のパッケージがあれば、警告を表示してコピーをスキップする
        //                        if (LibraryExists(dstDrive, version))
        //                        {
        //                            WriteError(new ErrorRecord(new InvalidOperationException($"\"{version.GetPSPath()}:{version.Version}\": Library already exists in {dstDrive.NameColonSeparator}. Skipping the copy."), "CopyLibraryError", ErrorCategory.WriteError, dstDrive));
        //                            continue;
        //                        }

        //                        if (ShouldProcess(target, $"Copy Library"))
        //                        {
        //                            // 進捗は、実際にコピーするときにだけ表示された方が良い
        //                            reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {key}.nupkg to {dstDrive.NameColonSeparator}");

        //                            if (fileName is null)
        //                            {
        //                                if (!DownloadLibrary(srcDrive, version, out fileName, out fileContent, target)) break;
        //                            }
        //                            UploadLibrary(version, fileName, fileContent, dstDrive);
        //                        }
        //                    }
        //                }
        //            }
        //            catch (OperationCanceledException)
        //            {
        //                throw;
        //            }
        //            catch (Exception ex)
        //            {
        //                WriteError(new ErrorRecord(new OrchException(library.GetPSPath(), ex), "GetLibraryVersionError", ErrorCategory.InvalidOperation, library));
        //            }
        //        }
        //    }
        //    catch (OrchException ex)
        //    {
        //        WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
        //    }
        //}
    }
}
