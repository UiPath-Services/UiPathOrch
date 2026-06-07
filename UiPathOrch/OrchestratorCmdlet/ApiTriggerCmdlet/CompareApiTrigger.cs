using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Compare API (HTTP) triggers between two folders or Orchestrator instances. Matches by Name
// and compares the HTTP invocation settings and execution config. The signing Secret and the
// auto-generated Slug are not compared. See Compare-OrchAsset for the shared model.
[Cmdlet(VerbsData.Compare, "OrchApiTrigger")]
[OutputType(typeof(OrchComparison))]
public class CompareApiTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string? LiteralPath { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [SupportsWildcards]
    public string? DifferencePath { get; set; }

    [Parameter(Position = 2)]
    [ArgumentCompleter(typeof(ApiTriggerNameCompleter))]
    public string? DifferenceName { get; set; }

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ApiTriggerNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(ComparePropertyCompleter))]
    public string[]? Property { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public SwitchParameter IncludeEqual { get; set; }

    private static readonly (string Name, Func<HttpTrigger, object?> Get)[] Comparators =
    [
        ("Enabled", t => t.Enabled),
        ("Method", t => t.Method),
        ("CallingMode", t => t.CallingMode),
        ("CallbackMode", t => t.CallbackMode),
        ("AllowInsecureSsl", t => t.AllowInsecureSsl),
        ("RunAsCaller", t => t.RunAsCaller),
        ("SuccessCallbackUrl", t => t.SuccessCallbackUrl),
        ("FailureCallbackUrl", t => t.FailureCallbackUrl),
        ("JobPriority", t => t.JobPriority),
        ("RuntimeType", t => t.RuntimeType),
        ("InputArguments", t => t.InputArguments),
        ("Description", t => t.Description),
        ("Tags", t => EntityComparison.NormalizeTags(t.Tags)),
    ];

    internal static readonly HashSet<string> ValidPropertyNames =
        new(Comparators.Select(c => c.Name), StringComparer.OrdinalIgnoreCase);

    protected override IEnumerable<string> GetTargetDriveNames()
    {
        foreach (var n in base.GetTargetDriveNames()) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("DifferencePath", out var dp))
            foreach (var n in ExtractDriveNamesFromBoundPath(dp)) yield return n;
        if (MyInvocation.BoundParameters.TryGetValue("LiteralPath", out var lp))
            foreach (var n in ExtractDriveNamesFromBoundPath(lp)) yield return n;
    }

    protected override void ProcessRecord()
    {
        var only = CompareParameterHelper.ResolvePropertyFilter(this, Property, ValidPropertyNames);

        FolderCompare.Run<HttpTrigger>(
            SessionState,
            EffectivePath(Path, LiteralPath),
            DifferencePath,
            DifferenceName,
            Name.ConvertToWildcardPatternList(),
            Recurse.IsPresent, Depth, IncludeEqual.IsPresent,
            only,
            (drive, folder) => drive.ApiTriggers.Get(folder),
            t => t?.Name,
            t => t!.GetPSPath(),
            Comparators,
            "GetApiTriggerError",
            WriteObject,
            WriteError);
    }
}
