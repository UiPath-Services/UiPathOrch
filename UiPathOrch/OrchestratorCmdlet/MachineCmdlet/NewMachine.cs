using System.Collections;
using UiPath.PowerShell.Positional;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "OrchMachine", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.CreatedMachine))]
public class NewMachineCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(NewMachineNameCompleter))]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public string? Description { get; set; }

    // Currently supports Template, Standard, and Serverless.
    // AutomationCloudRobot also needs to be supported.
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Template_Standard_Serverless>))]
    public string? Type { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Default_Serverless_AutomationCloudRobot>))]
    public string? Scope { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? UnattendedSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? NonProductionSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? TestAutomationSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Any_Foreground_Background>))]
    public string? AutomationType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Any_Windows_Portable>))]
    public string? TargetFramework { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineRobotUsersCompleter))]
    [RobotUserArgumentTransformation]
    public string[]? RobotUsers { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [TagArgumentTransformation]
    public string[]? Tags { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    private class NewMachineNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgumentCore(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolveOrchDrives(fakeBoundParameters);
            var results = ParallelResults.GroupBy(drives, drive => drive.Machines.Get());

            // Exclude Names already selected via parameter from the candidates
            var names = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

            var entities = results.SelectMany(e => e);
            yield return new CompletionResult(GenerateNewEntityName("NewMachine", names, entities, e => e.Name!));
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var processedRobotUsers = RobotUsers?.SplitValuesByUnescapedCommasPreservingEscapes();
        // CSV-piped rows surface absent values as [""] (one empty string); normalise to null
        // so we don't pointlessly hit /odata/Robots/.../FindAllAcrossFolders, which 404s on
        // older OCs (e.g. ApiVersion 11 / OC 20.10) and is unnecessary when no users were
        // requested.
        if (processedRobotUsers is not null && processedRobotUsers.All(string.IsNullOrWhiteSpace))
        {
            processedRobotUsers = null;
        }

        if (string.IsNullOrEmpty(Type)) { Type = "Template"; }
        if (string.IsNullOrEmpty(AutomationType)) { AutomationType = null; }
        if (string.IsNullOrEmpty(TargetFramework)) { TargetFramework = null; }

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            foreach (var name in Name!.WithCancellation(cancelHandler.Token))
            {
                if (Scope == "PersonalWorkspace")
                {
                    WriteWarning($"{drive.NameColonSeparator}{name}: Machines with the \"Scope\" set to \"PersonalWorkspace\" cannot be added with this cmdlet. Please enable the personal workspace using the Enable-OrchPersonalWorkspace cmdlet.");
                    continue;
                }

                string target = System.IO.Path.Combine(drive.NameColonSeparator, name);
                if (ShouldProcess(target, "New Machine"))
                {
                    List<RobotUser>? lstRobotUsers = null;
                    if (processedRobotUsers is not null)
                    {
                        var robots = drive.AllRobotsAcrossFolders.Get();
                        // Match -RobotUsers exactly by robot FullName (the CSV / manual form) OR numeric
                        // Id (the object-pipe form a piped RobotUser is transformed to) — no wildcards,
                        // so Get-OrchMachine | New-OrchMachine works.
                        var targetRobots = robots.Where(r =>
                            (r.User?.FullName is { } fn && processedRobotUsers!.Contains(fn, StringComparer.OrdinalIgnoreCase))
                            || (r.Id?.ToString() is { } id && processedRobotUsers!.Contains(id, StringComparer.OrdinalIgnoreCase)));
                        lstRobotUsers = targetRobots
                            .Select(r => new RobotUser()
                            {
                                UserName = r.Username,
                                RobotId = r.Id
                            })
                            .OrderBy(r => r.UserName)
                            .ToList();
                    }

                    ExtendedMachine machine = null;
                    try
                    {
                        if (Scope == "Serverless")
                        {
                            TargetFramework ??= "Portable";
                        }

                        machine = new()
                        {
                            Name = WildcardPattern.Unescape(name),
                            Description = Description,
                            Type = Type,
                            Scope = Scope,
                            NonProductionSlots = NonProductionSlots,
                            UnattendedSlots = UnattendedSlots,
                            TestAutomationSlots = TestAutomationSlots,
                            AutomationType = AutomationType,
                            TargetFramework = TargetFramework,
                            RobotUsers = lstRobotUsers?.ToArray()
                        };

                        machine.AssignTags(Tags, (m, v) => m.Tags = v);

                        var newMachine = drive.OrchAPISession.AddMachine(machine);
                        drive.Machines.ClearCache();
                        if (newMachine is not null)
                        {
                            newMachine.Path = drive.NameColonSeparator;
                            WriteObject(newMachine);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(target, ex), "NewMachineError", ErrorCategory.InvalidOperation, machine));
                    }
                }
            }
        }
    }
}
