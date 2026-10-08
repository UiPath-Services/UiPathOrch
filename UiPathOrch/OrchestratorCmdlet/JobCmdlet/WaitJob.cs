using System.Diagnostics;
using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Job = UiPath.PowerShell.Entities.Job;

namespace UiPath.PowerShell.Commands;

// Waits for jobs to reach a final state and writes each, re-read, as it gets there. Shared by
// Wait-OrchJob and Start-OrchJob -Wait, so "Start-OrchJob -Wait" and "Start-OrchJob |
// Wait-OrchJob" give the same output.
internal static class JobWaiter
{
    // Suspended counts as final, as it does for Wait-Job: a job waiting on a person (an
    // Action Center task) would otherwise hold the caller for as long as nobody acts.
    internal static readonly string[] FinalStates = ["Successful", "Faulted", "Stopped", "Suspended"];

    internal static bool IsFinal(string? state) => state is not null && FinalStates.Contains(state);

    // One request per folder per round, whatever the number of jobs (see GetJobsByIds). Not
    // shorter: #jp-help-infra recorded SQL deadlocks traced to scripts polling Orchestrator.
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    // timeoutSeconds null or negative: no limit. A job still not ended at the limit is written
    // as an OperationTimeout error carrying the job as last read; the job itself is left running.
    internal static void Wait(OrchestratorPSCmdlet cmdlet, IEnumerable<(OrchDriveInfo drive, Folder folder, Int64 id)> targets, int? timeoutSeconds, CancellationToken token)
    {
        // folder -> the ids still waited for there, and the job as last read
        var pending = targets
            .GroupBy(t => (t.drive, t.folder))
            .ToDictionary(g => g.Key, g => g.Select(t => t.id).Distinct().ToDictionary(id => id, _ => (Job?)null));
        int total = pending.Values.Sum(p => p.Count);
        if (total == 0) return;

        var stopwatch = Stopwatch.StartNew();
        HashSet<OrchDriveInfo> refusesIn = [];
        int done = 0;

        using ProgressReporter reporter = new(cmdlet, total, "Waiting for jobs");

        while (true)
        {
            foreach (var ((drive, folder), jobs) in pending.ToList())
            {
                token.ThrowIfCancellationRequested();
                string path = folder.GetPSPath();
                List<Job> read;
                try
                {
                    read = ReadJobs(drive, folder, jobs.Keys, refusesIn);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // A failed round is retried on the next one; the timeout still applies.
                    cmdlet.WriteError(new ErrorRecord(new OrchException(path, ex), "WaitJobReadError", ErrorCategory.ReadError, folder));
                    continue;
                }

                foreach (var job in read)
                {
                    if (job.Id is not Int64 id || !jobs.ContainsKey(id)) continue;
                    job.Path = path;
                    jobs[id] = job;
                    if (IsFinal(job.State))
                    {
                        jobs.Remove(id);
                        done++;
                        cmdlet.WriteObject(job);
                    }
                }

                // Asked for and not in the list. A job just started can be missing from the list
                // for a moment (Cloud, 2026-10-08: "not found" a second after Start-OrchJob, listed
                // a moment later), so it is read by its key; only a refusal of that read means the
                // job is not there.
                var returned = read.Where(j => j.Id is not null).Select(j => j.Id!.Value).ToHashSet();
                foreach (var id in jobs.Keys.Where(id => !returned.Contains(id)).ToList())
                {
                    Job? byKey;
                    try
                    {
                        byKey = drive.OrchAPISession.GetJob(folder.Id ?? 0, id);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException && IsNotFound(ex))
                    {
                        byKey = null;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        cmdlet.WriteError(new ErrorRecord(new OrchException(path, ex), "WaitJobReadError", ErrorCategory.ReadError, id));
                        continue;
                    }

                    if (byKey is null)
                    {
                        jobs.Remove(id);
                        done++;
                        cmdlet.WriteError(new ErrorRecord(
                            new OrchException(path, $"Job {id} was not found."),
                            "WaitJobNotFound", ErrorCategory.ObjectNotFound, id));
                        continue;
                    }

                    byKey.Path = path;
                    jobs[id] = byKey;
                    if (IsFinal(byKey.State))
                    {
                        jobs.Remove(id);
                        done++;
                        cmdlet.WriteObject(byKey);
                    }
                }

                if (jobs.Count == 0) pending.Remove((drive, folder));
            }
            if (pending.Count == 0) return;

            var waiting = pending.Values.SelectMany(j => j.Values).ToList();
            reporter.WriteProgress(done, $"{waiting.Count(j => j?.State == "Running")} running, {waiting.Count(j => j?.State == "Pending")} pending, {waiting.Count} of {total} not ended");

            if (timeoutSeconds is >= 0 && stopwatch.Elapsed >= TimeSpan.FromSeconds(timeoutSeconds.Value))
            {
                foreach (var job in waiting)
                {
                    cmdlet.WriteError(new ErrorRecord(
                        new TimeoutException($"{job?.Path}: Job {job?.Id} ({job?.ReleaseName}) is still {job?.State} after {timeoutSeconds} seconds."),
                        "WaitJobTimeout", ErrorCategory.OperationTimeout, job));
                }
                return;
            }

            var wait = PollInterval;
            if (timeoutSeconds is >= 0)
            {
                var left = TimeSpan.FromSeconds(timeoutSeconds.Value) - stopwatch.Elapsed;
                if (left < wait) wait = left < TimeSpan.Zero ? TimeSpan.Zero : left;
            }
            if (token.WaitHandle.WaitOne(wait)) token.ThrowIfCancellationRequested();
        }
    }

