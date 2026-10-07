using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchLicenseNamedUser")]
[OutputType(typeof(LicenseNamedUser))]
public class GetLicenseNamedUserCmdlet : OrchestratorPSCmdlet
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

        var specifiedRobotType = Positional.LicenseRobotTypeItems.Items
            .FilterByWildcards(rt => rt, wpRobotType)
            .OrderBy(rt => rt)
            .ToList();
        var named = LicenseRobotTypeSupport.NamedTypes(RobotType);

        // Compute the Cartesian product of all drives and robotTypes, leaving out the types the
        // user did not name that this server is known not to have (LicenseRobotTypeSupport).
        var drivesRobottypes = drives
            .SelectMany(drive => { var api = LicenseRobotTypeSupport.ApiVersionOf(drive); return specifiedRobotType.Select(rt => (drive, robotType: rt, api)); })
            .Where(dr => named.Contains(dr.robotType)
                || !LicenseRobotTypeSupport.KnownAbsent(dr.robotType, dr.api, runtime: false))
            .Select(dr => (dr.drive, dr.robotType))
            .ToList();

        using var results = OrchThreadPool.RunForEach(drivesRobottypes,
            dr => dr.drive.NameColonSeparator,
            dr => dr.drive,
            dr => dr.drive.LicenseNamedUsers.Get(dr.robotType));

        var reporter = new UnknownRobotTypeReporter(this, "GetLicenseNamedUserError", named);
        using var cancelHandler = new ConsoleCancelHandler();
        int index = 0;
        foreach (var result in results)
        {
            // The pool drains in submission order, so this is the pair the result belongs to
            // (result.Source cannot be read once GetResult has thrown).
            var (drive, robotType) = drivesRobottypes[index++];
            try
            {
                var licenses = result.GetResult(cancelHandler.Token);
                reporter.Answered(drive);
                if (licenses is null) continue;

                WriteObject(licenses, true);
            }
            catch (OrchException ex)
            {
                reporter.Failed(drive, robotType, ex);
            }
        }
        reporter.Finish();
    }
}
