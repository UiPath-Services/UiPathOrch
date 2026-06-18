using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UnitTests;

// Tier-2 provider harness: mounts a real UiPathOrch PSDrive in an in-process runspace with a
// SEEDED folder catalog — no live tenant, no auth. NewDrive only constructs the OrchDriveInfo
// (auth is lazy), and GetChildNames / HasChildItems / ItemExists / GetItem / GetParentPath read
// the seeded catalog without fetching. This lets tests drive the ACTUAL PowerShell engine globber
// against the provider (wildcard resolution, Test-Path, Split-Path re-rooting, Get-Item /
// PSParentPath) — the engine<->override interactions that the pure-helper unit tests cannot reach.
public sealed class OrchProviderHarness : IDisposable
{
    private readonly Runspace _runspace;
    public OrchDriveInfo Drive { get; }

    public OrchProviderHarness()
    {
        // Suppress the first-import config-file creation / notepad branch when no config file exists
        // (e.g. on CI): InitializeDefaultDrives then just returns null instead of prompting.
        System.Environment.SetEnvironmentVariable("UIPATHORCH_SUPPRESS_CONFIG_CREATION", "1");

        string dll = typeof(OrchProvider).Assembly.Location;
        var iss = InitialSessionState.CreateDefault2();
        iss.ImportPSModule(new[] { dll });
        _runspace = RunspaceFactory.CreateRunspace(iss);
        _runspace.Open();

        // NewDrive just builds an OrchDriveInfo from the dynamic params (lazy auth) — no network.
        Run("New-PSDrive -Name Test -PSProvider UiPathOrch -Root 'https://example.com/org/tenant' " +
            "-AppId '00000000-0000-0000-0000-000000000001' -AppSecret 'x' " +
            "-OAuthScope 'OR.Folders OR.Settings OR.Users' -Scope Global -ErrorAction Stop | Out-Null");

        Drive = (OrchDriveInfo)Run("Get-PSDrive -Name Test")[0].BaseObject;
    }

    public void Seed(IEnumerable<Folder> folders) => Drive.SeedFolderCatalogForTest(folders);

    // Run a script, failing the test if the script wrote to the error stream.
    public Collection<PSObject> Run(string script)
    {
        using var ps = PowerShell.Create();
        ps.Runspace = _runspace;
        ps.AddScript(script);
        var result = ps.Invoke();
        if (ps.HadErrors)
        {
            string err = ps.Streams.Error.Count > 0 ? ps.Streams.Error[0].ToString() : "unknown error";
            throw new InvalidOperationException($"Script failed: {script}\n{err}");
        }
        return result;
    }

    // Run a script tolerating errors (negative tests, e.g. a wildcard that matches nothing).
    public Collection<PSObject> RunAllowErrors(string script, out int errorCount)
    {
        using var ps = PowerShell.Create();
        ps.Runspace = _runspace;
        ps.AddScript(script);
        var result = ps.Invoke();
        errorCount = ps.Streams.Error.Count;
        return result;
    }

    // Build a seeded Folder, stamping the fields the provider reads (FullyQualifiedName for lookup
    // and depth, DisplayName for the leaf, Id/ParentId for child selection, FullName for PSPath).
    public static Folder F(string fqn, long id, long? parentId)
    {
        char sep = System.IO.Path.DirectorySeparatorChar;
        string leaf = fqn.Contains('/') ? fqn[(fqn.LastIndexOf('/') + 1)..] : fqn;
        return new Folder
        {
            DisplayName = leaf,
            FullyQualifiedName = fqn,
            FullyQualifiedNameOrderable = fqn,
            Id = id,
            ParentId = parentId,
            FolderType = "Standard",
            FeedType = "FolderHierarchy",
            FullName = "Test:" + sep + fqn.Replace('/', sep),
        };
    }

    public void Dispose() => _runspace?.Dispose();
}
