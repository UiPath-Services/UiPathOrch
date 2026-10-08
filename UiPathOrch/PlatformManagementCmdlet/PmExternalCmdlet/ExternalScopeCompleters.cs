using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Scope arguments of New-PmExternalApplication and Test-PmExternalApplication: several values
// (comma-separated), each of which may also hold several scopes separated by spaces, as an OAuth
// scope value and the configuration file's Scope do, and may be a wildcard pattern.
internal static class ExternalScopeArguments
{
    // The scopes the values name: a plain name as given (the server judges it), a wildcard pattern
    // expanded against known. Patterns that match nothing go to unmatched. Pure, so unit-testable.
    internal static List<string> Expand(IEnumerable<string?>? values, IEnumerable<string> known, out List<string> unmatched)
    {
        List<string> result = [];
        unmatched = [];
        var knownList = known.Where(k => !string.IsNullOrEmpty(k)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var token in (values ?? []).SelectMany(v => (v ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)))
        {
            if (WildcardPattern.ContainsWildcardCharacters(token))
            {
                var wp = new WildcardPattern(token, WildcardOptions.IgnoreCase);
                var matched = knownList.Where(k => wp.IsMatch(k)).ToList();
                if (matched.Count == 0) unmatched.Add(token);
                result.AddRange(matched);
            }
            else
            {
                result.Add(token);
            }
        }
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    internal static bool HasWildcard(IEnumerable<string?>? values) =>
        (values ?? []).Any(v => WildcardPattern.ContainsWildcardCharacters(v ?? ""));

    // The organization's scope catalog (GET /api/ExternalApiResource). Its type differs from a
    // registration's: 0 user only, 1 application only, 2 either (yotsuda org, 2026-10-08: AI Center
    // and IXP scopes are 1, every Orchestrator scope 2).
    internal static IEnumerable<(string name, string resource)> Catalog(OrchDriveInfo drive, params int[] types) =>
        drive.PmExternalApiResources.Get()
            .SelectMany(r => (r.scopes ?? [])
                .Where(s => s.type is int t && types.Contains(t) && !string.IsNullOrEmpty(s.name))
                .Select(s => (s.name!, r.name ?? "")));
}

// Completion of external-application scopes, shared by New-PmExternalApplication
// (-ApplicationScope, -UserScope) and Test-PmExternalApplication (-Scope). A value may hold several
// scopes separated by spaces, as an OAuth scope does: the last word is completed, and the scopes
// already in the value, or in the parameter's other values, are left out. A subclass supplies the
// candidates.
internal abstract class ExternalScopeCompleter : OrchArgumentCompleter
{
    protected abstract IEnumerable<(string name, string tip)> GetCandidates(OrchDriveInfo drive, IDictionary fakeBoundParameters);

    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName, string parameterName, string wordToComplete,
        CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        var drive = ResolvePmDrives(fakeBoundParameters).FirstOrDefault();
        if (drive is null) yield break;

        var (typed, partial) = SplitScopeWord(wordToComplete);
        var exclude = typed
            .Concat(GetSelfExclusionValues(commandAst, parameterName, wordToComplete).SelectMany(v => SplitScopeWord(v + " ").typed))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<(string name, string tip)> candidates;
        try
        {
            candidates = GetCandidates(drive, fakeBoundParameters).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            yield break;
        }

        var wp = CreateWPFromWordToComplete(partial);
        foreach (var (name, tip) in candidates
            .Where(c => !string.IsNullOrEmpty(c.name) && wp.IsMatch(c.name) && !exclude.Contains(c.name))
            .DistinctBy(c => c.name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c.name, StringComparer.OrdinalIgnoreCase))
        {
            if (typed.Length == 0)
            {
                yield return new CompletionResult(name, name, CompletionResultType.ParameterValue, tip);
            }
            else
            {
                string value = string.Join(' ', typed.Append(name));
                yield return new CompletionResult($"'{value.Replace("'", "''")}'", name, CompletionResultType.ParameterValue, tip);
            }
        }
    }

    // "'OR.Jobs OR.As" -> (["OR.Jobs"], "OR.As"); a trailing space leaves the partial empty.
    // Pure, so the splitting is unit-testable.
    internal static (string[] typed, string partial) SplitScopeWord(string? wordToComplete)
    {
        string w = (wordToComplete ?? "").Trim('\'', '"');
        var parts = w.Split(' ');
        string partial = parts[^1];
        string[] typed = parts[..^1].Where(p => p.Length > 0).ToArray();
        return (typed, partial);
    }

    protected static IEnumerable<(string name, string tip)> CatalogScopes(OrchDriveInfo drive, params int[] types) =>
        ExternalScopeArguments.Catalog(drive, types);
}

// New-PmExternalApplication -ApplicationScope
internal class ApplicationScopeCompleter : ExternalScopeCompleter
{
    protected override IEnumerable<(string name, string tip)> GetCandidates(OrchDriveInfo drive, IDictionary fakeBoundParameters)
        => CatalogScopes(drive, 1, 2);
}

// New-PmExternalApplication -UserScope
internal class UserScopeCompleter : ExternalScopeCompleter
{
    protected override IEnumerable<(string name, string tip)> GetCandidates(OrchDriveInfo drive, IDictionary fakeBoundParameters)
        => CatalogScopes(drive, 0, 2);
}

// Test-PmExternalApplication -Scope: the application scopes registered on the application being
// tested (-AppId, or the drive's own one), else every scope that can be an application scope.
internal class TestApplicationScopeCompleter : ExternalScopeCompleter
{
    protected override IEnumerable<(string name, string tip)> GetCandidates(OrchDriveInfo drive, IDictionary fakeBoundParameters)
    {
        // As the cmdlet: -AppId, else -Name, else the drive's own application.
        string? appId = new[] { "AppId", "ClientId", "id" }
            .SelectMany(n => GetFakeBoundParameters(fakeBoundParameters, n))
            .FirstOrDefault(v => !string.IsNullOrEmpty(v));
        string? name = GetFakeBoundParameters(fakeBoundParameters, "Name").FirstOrDefault(v => !string.IsNullOrEmpty(v));

        if (!string.IsNullOrEmpty(appId) || !string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(drive._psDrive.AppId))
        {
            // A -Name pattern names one application here only when it matches exactly one.
            ExternalClient? registration = !string.IsNullOrEmpty(appId)
                ? TestPmExternalApplicationCmdlet.FindRegistration(SessionState, drive, appId).registration
                : !string.IsNullOrEmpty(name)
                    ? TestPmExternalApplicationCmdlet.FindRegistrationsByName(SessionState, drive, [name]).found is { Count: 1 } one ? one[0] : null
                    : TestPmExternalApplicationCmdlet.FindRegistration(SessionState, drive, drive._psDrive.AppId!).registration;
            // A non-confidential application is checked against its user scopes.
            bool user = registration?.isConfidential == false;
            int type = user ? TestPmExternalApplicationCmdlet.UserScopeType : TestPmExternalApplicationCmdlet.ApplicationScopeType;
            var registered = registration?.resources?
                .SelectMany(r => (r.scopes ?? [])
                    .Where(s => s.type == type)
                    .Select(s => (s.name!, $"{r.name}: {(user ? "user" : "application")} scope of '{registration.name}'")))
                .ToList();
            if (registered is { Count: > 0 }) return registered;
        }
        return CatalogScopes(drive, 1, 2);
    }
}
