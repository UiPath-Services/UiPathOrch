using System.Management.Automation;
using UiPath.PowerShell.Positional;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchBucket")]
[OutputType(typeof(Bucket))]
public class GetBucketCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(BucketNameCompleter<False>))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    private static readonly string DefaultCsvName = "ExportedBuckets.csv";
    private static readonly string[] CsvHeaders = [
        "Path",
        "Name",
        "Description",
        "StorageProvider",
        "StorageContainer",
        "StorageParameters",
        "CredentialStore",
        "Password",
        "ExternalName",
        "Options",
        "Tags"];

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    private void WriteCsvContent(StreamWriter writer, OrchDriveInfo drive, IEnumerable<Bucket> output)
    {
        foreach (var bucket in output)
        {
            #region Convert CredentialId to Name
            string credentialStoreName = null;
            if (bucket.CredentialStoreId is not null)
            {
                try
                {
                    var credentialStores = drive.CredentialStores.Get();
                    credentialStoreName = credentialStores.FirstOrDefault(c => c.Id == bucket.CredentialStoreId)?.Name;
                }
                catch (Exception ex)
                {
                    WriteWarning($"{bucket.GetPSPath()}: Failed to retrieve CredentialStore: {ex.Message}");
                }
            }
            #endregion

            string[] line = [
                EscapeCsvValue(bucket.Path, true),
                EscapeCsvValue(bucket.Name, true),
                EscapeCsvValue(bucket.Description),
                EscapeCsvValue(bucket.StorageProvider),
                EscapeCsvValue(bucket.StorageContainer),
                EscapeCsvValue(bucket.StorageParameters),
                EscapeCsvValue(credentialStoreName, true),
                EscapeCsvValue(bucket.Password),
                EscapeCsvValue(bucket.ExternalName),
                EscapeCsvValue(bucket.Options),
                EscapeCsvValue(bucket.Tags)
            ];
            writer.WriteCsvLine(line);
        }
    }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        using var results = OrchThreadPool.RunForEach(drivesFolders,
            df => df.folder.GetPSPath(),
            df => df.folder,
            df => df.drive.Buckets.Get(df.folder));

        using var cancelHandler = new ConsoleCancelHandler();
        using var reporter = new ProgressReporter(this, 1, results.Count, "Getting buckets");
        foreach (var result in results)
        {
            try
            {
                var entities = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                if (entities is null) continue;

                var targetEntities = entities
                    .FilterByWildcards(s => s?.Name, wpName)
                    .OrderBy(s => s.Name);

                if (writer is not null)
                {
                    var (drive, _) = result.Source;
                    WriteCsvContent(writer, drive, targetEntities);
                }
                else
                {
                    WriteObject(targetEntities, true);
                }
            }
            catch (OrchException ex)
            {
                WriteError(new ErrorRecord(ex, "GetBucketError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }

        WriteCSVExportedMessage(this, providerCsvPath);
    }
}
