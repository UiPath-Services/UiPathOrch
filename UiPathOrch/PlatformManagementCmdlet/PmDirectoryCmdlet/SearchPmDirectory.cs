using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Search, "PmDirectory")]
[OutputType(typeof(PmDirectoryEntityInfo))]
public class SearchPmDirectoryCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    public string? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            string name = GetFakeBoundParameter(fakeBoundParameters, parameterName);
            if (string.IsNullOrEmpty(name))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var drives = ResolvePmDrives(fakeBoundParameters);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.SearchPmDirectoryCache.Get(name.ToLower()));

            foreach (var result in results)
            {
                // PmDirectoryEntityInfo is org-shared (no drive-local Path field
                // after Phase 3); take the drivePath from the SourceGroup.
                string drivePath = result.Source.NameColonSeparator;
                foreach (var directoryEntry in result
                    .OrderBy(s => s.identityName))
                {
                    string tiphelp = directoryEntry.GetPSPath(drivePath);
                    yield return new CompletionResult(PathTools.EscapePSText(directoryEntry.identityName), directoryEntry.identityName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        foreach (var drive in drives)
        {
            try
            {
                var entityInfo = drive.SearchPmDirectoryCache.Get(Name!.ToLower());
                if (entityInfo is null) continue;

                // PmDirectoryEntityInfo is org-shared (KeyedSingleCachePerOrganization);
                // attach the drive-local Path as a PSObject note property per emit.
                WriteObject(entityInfo.Select(e => e.WithPath(drive.NameColonSeparator)), true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "SearchPmDirectoryError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
