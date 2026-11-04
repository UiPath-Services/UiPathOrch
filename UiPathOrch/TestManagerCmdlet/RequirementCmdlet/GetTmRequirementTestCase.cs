using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// One row per test case assigned to a requirement; see TmTestCaseAssignment.cs.
[Cmdlet(VerbsCommon.Get, "TmRequirementTestCase")]
[OutputType(typeof(TmRequirementTestCase))]
public class GetTmRequirementTestCaseCmdlet : GetTmTestCaseAssignmentCmdletBase
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmRequirementNameCompleter))]
    [SupportsWildcards]
    public string[]? Requirement { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TmTestCaseNameCompleter))]
    [SupportsWildcards]
    public string[]? TestCase { get; set; }

    [Parameter]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    internal override TmTestCaseAssignmentKind Kind => TmRequirementAssignmentKind.Instance;
    protected override string[]? ContainerPatterns => Requirement;
    protected override string[]? TestCasePatterns => TestCase;
    protected override string? ExportCsvPath => ExportCsv;
    protected override Encoding? ExportCsvEncoding => CsvEncoding;
    protected override string DefaultCsvName => "ExportedTmRequirementTestCases.csv";
}
