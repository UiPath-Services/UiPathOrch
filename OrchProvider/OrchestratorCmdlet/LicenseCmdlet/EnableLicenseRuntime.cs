using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.RobotType_Key;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Enable, "OrchLicenseRuntime", SupportsShouldProcess = true)]
    public class EnableLicenseRuntimeCommand : OrchestratorPSCmdlet
    {
        private static string[] robotTypes = [
            "Attended",
            "AttendedStudioWeb",
            "AutomationCloud",
            "AutomationCloudTestAutomation",
            "AutomationKit",
            "Development",
            "Headless",
            "NonProduction",
            "Serverless",
            "ServerlessTestAutomation",
            "StudioPro",
            "StudioX",
            "TestAutomation",
            "Unattended",
            //"CitizenDeveloper",
            //"RpaDeveloper",
            //"RpaDeveloperPro",
            //"Studio",
        ];

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(RobotTypeCompleter))]
        [SupportsWildcards]
        public string[]? RobotType { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(KeyCompleter))]
        [SupportsWildcards]
        public string[]? Key { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.RobotType_Key>))]
        public string[]? Path { get; set; }

        private class RobotTypeCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
                var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
                var drivesFolders = OrchDriveInfo.EnumOrchDrives(paramPath);

                // パラメータで選択済みの RobotType は、候補から除外する
                var wpRobotTypes = CreateWPListFromParameter(commandAst, "RobotType", Positional.RobotType_Key.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var robotType in robotTypes
                    .Where(rt => wp.IsMatch(rt))
                    .ExcludeByWildcards(rt => rt, wpRobotTypes)
                    .OrderBy(rt => rt))
                {
                    yield return new CompletionResult(robotType);
                }
            }
        }

        private class KeyCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択された RobotType のみ対象とする
                var wpRobotTypes = CreateWPListFromOtherParameters(commandAst, "RobotType", Positional.RobotType_Key.Parameters);

                // パラメータで選択済みの Key は、候補から除外する
                var wpKey = CreateWPListFromParameter(commandAst, "Key", Positional.RobotType_Key.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var specifiedRobotTypes = robotTypes
                    .FilterByWildcards(rt => rt, wpRobotTypes)
                    // .OrderBy(rt => rt);
                    .ToList();

                var results = new ConcurrentBag<IEnumerable<LicenseRuntime>>();
                Parallel.ForEach(drives, drive =>
                {
                    // キャッシュされていない robotType についてのみ GetLicenseRuntime() で取得する
                    IEnumerable<string> nonCachedRobotTypes;
                    if (drive._dicLicenseRuntime == null)
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
                        .Where(l => !(l.Enabled ?? false))
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
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpRobotType = RobotType.ConvertToWildcardPatternList();
            var wpKey = Key.ConvertToWildcardPatternList();

            var specifiedRobotType = robotTypes
                .FilterByWildcards(rt => rt, wpRobotType)
                .OrderBy(rt => rt)
                .ToList();

            // drive と robotType の全ての要素の組み合わせを計算
            var drivesRobotTypes = drives
                .SelectMany(drive => specifiedRobotType, (drive, robotType) => (drive, robotType))
                .ToList();

            // あらかじめ、非同期で対象の LicenseRuntime を取得しておく
            //Parallel.ForEach(drivesRobotTypes, driveRobotType =>
            //{
            //    var (drive, robotType) = driveRobotType;
            //    try
            //    {
            //        drive.GetLicenseRuntime(robotType);
            //    }
            //    catch { }
            //});kkk

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                foreach (var robotType in specifiedRobotType)
                {
                    //if (drive._dicLicenseRuntime!.TryGetValue(robotType, out var licenses))
                    var licenses = drive.GetLicenseRuntime(robotType);
                    foreach (var license in licenses
                        .Where(l => !(l.Enabled ?? false))
                        .FilterByWildcards(l => l?.Key, wpKey))
                    {
                        cancelHandler.Token.ThrowIfCancellationRequested();

                        string target = drive.NameColonSeparator + license.Key;
                        if (ShouldProcess(target, "Enable LicenseRuntime " + robotType))
                        {
                            try
                            {
                                drive.OrchAPISession.ToggleLicenseRuntime(robotType, license.Key!, license.MachineName!, true);
                                drive._dicLicenseRuntime?.TryRemove(robotType, out _);
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(target, ex), "EnableLicenseRuntimeError", ErrorCategory.InvalidOperation, license));
                            }
                        }
                    }
                }
            }
        }
    }
}
