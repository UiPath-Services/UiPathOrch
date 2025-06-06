using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using TPositional = UiPath.PowerShell.Positional.Name;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "OrchBucket", SupportsShouldProcess = true)]
[OutputType(typeof(Bucket))]
public class NewBucketCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewBucketNameCompleter))]
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
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter<TPositional>))]
    public string? StorageContainer { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CredentialStoreNameCompleter<TPositional>))]
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

    private class NewBucketNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
            var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Buckets.Get(df.folder));

            // パラメータで選択済みの Name は、候補から除外する
            var names = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var entities = results.SelectMany(e => e.Result ?? []);
            yield return new CompletionResult(GenerateNewEntityName("NewBucket", names, entities, e => e.Name!));
        }
    }

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
                if (ShouldProcess(target, "New Bucket"))
                {
                    try
                    {
                        Bucket postingBucket = new()
                        {
                            Name = WildcardPattern.Unescape(name),
                            Identifier = Guid.NewGuid().ToString(),
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
                        drive.Buckets.ClearCache(folder);
                        if (newBucket is not null)
                        {
                            newBucket.Path = folder.GetPSPath();
                            WriteObject(newBucket);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewBucketError", ErrorCategory.InvalidOperation, drive));
                    }
                }
            }
        }
    }
}
