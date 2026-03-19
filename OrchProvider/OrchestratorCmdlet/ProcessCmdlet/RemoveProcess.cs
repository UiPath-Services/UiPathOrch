using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Remove, "OrchProcess", SupportsShouldProcess = true)]
public class RemoveProcessCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ProcessNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(Path, Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                foreach (var proc in drive.GetReleases(folder).FilterByWildcards(p => p?.Name, wpName))
                {
                    cancelHandler.Token.ThrowIfCancellationRequested();

                    string target = proc.GetPSPath();
                    if (ShouldProcess(target, "Remove Process"))
                    {
                        try
                        {
                            drive.OrchAPISession.RemoveRelease(folder.Id ?? 0, proc.Id ?? 0);
                            drive._dicReleases?.TryRemove(folder.Id ?? 0, out var _);
                            //drive._dicReleaseList?.TryRemove(folder.Id ?? 0, out var _);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(target, ex), "RemoveProcessError", ErrorCategory.InvalidOperation, proc));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetProcessError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
