using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.RobotType_Key;

namespace UiPath.PowerShell.Commands;

public class EnableLicenseRuntimeCommandBase<Enable> : OrchestratorPSCmdlet where Enable : IBoolParameter
{
    public virtual string[]? RobotType { get; set; }

    public virtual string[]? Key { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    internal class KeyCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);

            // パラメータで選択された RobotType のみ対象とする
            var wpRobotTypes = CreateWPListFromOtherParameters(commandAst, "RobotType", TPositional.Parameters);

            // パラメータで選択済みの Key は、候補から除外する
            var wpKey = CreateWPListFromParameter(commandAst, "Key", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            var specifiedRobotTypes = LicenseRobotTypeItems.Parameters
                .FilterByWildcards(rt => rt, wpRobotTypes)
                // .OrderBy(rt => rt);
                .ToList();

            var results = new ConcurrentBag<IEnumerable<LicenseRuntime>>();
            Parallel.ForEach(drives, drive =>
            {
                // キャッシュされていない robotType についてのみ GetLicenseRuntime() で取得する
                IEnumerable<string> nonCachedRobotTypes;
                if (drive._dicLicenseRuntime is null)
                {
                    // キャッシュがなければ全て取得
                    nonCachedRobotTypes = specifiedRobotTypes;
                }
                else
                {
                    // キャッシュがあれば、まだキャッシュしていないものだけ取得
                    nonCachedRobotTypes = specifiedRobotTypes
                        .Where(rt => !drive._dicLicenseRuntime!.ContainsKey(rt));
                }

                Parallel.ForEach(nonCachedRobotTypes, robotType =>
                {
                    try
                    {
                        results.Add(drive.GetLicenseRuntime(robotType));
                    }
                    catch { }
                });
            });

            foreach (var drive in drives)
            {
                foreach (var license in specifiedRobotTypes
                    .Where(key => drive._dicLicenseRuntime!.ContainsKey(key))
                    .Select(key => drive._dicLicenseRuntime![key])
                    .SelectMany(l => l)
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
        var drives = SessionState.EnumOrchDrives(Path);
        var wpRobotType = RobotType.ConvertToWildcardPatternList();
        var wpKey = Key.ConvertToWildcardPatternList();

        var specifiedRobotType = LicenseRobotTypeItems.Parameters
            .FilterByWildcards(rt => rt, wpRobotType)
            .OrderBy(rt => rt)
            .ToList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            foreach (var robotType in specifiedRobotType)
            {
                var licenses = drive.GetLicenseRuntime(robotType);

                foreach (var license in licenses
                    .Where(t => Enable.Value
                        ? !t.Enabled.GetValueOrDefault()
                        : t.Enabled.GetValueOrDefault())
                    .FilterByWildcards(l => l?.Key, wpKey))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = drive.NameColonSeparator + license.Key;

                    string action = $"{(Enable.Value ? "Enable" : "Disable")} LicenseRuntime {robotType}";

                    if (ShouldProcess(target, action))
                    {
                        try
                        {
                            drive.OrchAPISession.ToggleLicenseRuntime(robotType, license.Key!, license.MachineName!, Enable.Value);
                            drive._dicLicenseRuntime?.TryRemove(robotType, out _);
                        }
                        catch (Exception ex)
                        {
                            string errorId = $"{(Enable.Value ? "Enable" : "Disable")}LicenseRuntimeError";
                            WriteError(new ErrorRecord(new OrchException(target, ex), errorId, ErrorCategory.InvalidOperation, license));
                        }
                    }
                }
            }
        }
    }
}
