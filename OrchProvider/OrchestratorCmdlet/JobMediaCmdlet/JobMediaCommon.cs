using UiPath.PowerShell.Entities;

namespace OrchProvider.JobMediaCmdlet;

public class JobMediaCommon
{
    public static string MediaFileName(Int64 folderId, ExecutionMedia media)
    {
        return $"fid{folderId}_{media.ReleaseName}_{media.JobId}_{media.Name}";
    }
}
