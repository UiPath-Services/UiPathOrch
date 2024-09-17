using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchJobVideo")]
    public class GetJobVideoCommand : OrchestratorPSCmdlet
    {
        [Parameter]
        public ulong? Skip { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Item10>))]
        public ulong? First { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        private static string MakeFilter()
        {
            // ビデオは7日間経過すると自動で削除されるので、それ以上過去のジョブは取得する必要がない
            // 1日余裕をみて、最近の8日間のジョブを取得する
            return $"&$filter=((ProcessType eq 'Process') and (StartTime ge {DateTime.UtcNow.AddDays(-8):yyyy-MM-ddTHH:mm:ss.fffZ}))";
            // TODO HasVideoRecorded = true の条件を、上に入れられないのか？
        }

        protected override void ProcessRecord()
        {
            ulong skip = Skip ?? 0;
            ulong first = First ?? int.MaxValue;

            var drivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

            string filter = MakeFilter();

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetJobs(df.folder, filter, skip, first));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var jobs = result.GetResult(cancelHandler.Token);
                    if (jobs == null) continue;

                    WriteObject(jobs
                        .Where(j => j.HasVideoRecorded.GetValueOrDefault()),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetJobError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
