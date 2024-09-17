using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.PortableExecutable;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using OrchCollectionExtensions = UiPath.PowerShell.Core.OrchCollectionExtensions;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchCalendar", SupportsShouldProcess = true)]
    [OutputType(typeof(ExtendedCalendar))]
    public class CopyCalendarCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(CalendarNameCompleter<Positional.Name_Destination>))]
        [SupportsWildcards]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DestinationCompleter))]
        [SupportsWildcards]
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
                    string driveName = drive.NameColon;
                    string tiphelp = drive.DisplayRoot;
                    if (!string.IsNullOrEmpty(drive.Description))
                        tiphelp += $" ({drive.Description})";
                    yield return new CompletionResult(PathTools.EscapePSText(driveName), driveName, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }

        protected override void ProcessRecord()
        {
            var srcDrive = OrchDriveInfo.GetOrchDrive(Path!);

            var dstDrives = OrchDriveInfo.EnumDestinationDrives(Destination!);

            var wpName = Name.ConvertToWildcardPatternList();

            srcDrive._dicCalendars = null;
            srcDrive._dicCalendars_Exceptions.ClearCache();

            // この実装はこれで良い。
            ICollection<ExtendedCalendar>? srcCalendars = null;
            try
            {
                srcCalendars = srcDrive.GetCalendars();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(srcDrive.NameColonSeparator, ex), "GetCalendarError", ErrorCategory.InvalidOperation, srcDrive));
                return;
            }
            if (srcCalendars == null) return;

            string msg = "Copying calendars";
            using var reporter = new ProgressReporter(this, 1, 100, msg, msg);

            int index = 0;
            reporter.TotalNum = dstDrives.Count * srcCalendars.Count;

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var dstDrive in dstDrives)
            {
                foreach (var srcCalendar in srcCalendars)
                {
                    string item = srcCalendar.GetPSPath();
                    string destination = dstDrive.NameColonSeparator;

                    cancelHandler.Token.ThrowIfCancellationRequested();

                    reporter.WriteProgress(++index, $"{index:D}/{reporter.TotalNum} {srcCalendar.GetPSPath()} to {dstDrive.NameColonSeparator}");

                    if (ShouldProcess($"Item: {item} Destination: {destination}", "Copy Calendar"))
                    {
                        try
                        {
                            var srcDetailedCalendar = srcDrive.GetCalendar(srcCalendar);
                            if (srcDetailedCalendar == null)
                            {
                                continue;
                            }
                            var newCalendar = OrchCollectionExtensions.DeepCopy(srcDetailedCalendar);
                            newCalendar.TimeZoneId = null;
                            newCalendar.Key = null;
                            newCalendar.Id = null;
                            //newCalendar.Path = null; // JsonIgnore 属性がついているので不要
                            var createdCalendar = dstDrive.OrchAPISession.PostCalendar(newCalendar);
                            if (createdCalendar != null)
                            {
                                //createdCalendar.Path = dstDrive.NameColonSeparator;
                                //WriteObject(createdCalendar);
                                dstDrive._dicCalendars = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(item, ex), "CreateCalendarError", ErrorCategory.InvalidOperation, destination));
                        }
                    }
                }
            }
        }
    }
}
