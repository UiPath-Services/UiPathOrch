using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Id_Version_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Export, "OrchLibrary", SupportsShouldProcess = true)]
public class ExportLibraryCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(LibraryIdCompleter<TPositional>))]
    public string[]? Id { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(LibraryVersionCompleter<TPositional>))]
    public string[]? Version { get; set; }

    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    public string? Destination { get; set; }

    //[Parameter]
    //public SwitchParameter HostFeed { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = OrchDriveInfo.EnumOrchDrives(Path);
        var wpId = Id.ConvertToWildcardPatternList();
        var wpVersion = Version.ConvertToWildcardPatternList();

        if (Destination is null)
        {
            Destination = SessionState.Path.CurrentFileSystemLocation.Path;
        }

        // PSDrive のパスを、実際のファイルシステムのパスに変換
        Destination = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);

        if (!Directory.Exists(Destination))
        {
            throw new DirectoryNotFoundException($"A directory '{Destination}' does not exist.");
        }

        // 最初にすべてまとめて非同期に API call するバージョン
        // ちゃんと動いているけど、API call の数が多すぎてしまうかも。
#if false
        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => {
                var libraries = drive.GetLibraries();
                return OrchThreadPool.RunForEach(libraries
                        .FilterByWildcards(l => l.Id!, wpId)
                        .Select(library => (drive, library)),
                    dl => dl.library.GetPSPath(),
                    dl => dl.library,
                    dl => drive.GetLibraryVersions(dl.library.Id!)
                        .FilterByWildcards(l => l.Version!, wpVersion)
                        .OrderBy(l => l.Version!, new VersionComparer()));
            });

        using var reporter = new ProgressReporter(this, 1, 100, "Export Library", "Export Library");
        foreach (var result in results)
        {
            try
            {
                using var threads = result.GetResult();

                foreach (var thread in threads!)
                {
                    try
                    {
                        var versions = thread.GetResult();
                        var (drive, library) = thread.Source;

                        int index = 0;
                        int totalNum = versions!.Count();

                        reporter.TotalNum = totalNum;
                        foreach (var version in versions!)
                        {
                            reporter.WriteProgress(++index, $"{index:D}/{totalNum} {version.Id}:{version.Version}");
                            if (ShouldProcess(version.GetPSPath(), "Export Library"))
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
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetLibraryVersionError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetLibraryError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
#endif
        // 最初の GetLibrary() だけ非同期に実行するバージョン
        // GetLibraryVersion() は、ダウンロードの直前に呼び出す。
        // これくらいの方がバランスが良いか。
        // GetLibraryVersion() の実行時間は、さほど長くないし。
#if true
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
                var drive = result.Source; // Source プロパティを参照するのは、GetResult() の後にする必要がある

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
                        foreach (var version in versions)
                        {
                            cancelHandler.Token.ThrowIfCancellationRequested();

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

#endif
    }
}
