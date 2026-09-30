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
    public int? HeadlessSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? HostingSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AppTestSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? PerformanceTestSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AgentSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? FunctionSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AutomationCloudSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    public int? AutomationCloudTestAutomationSlots { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Any_Foreground_Background>))]
    public string? AutomationType { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(StaticTextsCompleter<Any_Windows_Portable>))]
    public string? TargetFramework { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(MachineRobotUsersCompleter))]
    [SupportsWildcards]
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
            // OrchAPISession.AddMachine strips fields an older server does not model; saying
            // so here keeps a requested value from vanishing without a word. ApiVersion is
            // only published once the session has seen an API response, so prime it — but
            // only when a version-gated field was actually asked for, to keep a plain create
            // at one request.
            bool versionGatedFieldRequested =
                AppTestSlots is not null || HostingSlots is not null ||
                PerformanceTestSlots is not null || FunctionSlots is not null;
            if (versionGatedFieldRequested && drive.OrchAPISession.ApiVersion is null)
            {
                _ = drive.Machines.Get();
            }

            double? api = drive.OrchAPISession.ApiVersion;
            string driveTarget = drive.NameColonSeparator;
            var appTestSlots = MachineFieldVersions.SupportedOrNull(AppTestSlots, MachineFieldVersions.AppTestSlots, api, nameof(AppTestSlots), driveTarget, WriteWarning);
            var hostingSlots = MachineFieldVersions.SupportedOrNull(HostingSlots, MachineFieldVersions.HostingSlots, api, nameof(HostingSlots), driveTarget, WriteWarning);
            var performanceTestSlots = MachineFieldVersions.SupportedOrNull(PerformanceTestSlots, MachineFieldVersions.PerformanceTestSlots, api, nameof(PerformanceTestSlots), driveTarget, WriteWarning);
            var functionSlots = MachineFieldVersions.SupportedOrNull(FunctionSlots, MachineFieldVersions.FunctionSlots, api, nameof(FunctionSlots), driveTarget, WriteWarning);
            // Tags / AutomationType / TargetFramework / RobotUsers keep going through
            // OrchAPISession.AddMachine's long-standing per-version strips: probing an API 13.0
            // server showed those version notes to be wrong in both directions, so re-deciding
            // it here would only add a second, equally unreliable gate.
            var tags = Tags;
            var automationType = AutomationType;
            var targetFramework = TargetFramework;
            var driveRobotUsers = processedRobotUsers?.ToArray();
            var automationCloudSlots = AutomationCloudSlots;
            var automationCloudTestAutomationSlots = AutomationCloudTestAutomationSlots;

            foreach (var name in Name!
                .WithProgressBar(this, $"Creating machines in {drive.NameColonSeparator}", name => name)
                .WithCancellation(cancelHandler.Token))
            {
                if (Scope == "PersonalWorkspace")
                {
                    WriteWarning($"\"{drive.NameColonSeparator}{name}\": Machines with the \"Scope\" set to \"PersonalWorkspace\" cannot be added with this cmdlet. Please enable the personal workspace using the Enable-OrchPersonalWorkspace cmdlet.");
                    continue;
                }

                string target = System.IO.Path.Combine(drive.NameColonSeparator, name);
                if (ShouldProcess(target, "New Machine"))
                {
                    List<RobotUser>? lstRobotUsers = null;
                    if (driveRobotUsers is not null)
                    {
                        var robots = drive.AllRobotsAcrossFolders.Get();
                        var wpRobotUsers = driveRobotUsers.ConvertToWildcardPatternList();
                        // Match on User.FullName (the CSV / manual form) OR Id (the object-pipe form a
                        // piped RobotUser is transformed to), so Get-OrchMachine | New-OrchMachine works.
                        var targetRobots = robots.FilterByWildcardsAny([r => r?.User?.FullName, r => r?.Id?.ToString()], wpRobotUsers);
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
                            targetFramework ??= "Portable";
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
                            HeadlessSlots = HeadlessSlots,
                            HostingSlots = hostingSlots,
                            AppTestSlots = appTestSlots,
                            PerformanceTestSlots = performanceTestSlots,
                            AgentSlots = AgentSlots,
                            FunctionSlots = functionSlots,
                            AutomationCloudSlots = automationCloudSlots,
                            AutomationCloudTestAutomationSlots = automationCloudTestAutomationSlots,
                            AutomationType = automationType,
                            TargetFramework = targetFramework,
                            RobotUsers = lstRobotUsers?.ToArray()
                        };

                        machine.AssignTags(tags, (m, v) => m.Tags = v);

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
