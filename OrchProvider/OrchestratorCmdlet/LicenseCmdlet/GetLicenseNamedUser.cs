using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.RobotType;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchLicenseNamedUser")]
    [OutputType(typeof(LicenseNamedUser))]
    public class GetLicenseNamedUserCommand : OrchestratorPSCmdlet
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

        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(RobotTypeCompleter))]
        [SupportsWildcards]
        public string[]? RobotType { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.RobotType>))]
        public string[]? Path { get; set; }

        // TODO: StaticTextCompleter で書き直す
        private class RobotTypeCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                // パラメータで選択済みの RobotType は、候補から除外する
                var wpRobotTypes = CreateWPListFromParameter(commandAst, "RobotType", Positional.RobotType.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var robotType in robotTypes
                    .Where(wp.IsMatch)
                    .ExcludeByWildcards(rt => rt, wpRobotTypes)
                    .OrderBy(rt => rt))
                {
                    yield return new CompletionResult(robotType);
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpRobotType = RobotType.ConvertToWildcardPatternList();

            var specifiedRobotType = robotTypes
                .FilterByWildcards(rt => rt, wpRobotType)
                .OrderBy(rt => rt)
                .ToList();

            // drive と robotType の全ての要素の組み合わせを計算
            var drivesRobottypes = drives
                .SelectMany(drive => specifiedRobotType, (drive, robotType) => (drive, robotType))
                .ToList();

            using var results = OrchThreadPool.RunForEach(drivesRobottypes,
                dr => dr.drive.NameColonSeparator,
                dr => dr.drive,
                dr => dr.drive.GetLicenseNamedUser(dr.robotType));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var licenses = result.GetResult(cancelHandler.Token);
                    if (licenses == null) continue;

                    WriteObject(licenses, true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetLicenseNamedUserError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
