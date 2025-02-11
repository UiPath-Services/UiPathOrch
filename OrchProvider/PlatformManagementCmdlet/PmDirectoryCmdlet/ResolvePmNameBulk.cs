using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using TPositional = UiPath.PowerShell.Positional.Kind_Name;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsDiagnostic.Resolve, "OrchPmDirectoryNameBulk")]
[OutputType(typeof(DirectoryUser))]
[OutputType(typeof(DirectoryGroup))]
[OutputType(typeof(DirectoryApplication))]
public class SearchPmDirectoryBulkCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<User_Group_Application>))]
    public string? Kind { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    public string[]? Name { get; set; }

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
            if (string.IsNullOrEmpty(wordToComplete))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var kind = GetParameterValue(commandAst, "Kind", TPositional.Parameters);
            string kind2 = kind?.ToLower() switch
            {
                "user" => "DirectoryUser",
                "group" => "DirectoryGroup",
                "application" => "Application",
                _ => null
            };
            if (kind2 is null) yield break;

            var names = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var drives = ResolveDrives(fakeBoundParameters);
            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.ForEach(drives, drive => drive.SearchPmDirectory(wordToComplete));

            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;
                if (entities is null) continue;

                var drive = result.Source;

                foreach (var s in entities
                    .Where(s => !names.Contains(s.identityName)) // 入力済みのものを除く
                    .Where(s => s.objectType == kind2)
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
            drive => drive.PmBulkResolveByName(Kind!, Name!, name => name));

        string viewName = Kind!.ToLower() switch
        {
            "user" => "UiPath.PowerShell.Entities.DirectoryUser",
            "group" => "UiPath.PowerShell.Entities.DirectoryGroup",
            "application" => "UiPath.PowerShell.Entities.DirectoryApplication",
            _ => null
        };

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var directoryObjects = result.GetResult(cancelHandler.Token);
                if (directoryObjects is null) continue;

                foreach (var directoryObject in directoryObjects.Values.Where(v => v is not null).OrderBy(v => v!.name))
                {
                    var psObject = new PSObject(directoryObject);
                    psObject.TypeNames.Clear();
                    psObject.TypeNames.Add(viewName);
                    WriteObject(psObject);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "ResolvePmDirectoryNameBulkError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
