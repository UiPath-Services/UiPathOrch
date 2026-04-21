using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "PmLicense")]
[OutputType(typeof(Entities.AvailableUserBundle))]
public class GetPmLicense : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LicenseCompleter))]
    [SupportsWildcards]
    public string[]? License { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(CodeCompleter))]
    [SupportsWildcards]
    public string[]? Code { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter HasCapacity { get; set; }

    private class LicenseCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpLicense = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
            var wpCode = GetFakeBoundParameters(fakeBoundParameters, "Code").ConvertToWildcardPatternList();

            var results = ParallelResults.GroupBy(drives, drive => drive.PmLicenses.Get());

            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var bundle in result
                    .Where(b => !string.IsNullOrEmpty(b?.name))
                    .ExcludeByWildcards(b => b?.name!, wpLicense)
                    .FilterByWildcards(b => b?.code, wpCode)
                    .OrderBy(b => b?.name))
                {
                    string tiphelp = $"{drive.NameColonSeparator}{bundle.name}  {bundle.allocated}/{bundle.total}";
                    yield return new CompletionResult(PathTools.EscapePSText(bundle.name), bundle.name, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    private class CodeCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpLicense = GetFakeBoundParameters(fakeBoundParameters, "License").ConvertToWildcardPatternList();
            var wpCode = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

            var results = ParallelResults.GroupBy(drives, drive => drive.PmLicenses.Get());

            foreach (var result in results)
            {
                var drive = result.Source;

                foreach (var bundle in result
                    .Where(b => !string.IsNullOrEmpty(b?.code))
                    .FilterByWildcards(b => b?.name!, wpLicense)
                    .ExcludeByWildcards(b => b?.code, wpCode)
                    .OrderBy(b => b?.code))
                {
                    string tiphelp = $"{drive.NameColonSeparator}{bundle.code}  {bundle.name}  {bundle.allocated}/{bundle.total}";
                    yield return new CompletionResult(PathTools.EscapePSText(bundle.code), bundle.code, CompletionResultType.Text, tiphelp);
                }
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);

        var wpLicense = License.ConvertToWildcardPatternList();
        var wpCode = Code.ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            try
            {
                var entities = drive.PmLicenses.Get();
                if (entities is null) continue;

                var targetEntities = entities
                    .FilterByWildcards(b => b?.name, wpLicense)
                    .FilterByWildcards(b => b?.code, wpCode)
                    .OrderBy(b => b?.name);

                if (HasCapacity)
                {
                    targetEntities = targetEntities
                        .Where(b => b.allocated.HasValue && b.total.HasValue && b.allocated < b.total)
                        .OrderBy(b => b?.name);
                }

                WriteObject(targetEntities, true);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmLicenseError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
