using System.Management.Automation;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsDiagnostic.Test, "OrchUserMappingCsv")]
class TestUserMappingCsvCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true)]
    //[ArgumentCompleter(typeof(BucketNameCompleter<TPositional>))]
    [SupportsWildcards]
    public string? CsvFile { get; set; }

    [Parameter(Position = 1)]
    public string? SourceTenant { get; set; }

    [Parameter(Position = 2)]
    public string? DestinationTenant { get; set; }

    protected override void ProcessRecord()
    {
    }
}
