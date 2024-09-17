using System.Collections;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Id;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchPackage")]
    [OutputType(typeof(Entities.Package))]
    public class GetPackageCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(PackageIdCompleter<Positional.Id>))]
        [SupportsWildcards]
        public string[]? Id { get; set; }

        [Parameter]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        //[Parameter]
        //public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(Path, Recurse.IsPresent);
            var wpId = Id?.Select(id => new WildcardPattern(id, WildcardOptions.IgnoreCase)).ToList();

            using var results = OrchThreadPool.RunForEach(drivesFolders,
                df => df.folder.GetPSPath(),
                df => df.folder,
                df => df.drive.GetPackages(df.folder));

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var packages = result.GetResult(cancelHandler.Token);
                    if (packages == null) continue;

                    WriteObject(packages
                        .FilterByWildcards(p => p?.Id, wpId)
                        .OrderBy(p => p.Id!.ToLower()),
                        true);
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetPackageUserError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
