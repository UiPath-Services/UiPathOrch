using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Edit, "OrchProcess")]
    public class EditProcessCommand : OrchestratorPSCmdlet
    {
        [Parameter (Position = 0)]
        [ArgumentCompleter(typeof(ProcessNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path);
            var wpName = Name?.Select(name => new WildcardPattern(name, WildcardOptions.IgnoreCase)).ToList();

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetReleases(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var releases = result.GetResult(cancelHandler.Token);
                    if (releases == null) continue;

                    var (drive, folder) = result.Source;

                    foreach (var release in releases)
                    {
                        string endpoint = $"{drive.OrchAPISession._base_url}/orchestrator_/processes/{release.Id}/edit?fid={folder.Id ?? 0}";
                        Process.Start(new ProcessStartInfo(endpoint) { UseShellExecute = true });
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetProcessError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
