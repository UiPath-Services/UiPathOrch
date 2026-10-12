using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Get, "OrchUserPrivilege")]
[OutputType(typeof(Entities.UserPrivilege))]
public class GetUserPrivilegeCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    [ArgumentCompleter(typeof(TenantUserUserNameCompleter))]
    public string[]? UserName { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpUserName = UserName.ConvertToWildcardPatternList();

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                var users = drive.Users.Get();
                var targetUsers = users
                    .FilterByWildcards(u => u?.UserName, wpUserName)
                    .Where(u => u.Type == "DirectoryUser" || u.Type == "DirectoryGroup")
                    .OrderBy(u => u.UserName)
                    .ToList();

                // /api/Users/GetPrivileges answers on Automation Cloud only: on every standalone
                // server measured (20.10.16 to 25.10.2) and on Automation Suite 24.10.11 it is a
                // 404 for every user and group (2026-10-07), which printed one identical error per
                // user. Off Cloud, ask for the first user alone: a 404 there is the server's answer
                // for the whole drive, reported once. On Cloud the endpoint is there, and a 404 is
                // that user's own (a user removed from the directory since the list was read), so
                // nothing is probed.
                Exception? notFound = null;
                if (drive._psDrive.ResolvedEdition != OrchEdition.Cloud && targetUsers.Count > 0)
                {
                    try
                    {
                        drive.UserPrivileges.Get(targetUsers[0]);
                    }
                    catch (Exception ex) when (IsNotFound(ex))
                    {
                        notFound = ex;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException and not PipelineStoppedException)
                    {
                        // Anything else is that user's own error, reported with the rest below.
                    }
                }
                if (notFound is not null)
                {
                    WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator,
                        "User privileges are not available on this Orchestrator (/api/Users/GetPrivileges)", notFound),
                        "GetUserPrivilegesError", ErrorCategory.NotImplemented, drive));
                    continue;
                }

                using var results = OrchThreadPool.RunForEach(targetUsers
                        .FilterByWildcards(u => u?.UserName, wpUserName)
                        .OrderBy(u => u.UserName),
                    user => user.GetPSPath(),
                    user => user,
                    user => drive.UserPrivileges.Get(user));

                using var reporter = new ProgressReporter(this, results.Count, "Getting user privileges");
                foreach (var result in results)
                {
                    try
                    {
                        // Bar fills with the TRUE number of background fetches completed, so it
                        // keeps advancing while output is blocked on an early-but-slow user.
                        var entities = results.GetResultWithProgress(result, reporter, cancelHandler.Token);
                        if (entities is null) continue;

                        WriteObject(entities);
                    }
                    catch (OrchException ex)
                    {
                        WriteError(new ErrorRecord(ex, "GetUserPrivilegesError", ErrorCategory.InvalidOperation, ex.Target));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw; // Ctrl+C: propagate the stop instead of one canceled-error per drive
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }

    // Also looks through an AggregateException, which a background read wraps a 404 in.
    internal static bool IsNotFound(Exception ex)
        => OrchException.FindHttpStatus(ex) == System.Net.HttpStatusCode.NotFound;
}
