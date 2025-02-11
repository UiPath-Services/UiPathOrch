using System.Collections;
using System.Collections.ObjectModel;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Provider;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;


namespace UiPath.PowerShell.Core;

[CmdletProvider("UiPathOrchLib", ProviderCapabilities.ShouldProcess)]
class LibraryProvider : NavigationCmdletProvider, IContentCmdletProvider
{
    #region CmdletProvider overrides

    internal LibraryDriveInfo LibraryDriveInfo => (LibraryDriveInfo)this.PSDriveInfo;

    protected override ProviderInfo Start(ProviderInfo providerInfo)
    {
        ProviderInfo ret = base.Start(providerInfo);
        LibraryDriveInfo.SessionState = base.SessionState;
        return ret;
    }

    //protected OrchDriveInfo? GetOrchDriveInfo(string path)
    //{
    //    if (OrchDriveInfo is not null)
    //    {
    //        return OrchDriveInfo;
    //    }
    //    string driveName = OrchDriveInfo.ExtractDriveName(path);
    //    return SessionState.Drive.Get(driveName) as OrchDriveInfo;
    //}

    #endregion CmdletProvider overrides

    #region DriveCmdletProvider overrides

    internal Dictionary<string, LibraryVersion>? _dicLibraryVersions = null;
    //private bool disposedValue;

    protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
    {
        return base.RemoveDrive(drive);
    }

    #endregion DriveCmdletProvider overrides

    #region ItemCmdletProvider overrides

    protected override void GetItem(string path)
    {
        //_dicLibraryVersions ??= GetAllLibraryVersions().ToDictionary(v => System.IO.Path.Combine(v.Path!, v.Name!));

//            if (_dicLibraryVersions.TryGetValue(path, out var version))
        {
            //WriteItemObject(version, System.IO.Path.Combine(path, Path.Combine(version.Path!, version.Name!)), false);
        }
    }

    // TODO: これ何か実装した方が良いのでは？ でも呼ばれたことがないような気がする。。
    protected override bool IsValidPath(string path)
    {
        return true;
    }

    // ItemExists メソッドは、ワイルドカードを処理する必要はないっぽい。
    protected override bool ItemExists(string path)
    {
        if (_dicLibraryVersions is null)
        {
            return true;
        }
        //_dicLibraryVersions ??= GetAllLibraryVersions().ToDictionary(v => System.IO.Path.Combine(v.Path!, v.Name!));
        return _dicLibraryVersions.ContainsKey(path);
    }

    #endregion ItemCmdletProvider overrides

    #region ContainerCmdletProvider overrides

    private IEnumerable<(ReadOnlyCollection<LibraryVersion>? versions, ErrorRecord? error)> GetLibraryVersions()
    {
        var libraries = LibraryDriveInfo.ParentDrive.LibrariesInTenant.Get().OrderBy(l => l.Id);
        yield break;
        //using var resultTracker = new ThreadResultsTracker<LibraryVersion>(libraries.Count());

        //// 並行にライブラリバージョンを取得
        //Task.Run(() =>
        //{
        //    ForEach(libraries, (library, state, index) =>
        //    {
        //        string target = System.IO.Path.Combine($"{LibraryDriveInfo.ParentDrive.Name}-lib:", library.Id!);
        //        try
        //        {
        //            var versions = LibraryDriveInfo.ParentDrive.GetLibraryVersions(library.Id!);
        //            resultTracker.SetResult(index, versions);
        //        }
        //        catch (Exception ex)
        //        {
        //            var error = new ErrorRecord(new OrchException(target, ex), "GetLibraryVersionError", ErrorCategory.InvalidOperation, target);
        //            resultTracker.SetError(index, error);
        //        }
        //    });
        //});

        //int currentIndex = 0;
        //while (currentIndex < libraries.Count())
        //{
        //    var (versions, error) = resultTracker.GetResult(currentIndex);
        //    if (versions is not null)
        //    {
        //        foreach (var version in versions
        //            .OrderBy(l => l.Version!, new VersionComparer()))
        //        {
        //            var versionCopy = CollectionExtensions.DeepCopy(version);
        //        }
        //        yield return (versions, null);
        //    }
        //    else
        //    {
        //        yield return (null, error);
        //    }
        //    ++currentIndex;
        //}
    }

    //private IEnumerable<LibraryVersion> GetAllLibraryVersions()
    //{
    //    var libraries = GetLibraryVersions().Select(v => v.versions).SelectMany(v => v);
    //    return libraries;
    //}

