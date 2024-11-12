using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchRole", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.Role))]
    public class CopyRoleCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        [ArgumentCompleter(typeof(StaticRoleNameCompleter<Positional.Name_Destination>))]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationCompleter))]
        public string[]? Destination { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string? Path { get; set; }

        // DriveCompleter と良く似ているのだけど、これはコピー元のドライブを除外する機能がある。
        public class DestinationCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = OrchDriveInfo.EnumAllOrchDrives();

                // パラメータで選択済みのドライブは、候補から除外する
                var paramPath = GetParameterValues(commandAst, "Path", Positional.Name_Destination.Parameters).Select(p => p.TrimEnd(':'));
                var paramPathDriveNames = OrchDriveInfo.EnumOrchDrives(paramPath).Select(d => d.Name);
                var wpPath = paramPathDriveNames.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                // パラメータで選択済みのドライブは、候補から除外する
                var paramDestination = GetParameterValues(commandAst, "Destination", Positional.Name_Destination.Parameters, wordToComplete).Select(p => p.TrimEnd(':'));
                var wpDestination = paramDestination.Select(p => new WildcardPattern(p, WildcardOptions.IgnoreCase)).ToList();

                var wp = CreateWPFromWordToComplete(wordToComplete);

                foreach (var drive in drives
                    .ExcludeByWildcards(d => d?.Name, wpPath)
                    .ExcludeByWildcards(d => d?.Name, wpDestination)
                    .Where(d => wp.IsMatch(d.NameColon)))
                {
                    string tiphelp = TipHelp(drive);
                    yield return new CompletionResult(PathTools.EscapePSText(drive.NameColonSeparator), drive.NameColonSeparator, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }

        protected override void ProcessRecord()
        {
            var wpName = Name?.Select(name => new WildcardPattern(name, WildcardOptions.IgnoreCase)).ToList();

            var srcDrive = OrchDriveInfo.GetOrchDrive(Path!);
            if (srcDrive == null)
                throw new Exception("Path is not OrchDrive.");

            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

            srcDrive.Roles.ClearCache();

            var srcRoles = srcDrive!.Roles.Get()
                .FilterByWildcards(role => role?.Name, wpName)
                .OrderBy(role => role.Name)
                .ToList();

            string msg = "Copying roles";
            using var reporter = new ProgressReporter(this, 1, 100, msg, msg);

            int index = 0;
            reporter.TotalNum = dstDrives.Count * srcRoles.Count;

            foreach (var dstDrive in dstDrives)
            {
                if (srcDrive == dstDrive) continue;

                string target = dstDrive.NameColonSeparator;

                foreach (var role in srcRoles
                    //.Where(r => !r.IsStatic.GetValueOrDefault())
                    .OrderBy(r => r.Name))
                {
                    string item = System.IO.Path.Combine(srcDrive.NameColon, role.Name!);
                    string destination = dstDrive.NameColonSeparator;

                    reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {role.GetPSPath()} to {dstDrive.NameColonSeparator}");

                    if (ShouldProcess($"Item: {item} Destination: {destination}", $"Copy Role"))
                    {
                        try
                        {
                            var addedRole = dstDrive.OrchAPISession.PostRole(role);
                            //addedRole.Path = dstDrive.NameColonSeparator;
                            //WriteObject(addedRole);
                            dstDrive.Roles.ClearCache();
                        }
                        catch (Exception ex)
                        {
                            var errorRecord = new ErrorRecord(new OrchException(item, ex), "CopyRoleError", ErrorCategory.InvalidOperation, destination);
                            WriteError(errorRecord);
                        }
                    }
                }
            }
        }
    }
}
