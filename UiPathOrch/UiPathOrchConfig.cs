using System.Text.RegularExpressions;

namespace UiPath.PowerShell.Core;

public class UiPathOrchConfig : PSDrive
{
    public List<PSDrive>? PSDrives { get; set; }
}

public class Credentials
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class ProxySettings
{
    public bool? UseDefaultWebProxy { get; set; }
    public string? Url { get; set; }
    public bool? BypassProxyOnLocal { get; set; }
    public bool? UseDefaultCredentials { get; set; }
    public Credentials? Credentials { get; set; }
    public bool? Enabled { get; set; }
}

public enum LoggingLevel
{
    Error,
    Info,
    Trace,
    Verbose
}

public enum OrchEdition
{
    // cloud.uipath.com – tenant lives in the URL path: /{org}/{tenant}/orchestrator_/...
    Cloud,
    // Self-hosted Automation Suite – same URL shape as Cloud but on a customer domain.
    AutomationSuite,
    // MSI / Standalone Orchestrator – tenant is sent via the X-UIPATH-TenantName header,
    // not embedded in the URL path.
    OnPremises,
}

public class LoggingSettings
{
    public bool? Enabled { get; set; }

    private LoggingLevel? _level;
    public string? Level
    {
        get => _level?.ToString();
        set => _level = value?.Trim().ToLower() switch
        {
            null
                => null,
            "error"
                => LoggingLevel.Error,
            "info" or "information"
                => LoggingLevel.Info,
            "trace"
                => LoggingLevel.Trace,
            "verbose"
                => LoggingLevel.Verbose,
            _ => throw new ArgumentException("Invalid logging level. Valid values are: Error, Info, Trace, Verbose.")
        };
    }
    public LoggingLevel? InternalLogLevel => _level;
}

public class PSDrive
{
    public string? Name { get; set; }
    public string? Description { get; set; }

    // I'd like to allow specifying this separately as BaseUrl and TenancyName, but...
    private string? _root;
    public string? Root
    {
        get => _root;
        set => _root = value;
    }

    private OrchEdition? _edition;
    public string? Edition
    {
        get => _edition?.ToString();
        set => _edition = value?.Trim().ToLower() switch
        {
            null or "" => null,
            "cloud" or "automationcloud" => OrchEdition.Cloud,
            "automationsuite" or "as" => OrchEdition.AutomationSuite,
            "onpremises" or "onpremise" or "onprem" or "msi" or "standalone" => OrchEdition.OnPremises,
            _ => throw new ArgumentException(
                "Invalid Edition. Valid values are: Cloud, AutomationSuite, OnPremises.")
        };
    }

    //public string? BaseUrl { get; set; }
    //public string? TenancyName { get; set; }

    private string? _identityUrl;
    public string? IdentityUrl
    {
        get => _identityUrl;
        set => _identityUrl = value?.TrimEnd('/');
    }

    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string? AccessToken { get; set; }

    private string? _redirectUrl;
    public string? RedirectUrl
    {
        get => _redirectUrl;
        set => _redirectUrl = value?.TrimEnd('/');
    }

    public string? HttpListener { get; set; }

    private string? _scope;
    public string? Scope
    {
        get => _scope;
        set => _scope = ShortenScope(value);
    }

    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool? Enabled { get; set; }
    public ProxySettings? Proxy { get; set; }
    public bool? IgnoreSslErrors { get; set; }

    public LoggingSettings? Logging { get; set; }

