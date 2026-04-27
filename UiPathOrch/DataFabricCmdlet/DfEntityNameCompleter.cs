using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Completer;

// Tab-completes Data Fabric entity names for -Name (Get-OrchDfEntity) and -Entity
// (Get-OrchDfRecord / Invoke-OrchDfQuery). Reads from the per-folder cache so repeated
// tab presses during typing do not re-hit the /api/Entity endpoint.
public class DfEntityNameCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters, includeRoot: true);
        var exclude = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (drive, folder) in drivesFolders)
        {
            IEnumerable<UiPath.PowerShell.Entities.DfEntity>? entities;
            try { entities = drive.DfEntities.Get(folder); }
            catch { continue; }

            if (entities is null) continue;

            foreach (var entity in entities
                .Where(e => e?.name is not null && wp.IsMatch(e.name))
                .ExcludeByWildcards(e => e?.name, exclude)
                .OrderBy(e => e!.name))
            {
                if (!seen.Add(entity!.name!)) continue;

                string tooltip = string.IsNullOrEmpty(entity.displayName)
                    ? $"{folder.GetPSPath()}  {entity.name}"
                    : $"{folder.GetPSPath()}  {entity.name} ({entity.displayName})";
                yield return new CompletionResult(
                    PathTools.EscapePSText(entity.name!),
                    entity.name!,
                    CompletionResultType.ParameterValue,
                    tooltip);
            }
        }
    }
}
