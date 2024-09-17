using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Path;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "OrchPSDrive")]
    [OutputType(typeof(Entities.OrchPSDrive))]
    public class GetOrchPSDriveCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Path>))]
        [SupportsWildcards]
        public string[]? Path { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        //public class NameCompleter : OrchArgumentCompleter
        //{
        //    public override IEnumerable<CompletionResult> CompleteArgument(
        //        string commandName,
        //        string parameterName,
        //        string wordToComplete,
        //        CommandAst commandAst,
        //        IDictionary fakeBoundParameters)
        //    {
        //        var drives = OrchDriveInfo.EnumAllOrchDrives();

        //        // パラメータで選択済みのドライブは、候補から除外する
        //        var paramName = GetParameterValues(commandAst, "Name", positionalParams, wordToComplete).Select(p => p.TrimEnd(':'));
        //        var wpName = paramName.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

        //        var wp = CreateWPFromWordToComplete(wordToComplete);

        //        foreach (var drive in drives
        //            .ExcludeByWildcards(d => d.Name, wpName)
        //            .Where(d => wp.IsMatch(d.Name)))
        //        {
        //            string tiphelp = drive.DisplayRoot;
        //            if (!string.IsNullOrEmpty(drive.Description))
        //                tiphelp += $" ({drive.Description})";
        //            yield return new CompletionResult(PathTools.EscapePSText(drive.Name), drive.Name, CompletionResultType.ParameterValue, tiphelp);
        //        }
        //    }
        //}

        protected override void ProcessRecord()
        {
            IEnumerable<OrchDriveInfo> drives = null;
            if (Path == null)
            {
                drives = OrchDriveInfo.EnumAllOrchDrives();
            }
            else
            {
                drives = OrchDriveInfo.EnumOrchDrives(Path);
            }

            foreach (var drive in drives)
            {
                if (Force.IsPresent)
                {
                    try
                    {
                        drive.OrchAPISession.EnsureAuthenticated();
                        drive.GetPartitionGlobalId();
                        drive.GetTenantId();
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(drive.NameColon, ex), "GetActivitySettingsError", ErrorCategory.InvalidOperation, drive));
                    }
                }

                OrchPSDrive info = new()
                {
                    Name = drive.Name,
                    Root = drive.DisplayRoot,
                    ApiVersion = drive.OrchAPISession.ApiVersion,
                    IsConfidential = drive.OrchAPISession.AuthManager.IsConfidentialApp,
                    CurrentUser = drive.OrchAPISession.AuthManager.IsConfidentialApp ? "N/A" : drive._dicCurrentUser?.UserName,
                    Description = drive.Description,
                    CurrentLocation = drive.CurrentLocation,
                    PartitionGlobalId = drive._dicPartitionGlobalId,
                    TenantId = drive._dicTenantId,
                    BearerToken = drive.OrchAPISession.AuthManager.AccessToken
                };

                WriteObject(info);
            }
        }
    }
}
