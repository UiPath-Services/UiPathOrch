using System.Management.Automation;
using UiPath.OrchAPI;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

public class ExternalApplicationTestResult
{
    public string? Path { get; set; }
    public string? AppId { get; set; }
    public string? Name { get; set; }
    public bool? IsConfidential { get; set; }
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public string[]? RequestedScope { get; set; }
    public string[]? GrantedScope { get; set; }
    public int? ExpiresIn { get; set; }
    public string[] Problems { get; set; } = [];
}

// Requests a token for an external application with client credentials, as a script using the
// application would, and says why it is refused. The token endpoint answers invalid_client or
// invalid_scope with no description, on purpose; #jp-help-infra spent a thread finding that an
// application's user scopes and application scopes had been mixed up. The cause is read from the
// application's registration in the organization, when a mounted drive of that organization can
// read it.
[Cmdlet(VerbsDiagnostic.Test, "PmExternalApplication")]
[OutputType(typeof(ExternalApplicationTestResult))]
public class TestPmExternalApplicationCmdlet : OrchestratorPSCmdlet
{
    // Omitted: the drive's own confidential application (its AppId, AppSecret and Scope).
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [Alias("ClientId", "id")]
    public string? AppId { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [Alias("ClientSecret")]
    public string? AppSecret { get; set; }

    // Omitted with -AppId: every application scope registered on the application.
    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    public string? Scope { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    // ExternalScopeDto.type, as the registrations show it: a confidential application's scopes are
    // 1 and a non-confidential one's are 0 (yotsuda org, 2026-10-08, all 11 applications).
    internal const int UserScopeType = 0;
    internal const int ApplicationScopeType = 1;

    protected override void ProcessRecord()
    {
        foreach (var drive in SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath)))
        {
            string target = drive.NameColonSeparator;

            string? clientId = AppId, secret = AppSecret, scope = Scope;
            if (string.IsNullOrEmpty(clientId))
            {
                if (string.IsNullOrEmpty(drive._psDrive.AppSecret))
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException($"{target} does not sign in with a confidential application. Specify -AppId and -AppSecret."),
                        "TestPmExternalApplicationNoApp", ErrorCategory.InvalidArgument, target));
                    continue;
                }
                clientId = drive._psDrive.AppId;
                secret = drive._psDrive.AppSecret;
                scope ??= drive._psDrive.Scope;
            }
            else if (string.IsNullOrEmpty(secret))
            {
                WriteError(new ErrorRecord(
                    new ArgumentException("-AppSecret is required with -AppId."),
                    "TestPmExternalApplicationNoSecret", ErrorCategory.InvalidArgument, clientId));
                continue;
            }

            var (registration, registrationNote) = FindRegistration(drive, clientId!);

            if (string.IsNullOrEmpty(scope))
            {
                var registered = ApplicationScopes(registration);
                if (registered.Length == 0)
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException($"Specify -Scope: the scopes registered on application {clientId} could not be read ({registrationNote ?? "it has no application scope"})."),
                        "TestPmExternalApplicationNoScope", ErrorCategory.InvalidArgument, clientId));
                    continue;
                }
                scope = string.Join(' ', registered);
            }

            string[] requested = scope!.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            AuthManagerProbe probe;
            try
            {
                var r = drive.OrchAPISession.AuthManager.ProbeClientCredentials(clientId!, secret!, scope!);
                probe = new(r.StatusCode, r.Error, r.ErrorDescription, r.GrantedScope, r.ExpiresInSeconds);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                WriteError(new ErrorRecord(new OrchException(target, ex), "TestPmExternalApplicationRequestError", ErrorCategory.ConnectionError, target));
                continue;
            }

            bool succeeded = probe.StatusCode is >= 200 and < 300;
            var problems = Diagnose(succeeded, probe.Error, clientId!, registration, registrationNote is null, requested, DateTime.Now);
            if (registrationNote is not null)
            {
                problems.Add($"The application's registration could not be read, so the cause is not narrowed down: {registrationNote}");
            }
            if (!succeeded && string.Equals(probe.Error, "invalid_scope", StringComparison.OrdinalIgnoreCase)
                && OrchestratorAuthManager.BuildScopeTooLongAdvice(target, scope!.Length, drive.ProductVersion.CachedValue?.version, lengthIsCertain: false) is string tooLong)
            {
                problems.Add(tooLong);
            }

            WriteObject(new ExternalApplicationTestResult
            {
                Path = target,
                AppId = clientId,
                Name = registration?.name,
                IsConfidential = registration?.isConfidential,
                Succeeded = succeeded,
                Error = succeeded ? null : (probe.Error ?? $"HTTP {probe.StatusCode}"),
                ErrorDescription = probe.ErrorDescription,
                RequestedScope = requested,
                GrantedScope = probe.GrantedScope?.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                ExpiresIn = probe.ExpiresIn,
                Problems = [.. problems],
            });
        }
    }

    private sealed record AuthManagerProbe(int StatusCode, string? Error, string? ErrorDescription, string? GrantedScope, int? ExpiresIn);

    internal static string[] ApplicationScopes(ExternalClient? registration) =>
        registration?.resources?
            .SelectMany(r => r.scopes ?? [])
            .Where(s => s.type == ApplicationScopeType && !string.IsNullOrEmpty(s.name))
            .Select(s => s.name!)
            .Distinct()
            .ToArray() ?? [];

    // registration null with registrationRead true: the organization has no application with
    // this client ID. Pure, so each cause is unit-testable.
    internal static List<string> Diagnose(bool succeeded, string? error, string clientId, ExternalClient? registration, bool registrationRead, string[] requestedScope, DateTime now)
    {
        List<string> problems = [];
        if (!registrationRead) return problems;

        if (registration is null)
        {
            problems.Add($"No external application with App ID {clientId} is registered in this organization. The ID is wrong, or the application belongs to another organization.");
            return problems;
        }

        string name = registration.name ?? clientId;

        if (registration.isConfidential == false)
        {
            problems.Add($"'{name}' is a non-confidential application. It has no secret and cannot request a token with client credentials; sign in with it interactively instead, or register a confidential application.");
            return problems;
        }

        var secrets = registration.secrets ?? [];
        if (secrets.Length > 0 && secrets.All(s => s.expiryTime is DateTime t && t <= now))
        {
            problems.Add($"Every secret of '{name}' has expired. Add a new secret to the application.");
        }
        else if (!succeeded && string.Equals(error, "invalid_client", StringComparison.OrdinalIgnoreCase))
        {
            problems.Add($"The App Secret does not match a valid secret of '{name}'. Check the value, or add a new secret to the application.");
        }

        var allScopes = registration.resources?.SelectMany(r => r.scopes ?? []).Where(s => !string.IsNullOrEmpty(s.name)).ToList() ?? [];
        var applicationScopes = allScopes.Where(s => s.type == ApplicationScopeType).Select(s => s.name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var userScopes = allScopes.Where(s => s.type == UserScopeType).Select(s => s.name!).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var s in requestedScope.Where(s => !applicationScopes.Contains(s)))
        {
            problems.Add(userScopes.Contains(s)
                ? $"'{s}' is registered on '{name}' as a user scope. A token requested with client credentials carries application scopes only; register it as an application scope."
                : $"'{s}' is not registered on '{name}'. Add it as an application scope, or leave it out of the request.");
        }

        return problems;
    }

    // The application's registration, read through the drive itself or another signed-in drive
    // of the same organization -- the drive under test often lacks the PM scopes that reading it
    // needs. Drives not signed in are not tried: that would start a sign-in. Returns the
    // registration, or null with null note when the organization has none with this ID, or null
    // with a note when it could not be read.
    private (ExternalClient? registration, string? note) FindRegistration(OrchDriveInfo drive, string clientId)
    {
        // The passive OrgCacheKey (null until a drive has signed in), never IsSameOrganization:
        // that one signs a drive in to learn its organization, which started a browser sign-in
        // for every mounted drive of another organization (2026-10-08, first live run).
        var candidates = new List<OrchDriveInfo> { drive };
        string? orgKey = drive.GetOrgCacheKey();
        if (orgKey is not null)
        {
            candidates.AddRange(SessionState.EnumAllOrchDrives()
                .Where(d => d != drive && d.OrchAPISession.AuthManager.IsAuthenticated && d.OrgCacheKey == orgKey));
        }

        string? lastError = null;
        foreach (var d in candidates)
        {
            try
            {
                var list = d.PmExternalClients.Get().ToList();
                var match = list.FirstOrDefault(c => string.Equals(c.id, clientId, StringComparison.OrdinalIgnoreCase));
                if (match is null) return (null, null);
                var detail = d.OrchAPISession.GetPmExternalClient(d.GetPartitionGlobalId(), match.id!);
                return (detail ?? match, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = $"{d.NameColonSeparator} {ex.Message}";
            }
        }
        return (null, lastError ?? "no signed-in drive of this organization can read external applications");
    }
}
