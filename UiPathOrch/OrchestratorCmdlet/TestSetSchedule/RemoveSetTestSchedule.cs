using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchTestSetSchedule", SupportsShouldProcess = true)]
public class RemoveTestSetScheduleCommand : RemoveFolderEntityCmdletBase<TestSetSchedule>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TestScheduleNameCompleter))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "TestSetSchedule";
    protected override Func<TestSetSchedule?, string?> GetName => s => s?.Name;
    protected override Func<TestSetSchedule, string> GetPSPath => s => s.GetPSPath();
    protected override bool ExcludePersonalWorkspace => true;

    protected override IEnumerable<TestSetSchedule> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.TestSetSchedules.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, TestSetSchedule schedule)
    {
        drive.OrchAPISession.RemoveTestSetSchedules(folder.Id ?? 0, schedule.Id ?? 0);
        drive.TestSetSchedules.ClearCache(folder);
    }
}
