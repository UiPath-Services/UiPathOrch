using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLicenseRuntime")]
[OutputType(typeof(LicenseRuntime))]
public class GetLicenseRuntimeCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<LicenseRobotTypeItems>))]
    public string[]? RobotType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpRobotType = RobotType.ConvertToWildcardPatternList();

        var specifiedRobotType = LicenseRobotTypeItems.Items
            .FilterByWildcards(rt => rt, wpRobotType)
            .OrderBy(rt => rt)
            .ToList();

        // Compute the Cartesian product of all drives and robotTypes
        var drivesRobottypes = drives
            .SelectMany(drive => specifiedRobotType, (drive, robotType) => (drive, robotType))
            .ToList();

        using var results = OrchThreadPool.RunForEach(drivesRobottypes,
            dr => dr.drive.NameColonSeparator,
            dr => dr.drive,
            dr => dr.drive.LicenseRuntimes.Get(dr.robotType));

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var result in results)
        {
            try
            {
                var licenses = result.GetResult(cancelHandler.Token);
                if (licenses is null) continue;

                WriteObject(licenses, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetLicenseRuntimeError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
