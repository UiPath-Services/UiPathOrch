using System.Management.Automation;
using UiPath.OrchAPI;

namespace UiPath.PowerShell.Core;

public class LibraryDriveInfo : PSDriveInfo
{
    internal OrchDriveInfo ParentDrive { get; }
    public OrchAPISession OrchAPISession { get; }

    // LibraryProvider の Start で初期化する
    internal static SessionState? SessionState;

    //internal List<LibraryVersion> _dicLibraryVersion = null;
    //public IEnumerable<Library> GetLibraries()
    //{
    //    ParentDrive.GetLibraries();
    //    foreach (var lib in ParentDrive.GetLibraries())
    //    {
    //        var ret = CollectionExtensions.DeepCopy(lib);
    //        ret.Path = Name + ":\\";
    //        yield return ret;
    //    }
    //}

    public LibraryDriveInfo(OrchDriveInfo parentDrive, ProviderInfo provider)
        : base(parentDrive.Name + "Lib", provider, parentDrive.Name + "Lib:\\", "", null, $" -> The tenant library feed.")
    {
        ParentDrive = parentDrive;
        OrchAPISession = parentDrive.OrchAPISession;
        ProviderInfo = provider;
    }

    public ProviderInfo ProviderInfo { get; private set; }

    public void ClearAllCache()
    {
//            _dicFolders = null;
    }
}
