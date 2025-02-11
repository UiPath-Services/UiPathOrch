using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// How to test PSDrive.ShortenScope() method:
// 1. Make this cmdlet class public and build the module
// 2. Execute the following:
// PS> Import-Csv ShortenScopeTestCases.csv | Test-OrchShortenScope | select Id,Success

[Cmdlet(VerbsDiagnostic.Test, "OrchShortenScope")]
[OutputType(typeof(TestShortenScopeOutput))]
class TestShortenScopeCommand : OrchestratorPSCmdlet
{
    public class TestShortenScopeOutput
    {
        public string? Id { get; set; }
        public string? Input { get; set; }
        public string? Expected { get; set; }
        public string? Actual { get; set; }
        public bool? Success { get; set; }
    }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Id { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Input { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Expected { get; set; }

    protected override void ProcessRecord()
    {
        string actual = PSDrive.ShortenScope(Input!);

        WriteObject(new TestShortenScopeOutput()
        {
            Id = Id,
            Input = Input,
            Expected = Expected,
            Actual = actual,
            Success = string.Equals(Expected, actual, StringComparison.Ordinal)
        });
    }
}
