using System.Collections.Generic;
using System.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins the folder-structure ordering that OrchDriveInfo.GetFolders() / EnumFolders()
// depend on, extracted into the pure BuildFolderViews. The two views group and sort
// differently, and both sort by FullyQualifiedNameOrderable (NOT DisplayName/FQN) —
// subtle behavior that must survive the planned move into a dedicated cache class.
//
//   main view  : [prepended PW] -> root-level -> PW-feed nested -> other nested
//   enum view  : ([prepended PW] + ALL PW-feed) sorted together -> then non-PW
public class FolderViewOrderingTests
{
    private static Folder F(string name, string fqn, string feedType, string? orderable = null) => new()
    {
        DisplayName = name,
        FullyQualifiedName = fqn,
        FullyQualifiedNameOrderable = orderable ?? fqn,
        FeedType = feedType,
    };

    private static List<string> Names(IEnumerable<Folder> folders) => folders.Select(f => f.DisplayName!).ToList();

    [Fact]
    public void Main_view_groups_root_then_pw_nested_then_other_nested()
    {
        var pw = new List<Folder> { F("MyWS", "MyWS", "FolderHierarchy") };
        var api = new List<Folder>
        {
            F("Beta", "Beta", "Standard"),                 // root-level
            F("Alpha", "Alpha", "Standard"),               // root-level
            F("WsSub", "MyWS/Sub", "PersonalWorkspace"),   // PW-feed nested
            F("BetaChild", "Beta/Child", "Standard"),      // other nested
            F("AlphaChild", "Alpha/Child", "Standard"),    // other nested
        };

        var (main, _) = OrchDriveInfo.BuildFolderViews(new List<Folder>(pw), new List<Folder>(pw), api);

        // prepended PW, then root-level sorted, then PW-feed nested, then other nested sorted.
        Assert.Equal(new[] { "MyWS", "Alpha", "Beta", "WsSub", "AlphaChild", "BetaChild" }, Names(main));
    }

    [Fact]
    public void Enum_view_groups_pw_together_then_non_pw()
    {
        var pw = new List<Folder> { F("MyWS", "MyWS", "FolderHierarchy") };
        var api = new List<Folder>
        {
            F("Beta", "Beta", "Standard"),
            F("Alpha", "Alpha", "Standard"),
            F("WsSub", "MyWS/Sub", "PersonalWorkspace"),
            F("BetaChild", "Beta/Child", "Standard"),
            F("AlphaChild", "Alpha/Child", "Standard"),
        };

        var (_, enumView) = OrchDriveInfo.BuildFolderViews(new List<Folder>(pw), new List<Folder>(pw), api);

        // (prepended PW + all PW-feed) sorted by orderable, THEN all non-PW sorted by orderable.
        // Non-PW here includes root-level folders too (no '/' filter in the enum view).
        Assert.Equal(new[] { "MyWS", "WsSub", "Alpha", "AlphaChild", "Beta", "BetaChild" }, Names(enumView));
    }

    [Fact]
    public void Sort_key_is_orderable_not_display_name_or_fqn()
    {
        var api = new List<Folder>
        {
            F("Zeta", "Zeta", "Standard", orderable: "1"),   // alphabetically last, but orderable first
            F("Apple", "Apple", "Standard", orderable: "2"),
        };

        var (main, _) = OrchDriveInfo.BuildFolderViews([], [], api);

        Assert.Equal(new[] { "Zeta", "Apple" }, Names(main));
    }

    [Fact]
    public void Empty_api_folders_yields_just_the_prepended()
    {
        var pw = new List<Folder> { F("MyWS", "MyWS", "FolderHierarchy") };

        var (main, enumView) = OrchDriveInfo.BuildFolderViews(new List<Folder>(pw), new List<Folder>(pw), []);

        Assert.Equal(new[] { "MyWS" }, Names(main));
        Assert.Equal(new[] { "MyWS" }, Names(enumView));
    }

    [Fact]
    public void Root_level_pw_feed_folder_diverges_between_main_and_enum_views()
    {
        // A root-level PersonalWorkspace-feed folder is treated differently:
        //   main : it sits in the root-level group, sorted among root folders by orderable.
        //   enum : it is pulled into the PW group (sorted first), ahead of all non-PW.
        // Orderable "1" < "2" makes the divergence observable.
        var api = new List<Folder>
        {
            F("Std", "Std", "Standard", orderable: "1"),
            F("PwX", "PwX", "PersonalWorkspace", orderable: "2"),
        };

        var (main, enumView) = OrchDriveInfo.BuildFolderViews([], [], api);

        Assert.Equal(new[] { "Std", "PwX" }, Names(main));     // root group sorted by orderable: Std(1) < PwX(2)
        Assert.Equal(new[] { "PwX", "Std" }, Names(enumView)); // PwX in PW group first, then non-PW Std
    }
}
