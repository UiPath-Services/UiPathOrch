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
    public SwitchParameter OrderDescending { get; set; }

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
            int[] status = Array.ConvertAll(State, status => UserSessionStateItems.Items.ResolveKeyOrThrow(status, nameof(State)));
            IEnumerable<string> f = status.Select(i => $"(State eq '{i}')");
            filter.Add($"({string.Join(" or ", f)})");
        }
        #endregion

        #region Type
        if (Type is not null && Type.Length > 0)
        {
            int[] t = Array.ConvertAll(Type, t => UserSessionTypeItems.Items.ResolveKeyOrThrow(t, nameof(Type)));
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

    // internal (not private) so the -OrderDescending wiring is unit-testable
    // end to end: parameter -> MakeOrderBy -> BuildSessionOrderByClause.
    internal string? MakeOrderBy() => BuildSessionOrderByClause(OrderBy, OrderDescending.IsPresent);

    // OData $orderby for the session list. Pure/static so the asc-vs-desc and
    // multi-field formatting is unit-testable. The direction is applied per
    // field -- OData "A,B desc" leaves A at its default, so each field carries
    // its own direction. Default is ascending -- the natural order for the
    // categorical session fields (User / Domain / Hostname / Type / Version);
    // -OrderDescending flips it. Throws (via ResolveKeyOrThrow) on an OrderBy
    // value outside the lookup.
    internal static string? BuildSessionOrderByClause(string[]? orderBy, bool descending)
    {
        if (orderBy is null || orderBy.Length == 0)
            return null;

        string dir = descending ? "desc" : "asc";
        IEnumerable<string> fields = orderBy.Select(
            o => $"{UserSessionOrderableItems.Items.ResolveKeyOrThrow(o, "OrderBy")} {dir}");
        return $"&$orderby={string.Join(",", fields)}";
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
