namespace UiPath.PowerShell.Core
{
    public class UiPathOrchConfig
    {
        public List<PSDrive>? PSDrives { get; set; }
    }

    public class PSDrive
    {
        public string? Name { get; set; }
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
    }
}
