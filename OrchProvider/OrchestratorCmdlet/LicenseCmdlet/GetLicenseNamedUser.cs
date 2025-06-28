using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.RobotType;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLicenseNamedUser")]
[OutputType(typeof(LicenseNamedUser))]
public class GetLicenseNamedUserCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<LicenseRobotTypeItems>))]
    [SupportsWildcards]
    public string[]? RobotType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(Path);
        var wpRobotType = RobotType.ConvertToWildcardPatternList();

        var specifiedRobotType = Positional.LicenseRobotTypeItems.Parameters
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
                if (licenses is null) continue;

                WriteObject(licenses, true);
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetLicenseNamedUserError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
