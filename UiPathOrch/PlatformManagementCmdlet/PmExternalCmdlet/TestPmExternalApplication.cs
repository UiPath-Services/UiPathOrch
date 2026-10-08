using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
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
    // Null when no token was requested: a non-confidential application is checked without signing in.
    public bool? Succeeded { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }
    public string[]? RequestedScope { get; set; }
    public string[]? GrantedScope { get; set; }
    public int? ExpiresIn { get; set; }
    // The application's own token (client credentials), to call the API with as the application
    // would -- as Get-OrchPSDrive gives a drive's, a documented diagnostic feature.
    public string? AccessToken { get; set; }
    public string[] Problems { get; set; } = [];
}

// Requests a token for an external application with client credentials, as a script using the
// application would, and says why it is refused. The token endpoint answers a wrong secret or an
// unregistered scope with a bare invalid_client / invalid_scope, and a user scope with
// invalid_request "Client=... is not allowed to access User scopes", naming no scope (Cloud,
// 2026-10-08); #jp-help-infra spent a thread finding that an application's user scopes and
// application scopes had been mixed up. The cause is read from the
// application's registration in the organization, when a mounted drive of that organization can
// read it.
[Cmdlet(VerbsDiagnostic.Test, "PmExternalApplication")]
[OutputType(typeof(ExternalApplicationTestResult))]
public class TestPmExternalApplicationCmdlet : OrchestratorPSCmdlet
{
    // Application names, wildcards allowed; each match is tested. With neither -Name nor -AppId:
    // the drive's own confidential application (its AppId, AppSecret and Scope).
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ExternalApplicationNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    // Wins over -Name. Piped from Get-PmExternalApplication, its id binds here and its name to -Name.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(AppIdCompleter))]
    [Alias("ClientId", "id")]
    public string? AppId { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [Alias("ClientSecret")]
    public string? AppSecret { get; set; }

    // Comma-separated, or space-separated as in the configuration file; wildcards expand against the
    // application scopes registered on the application (else the organization's catalog).
    // Omitted with -AppId: every application scope registered on the application.
    [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestApplicationScopeCompleter))]
    [SupportsWildcards]
    public string[]? Scope { get; set; }

    // A non-confidential application only: the redirect URL the other tool signs in with, compared
    // with the one registered.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? RedirectUri { get; set; }

    // The organization's external applications; the App ID is inserted, the name is the tip.
    // What is typed is matched against both, so "OCM<Tab>" finds the application named OCM.
    private class AppIdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName, string parameterName, string wordToComplete,
            CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            var wp = CreateWPFromWordToComplete(wordToComplete);
            var results = ParallelResults.GroupBy(ResolvePmDrives(fakeBoundParameters), drive => drive.PmExternalClients.Get());

            foreach (var app in results.SelectMany(r => r)
                .Where(a => !string.IsNullOrEmpty(a?.id))
                .Where(a => wp.IsMatch(a.id) || (a.name is not null && wp.IsMatch(a.name)))
                .DistinctBy(a => a.id)
                .OrderBy(a => a.name))
            {
                string tip = app.isConfidential == false ? $"{app.name} (non-confidential)" : app.name ?? app.id!;
                yield return new CompletionResult(app.id, app.id, CompletionResultType.ParameterValue, tip);
            }
        }
    }

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
            string? secret = AppSecret;
            string[]? scopeValues = Scope;

            // The applications to test: -AppId, else every application -Name matches, else the
            // drive's own confidential application.
            List<(string id, ExternalClient? registration, string? note)> apps = [];
            if (!string.IsNullOrEmpty(AppId))
            {
                var (registration, note) = FindRegistration(SessionState, drive, AppId);
                apps.Add((AppId, registration, note));
            }
            else if (Name is { Length: > 0 })
            {
                var (found, unmatched, note) = FindRegistrationsByName(SessionState, drive, Name);
                if (note is not null)
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException($"{target} The applications could not be looked up by name: {note}. Specify -AppId instead."),
                        "TestPmExternalApplicationLookupFailed", ErrorCategory.ReadError, Name));
                    continue;
                }
                foreach (var n in unmatched)
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException($"No external application named '{n}' is registered in the organization of {target}."),
                        "TestPmExternalApplicationNameNotFound", ErrorCategory.ObjectNotFound, n));
                }
                apps.AddRange(found.Select(r => (r.id!, (ExternalClient?)r, (string?)null)));
            }
            else
            {
                if (string.IsNullOrEmpty(drive._psDrive.AppSecret))
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException($"{target} does not sign in with a confidential application. Specify -Name (or -AppId)."),
                        "TestPmExternalApplicationNoApp", ErrorCategory.InvalidArgument, target));
                    continue;
                }
                var (registration, note) = FindRegistration(SessionState, drive, drive._psDrive.AppId!);
                apps.Add((drive._psDrive.AppId!, registration, note));
                secret = drive._psDrive.AppSecret;
                scopeValues ??= string.IsNullOrEmpty(drive._psDrive.Scope) ? null : [drive._psDrive.Scope];
            }

            // A secret belongs to one application.
            if (!string.IsNullOrEmpty(secret) && apps.Count > 1)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException($"-AppSecret is the secret of one application, but -Name matches {apps.Count}: {string.Join(", ", apps.Select(a => a.registration?.name))}."),
                    "TestPmExternalApplicationSecretForMany", ErrorCategory.InvalidArgument, Name));
                continue;
            }

            foreach (var (id, registration, note) in apps.OrderBy(a => a.registration?.name, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(secret))
                    CheckRegistration(target, id, registration, note, scopeValues);
                else
                    RequestToken(drive, target, id, registration, note, secret, scopeValues);
            }
        }
    }

    // Without a secret nothing is signed in: the registration is checked. A non-confidential
    // application signs in in the other tool's browser flow, which returns to that tool's redirect
    // URL; a confidential one is checked for what would make its token request fail.
    private void CheckRegistration(string target, string id, ExternalClient? registration, string? note, string[]? scopeValues)
    {
        if (registration is null)
        {
            if (note is null)
            {
                WriteObject(new ExternalApplicationTestResult
                {
                    Path = target, AppId = id,
                    Problems = [.. Diagnose(false, null, id, null, true, [], DateTime.Now)],
                });
            }
            else
            {
                WriteError(new ErrorRecord(
                    new InvalidOperationException($"The registration of application {id} could not be read: {note}. Give -AppSecret to request a token instead."),
                    "TestPmExternalApplicationLookupFailed", ErrorCategory.ReadError, id));
            }
            return;
        }

        bool confidential = registration.isConfidential != false;
        string[]? requested = null;
        if (scopeValues is not null && !scopeValues.All(string.IsNullOrWhiteSpace))
        {
            var known = confidential ? ApplicationScopes(registration) : UserScopes(registration);
            requested = [.. ExternalScopeArguments.Expand(scopeValues, known, out var unmatched)];
            if (unmatched.Count > 0)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException($"-Scope {string.Join(", ", unmatched)} matches no {(confidential ? "application" : "user")} scope of '{registration.name}'."),
                    "TestPmExternalApplicationScopeNotMatched", ErrorCategory.InvalidArgument, unmatched));
                return;
            }
        }

        WriteObject(new ExternalApplicationTestResult
        {
            Path = target,
            AppId = registration.id ?? id,
            Name = registration.name,
            IsConfidential = registration.isConfidential,
            RequestedScope = requested,
            Problems = [.. confidential
                ? DiagnoseConfidentialRegistration(registration, requested, DateTime.Now)
                : DiagnoseNonConfidential(registration, requested, RedirectUri)],
        });
    }

    // The token request a script using the application makes.
    private void RequestToken(OrchDriveInfo drive, string target, string id, ExternalClient? registration, string? note, string secret, string[]? scopeValues)
    {
        var registered = ApplicationScopes(registration);
        string[] requested;
        if (scopeValues is null || scopeValues.All(string.IsNullOrWhiteSpace))
        {
            if (registered.Length == 0)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException($"Specify -Scope: the scopes registered on application {id} could not be read ({note ?? "it has no application scope"})."),
                    "TestPmExternalApplicationNoScope", ErrorCategory.InvalidArgument, id));
                return;
            }
            requested = registered;
        }
        else
        {
            // Wildcards expand against the application's own application scopes; when its
            // registration cannot be read, against every scope that can be an application scope.
            IEnumerable<string> known = registered;
            if (registered.Length == 0 && ExternalScopeArguments.HasWildcard(scopeValues))
            {
                try { known = ExternalScopeArguments.Catalog(drive, 1, 2).Select(c => c.name).ToList(); }
                catch (Exception ex) when (ex is not OperationCanceledException) { known = []; }
            }
            requested = [.. ExternalScopeArguments.Expand(scopeValues, known, out var unmatched)];
            if (unmatched.Count > 0)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException($"-Scope {string.Join(", ", unmatched)} matches no application scope{(registration is null ? "" : $" of '{registration.name}'")}."),
                    "TestPmExternalApplicationScopeNotMatched", ErrorCategory.InvalidArgument, unmatched));
                return;
            }
        }
        string scope = string.Join(' ', requested);

        AuthManagerProbe probe;
        try
        {
            var r = drive.OrchAPISession.AuthManager.ProbeClientCredentials(id, secret, scope);
            probe = new(r.StatusCode, r.Error, r.ErrorDescription, r.GrantedScope, r.ExpiresInSeconds, r.AccessToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            WriteError(new ErrorRecord(new OrchException(target, ex), "TestPmExternalApplicationRequestError", ErrorCategory.ConnectionError, target));
            return;
        }

        bool succeeded = probe.StatusCode is >= 200 and < 300;
        var problems = Diagnose(succeeded, probe.Error, id, registration, note is null, requested, DateTime.Now);
        if (note is not null)
        {
            problems.Add($"The application's registration could not be read, so the cause is not narrowed down: {note}");
        }
        if (!succeeded && string.Equals(probe.Error, "invalid_scope", StringComparison.OrdinalIgnoreCase)
            && OrchestratorAuthManager.BuildScopeTooLongAdvice(target, scope.Length, drive.ProductVersion.CachedValue?.version, lengthIsCertain: false) is string tooLong)
        {
            problems.Add(tooLong);
        }

        WriteObject(new ExternalApplicationTestResult
        {
            Path = target,
            AppId = id,
            Name = registration?.name,
            IsConfidential = registration?.isConfidential,
            Succeeded = succeeded,
            Error = succeeded ? null : (probe.Error ?? $"HTTP {probe.StatusCode}"),
            ErrorDescription = probe.ErrorDescription,
            RequestedScope = requested,
            GrantedScope = probe.GrantedScope?.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            ExpiresIn = probe.ExpiresIn,
            AccessToken = probe.AccessToken,
            Problems = [.. problems],
        });
    }

    // What a confidential application's registration shows without a token request: its secrets,
    // and the scopes a client-credentials token can carry. An application with user scopes only is
    // one a user signs in to (a web application), not a fault, as long as it can return somewhere.
    // requestedScope null: none given. Pure.
    internal static List<string> DiagnoseConfidentialRegistration(ExternalClient registration, string[]? requestedScope, DateTime now)
    {
        List<string> problems = [];
        string name = registration.name ?? registration.id ?? "";

        var secrets = registration.secrets ?? [];
        if (secrets.Length == 0)
            problems.Add($"'{name}' has no secret, so it cannot request a token. Add a secret to the application.");
        else if (secrets.All(s => s.expiryTime is DateTime t && t <= now))
            problems.Add($"Every secret of '{name}' has expired. Add a new secret to the application.");
        else if (secrets.Where(s => s.expiryTime is not DateTime t || t > now).All(s => s.expiryTime is DateTime t && t <= now.AddDays(30)))
            problems.Add($"Every valid secret of '{name}' expires within 30 days (the last on {secrets.Max(s => s.expiryTime):yyyy-MM-dd}). Add a new secret before then.");

        var application = ApplicationScopes(registration).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var user = UserScopes(registration).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (application.Count == 0 && user.Count == 0)
            problems.Add($"'{name}' has no scope, so a token for it carries none. Add the scopes the tool needs.");
        else if (application.Count == 0 && string.IsNullOrEmpty(registration.redirectUri))
            problems.Add($"'{name}' has user scopes only and no redirect URL: no sign-in can return to it, and a client-credentials token carries no scope.");

        foreach (var s in (requestedScope ?? []).Where(s => !application.Contains(s)))
        {
            problems.Add(user.Contains(s)
                ? $"'{s}' is registered on '{name}' as a user scope. A token requested with client credentials carries application scopes only; register it as an application scope."
                : $"'{s}' is not registered on '{name}'. Add it as an application scope, or leave it out of the request.");
        }
        return problems;
    }

    private sealed record AuthManagerProbe(int StatusCode, string? Error, string? ErrorDescription, string? GrantedScope, int? ExpiresIn, string? AccessToken);

    internal static string[] UserScopes(ExternalClient? registration) =>
        registration?.resources?
            .SelectMany(r => r.scopes ?? [])
            .Where(s => s.type == UserScopeType && !string.IsNullOrEmpty(s.name))
            .Select(s => s.name!)
            .Distinct()
            .ToArray() ?? [];

    // What a non-confidential application's registration shows without signing in: its sign-in
    // (authorization code with PKCE, in the browser) receives user scopes only, and returns to the
    // registered redirect URL. requestedScope null: none given, so only the registration itself is
    // checked. Pure.
    internal static List<string> DiagnoseNonConfidential(ExternalClient registration, string[]? requestedScope, string? redirectUri)
    {
        List<string> problems = [];
        string name = registration.name ?? registration.id ?? "";
        var user = UserScopes(registration).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (user.Count == 0)
            problems.Add($"'{name}' has no user scope, so a sign-in with it receives no scope. Add the user scopes the tool needs.");
        if (string.IsNullOrEmpty(registration.redirectUri))
            problems.Add($"'{name}' has no redirect URL registered, so a browser sign-in with it has nowhere to return.");

        foreach (var s in (requestedScope ?? []).Where(s => !user.Contains(s)))
            problems.Add($"'{s}' is not registered on '{name}' as a user scope. Add it as a user scope, or leave it out of the request.");

        if (!string.IsNullOrEmpty(redirectUri) && !string.IsNullOrEmpty(registration.redirectUri)
            && !string.Equals(redirectUri.TrimEnd('/'), registration.redirectUri.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
            problems.Add($"Redirect URL {redirectUri} differs from the one registered on '{name}', {registration.redirectUri}; the sign-in would be refused.");

        return problems;
    }

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
            problems.Add($"'{name}' is a non-confidential application. It has no secret and cannot request a token with client credentials; it signs in in the browser. Run Test-PmExternalApplication without -AppSecret to check its registration, or register a confidential application.");
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
    internal static (ExternalClient? registration, string? note) FindRegistration(SessionState? sessionState, OrchDriveInfo drive, string clientId)
    {
        var (list, reader, note) = ReadApplications(sessionState, drive);
        if (list is null) return (null, note);
        var match = list.FirstOrDefault(c => string.Equals(c.id, clientId, StringComparison.OrdinalIgnoreCase));
        if (match is null) return (null, null);
        try
        {
            return (Detail(reader!, match), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (null, $"{reader!.NameColonSeparator} {ex.Message}");
        }
    }

    // The applications -Name matches, by name as the portal shows it, wildcards allowed; each read
    // in full. unmatched: the names without wildcards that match none (a pattern matching none is
    // not an error, as with Get-PmExternalApplication).
    internal static (List<ExternalClient> found, List<string> unmatched, string? note) FindRegistrationsByName(SessionState? sessionState, OrchDriveInfo drive, string[] names)
    {
        var (list, reader, note) = ReadApplications(sessionState, drive);
        if (list is null) return ([], [], note);

        List<ExternalClient> matched = [];
        List<string> unmatched = [];
        foreach (var name in names.Where(n => !string.IsNullOrEmpty(n)))
        {
            var wp = new WildcardPattern(name, WildcardOptions.IgnoreCase);
            var hits = list.Where(c => c.name is not null && wp.IsMatch(c.name)).ToList();
            if (hits.Count == 0 && !WildcardPattern.ContainsWildcardCharacters(name)) unmatched.Add(name);
            matched.AddRange(hits);
        }

        try
        {
            return ([.. matched.DistinctBy(c => c.id).Select(c => Detail(reader!, c))], unmatched, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ([], [], $"{reader!.NameColonSeparator} {ex.Message}");
        }
    }

    // The list carries no scopes or secrets; the per-application read does.
    private static ExternalClient Detail(OrchDriveInfo reader, ExternalClient listed)
        => reader.OrchAPISession.GetPmExternalClient(reader.GetPartitionGlobalId(), listed.id!) ?? listed;

    // The organization's application list, read through the drive itself or another signed-in
    // drive of the same organization. Returns the list and the drive that read it, or a note why
    // none could.
    private static (List<ExternalClient>? list, OrchDriveInfo? reader, string? note) ReadApplications(SessionState? sessionState, OrchDriveInfo drive)
    {
        // The passive OrgCacheKey (null until a drive has signed in), never IsSameOrganization:
        // that one signs a drive in to learn its organization, which started a browser sign-in
        // for every mounted drive of another organization (2026-10-08, first live run).
        var candidates = new List<OrchDriveInfo> { drive };
        string? orgKey = drive.GetOrgCacheKey();
        if (orgKey is not null)
        {
            candidates.AddRange(sessionState.EnumAllOrchDrives()
                .Where(d => d != drive && d.OrchAPISession.AuthManager.IsAuthenticated && d.OrgCacheKey == orgKey));
        }

        string? lastError = null;
        foreach (var d in candidates)
        {
            try
            {
                return (d.PmExternalClients.Get().ToList(), d, null);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = $"{d.NameColonSeparator} {ex.Message}";
            }
        }
        return (null, null, lastError ?? "no signed-in drive of this organization can read external applications");
    }
}