    private static bool IsNotFound(Exception? ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is HttpResponseException { StatusCode: System.Net.HttpStatusCode.NotFound }) return true;
        }
        return false;
    }

    // "Id in (...)" first; a server that refuses it gets the "or" form from then on.
    private static List<Job> ReadJobs(OrchDriveInfo drive, Folder folder, IEnumerable<Int64> ids, HashSet<OrchDriveInfo> refusesIn)
    {
        if (!refusesIn.Contains(drive))
        {
            try
            {
                return drive.OrchAPISession.GetJobsByIds(folder.Id ?? 0, ids, useOr: false).ToList();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                refusesIn.Add(drive);
            }
        }
        return drive.OrchAPISession.GetJobsByIds(folder.Id ?? 0, ids, useOr: true).ToList();
    }
}

[Cmdlet(VerbsLifecycle.Wait, "OrchJob", DefaultParameterSetName = "FromCommandLine")]
[OutputType(typeof(Job))]
public class WaitJobCmdlet : OrchestratorPSCmdlet
{
    [Parameter(ParameterSetName = "FromCommandLine", Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StopJobCmdlet.IdCompleter))]
    public Int64[]? Id { get; set; }

    [Parameter(ParameterSetName = "FromCommandLine", DontShow = true, ValueFromPipeline = true)]
    public Job? Job { get; set; }

    // Every job one StartJobs call created shares Job.BatchExecutionKey.
    [Parameter(ParameterSetName = "ByBatch", Mandatory = true)]
    public string[]? BatchExecutionKey { get; set; }

    // Seconds, as Wait-Job -Timeout. Omitted or negative: no limit.
    [Parameter]
    [Alias("TimeoutSec")]
    public int? Timeout { get; set; }

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

    // Looking for -Id / -BatchExecutionKey: up to 5 searches 2 s apart, about 8 s in all.
    private const int ResolveAttempts = 5;
    private static readonly TimeSpan ResolveRetryInterval = TimeSpan.FromSeconds(2);

    // Piped jobs, whose folder is known.
    private readonly List<(OrchDriveInfo drive, Folder folder, Int64 id)> targets = [];

    // -Id / -BatchExecutionKey with the folders to look in; resolved in EndProcessing.
    private readonly List<(OrchDriveInfo drive, Folder folder, Int64[] ids)> idSearches = [];
    private readonly List<(OrchDriveInfo drive, Folder folder, Guid key)> batchSearches = [];

    protected override void ProcessRecord()
    {
        if (BatchExecutionKey is not null)
        {
            var keys = new List<Guid>();
            foreach (var k in BatchExecutionKey.Where(k => !string.IsNullOrEmpty(k)))
            {
                if (Guid.TryParse(k, out var g)) keys.Add(g);
                else WriteError(new ErrorRecord(new ArgumentException($"-BatchExecutionKey must be a GUID; got '{k}'."), "WaitJobInvalidBatchKey", ErrorCategory.InvalidArgument, k));
            }
            foreach (var (drive, folder) in SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth))
            {
                foreach (var key in keys) batchSearches.Add((drive, folder, key));
            }
            return;
        }

        if (Job is not null)
        {
            if (Job.Id is not Int64 jobId || string.IsNullOrEmpty(Job.Path)) return;
            foreach (var (drive, folder) in SessionState.EnumFolders(new string[] { Job.Path }))
            {
                targets.Add((drive, folder, jobId));
            }
            return;
        }

        foreach (var (drive, folder) in SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth))
        {
            idSearches.Add((drive, folder, Id!));
        }
    }

    protected override void EndProcessing()
    {
        using var cancelHandler = new ConsoleCancelHandler();

        // A job lives in one folder, and so does a batch; find which. The job list can lag a job
        // just created by a few seconds (Cloud, 2026-10-08: -Id and -BatchExecutionKey right after
        // Start-OrchJob found nothing, and found the jobs a moment later), so what is not found is
        // looked for again before it is reported.
        var wantedIds = idSearches.SelectMany(s => s.ids.Select(id => (s.drive, id))).ToHashSet();
        var foundIds = new HashSet<(OrchDriveInfo, Int64)>();
        var wantedKeys = batchSearches.Select(b => (b.drive, b.key)).ToHashSet();
        var foundKeys = new HashSet<(OrchDriveInfo, Guid)>();
        var readErrors = new Dictionary<Folder, Exception>();

        for (int attempt = 1; ; attempt++)
        {
            foreach (var (drive, folder, ids) in idSearches)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                var missing = ids.Where(id => !foundIds.Contains((drive, id))).ToList();
                if (missing.Count == 0) continue;
                try
                {
                    foreach (var job in drive.OrchAPISession.GetJobsByIds(folder.Id ?? 0, missing, useOr: false))
                    {
                        if (job.Id is Int64 id && foundIds.Add((drive, id))) targets.Add((drive, folder, id));
                    }
                    readErrors.Remove(folder);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    readErrors[folder] = ex;
                }
            }

            foreach (var (drive, folder, key) in batchSearches)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();
                if (foundKeys.Contains((drive, key))) continue;
                try
                {
                    foreach (var job in drive.OrchAPISession.GetJobsByBatchExecutionKey(folder.Id ?? 0, key))
                    {
                        if (job.Id is not Int64 id) continue;
                        foundKeys.Add((drive, key));
                        targets.Add((drive, folder, id));
                    }
                    readErrors.Remove(folder);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    readErrors[folder] = ex;
                }
            }

            bool allFound = wantedIds.All(foundIds.Contains) && wantedKeys.All(foundKeys.Contains);
            if (allFound || attempt == ResolveAttempts) break;
            if (cancelHandler.Token.WaitHandle.WaitOne(ResolveRetryInterval)) cancelHandler.Token.ThrowIfCancellationRequested();
        }

        foreach (var (folder, ex) in readErrors)
        {
            WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "WaitJobReadError", ErrorCategory.ReadError, folder));
        }
        foreach (var (drive, id) in wantedIds.Where(w => !foundIds.Contains(w)))
        {
            WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, $"Job {id} was not found in the target folders."), "WaitJobNotFound", ErrorCategory.ObjectNotFound, id));
        }
        foreach (var (drive, key) in wantedKeys.Where(k => !foundKeys.Contains(k)))
        {
            WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, $"No job with BatchExecutionKey {key} was found in the target folders."), "WaitJobNotFound", ErrorCategory.ObjectNotFound, key));
        }

        JobWaiter.Wait(this, targets, Timeout, cancelHandler.Token);
    }
}
