using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

class InputParameter
{
    public OrchDriveInfo? drive;
    public Folder? folder;
}

[Cmdlet(VerbsCommon.Get, "OrchFolderUsage")]
[OutputType(typeof(Entities.EntitySummary))]
public class GetFolderUsageCmdlet : OrchestratorPSCmdlet
{
    private List<InputParameter>? _inputParameters;

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    private void WriteResult(EntitiesSummary ret)
    {
        if (ret.DeletableEntities is not null)
        {
            foreach (var e in ret.DeletableEntities)
            {
                e.Category = "DeletableEntity";
                e.Path = ret.Path;
                WriteObject(e);
            }
        }
        if (ret.StoppableJobs is not null)
        {
            foreach (var e in ret.StoppableJobs)
            {
                e.Category = "StoppableJob";
                e.Path = ret.Path;
                WriteObject(e);
            }
        }
    }

    protected override void ProcessRecord()
    {
        _inputParameters ??= [];
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        foreach (var (drive, folder) in drivesFolders)
        {
            var param = new InputParameter()
            {
                drive = drive,
                folder = folder,
            };
            _inputParameters.Add(param);
        }
    }

    protected override void EndProcessing()
    {
        if (_inputParameters is null)
        {
            return;
        }

        // Calling with multiple threads seems to make the server results unstable...
        // Query using a single thread here.

        foreach (var p in _inputParameters)
        {
            string? target = p.folder?.GetPSPath();
            try
            {
                var ret = p.drive!.EntitiesSummary.Get(p.folder!);
                if (ret is not null)
                {
                    ret.Path = target;
                    WriteResult(ret);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(target!, ex), "GetFolderSummaryError", ErrorCategory.InvalidOperation, target));
            }
        }
    }
}
