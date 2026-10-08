using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Registers an external application, as the Add Application page of the organization's admin
// portal does: a name, the application type, the user and application scopes, and a redirect
// URL. Returns the new application with its id and, for a confidential one, its secret, which is
// shown only now, as Copy-PmExternalApplication does.
[Cmdlet(VerbsCommon.New, "PmExternalApplication", SupportsShouldProcess = true)]
[OutputType(typeof(ExternalClientCreated))]
public class NewPmExternalApplicationCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    public string? Name { get; set; }

    // ExternalClientDto.isConfidential. Omitted: confidential, the type that takes application scopes.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BoolCompleter))]
    public string? IsConfidential { get; set; }

    // Scopes the application receives with client credentials (ExternalScopeDto.type 1). Wildcards
    // expand against the organization's scopes that can be application scopes.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ApplicationScopeCompleter))]
    [SupportsWildcards]
    public string[]? ApplicationScope { get; set; }

    // Scopes the application receives on behalf of a signed-in user (ExternalScopeDto.type 0).
    // Wildcards expand against the organization's scopes that can be user scopes.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserScopeCompleter))]
    [SupportsWildcards]
    public string[]? UserScope { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? RedirectUri { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // Pure, so the payload is unit-testable. Scope values may also be space-separated in one string.
    internal static CreateExternalClientCommand BuildCommand(string partitionGlobalId, string name, bool isConfidential, string? redirectUri, string[]? applicationScope, string[]? userScope)
    {
        static IEnumerable<string> Split(string[]? values) =>
            (values ?? []).SelectMany(v => (v ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)).Distinct(StringComparer.OrdinalIgnoreCase);

        var scopes = Split(applicationScope).Select(s => new ExternalScope { name = s, type = TestPmExternalApplicationCmdlet.ApplicationScopeType })
            .Concat(Split(userScope).Select(s => new ExternalScope { name = s, type = TestPmExternalApplicationCmdlet.UserScopeType }))
            .ToArray();

        return new CreateExternalClientCommand
        {
            partitionGlobalId = partitionGlobalId,
            name = name,
            isConfidential = isConfidential,
            redirectUri = string.IsNullOrEmpty(redirectUri) ? null : redirectUri,
            scopes = scopes,
        };
    }

    // Expands the wildcards of one scope parameter against the organization's catalog (types: the
    // catalog types that can serve as this kind of scope); a pattern matching nothing is an error.
    private bool TryExpand(OrchDriveInfo drive, string target, string[]? values, int[] types, string kind, out string[]? expanded)
    {
        expanded = values;
        if (values is null) return true;
        IEnumerable<string> known = ExternalScopeArguments.HasWildcard(values)
            ? ExternalScopeArguments.Catalog(drive, types).Select(c => c.name).ToList()
            : [];
        expanded = [.. ExternalScopeArguments.Expand(values, known, out var unmatched)];
        if (unmatched.Count == 0) return true;

        WriteError(new ErrorRecord(
            new ArgumentException($"\"{target}\": {string.Join(", ", unmatched)} matches no scope of the organization that can be {(kind == "application" ? "an" : "a")} {kind} scope."),
            "NewPmExternalApplicationScopeNotMatched", ErrorCategory.InvalidArgument, unmatched));
        return false;
    }

    protected override void ProcessRecord()
    {
        bool isConfidential = true;
        if (!string.IsNullOrEmpty(IsConfidential) && !bool.TryParse(IsConfidential, out isConfidential))
        {
            WriteError(new ErrorRecord(new ArgumentException($"-IsConfidential must be true or false; got '{IsConfidential}'."), "NewPmExternalApplicationInvalidType", ErrorCategory.InvalidArgument, IsConfidential));
            return;
        }

        // A non-confidential application has no secret, so it never uses client credentials, the
        // grant application scopes are for; the portal offers it user scopes only.
        if (!isConfidential && ApplicationScope is { Length: > 0 } && ApplicationScope.Any(s => !string.IsNullOrWhiteSpace(s)))
        {
            WriteError(new ErrorRecord(
                new ArgumentException("A non-confidential application takes user scopes only. Use -UserScope, or omit -IsConfidential false to create a confidential application."),
                "NewPmExternalApplicationApplicationScopeOnPublic", ErrorCategory.InvalidArgument, ApplicationScope));
            return;
        }

        foreach (var drive in SessionState.EnumPmDrives(EffectivePath(Path, LiteralPath)))
        {
            string target = $"{drive.NameColonSeparator}{Name}";
            if (!ShouldProcess(target, "New PmExternalApplication")) continue;

            try
            {
                var partitionGlobalId = drive.GetPartitionGlobalId();
                if (string.IsNullOrEmpty(partitionGlobalId)) continue;

                // The API accepts a second application of the same name without complaint (see
                // Copy-PmExternalApplication), and the portal lists them by name.
                if (drive.PmExternalClients.Get().Any(a => string.Equals(a.name, Name, StringComparison.OrdinalIgnoreCase)))
                {
                    WriteError(new ErrorRecord(new OrchException(target, $"An external application named '{Name}' already exists."), "NewPmExternalApplicationExists", ErrorCategory.ResourceExists, Name));
                    continue;
                }

                if (!TryExpand(drive, target, ApplicationScope, [1, 2], "application", out var applicationScopes)) continue;
                if (!TryExpand(drive, target, UserScope, [0, 2], "user", out var userScopes)) continue;

                var created = drive.OrchAPISession.PostPmExternalClient(
                    BuildCommand(partitionGlobalId, Name!, isConfidential, RedirectUri, applicationScopes, userScopes));

                // As Remove-PmExternalApplication: the application list, and the groups and
                // directory searches that list applications, change with it.
                drive.PmExternalClients.ClearCache();
                drive.PmGroups.ClearCache();
                drive.SearchPmDirectoryCache.ClearCache();
                drive.SearchDirectoryCache.ClearCache();

                if (created is null)
                {
                    WriteWarning($"\"{target}\": Failed to create an external application '{Name}'.");
                    continue;
                }
                var c = created.ShallowClone();
                c.Path = drive.NameColonSeparator;
                WriteObject(c);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                WriteError(new ErrorRecord(new OrchException(target, ex), "NewPmExternalApplicationError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
