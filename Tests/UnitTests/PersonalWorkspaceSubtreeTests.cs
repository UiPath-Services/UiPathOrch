using System.Collections.Generic;
using System.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins GraftPersonalWorkspaceSubtrees, the pure merge behind personal-workspace
// SUBFOLDER visibility. /odata/Folders never returns folders nested under a personal
// workspace (a solution deployed there creates FolderType=Solution subfolders), so
// BuildFolderCache grafts them in from /api/Folders/GetAllForCurrentUser. The
// navigation items lack FeedType / FullyQualifiedNameOrderable; the graft stamps them.
public class PersonalWorkspaceSubtreeTests
{
    private static Folder Nav(long id, string name, string fqn, long? parentId = null) => new()
    {
        Id = id,
        DisplayName = name,
        FullyQualifiedName = fqn,
        FolderType = "Solution",
        ParentId = parentId,
        // No FeedType / FullyQualifiedNameOrderable — matches the live endpoint shape.
    };

    private static Folder Api(long id, string name, string fqn) => new()
    {
        Id = id,
        DisplayName = name,
        FullyQualifiedName = fqn,
        FullyQualifiedNameOrderable = fqn,
        FolderType = "Standard",
        FeedType = "Processes",
    };

    private static Folder PwRoot(long id, string name) => new()
    {
        Id = id,
        DisplayName = name,
        FullyQualifiedName = name,
        FolderType = "Personal",
        FeedType = "FolderHierarchy",
    };

    [Fact]
    public void Grafts_descendants_of_pw_root_and_stamps_missing_fields()
    {
        var apiFolders = new List<Folder> { Api(1, "Shared", "Shared") };
        var pwRoots = new List<Folder> { PwRoot(100, "me's workspace") };
        var nav = new List<Folder>
        {
            Nav(100, "me's workspace", "me's workspace"),                       // the root itself — NOT grafted
            Nav(200, "My solution", "me's workspace/My solution", 100),
            Nav(201, "Sub", "me's workspace/My solution/Sub", 200),
        };

        OrchDriveInfo.GraftPersonalWorkspaceSubtrees(apiFolders, pwRoots, nav);

        Assert.Equal(new long?[] { 1, 200, 201 }, apiFolders.Select(f => f.Id));
        var grafted = apiFolders.Single(f => f.Id == 200);
        Assert.Equal("Processes", grafted.FeedType);
        Assert.Equal("me's workspace/My solution", grafted.FullyQualifiedNameOrderable);
    }

    [Fact]
    public void Ignores_folders_outside_any_pw_root()
    {
        var apiFolders = new List<Folder> { Api(1, "Shared", "Shared") };
        var pwRoots = new List<Folder> { PwRoot(100, "me's workspace") };
        var nav = new List<Folder>
        {
            Nav(300, "Elsewhere", "Other/Elsewhere", 2),
            Nav(301, "Shared", "Shared"), // tenant folder echoed by the nav endpoint
        };

        OrchDriveInfo.GraftPersonalWorkspaceSubtrees(apiFolders, pwRoots, nav);

        Assert.Equal(new long?[] { 1 }, apiFolders.Select(f => f.Id));
    }

    [Fact]
    public void Prefix_match_requires_the_separator_not_just_a_name_prefix()
    {
        // "me's workspace2/X" must NOT match PW root "me's workspace".
        var apiFolders = new List<Folder>();
        var pwRoots = new List<Folder> { PwRoot(100, "me's workspace") };
        var nav = new List<Folder> { Nav(400, "X", "me's workspace2/X", 99) };

        OrchDriveInfo.GraftPersonalWorkspaceSubtrees(apiFolders, pwRoots, nav);

        Assert.Empty(apiFolders);
    }

    [Fact]
    public void Skips_items_already_present_by_id()
    {
        var apiFolders = new List<Folder> { Api(200, "My solution", "me's workspace/My solution") };
        var pwRoots = new List<Folder> { PwRoot(100, "me's workspace") };
        var nav = new List<Folder> { Nav(200, "My solution", "me's workspace/My solution", 100) };

        OrchDriveInfo.GraftPersonalWorkspaceSubtrees(apiFolders, pwRoots, nav);

        Assert.Single(apiFolders);
    }

    [Fact]
    public void Explored_workspace_subtrees_are_grafted_too()
    {
        var apiFolders = new List<Folder>();
        var pwRoots = new List<Folder> { PwRoot(100, "me's workspace"), PwRoot(110, "peer's workspace") };
        var nav = new List<Folder>
        {
            Nav(500, "Deployed", "peer's workspace/Deployed", 110),
        };

        OrchDriveInfo.GraftPersonalWorkspaceSubtrees(apiFolders, pwRoots, nav);

        Assert.Equal(new long?[] { 500 }, apiFolders.Select(f => f.Id));
    }

    [Fact]
    public void Null_navigation_list_and_no_pw_roots_are_no_ops()
    {
        var apiFolders = new List<Folder> { Api(1, "Shared", "Shared") };

        OrchDriveInfo.GraftPersonalWorkspaceSubtrees(apiFolders, new List<Folder> { PwRoot(100, "ws") }, null);
        OrchDriveInfo.GraftPersonalWorkspaceSubtrees(apiFolders, new List<Folder>(), new List<Folder> { Nav(2, "X", "ws/X") });

        Assert.Equal(new long?[] { 1 }, apiFolders.Select(f => f.Id));
    }
}
