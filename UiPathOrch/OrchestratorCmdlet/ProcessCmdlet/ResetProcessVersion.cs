using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Reset, "OrchProcessVersion", SupportsShouldProcess = true)]
public class ResetProcessVersionCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

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

    private class NameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude already-selected package names from candidates
            var wpName = CreateSelfExclusionList(commandAst, "Name", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Releases.Get(df.folder));

            foreach (var result in results)
            {
                foreach (var release in result
                    .Where(p => p.ProcessType != "TestAutomationProcess")
                    .Where(p => p.ReleaseVersions is not null && p.ReleaseVersions.Length >= 2)
                    .Where(p => wp.IsMatch(p.Name))
                    .ExcludeByWildcards(p => p?.Name, wpName)
                    .OrderBy(p => p.Name))
                {
                    string tiphelp = TipHelp(release);
                    yield return new CompletionResult(PathTools.EscapePSText(release.Name), release.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var releases = drive.Releases.Get(folder);

                foreach (var release in releases
                    .Where(e => e.ProcessType != "TestAutomationProcess")
                    .FilterByWildcards(e => e?.Name, wpName)
                    .OrderBy(e => e.Name))
                {
                    if (release.ReleaseVersions is null || release.ReleaseVersions.Length <= 1)
                    {
                        continue;
                    }

                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = release.GetPSPath();
                    if (ShouldProcess(target, "Reset ProcessVersion"))
                    {
                        try
                        {
                            drive.OrchAPISession.RollbackReleaseVersion(folder.Id!.Value, release.Id!.Value);
                            drive.Releases.ClearCache(folder);
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(new OrchException(target, ex), "ResetProcessVersionError", ErrorCategory.InvalidOperation, target);
                            WriteError(errorRecord);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetProcessError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
