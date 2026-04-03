using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Open, "OrchJob")] //, SupportsPaging = true)]
public class OpenJobCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(IdCompleter))]
    public Int64[]? Id { get; set; }

    //[Parameter]
    //public SwitchParameter Expanded { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    private class IdCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

            // Exclude Ids that have already been selected via parameters
            var paramId = GetSelfExclusionValues(commandAst, "Id", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            foreach (var (drive, folder) in drivesFolders)
            {
                var dicJobs = drive.Jobs.GetCache(folder);
                if (dicJobs is null) continue;

                foreach (var job in dicJobs.Values.ExcludeByClassValues(j => (j?.Id ?? 0).ToString(), paramId))
                {
                    if (!wp.IsMatch((job.Id ?? 0).ToString()))
                        continue;

                    yield return new CompletionResult(job.Id.ToString(), job.Id.ToString(), CompletionResultType.ParameterValue, job.FormatTooltip());
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path);

        foreach (var (drive, folder) in drivesFolders)
        {
            var dicJobs = drive.Jobs.GetCache(folder);

            foreach (var id in Id!)
            {
                Job? job = null;
                dicJobs?.TryGetValue(id, out job);
                if (job is null || string.IsNullOrEmpty(job.Key))
                {
                    try
                    {
                        job = drive.GetJob(folder, id);
                        if (job is null) { continue; }
                    }
                    catch (Exception ex)
                    {
                        string target = folder.GetPSPath();
                        WriteError(new ErrorRecord(new OrchException(target, ex), "GetJobError", ErrorCategory.InvalidOperation, target));
                    }
                }

                string endPoint = $"{drive.OrchAPISession._base_url}/orchestrator_/jobs(sidepanel:sidepanel/jobs/{job!.Key}/details)?fid={folder!.Id ?? 0}";
                //if (Expanded.IsPresent)
                //{
                //    endPoint += "&isExpanded=true";
                //}
                Process.Start(new ProcessStartInfo(endPoint) { UseShellExecute = true });
            }
        }
    }
}
