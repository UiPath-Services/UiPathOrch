using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;

namespace UiPath.PowerShell.Commands;

// Shared -MachineRobots argument completers for the trigger cmdlets so
// New-/Update-OrchTrigger and New-/Update-OrchApiTrigger all complete the same
// way. New-* offer the robots/machines available in the folder; Update-* show
// the trigger's current MachineRobots binding (which is trigger-type specific,
// so the base defers the per-folder trigger fetch to a subclass).

// Available-robots completer — New-OrchTrigger / New-OrchApiTrigger.
internal class MachineRobotsCompleter : OrchArgumentCompleter
{
    private static string MakeCandidateText(Entities.User? user, MachineFolder? machine, MachineSessionRuntime? session)
    {
        string sessionName = session?.HostMachineName;
        if (!string.IsNullOrEmpty(session?.ServiceUserName))
        {
            sessionName += (" - " + session.ServiceUserName);
        }

        MachineRobotSessionForSerialize ret = new()
        {
            // Emit a name the -MachineRobots resolver accepts for every robot
            // kind: the unattended-robot name, else the RobotProvision name
            // (robot accounts / modern robots), else the account login.
            UserName = user?.UnattendedRobot?.UserName ?? user?.RobotProvision?.UserName ?? user?.UserName,
            MachineName = machine?.Name,
            SessionName = sessionName
        };

        return JsonSerializer.Serialize(ret, JsonTools.jsoOneLine);
    }

    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

        // Exclude already-selected MachineRobots from candidates
        var wpMachineRobots = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        var usersPerFolders = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderUsersWithInherited.Get(df.folder));
        var robotsPerFolders = ParallelResults.GroupBy(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));
        var sessionsPerFolders = ParallelResults.GroupBy(drivesFolders, df => df.drive.MachineSessionRuntimesByFolder.Fetch(df.folder));

        List<UserRoles?> users = [null];
        users.AddRange(usersPerFolders.SelectMany(g => g));

        List<MachineFolder?> machines = [null];
        machines.AddRange(robotsPerFolders.SelectMany(g => g));

        List<MachineSessionRuntime?> sessions = [null];
        sessions.AddRange(sessionsPerFolders.SelectMany(g => g));

        // Generate and process all combinations
        var combinations = users
            .SelectMany(user => machines, (user, machine) => new { user, machine })
            .SelectMany(pair => sessions, (pair, session) => new { pair.user, pair.machine, session });

        foreach (var c in combinations)
        {
            // Skip if the session's MachineId does not match
            if (c.session is not null && c.machine?.Id != c.session.MachineId) continue;

            // For Dynamic Allocation, user can be omitted, but for machine
            // mapping, user must not be null. Too many candidates would be
            // inconvenient, so exclude all entries where user is null.
            if (c.user is null) continue;

            var drive = SessionState.GetOrchDrive(c.user?.Path);
            var targetUser = drive.Users.Get().Where(u => u.Id == c.user?.Id).FirstOrDefault();
            // Keep any provisioned robot — robot accounts and modern robots
            // carry their RobotId on RobotProvision, not UnattendedRobot.
            if (targetUser is null
                || (targetUser.UnattendedRobot?.RobotId is null && targetUser.RobotProvision?.RobotId is null)) continue;

            string text = MakeCandidateText(targetUser, c.machine, c.session);

            if (!wp.IsMatch(text)) continue;
            if (wpMachineRobots is not null && wpMachineRobots.Any(wpm => wpm.IsMatch(text))) continue;

            yield return new CompletionResult(PathTools.EscapePSText(text), text, CompletionResultType.Text, text);
        }
    }
}

// Current-values completer base — Update-OrchTrigger / Update-OrchApiTrigger.
// Shows each existing trigger's current MachineRobots binding so it can be
// edited/reused. Subclasses supply the per-folder trigger fetch per type.
internal abstract class MachineRobotsCurrentCompleterBase : OrchArgumentCompleter
{
    protected abstract IEnumerable<(string? Name, MachineRobotSession[]? MachineRobots)> GetBindings(OrchDriveInfo drive, Folder folder);

    public override IEnumerable<CompletionResult> CompleteArgumentCore(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

        // Exclude already-selected Names from candidates
        var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

        var results = ParallelResults.GroupBy(drivesFolders, df => GetBindings(df.drive, df.folder));

        bool bExists = false;
        foreach (var group in results)
        {
            var (drive, folder) = group.Source;

            foreach (var b in group
                .Where(t => t.MachineRobots is not null)
                .FilterByWildcards(t => t.Name, wpName)
                .OrderBy(t => t.Name))
            {
                string machineRobots = OrchestratorPSCmdlet.SerializeMachineRobotSessions(drive, folder!, b.MachineRobots);
                if (string.IsNullOrEmpty(machineRobots)) continue;

                bExists = true;
                string tiphelp = System.IO.Path.Combine(folder.GetPSPath(), b.Name ?? "");
                yield return new CompletionResult("'" + machineRobots.Replace("'", "''") + "'", machineRobots, CompletionResultType.Text, tiphelp);
            }
        }
        if (!bExists)
        {
            yield return new CompletionResult("'[{\"UserName\":\"\",\"MachineName\":\"\",\"SessionName\":\"\"}]'");
        }
    }
}

// Time / queue triggers (ProcessSchedule).
internal class TimeTriggerMachineRobotsCompleter : MachineRobotsCurrentCompleterBase
{
    protected override IEnumerable<(string? Name, MachineRobotSession[]? MachineRobots)> GetBindings(OrchDriveInfo drive, Folder folder)
        => drive.GetTriggers(folder).Select(t => (t.Name, t.MachineRobots));
}

// API triggers.
internal class ApiTriggerMachineRobotsCompleter : MachineRobotsCurrentCompleterBase
{
    protected override IEnumerable<(string? Name, MachineRobotSession[]? MachineRobots)> GetBindings(OrchDriveInfo drive, Folder folder)
        => drive.ApiTriggers.Get(folder).Select(t => (t.Name, t.MachineRobots));
}
