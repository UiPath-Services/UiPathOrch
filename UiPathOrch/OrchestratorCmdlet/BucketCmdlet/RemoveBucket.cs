using System.Management.Automation;
using UiPath.PowerShell.Positional;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchBucket", SupportsShouldProcess = true)]
public class RemoveBucketCommand : RemoveFolderEntityCmdletBase<Bucket>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public override string[]? Name { get; set; }

    protected override string EntityNoun => "Bucket";
    protected override Func<Bucket?, string?> GetName => b => b?.Name;
    protected override Func<Bucket, string> GetPSPath => b => b.GetPSPath();

    protected override IEnumerable<Bucket> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.Buckets.Get(folder);

    protected override void Remove(OrchDriveInfo drive, Folder folder, Bucket bucket)
    {
        drive.OrchAPISession.DeleteBucket(folder.Id ?? 0, bucket.Id ?? 0);
        drive.Buckets.ClearCache(folder);
        drive.BucketLinks.ClearCache();
    }
}
