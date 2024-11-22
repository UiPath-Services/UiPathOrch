using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.Emit;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;

using Positional = UiPath.PowerShell.Positional.Empty;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchUserSession")]
    [OutputType(typeof(Entities.Session))]
    public class GetUserSessionCommand : OrchestratorPSCmdlet
    {
        [Parameter]
        [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<UserSessionStateItems, int>))]
        public string[]? State { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<UserSessionTypeItems, int>))]
        public string[]? Type { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(KeyOfDictionaryCompleter<UserSessionOrderableItems, string>))]
        public string[]? OrderBy { get; set; }

        [Parameter]
        public ulong? Skip { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
        public ulong? First { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Empty>))]
        public string[]? Path { get; set; }

        private string? MakeFilter()
        {
            var filter = new List<string>();

            #region State
            if (State != null && State.Length > 0)
            {
                int[] status = Array.ConvertAll(State, status => UserSessionStateItems.Items[status]);
                IEnumerable<string> f = status.Select(i => $"(State eq '{i}')");
                filter.Add($"({string.Join(" or ", f)})");
            }
            #endregion

            #region Type
            if (Type != null)
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
            if (OrderBy != null && OrderBy.Length > 0)
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

            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            foreach (var drive in drives)
            {
                string query = "&$expand=Robot($expand=License,User),UpdateInfo" + MakeFilter() + MakeOrderBy();
                try
                {
                    var sessions = drive.OrchAPISession.GetGlobalSessions(query, skip, first);
                    foreach (var session in sessions)
                    {
                        session.Path = drive.NameColonSeparator;
                        WriteObject(session);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetUserSessionError", ErrorCategory.InvalidOperation, drive));
                }
            }
        }
    }
}
