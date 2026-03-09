using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using TPositional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "OrchRole", SupportsShouldProcess = true)]
public class CopyRoleCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(RoleNameCompleter<TPositional>))]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter<TPositional>))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    [SupportsWildcards]
    public string? Path { get; set; }

    internal static void CopyRoles(
        IWritableHost _this,
        OrchDriveInfo srcDrive, List<WildcardPattern>? wpName,
        IList<OrchDriveInfo> dstDrives,
        bool shouldProcess, CancellationToken cancelToken)
    {
        var srcRoles = srcDrive!.Roles.Get()
            .FilterByWildcards(role => role?.Name, wpName)
            .OrderBy(role => role.Name)
            .ToList();

        using var reporter = new ProgressReporter(_this, 1, 100, "Copying roles");

        int index = 0;
        reporter.TotalNum = dstDrives.Count * srcRoles.Count;

        foreach (var dstDrive in dstDrives)
        {
            if (srcDrive == dstDrive) continue;

            string target = dstDrive.NameColonSeparator;

            foreach (var role in srcRoles
                //.Where(r => !r.IsStatic.GetValueOrDefault())
                .OrderBy(r => r.Name))
            {
                cancelToken.ThrowIfCancellationRequested();
                reporter.WriteProgress(++index, $"{role.GetPSPath()} to {dstDrive.NameColonSeparator}");

                // Skip if the source role is static and a role with the same name exists at the destination
                if (role.IsStatic.GetValueOrDefault())
                {
                    var dstRoles = dstDrive.Roles.Get();
                    var existingRole = dstRoles.FirstOrDefault(r => string.Compare(r.Name, role.Name, true) == 0);
                    if (existingRole is not null) continue;
                }

                string item = System.IO.Path.Combine(srcDrive.NameColon, role.Name!);
                string destination = dstDrive.NameColonSeparator;

                if (shouldProcess || _this.ShouldProcess($"Item: {item} Destination: {destination}", $"Copy Role"))
                {
                    try
                    {
                        var addedRole = dstDrive.OrchAPISession.PostRole(role);
                        //addedRole.Path = dstDrive.NameColonSeparator;
                        //WriteObject(addedRole);
                        dstDrive.Roles.ClearCache();
                    }
                    catch (Exception ex)
                    {
                        var errorRecord = new ErrorRecord(new OrchException(item, ex), "CopyRoleError", ErrorCategory.InvalidOperation, destination);
                        _this.WriteError(errorRecord);
                    }
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var wpName = Name.ConvertToWildcardPatternList();

        var srcDrive = SessionState.GetOrchDrive(Path!);
        if (srcDrive is null)
            throw new Exception("Path is not OrchDrive.");

        var dstDrives = SessionState.EnumDestinationDrives(Destination!);

        srcDrive.Roles.ClearCache();

        using var cancelHandler = new ConsoleCancelHandler();
        CopyRoles(this, srcDrive, wpName, dstDrives, false, cancelHandler.Token);
    }
}
