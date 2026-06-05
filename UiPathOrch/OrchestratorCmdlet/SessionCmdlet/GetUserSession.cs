using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchUserSession")]
[OutputType(typeof(Entities.Session))]
public class GetUserSessionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<UserSessionStateItems, int>))]
    public string[]? State { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<UserSessionTypeItems, int>))]
    public string[]? Type { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<UserSessionOrderableItems, string>))]
    public string[]? OrderBy { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public ulong? Skip { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
    public ulong? First { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private string? MakeFilter()
    {
        var filter = new List<string>();

        #region State
        if (State is not null && State.Length > 0)
        {
            int[] status = Array.ConvertAll(State, status => UserSessionStateItems.Items[status]);
            IEnumerable<string> f = status.Select(i => $"(State eq '{i}')");
            filter.Add($"({string.Join(" or ", f)})");
        }
        #endregion

        #region Type
        if (Type is not null)
        {
            int[] t = Array.ConvertAll(Type, t => UserSessionTypeItems.Items[t]);
            IEnumerable<string> f = t.Select(i => $"(Robot/Type eq '{i}')");
            filter.Add($"({string.Join(" or ", f)})");
        }
        #endregion

        if (filter.Count != 0)
        {
            string ret = string.Join(" and ", filter);
            ret = "&$filter=(" + ret + ")"; // &$orderby=HostMachineName%20asc";
            return ret;
        }
        return null;
    }

    private string? MakeOrderBy()
    {
        if (OrderBy is not null && OrderBy.Length > 0)
        {
            IEnumerable<string> o1 = OrderBy.Select(o => UserSessionOrderableItems.Items[o]);
            string o2 = string.Join(",", o1);
            return $"&$orderby={o2} asc";
        }
        return null;
    }

    protected override void ProcessRecord()
    {
        ulong skip = Skip ?? 0;
        ulong first = First ?? ulong.MaxValue;
        string query = "&$expand=Robot($expand=License,User),UpdateInfo" + MakeFilter() + MakeOrderBy();

        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));

        using var results = OrchThreadPool.RunForEach(drives,
            drive => drive.NameColonSeparator,
            drive => drive,
            drive => drive.UserSessions.Fetch(query, skip, first));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var sessions = result.GetResult(cancelHandler.Token);
                if (sessions is null) continue;
                WriteObject(sessions, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetUserSessionError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
