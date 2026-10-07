using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableTriggerCmdletBase<Enable> : EnableFolderEntityCmdletBase<ProcessSchedule, Enable> where Enable : IBoolParameter
{
    protected override string EntityNoun => "Trigger";

    protected override IEnumerable<ProcessSchedule> GetEntities(OrchDriveInfo drive, Folder folder) => drive.GetTriggers(folder);

    protected override Func<ProcessSchedule?, string?> GetName => t => t?.Name;
    protected override Func<ProcessSchedule, string> GetPSPath => t => t.GetPSPath();
    protected override Func<ProcessSchedule, bool> IsEnabled => t => t.Enabled.GetValueOrDefault();

    protected override void SetEnabled(OrchDriveInfo drive, Folder folder, ProcessSchedule entity, bool enabled)
    {
        drive.OrchAPISession.EnableProcessSchedule(folder.Id ?? 0, [entity.Id ?? 0], enabled);
        drive.Triggers.ClearCache(folder);
        drive.TriggersDetailed.ClearCache(folder);
    }

    internal class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude already-selected Names from candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.GetTriggers(df.folder));

            foreach (var result in results)
            {
                foreach (var trigger in result
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .Where(t => wp.IsMatch(t.Name))
                    .ExcludeByWildcards(t => t?.Name, wpName)
                    .OrderBy(t => t.Name))
                {
                    string tiphelp = TipHelp(trigger);
                    yield return new CompletionResult(PathTools.EscapePSText(trigger.Name), trigger.Name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }
}
