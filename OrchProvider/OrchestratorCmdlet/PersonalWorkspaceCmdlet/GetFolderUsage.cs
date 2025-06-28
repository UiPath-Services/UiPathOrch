using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
//using TPositional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands;

class InputParameter
{
    public OrchDriveInfo? drive;
    public Int64? folderId;
    public string? path;
}

[Cmdlet(VerbsCommon.Get, "OrchFolderUsage")]
[OutputType(typeof(Entities.EntitySummary))]
public class GetFolderUsageCommand : OrchestratorPSCmdlet
{
    List<InputParameter>? inputParameters;

    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter(DontShow = true, ValueFromPipelineByPropertyName = true)]
    public Int64? Id { get; set; }

    [Parameter(DontShow = true, ValueFromPipelineByPropertyName = true)]
    public string? Name { get; set; }

    [Parameter(DontShow = true, ValueFromPipelineByPropertyName = true)]
    public string? DisplayName { get; set; }

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
        if (Id is not null)
        {
            var drives = SessionState.EnumOrchDrives(Path!);
            if (drives is null || !drives.Any())
            {
                return;
            }

            OrchDriveInfo drive = drives.FirstOrDefault();
            if (drive is null)
            {
                return;
            }

            string name = Name ?? DisplayName;

            inputParameters ??= [];
            var param = new InputParameter()
            {
                drive = drive,
                folderId = Id,
                path = System.IO.Path.Combine(Path?[0] ?? "", name ?? "")
            };
            inputParameters.Add(param);
        }
        else
        {
            var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
            foreach (var (drive, folder) in drivesFolders)
            {
                inputParameters ??= [];
                var param = new InputParameter()
                {
                    drive = drive,
                    folderId = folder.Id ?? 0,
                    path = folder.GetPSPath()
                };
                inputParameters.Add(param);
            }
        }
    }

    protected override void EndProcessing()
    {
        if (inputParameters is null)
        {
            return;
        }

        // マルチスレッドで呼び出すと、サーバーからの結果が不安定になるような気がする。。
        // ここはシングルスレッドで問い合わせておく。

        foreach (var p in inputParameters)
        {
            try
            {
                var ret = p.drive!.GetEntitiesSummary(p.folderId ?? 0, p.path!);
                if (ret is not null)
                {
                    ret.Path = p.path;
                    WriteResult(ret);
                }
            }
            catch (Exception ex)
            {
                string target = p.path;
                WriteError(new ErrorRecord(new OrchException(target!, ex), "GetFolderSummaryError", ErrorCategory.InvalidOperation, target));
            }
        }
    }
}
