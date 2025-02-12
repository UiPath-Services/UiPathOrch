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
    public string? Url { get; set; }
    public bool? BypassProxyOnLocal { get; set; }
    public bool? UseDefaultCredentials { get; set; }
    public Credentials? Credentials { get; set; }
    public bool? Enabled { get; set; }
}

public class PSDrive
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Root { get; set; } // これ、BaseUrl と TenancyName に分けて指定できるようにしたいが、
    //public string? BaseUrl { get; set; }
    //public string? TenancyName { get; set; }
    public string? IdentityUrl { get; set; }
    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string? RedirectUrl { get; set; }
    public string? HttpListener { get; set; }
    private string? _scope;
    public string? Scope
    {
        get => _scope;
        set => _scope = value is not null ? ShortenScope(value) : null;
    }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool? Enabled { get; set; }
    public ProxySettings? Proxy { get; set; }
    public bool? IgnoreSslErrors { get; set; }

    internal static string ShortenScope(string scope)
    {
        return string.Join(" ", Regex.Split(scope.Trim(), "\\s+")
            .Distinct()
            .Select(p => p.Split('.'))
            .GroupBy(parts => string.Join(".", parts.Take(2)))
            .Select(g => g.Any(p => p.Length == 2) || (g.Any(p => p.Length > 2 && p[2] == "Read") && g.Any(p => p.Length > 2 && p[2] == "Write"))
                ? g.Key
                : string.Join(" ", g.Where(p => !g.Any(q => q.Length == 2)).Select(p => string.Join(".", p))))
            .Order());
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
