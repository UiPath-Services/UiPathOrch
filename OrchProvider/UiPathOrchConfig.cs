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
        set
        {
            _root = value;
            _isCloud = _root?.Contains("uipath.com", StringComparison.InvariantCultureIgnoreCase) ?? false;
        }
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

    private bool? _isCloud;
    internal bool IsCloud => _isCloud.GetValueOrDefault();

    internal static string? ShortenScope(string? scope)
    {
        if (scope is null) return null;

        return string.Join(" ", Regex.Split(scope.Trim(), "\\s+")
            .Distinct()
            .Select(p => p.Split('.'))
            .GroupBy(parts => string.Join(".", parts.Take(2)))
            .Select(g => g.Any(p => p.Length == 2) || (g.Any(p => p.Length > 2 && p[2] == "Read") && g.Any(p => p.Length > 2 && p[2] == "Write"))
                ? g.Key
                : string.Join(" ", g.Where(p => !g.Any(q => q.Length == 2)).Select(p => string.Join(".", p))))
            .Order());
    }

    internal void CascadePSDriveFromGlobalSettings(UiPathOrchConfig? globalSettings)
    {
        Name ??= globalSettings?.Name;
        Description ??= globalSettings?.Description;
        Root ??= globalSettings?.Root;
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

        // I'd like to auto-generate the IdentityUrl here, but it got confusing...
        //if (string.IsNullOrEmpty(IdentityUrl))
        //{
        //    IdentityUrl = IsCloud ?
        //        _baseUrl + "/identity_/connect/token" :
        //        _baseUrl + "/identity/connect/token";
        //}

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
