using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Set, "OrchLocation")]
public class SetOrchLocationCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(ModuleNameCompleter))]
    [SupportsWildcards]
    public string ModuleName { get; set; } = "UiPath.PowerShell.OrchProvider";

    private class ModuleNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var wp = CreateWPFromWordToComplete(wordToComplete);

            IEnumerable<Assembly> assemblies = SetOrchLocationCommand.EnumAssenblies();

            foreach (var module in assemblies
                .Where(m => wp.IsMatch(m.GetName().Name)))
            {
                string moduleName = module.GetName().Name;
                string moduleDir = module.Location;
                yield return new CompletionResult(PathTools.EscapePSText(moduleName), moduleName, CompletionResultType.Text, moduleDir);
            }
        }
    }

    public static bool IsPowerShellModule(Assembly assembly)
    {
        try
        {
            // A PowerShell module often references System.Management.Automation
            // and has a '.dll' extension in its location.
            return assembly.GetReferencedAssemblies().Any(name => name.Name!.Equals("System.Management.Automation", StringComparison.OrdinalIgnoreCase))
                   && assembly.Location.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // If we cannot access the assembly metadata, assume it's not a module.
            return false;
        }
    }

    public static IEnumerable<Assembly> EnumAssenblies()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        return assemblies.Where(IsPowerShellModule);
    }

    protected override void ProcessRecord()
    {
        var wpModuleName = new WildcardPattern(ModuleName, WildcardOptions.IgnoreCase);

        IEnumerable<Assembly> assenblies = EnumAssenblies();

        var targets = assenblies.Where(a => wpModuleName.IsMatch(a.GetName().Name!)).ToList();

        if (targets.Count == 0)
        {
            throw new Exception("Not found.");
        }

        if (targets.Count > 1)
        {
            throw new Exception("multiple resolved.");
        }

        var target = targets.First();

        string moduleDir = Path.GetDirectoryName(target.Location);

        if (string.IsNullOrEmpty(moduleDir))
        {
            WriteError(new ErrorRecord(
                new InvalidOperationException($"Module '{ModuleName}' not found in the module path."),
                "ModuleNotFound",
                ErrorCategory.ObjectNotFound,
                ModuleName));
            return;
        }

        try
        {
            SessionState.Path.SetLocation(moduleDir);
            //WriteObject($"Current location set to '{moduleDir}'.");
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(
                ex,
                "ErrorChangingLocation",
                ErrorCategory.InvalidOperation,
                null));
        }
    }
}
