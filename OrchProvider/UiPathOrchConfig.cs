namespace UiPath.PowerShell.Core
{
    public class UiPathOrchConfig
    {
        //public string? BaseUrl { get; set; }
        public string? Description { get; set; }
        public string? RedirectUrl { get; set; }
        public string? HttpListener { get; set; }
        public string? Scope { get; set; }
        public bool? Enabled { get; set; }
        public ProxySettings? Proxy { get; set; }
        public List<PSDrive>? PSDrives { get; set; }
        //public List<ExternalApplication>? ExternalApplications{ get; set; }
        public bool? IgnoreSslErrors { get; set; }
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
        public string? Root { get; set; } // deprecated
        public string? BaseUrl { get; set; }
        public string? TenancyName { get; set; }
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
        public bool? IgnoreSslErrors { get; set; }
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
}
