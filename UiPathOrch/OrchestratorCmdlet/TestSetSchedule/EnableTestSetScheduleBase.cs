using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableTestSetScheduleCmdletBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    // This completer cannot be shared because it only shows disabled schedules
    internal class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
            var depth = ResolveDepth(fakeBoundParameters);

            // Extract path from parameters. If not specified, target the current directory
            var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
            var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

            // Exclude Names already selected by parameter from candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.TestSetSchedules.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var entity in result
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .Where(e => wp.IsMatch(e.Name!))
                    .ExcludeByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name))
                {
                    string tiphelp = TipHelp(entity);
                    yield return new CompletionResult(PathTools.EscapePSText(entity.Name), entity.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        string action = $"{(Enable.Value ? "Enable" : "Disable")} TestSetSchedule";
        string errorId = $"{(Enable.Value ? "Enable" : "Disable")}TestSetScheduleError";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            List<Int64> toChange;
            try
            {
                var schedules = drive.TestSetSchedules.Get(folder);

                // Only act on schedules whose current state differs from the target. The fetched
                // objects already carry .Enabled, so this filter is free, matches what the completer
                // offers, and skips a needless SetEnabled call on an already-in-state schedule.
                toChange = [];
                foreach (var schedule in schedules
                    .FilterByWildcards(ts => ts?.Name, wpName)
                    .Where(ts => ts.Enabled.GetValueOrDefault() != Enable.Value)
                    .OrderBy(ts => ts.Name).WithCancellation(cancelHandler.Token))
                {
                    // ShouldProcess stays per schedule (so -Confirm / -WhatIf are per-item); only the
                    // API call is batched below.
                    if (ShouldProcess(schedule.GetPSPath(), action))
                    {
                        toChange.Add(schedule.Id ?? 0);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTestSetScheduleError", ErrorCategory.InvalidOperation, folder));
                continue;
            }

            if (toChange.Count == 0)
            {
                continue;
            }

            // SetEnabled takes a list, so issue ONE batched call per folder instead of one request
            // per schedule.
            try
            {
                drive.OrchAPISession.EnableTestSetSchedules(folder.Id ?? 0, Enable.Value, toChange);
                drive.TestSetSchedules.ClearCache(folder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), errorId, ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