    // Resolved edition: explicit Edition wins; otherwise inferred from Root.
    //   *.uipath.com host        → Cloud
    //   2-segment URL path       → AutomationSuite (e.g. https://host/{org}/{tenant})
    //   anything else            → OnPremises
    // The inferred value is cached back into _edition on first read so subsequent
    // accesses (and the Edition getter) return the same resolved value without
    // recomputing. The config file on disk is not modified.
    // The 2-segment heuristic can theoretically false-positive for an on-prem deployment
    // sitting behind a multi-level reverse proxy. Such users should set Edition explicitly.
    internal OrchEdition ResolvedEdition
    {
        get
        {
            if (_edition.HasValue) return _edition.Value;

            OrchEdition inferred = OrchEdition.OnPremises;
            if (!string.IsNullOrWhiteSpace(_root)
                && Uri.TryCreate(_root.TrimEnd('/'), UriKind.Absolute, out var uri))
            {
                if (uri.Host.Contains("uipath.com", StringComparison.OrdinalIgnoreCase))
                {
                    inferred = OrchEdition.Cloud;
                }
                else
                {
                    var segments = uri.AbsolutePath.TrimEnd('/')
                        .Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length == 2)
                        inferred = OrchEdition.AutomationSuite;
                }
            }

            _edition = inferred;
            return inferred;
        }
    }

    // Both Cloud and AS keep the tenant in the URL path and use /identity_, /portal_,
    // /orchestrator_ suffixes. Existing call sites that branch on IsCloud were really
    // asking "is this a Cloud-shaped URL?", so AS belongs on the IsCloud=true side.
    internal bool IsCloud => ResolvedEdition is OrchEdition.Cloud or OrchEdition.AutomationSuite;

    // Top-level scope prefixes whose parent 2-part form is not a valid Identity scope,
    // so collapsing Read+Write (or an explicit 2-part input) would yield an audience-invalid token.
    // DataFabric exposes only DataFabric.Schema.Read / DataFabric.Data.Read / DataFabric.Data.Write;
    // DataFabric.Data and DataFabric.Schema are not accepted as scopes on their own.
    private static readonly HashSet<string> _nonCollapsibleTopLevelPrefixes = new(StringComparer.Ordinal)
    {
        "DataFabric",
    };

    internal static string? ShortenScope(string? scope)
    {
        if (scope is null) return null;

        return string.Join(" ", Regex.Split(scope.Trim(), "\\s+")
            .Distinct()
            .Select(p => p.Split('.'))
            .GroupBy(parts => string.Join(".", parts.Take(2)))
            .Select(g =>
            {
                if (_nonCollapsibleTopLevelPrefixes.Contains(g.First()[0]))
                {
                    return string.Join(" ", g.Select(p => string.Join(".", p)));
                }

                bool canCollapse = g.Any(p => p.Length == 2)
                    || (g.Any(p => p.Length > 2 && p[2] == "Read") && g.Any(p => p.Length > 2 && p[2] == "Write"));

                return canCollapse
                    ? g.Key
                    : string.Join(" ", g.Where(p => !g.Any(q => q.Length == 2)).Select(p => string.Join(".", p)));
            })
            .Order());
    }

    internal void CascadePSDriveFromGlobalSettings(UiPathOrchConfig? globalSettings)
    {
        Name ??= globalSettings?.Name;
        Description ??= globalSettings?.Description;
        Root ??= globalSettings?.Root;
        Edition ??= globalSettings?.Edition;
        IdentityUrl ??= globalSettings?.IdentityUrl;
        AppId ??= globalSettings?.AppId;
        AppSecret ??= globalSettings?.AppSecret;
        AccessToken ??= globalSettings?.AccessToken;
        RedirectUrl ??= globalSettings?.RedirectUrl;
        HttpListener ??= globalSettings?.HttpListener;
        Scope ??= globalSettings?.Scope;
        Username ??= globalSettings?.Username;
        Password ??= globalSettings?.Password;
        IgnoreSslErrors ??= globalSettings?.IgnoreSslErrors;
        Enabled ??= globalSettings?.Enabled;

        // Auto-generate the HttpListener prefix
        if (string.IsNullOrEmpty(HttpListener) && !string.IsNullOrEmpty(RedirectUrl))
        {
            // If HttpListener is not in the config file, auto-generate it from RedirectUrl
            Uri redirectUri = new(RedirectUrl!);
            HttpListener = $"{redirectUri.Scheme}://{redirectUri.Host}:{redirectUri.Port}{redirectUri.AbsolutePath}/";
        }

        // Auto-generate IdentityUrl from Root if not explicitly set
        // Cloud:   "https://cloud.uipath.com/{org}/{tenant}" → strip tenant → /{org}/identity_
        // On-prem: "https://server/{tenant}"                 → strip tenant → /identity
        // On-prem: "https://server"                          → no tenant    → /identity
        if (string.IsNullOrEmpty(IdentityUrl) && !string.IsNullOrEmpty(Root)
            && Uri.TryCreate(Root.TrimEnd('/'), UriKind.Absolute, out var uri))
        {
            // Strip the last path segment (tenant) to get the base URL
            var segments = uri.AbsolutePath.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            string basePath = segments.Length > 1
                ? "/" + string.Join("/", segments.Take(segments.Length - 1))
                : "";
            IdentityUrl = $"{uri.Scheme}://{uri.Authority}{basePath}" + (IsCloud ? "/identity_" : "/identity");
        }

        if (Proxy == null)
        {
            Proxy = globalSettings?.Proxy;
        }
        else
        {
            Proxy.Url ??= globalSettings?.Proxy?.Url;
            Proxy.BypassProxyOnLocal ??= globalSettings?.Proxy?.BypassProxyOnLocal;
            Proxy.UseDefaultCredentials ??= globalSettings?.Proxy?.UseDefaultCredentials;
            Proxy.Credentials ??= new();
            Proxy.Credentials.Username ??= globalSettings?.Proxy?.Credentials?.Username;
            Proxy.Credentials.Password ??= globalSettings?.Proxy?.Credentials?.Password;
            Proxy.Enabled ??= globalSettings?.Proxy?.Enabled;
        }

        if (Logging == null)
        {
            Logging = globalSettings?.Logging;
        }
        else
        {
            Logging.Enabled ??= globalSettings?.Logging?.Enabled;
            Logging.Level ??= globalSettings?.Logging?.Level;
        }
    }
}

public class ExternalApplication
{
    public string? Description { get; set; }
    public string? Root { get; set; }
    public string? IdentityUrl { get; set; }
    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string? RedirectUrl { get; set; }
    public string? HttpListener { get; set; }
    public string? Scope { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool? Enabled { get; set; }
    public ProxySettings? Proxy { get; set; }
}
