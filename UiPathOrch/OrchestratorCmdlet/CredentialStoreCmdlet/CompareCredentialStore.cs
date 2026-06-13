using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare credential stores between two Orchestrator instances. Credential stores are
// tenant-level, so the reference (-Path) and difference (-DifferencePath) are drives, not
// folders. Matches by Name and compares the store type and connection configuration. Secrets
// are not stored here and are never compared.
[Cmdlet(VerbsData.Compare, "OrchCredentialStore")]
[OutputType(typeof(OrchComparison))]
public class CompareCredentialStoreCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter))]
    [SupportsWildcards]
    public string? DifferenceName { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    private static readonly (string Name, Func<CredentialStore, object?> Get)[] Comparators =
    [
        ("Type", c => c.Type),
        ("ProxyType", c => c.ProxyType),
        ("HostName", c => c.HostName),
        ("AdditionalConfiguration", c => c.AdditionalConfiguration),
    ];

    internal static readonly HashSet<string> ValidPropertyNames =
        new(Comparators.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);

    protected override IEnumerable<string> GetTargetDriveNames()
    {
        foreach (var n in base.GetTargetDriveNames()) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("DifferencePath", out var dp))
            foreach (var n in ExtractDriveNamesFromBoundPath(dp)) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("LiteralPath", out var lp))
            foreach (var n in ExtractDriveNamesFromBoundPath(lp)) yield return n;
    }

    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        CompareParameterHelper.WarnSecretNotCompared(this, "credential-store secrets (AdditionalConfiguration)");
    }

    protected override void ProcessRecord()
    {
        var only = CompareParameterHelper.ResolvePropertyFilter(this, Property, ValidPropertyNames);

        TenantCompare.Run<CredentialStore>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name,
            IncludeEqual.IsPresent,
            only,
            drive => drive.CredentialStores.Get(),
            c => c?.Name,
            Comparators,
            "GetCredentialStoreError",
            WriteObject,
            WriteError);
    }
}
