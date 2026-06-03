using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Suggests the notification delivery modes for -Mode.
internal class PmNotificationModeCompleter : OrchArgumentCompleter
{
    private static readonly string[] Modes = ["InApp", "Email"];

    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wp = CreateWPFromWordToComplete(wordToComplete);
        foreach (var mode in Modes.Where(m => wp.IsMatch(m)))
        {
            yield return new CompletionResult(mode, mode, CompletionResultType.ParameterValue, mode);
        }
    }
}

// Suggests notification publisher names (Apps, Studio, ...) for -Publisher, read
// live from the notification service for each resolved drive (deduped across drives).
internal class PmNotificationPublisherCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var drives = ResolvePmDrives(fakeBoundParameters);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drives, drive =>
        {
            try
            {
                var pgi = drive.GetPartitionGlobalId();
                if (string.IsNullOrEmpty(pgi)) return Array.Empty<PmNotificationPublisher>();
                return drive.OrchAPISession.GetUserSubscriptions(pgi)?.publishers
                       ?? Array.Empty<PmNotificationPublisher>();
            }
            catch
            {
                return Array.Empty<PmNotificationPublisher>();
            }
        });

        foreach (var result in results)
        {
            var drive = result.Source;
            foreach (var pub in result
                .Where(p => p is not null && wp.IsMatch(p!.displayName ?? p.name ?? ""))
                .ExcludeByWildcards(p => p!.displayName ?? p.name, wpName) // Exclude already-entered items
                .OrderBy(p => p!.displayName ?? p.name))
            {
                string name = pub!.displayName ?? pub.name ?? "";
                string tiphelp = drive.NameColonSeparator + name;
                yield return new CompletionResult(PathTools.EscapePSText(name), name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}
