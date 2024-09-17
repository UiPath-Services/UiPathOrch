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
        static readonly Dictionary<string, int> stateMapping = new()
        {
            { "Available", 0 },
            { "Busy", 1 },
            { "Disconnected", 2 } // Unresponsible on OC web interface
        };

        static readonly Dictionary<string, int> typeMapping = new()
        {
            { "Attended (Attended User)", 1 },
            { "Attended (Citizen Developer)", 4 },
            { "Attended (RPA Developer)", 3 },
            { "Attended (Automation Developer)", 6 },
            { "Production (Unattended)", 2 },
            { "Cloud - VM", 8 }
        };

        static readonly Dictionary<string, string> orderableMapping = new()
        {
            { "User",     "Robot/User/UserName" },
            { "Domain",   "Robot/Username" },
            { "Hostname", "HostMachineName" },
            { "Type",     "Robot/Type" },
            { "Version",  "Version" }
        };

        [Parameter]
        [ArgumentCompleter(typeof(StateCompleter))]
        public string[]? State { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(TypeCompleter))]
        public string[]? Type { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(OrderByCompleter))]
        public string[]? OrderBy { get; set; }

        [Parameter]
        public ulong? Skip { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
        public ulong? First { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Empty>))]
        public string[]? Path { get; set; }

        // TODO: StaticTextCompleter で書き直す
        private class StateCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName, 
                string parameterName, 
                string wordToComplete, 
                CommandAst commandAst, 
                System.Collections.IDictionary 
                fakeBoundParameters)
            {
                // パラメータで選択済みの State は、候補から除外する
                var paramState = GetParameterValues(commandAst, "State", null, wordToComplete);
                var wpState = paramState.ConvertToWildcardPatternList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var candidate in stateMapping
                    .Select(c => c.Key)
                    .Where(c => wp.IsMatch(c))
                    .ExcludeByWildcards(c => c, wpState))
                {
                    yield return new CompletionResult(candidate);
                }
            }
        }

        // TODO: StaticTextCompleter で書き直す
        private class TypeCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                System.Collections.IDictionary
                fakeBoundParameters)
            {
                // パラメータで選択済みの Type は、候補から除外する
                var paramType = GetParameterValues(commandAst, "Type", null, wordToComplete);
                var wpType = paramType.ConvertToWildcardPatternList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var candidate in typeMapping
                    .Select(c => c.Key)
                    .Where(c => wp.IsMatch(c))
                    .ExcludeByWildcards(c => c, wpType))
                {
                    yield return new CompletionResult(PathTools.EscapePSText(candidate));
                }
            }
        }

        // TODO: StaticTextCompleter で書き直す
        private class OrderByCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                System.Collections.IDictionary
                fakeBoundParameters)
            {
                // パラメータで選択済みの Type は、候補から除外する
                var paramOrderBy = GetParameterValues(commandAst, "OrderBy", null, wordToComplete);
                var wpOrderBy = paramOrderBy.ConvertToWildcardPatternList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var candidate in orderableMapping
                    .Select(c => c.Key)
                    .Where(c => wp.IsMatch(c))
                    .ExcludeByWildcards(c => c, wpOrderBy))
                {
                    yield return new CompletionResult(PathTools.EscapePSText(candidate));
                }
            }
        }

        private string? MakeFilter()
        {
            var filter = new List<string>();

            #region State
            if (State != null && State.Length > 0)
            {
                int[] status = Array.ConvertAll(State, status => stateMapping[status]);
                IEnumerable<string> f = status.Select(i => $"(State eq '{i}')");
                filter.Add($"({string.Join(" or ", f)})");
            }
            #endregion

            #region Type
            if (Type != null)
            {
                int[] t = Array.ConvertAll(Type, t => typeMapping[t]);
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
                IEnumerable<string> o1 = OrderBy.Select(o => orderableMapping[o]);
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