    protected override void GetChildItems(string path, bool recurse)
    {
        GetChildItems(path, recurse, 0);
    }

    protected override void GetChildItems(string path, bool recurse, uint depth)
    {
        foreach (var versionsError in GetLibraryVersions())
        {
            var (versions, error) = versionsError;
            if (versions is not null)
            {
                foreach (var version in versions.OrderBy(v => v.Version!, VersionComparer.Instance))
                {
                    WriteItemObject(version,
                        System.IO.Path.Combine(path, $"{version.Id!}.{version.Version!}.nupkg"),
                        false);
                }
            }
            else
            {
                WriteError(error);
            }
        }
    }

    // GetChildnames は、オブジェクトでなく名前の string 値のみを WriteItemObject する必要がある。
    // このメソッドは、Get-ChildItem -Name を実行すると呼び出される。
    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        foreach (var versionsError in GetLibraryVersions())
        {
            var (versions, error) = versionsError;
            if (versions is not null)
            {
                foreach (var version in versions.OrderBy(v => v.Version!, VersionComparer.Instance))
                {
                    //WriteItemObject(version.Name, System.IO.Path.Combine(path, $"{version.Name}"), false);
                }
            }
            else
            {
                WriteError(error);
            }
        }
    }

    protected override bool HasChildItems(string path)
    {
        // path がルートの場合に限り、ファイルが存在すれば true を返す
        return IsItemContainer(path);
    }

    protected override void CopyItem(string path, string copyPath, bool recurse)
    {
        base.CopyItem(path, copyPath, recurse);

        Console.WriteLine(LibraryDriveInfo.Name);
    }

    protected override void RenameItem(string path, string newName)
    {
        base.RenameItem(path, newName);
    }

    protected override object RenameItemDynamicParameters(string path, string newName)
    {
        return base.RenameItemDynamicParameters(path, newName);
    }

    protected override void RemoveItem(string path, bool recurse)
    {
        base.RemoveItem(path, recurse);
    }

    #endregion

    #region NavigationCmdletProvider overrides

    protected override bool IsItemContainer(string path)
    {
        int colonIndex = path.IndexOf(':');

        if (colonIndex != -1)
        {
            path = path.Substring(colonIndex + 1);
        }

        return path == "\\";
    }

    protected override string MakePath(string parent, string child)
    {
        string ret = base.MakePath(parent, child);
        return ret;
        //string retNew = base.MakePath(parent, child);
        //if (retNew.EndsWith("\\") && retNew.Length > 1 && retNew[retNew.Length-2] != ':')
        //{
        //    retNew = retNew.Substring(0, retNew.Length - 1);
        //}
        //return retNew;
    }

    protected override void MoveItem(string path, string destination)
    {
        base.MoveItem(path, destination);
    }

    #endregion

    #region IContentCmdletProvider

    public object? GetContentReaderDynamicParameters(string path)
    {
        throw new NotImplementedException();
    }

    IContentReader? IContentCmdletProvider.GetContentReader(string path)
    {
        return new LibraryContentReader();
    }

    public object? GetContentWriterDynamicParameters(string path)
    {
        throw new NotImplementedException();
    }

    IContentWriter? IContentCmdletProvider.GetContentWriter(string path)
    {
        return new LibraryContentWriter();
    }

    public object? ClearContentDynamicParameters(string path)
    {
        throw new NotImplementedException();
    }

    void IContentCmdletProvider.ClearContent(string path)
    {
        throw new NotImplementedException();
    }

    #endregion
}

internal class LibraryContentReader : IContentReader
{
    private bool disposedValue;

    void IContentReader.Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    IList IContentReader.Read(long readCount)
    {
        throw new NotImplementedException();
    }

    void IContentReader.Close()
    {
        throw new NotImplementedException();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: マネージド状態を破棄します (マネージド オブジェクト)
            }

            // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
            // TODO: 大きなフィールドを null に設定します
            disposedValue = true;
        }
    }

    // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
    // ~LibraryContentReader()
    // {
    //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
    //     Dispose(disposing: false);
    // }

    void IDisposable.Dispose()
    {
        // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

internal class LibraryContentWriter : IContentWriter
{
    private bool disposedValue;

    public void Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public IList Write(IList content)
    {
        throw new NotImplementedException();
    }

    public void Close()
    {
        throw new NotImplementedException();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: マネージド状態を破棄します (マネージド オブジェクト)
            }

            // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
            // TODO: 大きなフィールドを null に設定します
            disposedValue = true;
        }
    }

    // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
    // ~LibraryContentWriter()
    // {
    //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
