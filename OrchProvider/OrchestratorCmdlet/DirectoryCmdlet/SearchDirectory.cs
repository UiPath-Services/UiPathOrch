using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Search, "OrchDirectory")]
    [OutputType(typeof(DirectoryObject))]
    public class SearchDirectoryCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [ArgumentCompleter(typeof(NameCompleter))]
        public string? Name { get; set; }

        [Parameter]
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
                string name = GetParameterValue(commandAst, parameterName, TPositional.Parameters);
                if (string.IsNullOrEmpty(name))
                {
                    yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                    yield break;
                }

                var drives = ResolveDrives(fakeBoundParameters);
                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.SearchDirectory(name));

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;
                    if (entities == null) continue;

                    var drive = result.Source;

                    foreach (var s in entities
                        .OrderBy(s => s.identityName))
                    {
                        string tiphelp = drive.NameColonSeparator + s.identityName;
                        yield return new CompletionResult(PathTools.EscapePSText(s.identityName), s.identityName, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.SearchDirectory(Name!));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var directoryObjects = result.GetResult(cancelHandler.Token);
                    if (directoryObjects == null) continue;

                    WriteObject(directoryObjects, true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "SearchDirectoryError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
