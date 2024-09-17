using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Completer;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Clear, "OrchCache", SupportsShouldProcess = true)]
    public class ClearCacheCommand : PSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(Position0AllDriveCompleter))]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter AllDrives { get; set; }

        public class Position0AllDriveCompleter : OrchArgumentCompleter
        {
            private static readonly string[] positionalParams = ["Path"];

            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                // パラメータで選択済みのドライブは、候補から除外する
                var paramPath = GetParameterValues(commandAst, "Path", positionalParams, wordToComplete).Select(p => p.TrimEnd(':'));
                var wpPath = paramPath.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var drives = OrchDriveInfo.EnumAllOrchDrives();
                foreach (var drive in drives
                    .ExcludeByWildcards(d => d?.Name, wpPath)
                    .Where(d => wp.IsMatch(d.NameColon)))
                {
                    string driveName = drive.NameColon;
                    string tiphelp = drive.DisplayRoot;
                    if (!string.IsNullOrEmpty(drive.Description))
                        tiphelp += $" ({drive.Description})";
                    yield return new CompletionResult(PathTools.EscapePSText(driveName), driveName, CompletionResultType.ParameterValue, tiphelp);
                }

                var duDrives = OrchDuDriveInfo.EnumAllOrchDrives();
                foreach (var drive in duDrives
                    .ExcludeByWildcards(d => d?.Name, wpPath)
                    .Where(d => wp.IsMatch(d.NameColon)))
                {
                    string driveName = drive.NameColon;
                    string tiphelp = drive.DisplayRoot;
                    if (!string.IsNullOrEmpty(drive.Description))
                        tiphelp += $" ({drive.Description})";
                    yield return new CompletionResult(PathTools.EscapePSText(driveName), driveName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }

        protected override void ProcessRecord()
        {
            List<OrchDriveInfo> drives = null;
            List<OrchDuDriveInfo> duDrives = null;
            List<OrchTmDriveInfo> tmDrives = null;
            if (!AllDrives.IsPresent)
            {
                drives = OrchDriveInfo.EnumOrchDrives(Path).ToList();
                duDrives = OrchDuDriveInfo.EnumOrchDrives(Path).ToList();
                tmDrives = OrchTmDriveInfo.EnumOrchDrives(Path).ToList();
            }

            // Path の指定がなく、カレントドライブが OrchDrive でない場合は、すべての OrchDrive のキャッシュをクリアする
            if (AllDrives.IsPresent || (drives!.Count == 0 && duDrives!.Count == 0 && tmDrives!.Count == 0))
            {
                foreach (var drive in OrchDriveInfo.EnumAllOrchDrives())
                {
                    if (ShouldProcess(drive.NameColonSeparator, "Clear Cache"))
                    {
                        drive.ClearAllCache();
                    }
                }
                foreach (var drive in OrchDuDriveInfo.EnumAllOrchDrives())
                {
                    if (ShouldProcess(drive.NameColonSeparator, "Clear Cache"))
                    {
                        drive.ClearAllCache();
                    }
                }
                foreach (var drive in OrchTmDriveInfo.EnumAllOrchDrives())
                {
                    if (ShouldProcess(drive.NameColonSeparator, "Clear Cache"))
                    {
                        drive.ClearAllCache();
                    }
                }
            }
            else
            {
                foreach (var drive in drives)
                {
                    if (ShouldProcess(drive.NameColonSeparator, "Clear Cache"))
                    {
                        drive.ClearAllCache();
                    }
                }
                foreach (var drive in duDrives ?? [])
                {
                    if (ShouldProcess(drive.NameColonSeparator, "Clear Cache"))
                    {
                        drive.ClearAllCache();
                    }
                }
                foreach (var drive in tmDrives ?? [])
                {
                    if (ShouldProcess(drive.NameColonSeparator, "Clear Cache"))
                    {
                        drive.ClearAllCache();
                    }
                }
            }
        }
    }
}
