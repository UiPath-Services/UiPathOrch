using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

public class EnableLicenseRuntimeCmdletBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? RobotType { get; set; }

    public virtual string[]? Key { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    internal class KeyCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // Target only the RobotTypes selected by the parameter
            var wpRobotTypes = GetFakeBoundParameters(fakeBoundParameters, "RobotType").ConvertToWildcardPatternList();

            // Exclude Keys already selected by the parameter from the candidates
            var wpKey = CreateSelfExclusionList(commandAst, "Key", wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var specifiedRobotTypes = LicenseRobotTypeItems.Items
                .FilterByWildcards(rt => rt, wpRobotTypes)
                // .OrderBy(rt => rt);
                .ToList();

            // Prefetch in parallel; LicenseRuntimes (KeyedListCachePerTenant) handles
            // "fetch only if not cached" internally, so no explicit ContainsKey filter is needed.
            Parallel.ForEach(drives, drive =>
            {
                Parallel.ForEach(specifiedRobotTypes, robotType =>
                {
                    try
                    {
                        drive.LicenseRuntimes.Get(robotType);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetLicenseRuntime prefetch failed for '{robotType}': {ex.Message}");
                    }
                });
            });

            foreach (var drive in drives)
            {
                foreach (var license in specifiedRobotTypes
                    .SelectMany(rt =>
                    {
                        try { return (IEnumerable<LicenseRuntime>)drive.LicenseRuntimes.Get(rt); }
                        catch { return Enumerable.Empty<LicenseRuntime>(); }
                    })
                    .Where(l => wp.IsMatch(l.Key))
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .ExcludeByWildcards(l => l?.Key, wpKey)
                    .OrderBy(l => l.Key))
                {
                    string tooltip = $"HMN:{license.HostMachineName}  SUN:{license.ServiceUserName}  MN:{license.MachineName}";
                    yield return new CompletionResult(PathTools.EscapePSText(license.Key), license.Key, CompletionResultType.Text, tooltip);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpRobotType = RobotType.ConvertToWildcardPatternList();
        var wpKey = Key.ConvertToWildcardPatternList();

        var specifiedRobotType = LicenseRobotTypeItems.Items
            .FilterByWildcards(rt => rt, wpRobotType)
            .OrderBy(rt => rt)
            .ToList();

        var named = LicenseRobotTypeSupport.NamedTypes(RobotType);
        string errorId = $"{(Enable.Value ? "Enable" : "Disable")}LicenseRuntimeError";

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            // Types the user did not name that this server is known not to have are left out,
            // and its "no such type" answer for the rest is verbose (see LicenseRobotTypeSupport,
            // shared with Get-OrchLicenseRuntime).
            var api = LicenseRobotTypeSupport.ApiVersionOf(drive);
            var reporter = new UnknownRobotTypeReporter(this, errorId, named);
            foreach (var robotType in specifiedRobotType
                .Where(rt => named.Contains(rt) || !LicenseRobotTypeSupport.KnownAbsent(rt, api, runtime: true))
                .WithCancellation(cancelHandler.Token))
            {
                // One type's listing failing used to end the whole command (terminating), so on
                // an older server `-RobotType *` stopped at the first type it lacked.
                IEnumerable<LicenseRuntime> licenses;
                try
                {
                    licenses = drive.LicenseRuntimes.Get(robotType);
                    reporter.Answered(drive);
                }
                catch (Exception ex) when (ex is not OperationCanceledException and not PipelineStoppedException)
                {
                    reporter.Failed(drive, robotType, ex as OrchException ?? new OrchException(drive.NameColonSeparator, ex));
                    continue;
                }

                foreach (var license in licenses
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .FilterByWildcards(l => l?.Key, wpKey).WithCancellation(cancelHandler.Token))
                {
                    string target = drive.NameColonSeparator + license.Key;

                    string action = $"{(Enable.Value ? "Enable" : "Disable")} LicenseRuntime {robotType}";

                    if (ShouldProcess(target, action))
                    {
                        try
                        {
                            drive.OrchAPISession.ToggleLicenseRuntime(robotType, license.Key!, license.MachineName!, Enable.Value);
                            drive.LicenseRuntimes.ClearCache(robotType);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), errorId, ErrorCategory.InvalidOperation, license));
                        }
                    }
                }
            }
            reporter.Finish();
        }
    }
}
