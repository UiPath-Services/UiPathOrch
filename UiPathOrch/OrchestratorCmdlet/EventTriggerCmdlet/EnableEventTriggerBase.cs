using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableEventTriggerCmdletBase<Enable> : EnableFolderEntityCmdletBase<ApiTrigger, Enable> where Enable : IBoolParameter
{
    protected override string EntityNoun => "EventTrigger";

    protected override IEnumerable<ApiTrigger> GetEntities(OrchDriveInfo drive, Folder folder) => drive.EventTriggers.Get(folder);

    protected override Func<ApiTrigger?, string?> GetName => t => t?.Name;
    protected override Func<ApiTrigger, string> GetPSPath => t => t.GetPSPath();
    protected override Func<ApiTrigger, bool> IsEnabled => t => t.Enabled.GetValueOrDefault();

    protected override void SetEnabled(OrchDriveInfo drive, Folder folder, ApiTrigger entity, bool enabled)
    {
        drive.OrchAPISession.EnableEventTriggers(folder.Id ?? 0, entity.Id!, enabled);
        drive.EventTriggers.ClearCache(folder);
    }

    // This cannot be shared because it only enumerates disabled Event triggers
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

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.EventTriggers.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var trigger in result
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .Where(t => wp.IsMatch(t.Name))
                    .ExcludeByWildcards(t => t?.Name, wpName))
                {
                    string tooltip = trigger.GetPSPath();
                    yield return new CompletionResult(PathTools.EscapePSText(trigger.Name), trigger.Name, CompletionResultType.Text, tooltip);
                }
            }
        }
    }
}
