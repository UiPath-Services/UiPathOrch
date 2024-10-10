using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;
using System.Collections;
using System.Management.Automation.Language;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Add, "OrchBucket", SupportsShouldProcess = true)]
    [OutputType(typeof(Bucket))]
    public class AddBucketCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(BucketNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Description { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Positional.BucketStorageProviderItems>))]
        public string? StorageProvider { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? StorageParameters { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(CredentialStoreNameCompleter<Positional.Name>))]
        public string? StorageContainer { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(CredentialStoreNameCompleter<Positional.Name>))]
        [SupportsWildcards]
        public string? CredentialStore { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? Password { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(StaticTextsCompleter<Positional.BucketOptionsItems>))]
        public string[]? Options { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string? ExternalName { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string[]? Tags { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        protected override void ProcessRecord()
        {
            var drivesFolders = OrchDriveInfo.EnumFolders(Path);
            WildcardPattern? wpCredentialStore = CredentialStore.ConvertToWildcardPattern();

            using var cancelHandler = new ConsoleCancelHandler();

            foreach (var (drive, folder) in drivesFolders)
            {
                foreach (var name in Name!)
                {
                    string target = System.IO.Path.Combine(folder.GetPSPath(), name);
                    if (ShouldProcess(target, "Add Bucket"))
                    {
                        try
                        {
                            Bucket postingBucket = new()
                            {
                                Name = WildcardPattern.Unescape(name),
                                Identifier = Guid.NewGuid(),
                            };
                            postingBucket.AssignStringIfNotNullOrEmpty(Description,       (b, v) => b.Description = v);
                            postingBucket.AssignStringIfNotNullOrEmpty(StorageProvider,   (b, v) => b.StorageProvider = v);
                            postingBucket.AssignStringIfNotNullOrEmpty(StorageContainer,  (b, v) => b.StorageContainer = v);
                            postingBucket.AssignStringIfNotNullOrEmpty(StorageParameters, (b, v) => b.StorageParameters = v);
                            postingBucket.AssignStringIfNotNullOrEmpty(Password,          (b, v) => b.Password = v);
                            postingBucket.AssignStringIfNotNullOrEmpty(ExternalName,      (b, v) => b.ExternalName = v);
                            postingBucket.AssignStringIfNotNullOrEmpty(string.Join(",", Options ?? []), (b, v) => b.Options = v);
                            postingBucket.AssignTags(Tags, (b, v) => b.Tags = v);

                            if (!string.IsNullOrEmpty(CredentialStore))
                            {
                                postingBucket.CredentialStoreId = FindCredentialStoreId(folder.GetPSPath(), drive, wpCredentialStore)?.Id ?? 0;
                            }

                            var newBucket = drive.OrchAPISession.PostBucket(folder.Id!.Value, postingBucket);
                            drive._dicBuckets?.TryRemove(folder.Id.Value, out var _);
                            if (newBucket != null)
                            {
                                newBucket.Path = folder.GetPSPath();
                                WriteObject(newBucket);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "AddBucketError", ErrorCategory.InvalidOperation, drive));
                        }
                    }
                }
            }
        }
    }
}
