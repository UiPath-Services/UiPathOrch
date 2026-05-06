using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Creates a new UiPathOrch PSDrive without using the configuration file.
/// </summary>
[Cmdlet(VerbsCommon.New, "OrchPSDrive", SupportsShouldProcess = true)]
[OutputType(typeof(OrchDriveInfo))]
public class NewPSDriveCommand : PSCmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; } = null!;

    [Parameter(Mandatory = true, Position = 1)]
    public string Root { get; set; } = null!;

    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Forces the deployment edition. Optional — when omitted, UiPathOrch infers it
    /// from <c>Root</c> (uipath.com host → Cloud, two-segment path → AutomationSuite,
    /// otherwise OnPremises). Pin it explicitly when the heuristic guesses wrong.
    /// </summary>
    [Parameter]
    [ArgumentCompleter(typeof(StaticTextsCompleter<OrchEditionItems>))]
    public string? Edition { get; set; }

    [Parameter(ParameterSetName = "AppAuth")]
    public string? IdentityUrl { get; set; }

    [Parameter(ParameterSetName = "AppAuth")]
    public string? AppId { get; set; }

    [Parameter(ParameterSetName = "AppAuth")]
    public string? AppSecret { get; set; }

    [Parameter(ParameterSetName = "AppAuth")]
    public string? RedirectUrl { get; set; }

    [Parameter(ParameterSetName = "AppAuth")]
    public string? HttpListener { get; set; }

    [Parameter(ParameterSetName = "AppAuth")]
    [Parameter(ParameterSetName = "TokenAuth")]
    public string? OAuthScope { get; set; }

    [Parameter(ParameterSetName = "TokenAuth")]
    public string? AccessToken { get; set; }

    [Parameter(ParameterSetName = "UserAuth")]
    public string? Username { get; set; }

    [Parameter(ParameterSetName = "UserAuth")]
    public string? Password { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(StaticTextsCompleter<True_False>))]
    public bool? IgnoreSslErrors { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess($"{Name}:{System.IO.Path.DirectorySeparatorChar}", "New-OrchPSDrive"))
        {
            return;
        }

        PSDrive psDrive = new()
        {
            Name = Name,
            Root = Root,
            Description = Description,
            Edition = Edition,
            IdentityUrl = IdentityUrl,
            AppId = AppId,
            AppSecret = AppSecret,
            RedirectUrl = RedirectUrl,
            HttpListener = HttpListener,
            Scope = OAuthScope,
            AccessToken = AccessToken,
            Username = Username,
            Password = Password,
            IgnoreSslErrors = IgnoreSslErrors,
            Enabled = true
        };

        ProviderInfo orchProvider;
        try
        {
            orchProvider = SessionState.Provider.GetOne("UiPathOrch");
        }
        catch
        {
            WriteError(new ErrorRecord(
                new System.InvalidOperationException("UiPathOrch provider is not loaded."),
                "ProviderNotFound", ErrorCategory.ObjectNotFound, "UiPathOrch"));
            return;
        }

        try
        {
            var orchDrive = new OrchDriveInfo(orchProvider, psDrive);
            SessionState.Drive.New(orchDrive, scope: "Global");
            WriteObject(orchDrive);
        }
        catch (System.Exception ex)
        {
            WriteError(new ErrorRecord(new OrchException(Name, ex),
                "NewPSDriveError", ErrorCategory.InvalidData, Name));
        }
    }
}
