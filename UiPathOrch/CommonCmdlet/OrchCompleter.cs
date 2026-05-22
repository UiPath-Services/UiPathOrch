using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using Path = System.IO.Path;
using Session = UiPath.PowerShell.Entities.Session;
using User = UiPath.PowerShell.Entities.User;

namespace UiPath.PowerShell.Completer;

public abstract partial class OrchArgumentCompleter : IArgumentCompleter
{
    protected static SessionState? SessionState => OrchDriveInfo.SessionState;

    public abstract IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters);

    protected static string? GetFakeBoundParameter(IDictionary fakeBoundParameters, string parameterName)
    {
        if (fakeBoundParameters is not null && fakeBoundParameters.Contains(parameterName))
        {
            return fakeBoundParameters[parameterName]!.ToString()!;
        }
        return null;
    }

    protected static bool GetFakeBoundParameterAsBool(IDictionary fakeBoundParameters, string parameterName)
    {
        if (fakeBoundParameters is not null && fakeBoundParameters.Contains(parameterName))
        {
            if (bool.TryParse(fakeBoundParameters[parameterName]!.ToString()!, out bool ret))
                return ret;
            return false;
        }
        return false;
    }

    protected static IEnumerable<string> GetFakeBoundParameters(IDictionary fakeBoundParameters, string parameterName)
    {
        if (fakeBoundParameters is null || !fakeBoundParameters.Contains(parameterName))
            yield break;

        object values = fakeBoundParameters[parameterName];
        if (values is object[] valueArray)
        {
            foreach (var v in valueArray)
            {
                yield return v.ToString()!;
            }
            yield break;
        }
        yield return values!.ToString()!;
    }

    protected static IEnumerable<string> SplitCommaSeparatedText(string? input)
    {
        if (input is null)
            yield break;

        var matches = CommaSeparatedTokenRegex().Matches(input);
        foreach (Match match in matches.Cast<Match>())
        {
            string value = match.Value.Trim();
            if (!string.IsNullOrEmpty(value))
                yield return PathTools.UnescapePSText(value);
        }
    }

    /// <summary>
    /// Resolves the bound value of a uint Depth parameter from fakeBoundParameters.
    /// Returns 0 if the dictionary is null, the key is missing, the value is null, or unparsable.
    /// Replaces the older AST-walking GetParameterValue(commandAst, "Depth") + uint.TryParse pattern,
    /// which is no longer needed: PowerShell populates fakeBoundParameters even for positional values.
    /// </summary>
    public static uint ResolveDepth(IDictionary? fakeBoundParameters)
    {
        if (fakeBoundParameters is null || !fakeBoundParameters.Contains("Depth")) return 0;
        object? value = fakeBoundParameters["Depth"];
        return value switch
        {
            null => 0,
            uint u => u,
            int i when i >= 0 => (uint)i,
            _ => uint.TryParse(value.ToString(), out var parsed) ? parsed : 0u,
        };
    }

    /// <summary>
    /// Resolves the bound value of a SwitchParameter from fakeBoundParameters.
    /// Returns false if the dictionary is null, the key is missing, the value is null, or non-truthy.
    /// Accepts SwitchParameter, bool, and the literal strings "true"/"false" (case-insensitive).
    /// Replaces the older AST-walking GetSwitchParameterValue(commandAst, name), which depended on a
    /// hand-maintained list of switch parameter names and silently broke when a new switch was added.
    /// </summary>
    public static bool ResolveSwitchParameter(IDictionary? fakeBoundParameters, string parameterName)
    {
        if (fakeBoundParameters is null || !fakeBoundParameters.Contains(parameterName)) return false;
        object? value = fakeBoundParameters[parameterName];
        return value switch
        {
            null => false,
            SwitchParameter sp => sp.IsPresent,
            bool b => b,
            _ => bool.TryParse(value.ToString(), out var b) && b,
        };
    }

    // Auto-discovered map of switch parameters per cmdlet, built once via reflection over
    // this assembly's [Cmdlet]-attributed types. Replaces the hand-maintained whitelist that
    // silently broke whenever a new SwitchParameter was added without registration.
    private static readonly Lazy<Dictionary<string, HashSet<string>>> _switchParametersByCmdlet =
        new(BuildSwitchParameterMap);

    private static Dictionary<string, HashSet<string>> BuildSwitchParameterMap()
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in typeof(OrchArgumentCompleter).Assembly.GetTypes())
        {
            var cmdletAttr = type.GetCustomAttribute<CmdletAttribute>();
            if (cmdletAttr is null) continue;

            string cmdletName = $"{cmdletAttr.VerbName}-{cmdletAttr.NounName}";
            var switches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.GetCustomAttributes<ParameterAttribute>().Any()) continue;
                if (prop.PropertyType != typeof(SwitchParameter)) continue;

                switches.Add(prop.Name);
                var aliasAttr = prop.GetCustomAttribute<AliasAttribute>();
                if (aliasAttr is not null)
                {
                    foreach (var alias in aliasAttr.AliasNames)
                    {
                        switches.Add(alias);
                    }
                }
            }

            // Last writer wins; cmdlet name collisions across the assembly aren't expected.
            map[cmdletName] = switches;
        }
        return map;
    }

    // PowerShell auto-adds these as switches whenever the cmdlet declares the matching
    // common-parameter support; reflection on the cmdlet type doesn't see them, so they're
    // listed explicitly here.
    private static readonly HashSet<string> CommonSwitchParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "Verbose", "Debug", "WhatIf", "Confirm",
    };

    private static bool IsKnownSwitchParameterName(CommandAst commandAst, string parameterName)
    {
        if (CommonSwitchParameters.Contains(parameterName)) return true;

        string? cmdletName = commandAst.GetCommandName();
        if (cmdletName is null) return false;

        return _switchParametersByCmdlet.Value.TryGetValue(cmdletName, out var switches)
            && switches.Contains(parameterName);
    }

    public static bool GetSwitchParameterValue(CommandAst commandAst, string parameterName)
    {
        if (!IsKnownSwitchParameterName(commandAst, parameterName))
        {
            return false;
        }

        // Iterate through each command element to find the specified parameter name
        foreach (var element in commandAst.CommandElements)
        {
            // Check the parameter name
            if (element is CommandParameterAst param &&
                param.ParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase) &&
                param.Argument is null) // Switch parameters have no arguments
            {
                return true;
            }
        }

        return false;
    }

    // TODO: The handling when a value is specified for a switch parameter may not be correct
    public static string? GetParameterValue(CommandAst commandAst, string parameterName, string[]? positionalParams = null)
    {
        var elements = commandAst.CommandElements.Skip(1).Select(e => e.ToString()).ToList();
        int positionIndex;
        if (positionalParams is not null)
            positionIndex = Array.IndexOf(positionalParams, parameterName);
        else
            positionIndex = -1;

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].StartsWith('-'))
            {
                string currentParamName = elements[i].TrimStart('-').Split(':')[0];
                bool isPositionalParam = positionalParams is not null && Array.IndexOf(positionalParams, currentParamName) >= 0;
                bool isSwitchParam = GetSwitchParameterValue(commandAst, currentParamName);

                if (currentParamName.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    // Handle when the current parameter is the one we want to extract
                    if (elements[i].Contains(':'))
                    {
                        return elements[i].Substring(elements[i].IndexOf(':') + 1).Trim();
                    }
                    if (elements.Count > i + 1 && !elements[i + 1].StartsWith('-'))
                        return elements[i + 1];
                    else
                        return "";
                }
                else if (isSwitchParam)
                {
                    // For switch parameters, simply remove them
                    elements.RemoveAt(i);
                    i--;
                    if (isPositionalParam && positionIndex > i)
                    {
                        positionIndex--;
                    }
                }
                else
                {
                    // For regular parameters, remove both the parameter name and value
                    elements.RemoveAt(i); // Remove the parameter name
                    if (i < elements.Count && !elements[i].StartsWith("-"))
                    {
                        elements.RemoveAt(i); // Remove the parameter value
                    }
                    i--;
                }
            }
        }

        // Get the positional parameter value from the remaining elements
        if (0 <= positionIndex && positionIndex < elements.Count)
        {
            return elements[positionIndex];
        }

        return null;
    }


    protected static List<OrchDriveInfo> ResolvePmDrives(IDictionary fakeBoundParameters)
    {
        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return SessionState.EnumPmDrives(paramPath);
    }

    protected static List<OrchDriveInfo> ResolveOrchDrives(IDictionary fakeBoundParameters)
    {
        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return SessionState.EnumOrchDrives(paramPath);
    }

    protected static List<OrchDuDriveInfo> ResolveDuDrives(IDictionary fakeBoundParameters)
    {
        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return SessionState.EnumDuDrives(paramPath);
    }

    // Do we not need a ResolveTmDrives()?

    protected static List<(OrchDriveInfo drive, Folder folder)> ResolvePath(CommandAst commandAst, IDictionary fakeBoundParameters, bool includeRoot = false)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
        var depth = ResolveDepth(fakeBoundParameters);

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return SessionState.EnumFolders(paramPath, recurse, depth, includeRoot);
    }

    protected static List<(OrchDuDriveInfo drive, DuProject project)> ResolveDuPath(CommandAst commandAst, IDictionary fakeBoundParameters) //, bool includeRoot = false)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return SessionState.EnumDuFolders(paramPath, recurse);
    }

    protected static List<(OrchDriveInfo drive, Folder folder)> ResolvePathWithoutPersonalWorkspace(CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
        var depth = ResolveDepth(fakeBoundParameters);

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);
    }

    /// <summary>
    /// Finds the AST text of the value currently being completed for the given parameter.
    /// For named parameters (-Name M*,), finds the value after -Name.
    /// For positional parameters (M*,), strips named params and finds the element containing commas.
    /// Returns null if no comma-separated values are found.
    /// </summary>
    private static string? FindCurrentParameterText(CommandAst commandAst, string parameterName)
    {
        // First try named parameter lookup
        var value = GetParameterValue(commandAst, parameterName);
        if (value is not null) return value;

        // Fallback for positional parameters:
        // Strip named parameters and their values, then find an element containing commas
        var elements = commandAst.CommandElements.Skip(1).Select(e => e.ToString()).ToList();
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].StartsWith('-'))
            {
                bool isSwitchParam = GetSwitchParameterValue(commandAst, elements[i].TrimStart('-').Split(':')[0]);
                elements.RemoveAt(i); // Remove param name
                if (!isSwitchParam && i < elements.Count && !elements[i].StartsWith("-"))
                    elements.RemoveAt(i); // Remove param value
                i--;
            }
        }

        // Among remaining positional elements, find the last one with commas
        // (the one being completed is typically the last positional argument)
        string? found = null;
        foreach (var elem in elements)
        {
            if (elem.Contains(',')) found = elem;
        }
        return found;
    }

    /// <summary>
    /// Gets already-entered comma-separated values for the parameter currently being completed.
    /// Works for both named (-Name M*,) and positional (M*,) parameters.
    /// </summary>
    protected static List<WildcardPattern>? CreateSelfExclusionList(CommandAst commandAst, string parameterName, string? wordToComplete)
    {
        var text = FindCurrentParameterText(commandAst, parameterName);
        if (text is null) return null;
        return SplitCommaSeparatedText(text.RemoveEnd(wordToComplete)).ConvertToWildcardPatternList();
    }

    /// <summary>
    /// Gets already-entered comma-separated values as strings (for Contains-based exclusion).
    /// </summary>
    protected static IEnumerable<string> GetSelfExclusionValues(CommandAst commandAst, string parameterName, string? wordToComplete)
    {
        var text = FindCurrentParameterText(commandAst, parameterName);
        if (text is null) return [];
        return SplitCommaSeparatedText(text.RemoveEnd(wordToComplete));
    }

    static internal string RemoveEnclosingQuotes(string? str)
    {
        if (!string.IsNullOrEmpty(str) && str.Length >= 2)
        {
            if (str.StartsWith('\'') && str.EndsWith('\''))
            {
                return str.Substring(1, str.Length - 2).Replace("''", "'");
            }
            else if (str.StartsWith('"') && str.EndsWith('"'))
            {
                return str.Substring(1, str.Length - 2);
            }
        }
        return str ?? "";
    }

    protected static WildcardPattern CreateWPFromWordToComplete(string? wordToComplete)
    {
        wordToComplete = RemoveEnclosingQuotes(wordToComplete);
        if (string.IsNullOrEmpty(wordToComplete)) wordToComplete = "*";

        string checker = wordToComplete.Replace("`*", "").Replace("`+", "");
        if (!checker.Contains('*') && !checker.Contains('?'))
            wordToComplete += '*';

        return new WildcardPattern(wordToComplete, WildcardOptions.IgnoreCase);
    }

    internal static string GenerateNewEntityName<T>(
        string newNamePrefix,
        IEnumerable<string>? specifiedNames,
        IEnumerable<T>? existingEntities,
        Func<T, string> getNameFunc)
    {
        int index = 1;
        string newName;
        while (true)
        {
            newName = $"{newNamePrefix}{index}";
            if ((specifiedNames?.Any(e => string.Compare(e, newName, StringComparison.OrdinalIgnoreCase) == 0) ?? false) ||
                (existingEntities?.Any(e => string.Compare(getNameFunc(e), newName, true) == 0) ?? false))
            {
                ++index;
                continue;
            }
            break;
        }
        // At this point, newName should be a suitable new name that does not already exist
        return newName;
    }

    protected static List<PmGroupMember> GetExistingMembers(List<OrchDriveInfo> drives, List<WildcardPattern>? wpGroupName)
    {
        var results = ParallelResults.GroupBy(drives, drive =>
        {
            var groups = drive.PmGroups.Get()
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name);
            return ParallelResults.ForEach(groups, group => drive.PmGroups.Get(group?.id));
        });

        List<PmGroupMember> existingMembers = [];
        foreach (var result in results)
        {
            foreach (var (group, groupDetailed) in result)
            {
                existingMembers.AddRange(groupDetailed.members ?? []);
            }
        }
        return existingMembers;
    }

    protected static string TipHelp(OrchDriveInfo drive)
    {
        string tiphelp = drive.DisplayRoot;
        if (!string.IsNullOrEmpty(drive.Description))
            tiphelp += $" ({drive.Description})";
        return tiphelp;
    }

    protected static string TipHelp(OrchDriveInfo drive, NuLicensedGroupMember member)
    {
        string tiphelp = member.GetPSPath(drive.NameColonSeparator);
        if (!string.IsNullOrEmpty(member.displayName))
            tiphelp += $" ({member.displayName})";
        return tiphelp;
    }

    protected static string TipHelp(Session entity)
    {
        //string driveName = drive.NameColon;
        //string tiphelp = drive.DisplayRoot;
        //if (!string.IsNullOrEmpty(drive.Description))
        //    tiphelp += $" ({drive.Description})";
        //return tiphelp;
        return entity.GetPSPath();
    }

    protected static string TipHelp(DirectoryObject entity)
    {
        string tiphelp = entity.type switch
        {
            0 => "DirectoryUser: ",
            1 => "DirectoryGroup: ",
            2 => "DirectoryMachine: ", // Is this correct?
            3 => "DirectoryRobot: ",
            4 => "DirectoryExternalApplication: ",
            _ => "unknown: "
        };

        tiphelp += (entity.Path + entity.identityName ?? entity.identifier);
        if (!string.IsNullOrEmpty(entity.displayName))
            tiphelp += $" ({entity.displayName})";
        return tiphelp;
    }

    protected static string TipHelp(PmGroupMember entity)
    {
        string tiphelp = $"{entity.name} ({entity.displayName})";
        return tiphelp;
    }

    protected static string TipHelp(MachineSessionRuntime entity)
    {
        string tiphelp = $"SID: {Path.Combine(entity.Path!, entity.SessionId?.ToString()!)}  HMN: '{entity.HostMachineName}'  SUN: '{entity.ServiceUserName}'";
        return tiphelp;
    }

    protected static string TipHelp(Webhook entity)
    {
        string tiphelp = entity.GetPSPath();
        //if (!string.IsNullOrEmpty(library.Description))
        //{
        //    tiphelp += $" ({library.Description})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(LibraryVersion version)
    {
        string tiphelp = $"{version.GetPSPath()}:{version.Version}";
        //if (!string.IsNullOrEmpty(version.Description))
        //{
        //    tiphelp += $" ({version.Description})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(Package package)
    {
        string tiphelp = $"{Path.Combine(package.Path!, package.Id!)}";
        //if (!string.IsNullOrEmpty(package.Description))
        //{
        //    tiphelp += $" ({package.Description})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(ExtendedMachine machine)
    {
        string tiphelp = $"{machine.Type} (UA{machine.UnattendedSlots} NP{machine.NonProductionSlots} TA{machine.TestAutomationSlots}) {Path.Combine(machine.Path!, machine.Name!)}";
        return tiphelp;
    }

    internal static string TipHelp(User user)
    {
        string tiphelp = user.UserName;
        if (!string.IsNullOrEmpty(user.FullName))
        {
            tiphelp += $" ({user.FullName})";
        }
        return tiphelp!;
    }

    protected static string TipHelp2(User user)
    {
        return (user.Type! + ':').PadRight(30) + Path.Combine(user.Path!, user.UserName!) + " (" + user.FullName + ")";
    }

    protected static string TipHelp(Robot robot)
    {
        //string tiphelp = $"{robot.User!.FullName} ({robot.Type})";
        string tiphelp = Path.Combine(robot.Path!, robot.User!.FullName!);
        //if (!string.IsNullOrEmpty(robot.User.Type))
        //{
        //    tiphelp += $" ({robot.User.Type})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(PersonalWorkspace personalWorkspace)
    {
        string tiphelp = Path.Combine(personalWorkspace.Path!, personalWorkspace.Name!);
        //if (!string.IsNullOrEmpty(personalWorkspace.OwnerName))
        //{
        //    tiphelp += $" ({personalWorkspace.OwnerName})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(Role role)
    {
        string tiphelp = role.GetPSPath();
        //if (!string.IsNullOrEmpty(role.Type))
        //{
        //    tiphelp += $" ({role.Type})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(TestCaseDefinition entities)
    {
        string tiphelp = entities.GetPSPath();
        //if (!string.IsNullOrEmpty(entities.PackageIdentifier))
        //{
        //    tiphelp += $" ({entities.PackageIdentifier})";
        //}
        return tiphelp;
    }

    // planned to be obsoleted
    protected static string TipHelp(MachineFolder machine)
    {
        //string tiphelp = $"(UA{machine.UnattendedSlots} NP{machine.NonProductionSlots} TA{machine.TestAutomationSlots}) {Path.Combine(machine.Path!, machine.Name!)}";
        string tiphelp = Path.Combine(machine.Path!, machine.Name!);
        return tiphelp;
    }

    protected static string TipHelp(UserRoles userRoles)
    {
        //string tiphelp = $"{userRoles.Id} {userRoles.UserEntity!.FullName} ({userRoles.UserEntity.UserName})";
        string tiphelp = $"{Path.Combine(userRoles.Path!, userRoles.UserEntity!.UserName!)}";
        //if (!string.IsNullOrEmpty(userRoles.UserEntity.FullName))
        //{
        //    tiphelp += $" ({userRoles.UserEntity.FullName})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(Release release)
    {
        string tiphelp = release.GetPSPath();
        //if (!string.IsNullOrEmpty(release.Description))
        //{
        //    tiphelp += $" ({release.Description})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(QueueDefinition queue)
    {
        string tiphelp = Path.Combine(queue.Path!, queue.Name!);
        //if (!string.IsNullOrEmpty(queue.Description))
        //{
        //    tiphelp += $" ({queue.Description})";
        //}
        return tiphelp;
    }

    public static string TipHelp(QueueItem item)
    {
        string tiphelp = item.GetPSPath();
        if (!string.IsNullOrEmpty(item.Reference))
        {
            tiphelp += $" ({item.Reference})";
        }
        return tiphelp;
    }

    protected static string TipHelp(Asset asset)
    {
        string tiphelp = Path.Combine(asset.Path!, asset.Name!);
        //if (!string.IsNullOrEmpty(asset.Description))
        //{
        //    tiphelp += $" ({asset.Description})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(CredentialStore credentialStore)
    {
        string tiphelp = credentialStore.GetPSPath();
        return tiphelp;
    }

    protected static string TipHelp(TestSet entity)
    {
        string tiphelp = entity.GetPSPath();
        //if (!string.IsNullOrEmpty(entity.Description))
        //{
        //    tiphelp += $" ({entity.Description})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(TestSetExecution entity)
    {
        string tiphelp = entity.GetPSPath();
        //if (!string.IsNullOrEmpty(entity?.TestSet?.Description))
        //{
        //    tiphelp += $" ({entity?.TestSet?.Description})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(TestCaseExecution entity)
    {
        string tiphelp = entity.Path ?? "";
        if (!string.IsNullOrEmpty(entity.EntryPointPath))
        {
            tiphelp += $" ({entity.EntryPointPath})";
        }
        return tiphelp;
    }

    protected static string TipHelp(TestSetSchedule entity)
    {
        string tiphelp = entity.GetPSPath();
        return tiphelp;
    }

    protected static string TipHelp(TestDataQueue entity)
    {
        string tiphelp = entity.GetPSPath();
        //if (!string.IsNullOrEmpty(entity?.Description))
        //{
        //    tiphelp += $" ({entity.Description})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(ProcessSchedule entity)
    {
        string tiphelp = entity.GetPSPath();
        //if (!string.IsNullOrEmpty(entity?.PackageName))
        //{
        //    tiphelp += $" ({entity?.PackageName})";
        //}
        return tiphelp;
    }

    protected static string TipHelp(BlobFile entity)
    {
        string tiphelp = entity.GetPSPath();
        return tiphelp;
    }

    protected static string TipHelp(OrchDriveInfo drive, PmUser entity)
    {
        string tiphelp = entity.GetPSPath(drive.NameColonSeparator);
        string username = (string.Join(" ", [entity.name, entity.surname])).Trim();
        if (!string.IsNullOrEmpty(username))
            tiphelp += $" ({username})";
        return tiphelp;
    }

    protected static string TipHelp(OrchDriveInfo drive, PmRobotAccount entity)
    {
        string tiphelp = entity.GetPSPath(drive.NameColonSeparator);
        return tiphelp;
    }

    // Matches comma-separated tokens, where single-quoted segments preserve embedded commas.
    [GeneratedRegex(@"(?:[^',]+|'[^']*')+")]
    private static partial Regex CommaSeparatedTokenRegex();
}

/// <summary>
/// Base class for completers that resolve entities within folder scope (ResolvePath).
/// </summary>
internal abstract class FolderScopedCompleter<TEntity> : OrchArgumentCompleter
{
    protected abstract IEnumerable<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract string GetName(TEntity entity);
    protected virtual string GetTipHelp(TEntity entity) => GetName(entity);
    protected virtual CompletionResultType ResultType => CompletionResultType.ParameterValue;

    protected virtual List<(OrchDriveInfo drive, Folder folder)> ResolveFolders(
        CommandAst commandAst, IDictionary fakeBoundParameters)
        => ResolvePath(commandAst, fakeBoundParameters);

    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName, string parameterName, string wordToComplete,
        CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolveFolders(commandAst, fakeBoundParameters);
        var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drivesFolders, df => GetEntities(df.drive, df.folder));

        foreach (var result in results)
        {
            foreach (var entity in result
                .Where(e => wp.IsMatch(GetName(e)))
                .ExcludeByWildcards(e => GetName(e!), wpName)
                .OrderBy(e => GetName(e)))
            {
                string name = GetName(entity);
                yield return new CompletionResult(PathTools.EscapePSText(name), name, ResultType, GetTipHelp(entity));
            }
        }
    }
}

/// <summary>
/// Base class for completers that resolve entities within drive scope (ResolveOrchDrives by default).
/// Override <see cref="ResolveDrives"/> to switch to a different drive resolver (e.g. ResolvePmDrives).
/// </summary>
public abstract class DriveScopedCompleter<TEntity> : OrchArgumentCompleter
{
    protected abstract IEnumerable<TEntity> GetEntities(OrchDriveInfo drive);
    protected abstract string GetName(TEntity entity);
    protected virtual string GetTipHelp(OrchDriveInfo drive, TEntity entity) => GetName(entity);
    protected virtual CompletionResultType ResultType => CompletionResultType.ParameterValue;
    protected virtual string? NotFoundMessage => null;
    protected virtual List<OrchDriveInfo> ResolveDrives(IDictionary fakeBoundParameters)
        => ResolveOrchDrives(fakeBoundParameters);

    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName, string parameterName, string wordToComplete,
        CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        var drives = ResolveDrives(fakeBoundParameters);
        var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drives, drive => GetEntities(drive));

        bool bFound = false;
        foreach (var result in results)
        {
            foreach (var entity in result
                .Where(e => wp.IsMatch(GetName(e)))
                .ExcludeByWildcards(e => GetName(e!), wpName)
                .OrderBy(e => GetName(e)))
            {
                bFound = true;
                string name = GetName(entity);
                yield return new CompletionResult(PathTools.EscapePSText(name), name, ResultType, GetTipHelp(result.Source, entity));
            }
        }

        if (!bFound && NotFoundMessage is not null)
        {
            yield return new CompletionResult($@"""({NotFoundMessage} '{RemoveEnclosingQuotes(wordToComplete)}')""");
        }
    }
}

internal class ActionCatalogNameCompleter : FolderScopedCompleter<TaskCatalog>
{
    protected override IEnumerable<TaskCatalog> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.ActionCatalogs.Get(folder);
    protected override string GetName(TaskCatalog e) => e.Name!;
    protected override string GetTipHelp(TaskCatalog e) => e.GetPSPath();
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

internal class ApiTriggerNameCompleter : FolderScopedCompleter<HttpTrigger>
{
    protected override IEnumerable<HttpTrigger> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.ApiTriggers.Get(folder);
    protected override string GetName(HttpTrigger e) => e.Name!;
    protected override string GetTipHelp(HttpTrigger e) => e.GetPSPath();
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

internal class EventTriggerNameCompleter : FolderScopedCompleter<ApiTrigger>
{
    protected override IEnumerable<ApiTrigger> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.EventTriggers.Get(folder);
    protected override string GetName(ApiTrigger e) => e.Name!;
    protected override string GetTipHelp(ApiTrigger e) => e.GetPSPath();
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

internal class AssetNameCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

        // Exclude Names already selected via the parameter
        var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        // Only target the ValueType selected via the parameter
        var wpValueType = GetFakeBoundParameters(fakeBoundParameters, "ValueType").ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var asset in result
                .Where(a => wp.IsMatch(a.Name))
                .FilterByWildcards(a => a?.ValueType, wpValueType)
                .ExcludeByWildcards(a => a?.Name, wpName)
                .OrderBy(a => a.Name))
            {
                string toolhelp = asset.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(asset.Name), asset.Name, CompletionResultType.Text, toolhelp);
            }
        }
    }
}

internal class AssetValueTypeCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

        var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
        var wpValueType = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

        HashSet<string> valueTypes = [];
        foreach (var result in results)
        {
            foreach (var asset in result
                .FilterByWildcards(a => a?.Name, wpName))
            {
                valueTypes.Add(asset.ValueType!);
            }
        }

        foreach (var valueType in valueTypes
            .Where(v => wp.IsMatch(v))
            .ExcludeByWildcards(v => v, wpValueType)
            .OrderBy(v => v))
        {
            yield return new CompletionResult(valueType);
        }
    }
}

/// <summary>
/// Shared scaffolding for completers that enumerate entities in a folder and
/// probe each one's AccessibleFoldersDto link visibility. Used by the
/// Remove-Orch{Asset|Bucket|Queue}Link completers so they can share a single
/// parallel enumeration / probe pipeline.
/// </summary>
internal abstract class EntityLinkCompleterBase<TEntity> : OrchArgumentCompleter
    where TEntity : class
{
    protected abstract IEnumerable<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract AccessibleFoldersDto? GetLinks(OrchDriveInfo drive, Folder folder, TEntity entity);
    protected abstract string GetName(TEntity entity);

    /// <summary>
    /// For each (drive, folder) in scope, enumerates entities in parallel,
    /// applies <paramref name="entityFilter"/>, then probes each surviving
    /// entity's links in parallel. Yields tuples whose AccessibleFolders is
    /// non-null; failures from GetLinks are swallowed (completers must not
    /// surface errors).
    /// </summary>
    protected IEnumerable<(OrchDriveInfo drive, Folder folder, TEntity entity, AccessibleFoldersDto links)>
        ProbeEntityLinks(
            List<(OrchDriveInfo drive, Folder folder)> drivesFolders,
            Func<TEntity, bool> entityFilter)
    {
        var grouped = ParallelResults.GroupBy(drivesFolders, df => GetEntities(df.drive, df.folder));

        foreach (var group in grouped)
        {
            var (drive, folder) = group.Source;
            var matched = group.Where(entityFilter).OrderBy(GetName).ToList();

            var probed = ParallelResults.ForEach(matched, e =>
            {
                try { return GetLinks(drive, folder, e); }
                catch { return null; }
            });

            foreach (var ws in probed)
            {
                if (ws.Item.AccessibleFolders is null) continue;
                yield return (drive, folder, ws.Source, ws.Item);
            }
        }
    }
}

/// <summary>
/// Completer for the entity-name parameter of Remove-Orch{Asset|Bucket|Queue}Link:
/// only enumerates entities that are currently linked to at least one folder
/// other than their home folder. Plain *NameCompleter would happily list every
/// entity and most would no-op when the cmdlet actually ran.
/// </summary>
internal abstract class LinkedEntityNameCompleter<TEntity> : EntityLinkCompleterBase<TEntity>
    where TEntity : class
{
    protected abstract string GetTipHelp(TEntity entity);

    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
        var wpSelfExcl = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        bool Match(TEntity e)
        {
            var name = GetName(e);
            if (!wp.IsMatch(name)) return false;
            if (wpSelfExcl is { Count: > 0 } && wpSelfExcl.Any(p => p.IsMatch(name))) return false;
            return true;
        }

        foreach (var (_, _, entity, links) in ProbeEntityLinks(drivesFolders, Match))
        {
            // AccessibleFolders always includes the entity's home folder; Length > 1
            // means at least one real link exists (same predicate used by
            // Get-Orch{Asset|Bucket|Queue}Link).
            if (links.AccessibleFolders is not { Length: > 1 }) continue;
            var name = GetName(entity);
            yield return new CompletionResult(
                PathTools.EscapePSText(name), name,
                CompletionResultType.Text, GetTipHelp(entity));
        }
    }
}

/// <summary>
/// Completer for the -Link parameter of Remove-Orch{Asset|Bucket|Queue}Link:
/// lists folder PSPaths the entity specified via -Name is currently linked to
/// (excluding the entity's own home folder). When -Name isn't bound yet, falls
/// back to the union of link folders for every entity in the source folder.
/// </summary>
internal abstract class EntityLinkFolderCompleter<TEntity> : EntityLinkCompleterBase<TEntity>
    where TEntity : class
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);
        var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();
        var wpSelfExcl = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        bool Match(TEntity e) =>
            wpName is null || wpName.Count == 0 || wpName.Any(p => p.IsMatch(GetName(e)));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (drive, folder, _, links) in ProbeEntityLinks(drivesFolders, Match))
        {
            Int64 srcId = folder.Id ?? 0;
            foreach (var linkFolder in links.AccessibleFolders!.OrderBy(f => f.FullyQualifiedName))
            {
                if ((linkFolder.Id ?? 0) == srcId) continue;
                var fqn = (linkFolder.FullyQualifiedName ?? "").Replace('/', Path.DirectorySeparatorChar);
                string psPath = drive.NameColonSeparator + fqn;
                if (!wp.IsMatch(psPath)) continue;
                if (wpSelfExcl is { Count: > 0 } && wpSelfExcl.Any(p => p.IsMatch(psPath))) continue;
                if (!seen.Add(psPath)) continue;
                yield return new CompletionResult(
                    PathTools.EscapePSText(psPath), psPath,
                    CompletionResultType.ParameterValue, psPath);
            }
        }
    }
}

internal class LinkedAssetNameCompleter : LinkedEntityNameCompleter<Asset>
{
    protected override IEnumerable<Asset> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Assets.Get(folder);
    protected override AccessibleFoldersDto? GetLinks(OrchDriveInfo drive, Folder folder, Asset e) => drive.GetFoldersForAsset(folder, e);
    protected override string GetName(Asset e) => e.Name!;
    protected override string GetTipHelp(Asset e) => e.GetPSPath();
}

internal class AssetLinkFolderCompleter : EntityLinkFolderCompleter<Asset>
{
    protected override IEnumerable<Asset> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Assets.Get(folder);
    protected override AccessibleFoldersDto? GetLinks(OrchDriveInfo drive, Folder folder, Asset e) => drive.GetFoldersForAsset(folder, e);
    protected override string GetName(Asset e) => e.Name!;
}

internal class LinkedBucketNameCompleter : LinkedEntityNameCompleter<Bucket>
{
    protected override IEnumerable<Bucket> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Buckets.Get(folder);
    protected override AccessibleFoldersDto? GetLinks(OrchDriveInfo drive, Folder folder, Bucket e) => drive.GetFoldersForBucket(folder, e);
    protected override string GetName(Bucket e) => e.Name!;
    protected override string GetTipHelp(Bucket e) => e.GetPSPath();
}

internal class BucketLinkFolderCompleter : EntityLinkFolderCompleter<Bucket>
{
    protected override IEnumerable<Bucket> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Buckets.Get(folder);
    protected override AccessibleFoldersDto? GetLinks(OrchDriveInfo drive, Folder folder, Bucket e) => drive.GetFoldersForBucket(folder, e);
    protected override string GetName(Bucket e) => e.Name!;
}

internal class LinkedQueueNameCompleter : LinkedEntityNameCompleter<QueueDefinition>
{
    protected override IEnumerable<QueueDefinition> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Queues.Get(folder);
    protected override AccessibleFoldersDto? GetLinks(OrchDriveInfo drive, Folder folder, QueueDefinition e) => drive.GetFoldersForQueue(folder, e);
    protected override string GetName(QueueDefinition e) => e.Name!;
    protected override string GetTipHelp(QueueDefinition e) => e.GetPSPath();
}

internal class QueueLinkFolderCompleter : EntityLinkFolderCompleter<QueueDefinition>
{
    protected override IEnumerable<QueueDefinition> GetEntities(OrchDriveInfo drive, Folder folder) => drive.Queues.Get(folder);
    protected override AccessibleFoldersDto? GetLinks(OrchDriveInfo drive, Folder folder, QueueDefinition e) => drive.GetFoldersForQueue(folder, e);
    protected override string GetName(QueueDefinition e) => e.Name!;
}

internal class BucketNameCompleter<WritableOnly> : FolderScopedCompleter<Bucket> where WritableOnly : IBoolParameter
{
    protected override IEnumerable<Bucket> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.Buckets.Get(folder)
            .Where(b => !WritableOnly.Value || !(b.Options?.Contains("ReadOnly") ?? false));
    protected override string GetName(Bucket e) => e.Name!;
    protected override string GetTipHelp(Bucket e) => e.GetPSPath();
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

internal class BucketFullPathCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

        // Only target Names selected via the parameter
        var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

        // Exclude FullPaths already selected via the parameter
        var wpFullPath = GetFakeBoundParameters(fakeBoundParameters, "FullPath").ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drivesFolders, df =>
        {
            var buckets = df.drive.Buckets.Get(df.folder).FilterByWildcards(e => e?.Name, wpName);
            return ParallelResults.GroupBy(buckets, bucket =>
                df.drive.BucketFiles.Get(df.folder, bucket));
        });

        foreach (var result in results)
        {
            foreach (var bucket in result)
            {
                foreach (var item in bucket
                    .Where(e => wp.IsMatch(e.FullPath))
                    .ExcludeByWildcards(e => e?.FullPath, wpFullPath)
                    .OrderBy(e => e.FullPath))
                {
                    string tiphelp = TipHelp(item);
                    yield return new CompletionResult(PathTools.EscapePSText(item.FullPath), item.FullPath, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }
}

internal class CalendarNameCompleter : DriveScopedCompleter<ExtendedCalendar>
{
    protected override IEnumerable<ExtendedCalendar> GetEntities(OrchDriveInfo drive)
        => drive.Calendars.Get();
    protected override string GetName(ExtendedCalendar e) => e.Name!;
    protected override string GetTipHelp(OrchDriveInfo drive, ExtendedCalendar e) => e.GetPSPath();
    protected override CompletionResultType ResultType => CompletionResultType.Text;
    protected override string? NotFoundMessage => "No calendars found for";
}

internal class CredentialStoreNameCompleter : DriveScopedCompleter<CredentialStore>
{
    protected override IEnumerable<CredentialStore> GetEntities(OrchDriveInfo drive)
        => drive.CredentialStores.Get();
    protected override string GetName(CredentialStore e) => e.Name!;
    protected override string GetTipHelp(OrchDriveInfo drive, CredentialStore e) => TipHelp(e);
    protected override string? NotFoundMessage => "No credential stores found for";
}

internal class FolderMachineNameCompleter : FolderScopedCompleter<MachineFolder>
{
    protected override IEnumerable<MachineFolder> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.FolderMachinesAssigned.Get(folder);
    protected override string GetName(MachineFolder e) => e.Name!;
    protected override string GetTipHelp(MachineFolder e) => TipHelp(e);
}

internal class MachineNameCompleter : DriveScopedCompleter<ExtendedMachine>
{
    protected override IEnumerable<ExtendedMachine> GetEntities(OrchDriveInfo drive)
        => drive.Machines.Get();
    protected override string GetName(ExtendedMachine e) => e.Name!;
    protected override string GetTipHelp(OrchDriveInfo drive, ExtendedMachine e) => TipHelp(e);
    protected override string? NotFoundMessage => "No machines found for";
}

internal class MachineRobotUsersCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolveOrchDrives(fakeBoundParameters);

        // Exclude Names already selected via the parameter
        var wpRobotUsers = GetFakeBoundParameters(fakeBoundParameters, "RobotUsers").ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drives, drive => drive.AllRobotsAcrossFolders.Get());

        foreach (var result in results)
        {
            foreach (var robot in result
                .Where(r => wp.IsMatch(r?.User?.FullName))
                .ExcludeByWildcards(p => p?.User?.FullName, wpRobotUsers)
                .OrderBy(p => p.User?.FullName))
            {
                yield return new CompletionResult(PathTools.EscapePSText(robot.User?.FullName), robot.User?.FullName, CompletionResultType.Text, robot.TipHelp());
            }
        }
    }
}

internal class LibraryIdCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolveOrchDrives(fakeBoundParameters);
        var hostFeed = ResolveSwitchParameter(fakeBoundParameters, "HostFeed");

        // Exclude Ids already selected via the parameter
        var wpId = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        // Only target Versions selected via the parameter
        var wpVersion = GetFakeBoundParameters(fakeBoundParameters, "Version").ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drives, drive => hostFeed ? drive.LibrariesInHost.Get() : drive.LibrariesInTenant.Get());

        foreach (var result in results)
        {
            foreach (var library in result
                .Where(l => wp.IsMatch(l.Id))
                .ExcludeByWildcards(l => l?.Id, wpId)
                .FilterByWildcards(l => l?.Version, wpVersion)
                .OrderBy(l => l.Id))
            {
                yield return new CompletionResult(PathTools.EscapePSText(library.Id), library.Id, CompletionResultType.ParameterValue, library.TipHelp());
            }
        }
    }
}

internal class LibraryVersionCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolveOrchDrives(fakeBoundParameters);
        var hostFeed = ResolveSwitchParameter(fakeBoundParameters, "HostFeed");

        // Exclude Ids already selected via the parameter
        var wpId = GetFakeBoundParameters(fakeBoundParameters, "Id").ConvertToWildcardPatternList();

        // Exclude Versions already selected via the parameter
        var wpVersion = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drives, drive =>
        {
            var libraries = hostFeed ?
                drive.LibrariesInHost.Get().FilterByWildcards(l => l?.Id, wpId) :
                drive.LibrariesInTenant.Get().FilterByWildcards(l => l?.Id, wpId);

            return ParallelResults.GroupBy(libraries, library => hostFeed ?
                drive.LibraryVersionsInHostFeed.Get(library.Id!) :
                drive.LibraryVersions.Get(library.Id!));
        });

        foreach (var libraries in results)
        {
            foreach (var library in libraries)
            {
                foreach (var version in library
                    .Where(v => wp.IsMatch(v.Version))
                    .ExcludeByWildcards(v => v?.Version, wpVersion))
                //.OrderBy(v => v.Item.Item.Version!, VersionComparer.Instance))
                {
                    string tiphelp = TipHelp(version);
                    yield return new CompletionResult(PathTools.EscapePSText(version.Version), version.Version, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }
}

internal class PackageIdCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);

        // Exclude Ids already selected via the parameter
        var wpId = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drivesFolders, df => df.drive.GetPackages(df.folder));

        foreach (var result in results)
        {
            foreach (var package in result
                .Where(m => wp.IsMatch(m.Id))
                .ExcludeByWildcards(p => p?.Id, wpId)
                .OrderBy(l => l.Id))
            {
                string tiphelp = TipHelp(package);
                yield return new CompletionResult(PathTools.EscapePSText(package.Id), package.Id, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}


internal class PackageVersionCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);

        // Only target Ids selected via the parameter
        var wpId = GetFakeBoundParameters(fakeBoundParameters, "Id").ConvertToWildcardPatternList();

        // Exclude Versions already selected via the parameter
        var wpVersion = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drivesFolders, df =>
        {
            var packages = df.drive.GetPackages(df.folder).FilterByWildcards(p => p?.Id, wpId);
            return ParallelResults.GroupBy(packages, package => df.drive.GetPackageVersions(df.folder, package.Id!));
        });

        foreach (var result in results)
        {
            foreach (var versions in result)
            {
                foreach (var version in versions
                    .Where(v => wp.IsMatch(v.Version))
                    .ExcludeByWildcards(v => v?.Version, wpVersion))
                {
                    string tiphelp = TipHelp(version);
                    yield return new CompletionResult(PathTools.EscapePSText(version.Version), version.Version, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }
}

internal class ProcessNameCompleter : FolderScopedCompleter<Release>
{
    protected override IEnumerable<Release> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.Releases.Get(folder);
    protected override string GetName(Release e) => e.Name!;
    protected override string GetTipHelp(Release e) => TipHelp(e);
}

internal class QueueNameCompleter : FolderScopedCompleter<QueueDefinition>
{
    protected override IEnumerable<QueueDefinition> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.Queues.Get(folder);
    protected override string GetName(QueueDefinition e) => e.Name!;
    protected override string GetTipHelp(QueueDefinition e) => TipHelp(e);
}

// Job Id completer shared by Get-OrchJob and Open-OrchJob (both surface the full Jobs cache).
// Stop-OrchJob uses StoppableJobs and Restart-OrchJob uses FaultedJobs, so they keep their own
// nested completers.
internal class JobIdCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

        // Exclude Ids that have already been selected via parameters
        var paramId = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var (drive, folder) in drivesFolders)
        {
            var jobs = drive.Jobs.GetCache(folder);
            if (jobs is null) continue;

            foreach (var job in jobs.Values.ExcludeByClassValues(j => (j?.Id ?? 0).ToString(), paramId))
            {
                if (!wp.IsMatch((job.Id ?? 0).ToString()))
                    continue;

                yield return new CompletionResult(job.Id.ToString(), job.Id.ToString(), CompletionResultType.ParameterValue, job.FormatTooltip());
            }
        }
    }
}

internal class ListReleasesCompleter : FolderScopedCompleter<Release>
{
    protected override IEnumerable<Release> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.Releases.Get(folder);
    protected override string GetName(Release e) => e.Name!;
    protected override string GetTipHelp(Release e) => TipHelp(e);
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

internal class RoleNameCompleter : DriveScopedCompleter<Role>
{
    protected override IEnumerable<Role> GetEntities(OrchDriveInfo drive)
        => drive.Roles.Get();
    protected override string GetName(Role e) => e.Name!;
    protected override string GetTipHelp(OrchDriveInfo drive, Role e) => TipHelp(e);
}

// Common base for the symmetric pair of completers that suggest values from the
// tenant user list. UserName and FullName completers differ only in which field
// is "self" (the parameter being completed, used for self-exclusion + display)
// and which is the "other" side (already-selected values used as a positive filter).
public abstract class TenantUserDualFieldCompleterBase : OrchArgumentCompleter
{
    protected abstract string SelfFieldName { get; }    // e.g. "UserName" — same as parameterName, also the matching field
    protected abstract string OtherFieldName { get; }   // e.g. "FullName" — sister parameter for cross-filter
    protected abstract Func<User, string?> SelfField { get; }
    protected abstract Func<User, string?> OtherField { get; }

    public sealed override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolveOrchDrives(fakeBoundParameters);

        // Exclude self-field values already selected via the parameter
        var wpSelf = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        // Only target other-field values selected via the sister parameter
        var wpOther = GetFakeBoundParameters(fakeBoundParameters, OtherFieldName).ConvertToWildcardPatternList();

        var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);
        var selfField = SelfField;
        var otherField = OtherField;

        var results = ParallelResults.GroupBy(drives, drive => drive.Users.Get());

        foreach (var result in results)
        {
            foreach (var user in result
                .Where(u => wp.IsMatch(selfField(u)))
                .ExcludeByWildcards(u => u is null ? null : selfField(u), wpSelf)
                .FilterByWildcards(u => u is null ? null : otherField(u), wpOther)
                .FilterByWildcards(u => u?.Type, wpType)
                .OrderBy(u => selfField(u)))
            {
                string tiphelp = TipHelp2(user);
                string value = selfField(user) ?? "";
                yield return new CompletionResult(PathTools.EscapePSText(value), value, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

public class TenantUserUserNameCompleter : TenantUserDualFieldCompleterBase
{
    protected override string SelfFieldName => "UserName";
    protected override string OtherFieldName => "FullName";
    protected override Func<User, string?> SelfField => u => u.UserName;
    protected override Func<User, string?> OtherField => u => u.FullName;
}

public class TenantUserFullNameCompleter : TenantUserDualFieldCompleterBase
{
    protected override string SelfFieldName => "FullName";
    protected override string OtherFieldName => "UserName";
    protected override Func<User, string?> SelfField => u => u.FullName;
    protected override Func<User, string?> OtherField => u => u.UserName;
}

internal class TimeZoneCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var timeZone in TimeZoneInfo.GetSystemTimeZones()
            .Where(t => wp.IsMatch(t.DisplayName)))
        {
            string tiphelp = $"{timeZone.DisplayName} (Id = '{timeZone.Id}')";
            yield return new CompletionResult(PathTools.EscapePSText(timeZone.DisplayName), timeZone.DisplayName, CompletionResultType.ParameterValue, tiphelp);
        }
    }
}

// Sister completer for parameters that bind to a TimeZoneInfo.Id value
// (e.g. "Tokyo Standard Time" / "Asia/Tokyo"), not the human DisplayName.
// Use this on `-TimeZoneId`-named parameters; use TimeZoneCompleter on
// `-TimeZone`-named ones that get resolved name→id at submit time.
internal class TimeZoneIdCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var timeZone in TimeZoneInfo.GetSystemTimeZones()
            .Where(t => wp.IsMatch(t.Id) || wp.IsMatch(t.DisplayName)))
        {
            string tiphelp = $"{timeZone.DisplayName} (Id = '{timeZone.Id}')";
            yield return new CompletionResult(PathTools.EscapePSText(timeZone.Id), timeZone.Id, CompletionResultType.ParameterValue, tiphelp);
        }
    }
}

internal class TriggerNameCompleter : FolderScopedCompleter<ProcessSchedule>
{
    protected override IEnumerable<ProcessSchedule> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.GetTriggers(folder);
    protected override string GetName(ProcessSchedule e) => e.Name!;
    protected override string GetTipHelp(ProcessSchedule e) => TipHelp(e);
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

internal class UpdatePolicyVersionCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drives = SessionState.EnumOrchDrives(paramPath);

        foreach (var drive in drives)
        {
            var versions = drive.AvailableVersions.Get();

            foreach (var version in versions ?? [])
            {
                string tiphelp = System.IO.Path.Combine(drive.NameColonSeparator, version);
                yield return new CompletionResult(PathTools.EscapePSText(version), version, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

internal class WebhookNameCompleter : DriveScopedCompleter<Webhook>
{
    protected override IEnumerable<Webhook> GetEntities(OrchDriveInfo drive)
        => drive.Webhooks.Get();
    protected override string GetName(Webhook e) => e.Name!;
    protected override string GetTipHelp(OrchDriveInfo drive, Webhook e) => TipHelp(e);
}

internal class WebhookEventTypeNameCompleter : DriveScopedCompleter<WebhookEventType>
{
    protected override IEnumerable<WebhookEventType> GetEntities(OrchDriveInfo drive)
        => drive.WebhookEventTypes.Get();
    protected override string GetName(WebhookEventType e) => e.Name!;
    protected override string GetTipHelp(OrchDriveInfo drive, WebhookEventType e) => $"{e.Group}: {e.Name}";
}

internal class TaskIdCompleter : FolderScopedCompleter<OrchTask>
{
    protected override IEnumerable<OrchTask> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.Tasks.Get(folder);
    protected override string GetName(OrchTask e) => (e.Id ?? 0).ToString();
    protected override string GetTipHelp(OrchTask e) => $"[{e.Status}/{e.Priority}] {e.Title}";
    protected override CompletionResultType ResultType => CompletionResultType.ParameterValue;
}

internal class TaskTitleCompleter : FolderScopedCompleter<OrchTask>
{
    protected override IEnumerable<OrchTask> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.Tasks.Get(folder).Where(t => !string.IsNullOrEmpty(t.Title));
    protected override string GetName(OrchTask e) => e.Title!;
    protected override string GetTipHelp(OrchTask e) => $"{e.Id} [{e.Status}/{e.Priority}] {e.GetPSPath()}";
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

internal class PmDirectoryNameCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        if (string.IsNullOrEmpty(wordToComplete))
        {
            yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
            yield break;
        }

        var entityType = GetFakeBoundParameter(fakeBoundParameters, "EntityType");
        string kind = entityType?.ToLower() switch
        {
            "user" => "DirectoryUser",
            "group" => "DirectoryGroup",
            "application" => "Application",
            _ => null
        };
        if (kind is null) yield break;

        var names = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

        var drives = ResolvePmDrives(fakeBoundParameters);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        wordToComplete = RemoveEnclosingQuotes(wordToComplete);
        var results = ParallelResults.GroupBy(drives, drive => drive.SearchPmDirectoryCache.Get(RemoveEnclosingQuotes(wordToComplete).ToLower()));

        foreach (var result in results)
        {
            foreach (var entityInfo in result
                .Where(s => !names.Contains(s.identityName)) // Exclude already-entered items
                .Where(s => s.objectType == kind)
                .OrderBy(s => s.identityName))
            {
                var drive = result.Source;
                string tiphelp = drive.NameColonSeparator + entityInfo.identityName;
                yield return new CompletionResult(PathTools.EscapePSText(entityInfo.identityName), entityInfo.identityName, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

internal class PmDirectoryNameCompleter4Du : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        if (string.IsNullOrEmpty(wordToComplete))
        {
            yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
            yield break;
        }

        var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();
        var types = DirectoryTypes.Items.FilterByWildcards(p => p, wpType);
        types = types.Select(t => t == "DirectoryApplication" ? "Application" : t);

        var names = GetSelfExclusionValues(commandAst, parameterName, wordToComplete);

        var drives = ResolveDuDrives(fakeBoundParameters);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        wordToComplete = RemoveEnclosingQuotes(wordToComplete);
        var results = ParallelResults.GroupBy(drives, drive => drive.ParentDrive.SearchPmDirectoryCache.Get(wordToComplete.ToLower()));

        foreach (var result in results)
        {
            var drive = result.Source;
            foreach (var entityInfo in result
                .Where(s => !names.Contains(s.identityName)) // Exclude already-entered items
                .Where(s => types.Contains(s?.objectType))
                .OrderBy(s => s.identityName))
            {
                string tiphelp = drive.NameColonSeparator + entityInfo.identityName;
                string name = !string.IsNullOrEmpty(entityInfo.email) ? entityInfo.email : entityInfo.identityName;
                yield return new CompletionResult(PathTools.EscapePSText(name), name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

internal class UserNameInPmGroupCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolvePmDrives(fakeBoundParameters);

        // Exclude UserNames already selected via the parameter
        var wpUserName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);

        // Get existing members of the specified group
        var wpGroupName = GetFakeBoundParameters(fakeBoundParameters, "GroupName").ConvertToWildcardPatternList();
        var existingMemberIds = GetExistingMembers(drives, wpGroupName);

        // Get details for each group
        var results = ParallelResults.GroupBy(drives, drive =>
        {
            var groups = drive.PmGroups.Get()
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name);
            return ParallelResults.ForEach(groups, group => drive.PmGroups.Get(group?.id));
        });

        // Collect DirectoryUsers that are members of the groups
        List<PmGroupMember> users = [];
        foreach (var result in results)
        {
            foreach (var (group, groupDetailed) in result)
            {
                foreach (var member in groupDetailed.members?
                    .FilterByWildcards(m => m?.objectType, wpType) ?? [])
                {
                    users.Add(member);
                }
            }
        }

        // Display matching DirectoryUsers as completion candidates
        foreach (var user in users
            .Where(e => wp.IsMatch(e?.name))
            .ExcludeByWildcards(e => e?.name!, wpUserName)
            .OrderBy(e => e?.name))
        {
            string tiphelp = TipHelp(user);
            yield return new CompletionResult(PathTools.EscapePSText(user.name), user.name, CompletionResultType.Text, tiphelp);
        }
    }
}

internal class TypeInPmGroupCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolvePmDrives(fakeBoundParameters);

        // Exclude UserNames already selected via the parameter
        var wpType = GetFakeBoundParameters(fakeBoundParameters, "Type").ConvertToWildcardPatternList();
        var wpUserName = GetFakeBoundParameters(fakeBoundParameters, "UserName").ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);

        // Get existing members of the specified group
        var wpGroupName = GetFakeBoundParameters(fakeBoundParameters, "GroupName").ConvertToWildcardPatternList();
        var existingMembers = GetExistingMembers(drives, wpGroupName);

        foreach (var member in existingMembers
            .Where(e => wp.IsMatch(e?.name))
            .FilterByWildcards(m => m?.name, wpUserName)
            .ExcludeByWildcards(m => m?.objectType, wpType)
            .OrderBy(m => m.objectType))
        {
            //string tiphelp = TipHelp(member);
            yield return new CompletionResult(PathTools.EscapePSText(member.objectType), member.objectType, CompletionResultType.Text, member.objectType);
        }
    }
}

internal class ExternalApplicationNameCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolvePmDrives(fakeBoundParameters);

        // Exclude Names already selected via the parameter
        var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drives, drive => drive.PmExternalClients.Get());

        foreach (var result in results)
        {
            foreach (var client in result
                .Where(a => wp.IsMatch(a?.name))
                .ExcludeByWildcards(a => a?.name!, wpName)
                .OrderBy(a => a?.name))
            {
                string tooltip = client?.GetPSPath(result.Source.NameColonSeparator);
                yield return new CompletionResult(PathTools.EscapePSText(client?.name), client?.name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class TestCaseNameCompleter : FolderScopedCompleter<TestCaseDefinition>
{
    protected override List<(OrchDriveInfo drive, Folder folder)> ResolveFolders(
        CommandAst commandAst, IDictionary fakeBoundParameters)
        => ResolvePathWithoutPersonalWorkspace(commandAst, fakeBoundParameters);
    protected override IEnumerable<TestCaseDefinition> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.TestCases.Get(folder);
    protected override string GetName(TestCaseDefinition e) => e.Name!;
    protected override string GetTipHelp(TestCaseDefinition e) => TipHelp(e);
}

internal class TestDataQueueNameCompleter : FolderScopedCompleter<TestDataQueue>
{
    protected override List<(OrchDriveInfo drive, Folder folder)> ResolveFolders(
        CommandAst commandAst, IDictionary fakeBoundParameters)
        => ResolvePathWithoutPersonalWorkspace(commandAst, fakeBoundParameters);
    protected override IEnumerable<TestDataQueue> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.TestDataQueues.Get(folder);
    protected override string GetName(TestDataQueue e) => e.Name!;
    protected override string GetTipHelp(TestDataQueue e) => TipHelp(e);
}

internal class TestScheduleNameCompleter : FolderScopedCompleter<TestSetSchedule>
{
    protected override List<(OrchDriveInfo drive, Folder folder)> ResolveFolders(
        CommandAst commandAst, IDictionary fakeBoundParameters)
        => ResolvePathWithoutPersonalWorkspace(commandAst, fakeBoundParameters);
    protected override IEnumerable<TestSetSchedule> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.TestSetSchedules.Get(folder);
    protected override string GetName(TestSetSchedule e) => e.Name!;
    protected override string GetTipHelp(TestSetSchedule e) => TipHelp(e);
}

internal class TestSetNameCompleter : FolderScopedCompleter<TestSet>
{
    protected override List<(OrchDriveInfo drive, Folder folder)> ResolveFolders(
        CommandAst commandAst, IDictionary fakeBoundParameters)
        => ResolvePathWithoutPersonalWorkspace(commandAst, fakeBoundParameters);
    protected override IEnumerable<TestSet> GetEntities(OrchDriveInfo drive, Folder folder)
        => drive.TestSets.Get(folder);
    protected override string GetName(TestSet e) => e.Name!;
    protected override string GetTipHelp(TestSet e) => TipHelp(e);
}

/// <summary>
/// Completer that retrieves name list from the TestSetExecution cache
/// </summary>
internal class TestSetExecutionNameCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
        var depth = ResolveDepth(fakeBoundParameters);

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var (drive, folder) in drivesFolders)
        {
            // Get TestSetExecution name list from cache (deduplicated by name)
            var cached = drive.TestSetExecutions.GetCache(folder);
            if (cached is not null)
            {
                foreach (var testSetExecution in cached.Values
                    .Where(te => te.Name is not null && wp.IsMatch(te.Name))
                    .DistinctBy(te => te.Name)
                    .OrderBy(te => te.Name))
                {
                    string tiphelp = TipHelp(testSetExecution);
                    yield return new CompletionResult(PathTools.EscapePSText(testSetExecution.Name!), testSetExecution.Name, CompletionResultType.ParameterValue, tiphelp);
                }
            }
        }
    }
}

/// <summary>
/// Completer that retrieves Id list from the TestCaseExecution and TestCaseAssertion caches
/// </summary>
internal class TestCaseExecutionIdCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
        var depth = ResolveDepth(fakeBoundParameters);

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        var wp = CreateWPFromWordToComplete(wordToComplete);
        var yielded = new HashSet<Int64>();

        foreach (var (drive, folder) in drivesFolders)
        {
            Int64 folderId = folder.Id ?? 0;

            // Get Ids from the TestCaseExecution cache
            var tceCache = drive.TestCaseExecutions.GetCache(folder)?.Values;
            if (tceCache is not null)
            {
                foreach (var tce in tceCache
                    .Where(tce => tce.Id is not null && wp.IsMatch(tce.Id.ToString()!))
                    .OrderByDescending(tce => tce.Id))
                {
                    if (yielded.Add(tce.Id!.Value))
                    {
                        string idStr = tce.Id.ToString()!;
                        string tiphelp = TipHelp(tce);
                        yield return new CompletionResult(idStr, idStr, CompletionResultType.ParameterValue, tiphelp);
                    }
                }
            }

            // Get TestCaseExecutionId from the TestCaseAssertion cache
            var tcaCache = drive.TestCaseAssertions.GetCache(folder);
            if (tcaCache is not null)
            {
                foreach (var testCaseExecutionId in tcaCache.Keys
                    .Where(id => wp.IsMatch(id.ToString()))
                    .OrderByDescending(id => id))
                {
                    if (yielded.Add(testCaseExecutionId))
                    {
                        string idStr = testCaseExecutionId.ToString();
                        yield return new CompletionResult(idStr, idStr, CompletionResultType.ParameterValue, folder.GetPSPath());
                    }
                }
            }
        }
    }
}

/// <summary>
/// Completer that retrieves a distinct list of EntryPointPath values from the TestCaseExecution cache
/// </summary>
internal class TestCaseExecutionEntryPointCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
        var depth = ResolveDepth(fakeBoundParameters);

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        // Exclude Names already selected via the parameter
        var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);
        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (drive, folder) in drivesFolders)
        {
            var cached = drive.TestCaseExecutions.GetCache(folder)?.Values;
            if (cached is not null)
            {
                foreach (var entryPointPath in cached
                    .Select(e => e.EntryPointPath)
                    .Where(p => p is not null && wp.IsMatch(p))
                    .ExcludeByWildcards(te => te, wpName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(p => p))
                {
                    if (yielded.Add(entryPointPath!))
                    {
                        yield return new CompletionResult(
                            PathTools.EscapePSText(entryPointPath),
                            entryPointPath,
                            CompletionResultType.ParameterValue,
                            entryPointPath!);
                    }
                }
            }
        }
    }
}

#region Completers for Platform Management cmdlets
public class PmGroupNameCompleter : DriveScopedCompleter<PmGroup>
{
    protected override List<OrchDriveInfo> ResolveDrives(IDictionary fbp) => ResolvePmDrives(fbp);
    protected override IEnumerable<PmGroup> GetEntities(OrchDriveInfo drive) => drive.PmGroups.Get();
    protected override string GetName(PmGroup e) => e.name!;
    protected override string GetTipHelp(OrchDriveInfo drive, PmGroup e) => e.GetPSPath(drive.NameColonSeparator);
    protected override CompletionResultType ResultType => CompletionResultType.Text;
    protected override string? NotFoundMessage => "No groups found for";
}

internal class PmRobotAccountNameCompleter : DriveScopedCompleter<PmRobotAccount>
{
    protected override List<OrchDriveInfo> ResolveDrives(IDictionary fbp) => ResolvePmDrives(fbp);
    protected override IEnumerable<PmRobotAccount> GetEntities(OrchDriveInfo drive)
        => drive.PmRobotAccounts.Get().Where(r => r is not null)!;
    protected override string GetName(PmRobotAccount e) => e.name!;
    protected override string GetTipHelp(OrchDriveInfo drive, PmRobotAccount e) => e.GetPSPath(drive.NameColonSeparator);
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

internal class PmUserEmailCompleter : DriveScopedCompleter<PmUser>
{
    protected override List<OrchDriveInfo> ResolveDrives(IDictionary fbp) => ResolvePmDrives(fbp);
    protected override IEnumerable<PmUser> GetEntities(OrchDriveInfo drive)
        => drive.PmUsers.Get().Where(u => !string.IsNullOrEmpty(u.email));
    protected override string GetName(PmUser e) => e.email!;
    protected override string GetTipHelp(OrchDriveInfo drive, PmUser e)
    {
        var tip = e.GetPSPath(drive.NameColonSeparator);
        if (!string.IsNullOrEmpty(e.displayName))
            tip += $" ({e.displayName})";
        return tip;
    }
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

internal class PmLicensedGroupNameCompleter : DriveScopedCompleter<NuLicensedGroup>
{
    protected override List<OrchDriveInfo> ResolveDrives(IDictionary fbp) => ResolvePmDrives(fbp);
    protected override IEnumerable<NuLicensedGroup> GetEntities(OrchDriveInfo drive)
        => drive.PmLicensedGroups.Get().Where(g => g is not null)!;
    protected override string GetName(NuLicensedGroup e) => e.name!;
    protected override string GetTipHelp(OrchDriveInfo drive, NuLicensedGroup e) => e.GetPSPath(drive.NameColonSeparator);
    protected override CompletionResultType ResultType => CompletionResultType.Text;
}

#endregion

#region Completers for Test Manager cmdlets

/// <summary>
/// Base class for Test Manager completers that resolve entities within (drive, project) scope.
/// Uses <c>EnumTmFolders</c> to honor the -Path / -Recurse parameters.
/// </summary>
internal abstract class TmProjectScopedCompleter<TEntity> : OrchArgumentCompleter
{
    protected abstract IEnumerable<TEntity> GetEntities(OrchTmDriveInfo drive, TmProject project);
    protected abstract string GetName(TEntity entity);
    protected virtual string GetTipHelp(TEntity entity) => GetName(entity);
    protected virtual CompletionResultType ResultType => CompletionResultType.Text;

    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName, string parameterName, string wordToComplete,
        CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesProjects = SessionState.EnumTmFolders(paramPath, recurse);

        var wpName = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drivesProjects, dp => GetEntities(dp.drive, dp.project));

        foreach (var result in results)
        {
            foreach (var entity in result
                .Where(e => wp.IsMatch(GetName(e)))
                .ExcludeByWildcards(e => GetName(e!), wpName)
                .OrderBy(e => GetName(e)))
            {
                string name = GetName(entity);
                yield return new CompletionResult(PathTools.EscapePSText(name), name, ResultType, GetTipHelp(entity));
            }
        }
    }
}

internal class TmRequirementNameCompleter : TmProjectScopedCompleter<TmRequirement>
{
    protected override IEnumerable<TmRequirement> GetEntities(OrchTmDriveInfo drive, TmProject project)
        => drive.TmRequirements.Get(project);
    protected override string GetName(TmRequirement e) => e.name!;
    protected override string GetTipHelp(TmRequirement e) => e.GetPSPath();
}

internal class TmTestSetNameCompleter : TmProjectScopedCompleter<TmTestSet>
{
    protected override IEnumerable<TmTestSet> GetEntities(OrchTmDriveInfo drive, TmProject project)
        => drive.TmTestSets.Get(project);
    protected override string GetName(TmTestSet e) => e.name!;
    protected override string GetTipHelp(TmTestSet e) => e.GetPSPath();
}

internal class TmTestCaseNameCompleter : TmProjectScopedCompleter<TmTestCase>
{
    protected override IEnumerable<TmTestCase> GetEntities(OrchTmDriveInfo drive, TmProject project)
        => drive.TmTestCases.Get(project);
    protected override string GetName(TmTestCase e) => e.name!;
    protected override string GetTipHelp(TmTestCase e) => e.GetPSPath();
}

internal class TmTestExecutionNameCompleter : TmProjectScopedCompleter<TmTestExecution>
{
    protected override IEnumerable<TmTestExecution> GetEntities(OrchTmDriveInfo drive, TmProject project)
        => drive.TmTestExecutions.Get(project);
    protected override string GetName(TmTestExecution e) => e.name!;
    protected override string GetTipHelp(TmTestExecution e) => e.GetPSPath();
}

#endregion

// This parameter only accepts a single value, so there is no need to consider positional parameters.

internal class StaticTextsCompleter<TItems> : OrchArgumentCompleter where TItems : IStaticCandidates
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wpParam = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var candidate in TItems.Items
            .Where(c => wp.IsMatch(c))
            .ExcludeByWildcards(c => c, wpParam))
        {
            yield return new CompletionResult(candidate);
        }
    }
}

internal class BoolCompleter : StaticTextsCompleter<True_False> { }

// key is always a string
internal class KeyOfDictionaryCompleter<TItems, TValue> : OrchArgumentCompleter where TItems : IDictionaryItems<TValue>
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wpParam = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        if (!wordToComplete?.EndsWith('?') ?? false) wordToComplete += '*';
        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var candidate in TItems.Items
            .Where(c => wp.IsMatch(c.Key))
            .ExcludeByWildcards(c => c.Key, wpParam))
        {
            var key = candidate.Key;
            if (string.IsNullOrEmpty(key)) continue;
            yield return new CompletionResult(PathTools.EscapePSText(key), key, CompletionResultType.ParameterValue, key);
        }
    }
}

// value is always a string
internal class ValueOfDictionaryCompleter<TItems> : OrchArgumentCompleter where TItems : IDictionaryItems<string>
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wpParam = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        if (!wordToComplete?.EndsWith('?') ?? false) wordToComplete += '*';
        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var candidate in TItems.Items.Values
            .Where(c => wp.IsMatch(c))
            .ExcludeByWildcards(c => c, wpParam))
        {
            if (string.IsNullOrEmpty(candidate)) continue;
            yield return new CompletionResult(candidate);
        }
    }
}

// This parameter only accepts a single value, so there is no need to consider positional parameters.
internal class TimeAfterCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        DateTime dt = DateTime.Today;
        yield return new CompletionResult("'" + dt.ToShortDateString() + " " + dt.ToLongTimeString() + "'");
    }
}

// This parameter only accepts a single value, so there is no need to consider positional parameters.
internal class TimeBeforeCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        DateTime dt = DateTime.Now;
        yield return new CompletionResult("'" + dt.ToShortDateString() + " " + dt.ToLongTimeString() + "'");
    }
}

internal class OneWeekAfterCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        string format = "yyyy-MM-ddTHH:mm:ss";
        DateTime dt = DateTime.Today.AddDays(7);
        yield return new CompletionResult("'" + dt.ToString(format) + "Z'");
    }
}

public class DriveCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = SessionState.EnumAllOrchDrives();

        // Exclude drives already selected via the parameter
        var wpPath = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        var matchingDrives = drives.ExcludeByWildcards(d => d?.NameColon, wpPath);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var drive in matchingDrives
            .Where(d => wp.IsMatch(d.NameColon)))
        {
            string tiphelp = drive.DisplayRoot;
            if (!string.IsNullOrEmpty(drive.Description))
                tiphelp += $" ({drive.Description})";
            yield return new CompletionResult(PathTools.EscapePSText(drive.NameColon), drive.NameColon, CompletionResultType.ParameterValue, tiphelp);
        }
    }
}

// Similar to DriveCompleter, but this has the ability to exclude the source drive.
internal class DestinationDriveCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var sourceDrives = ResolveOrchDrives(fakeBoundParameters);
        var drives = SessionState.EnumAllOrchDrives();

        // Exclude drives already selected via the parameter
        var wpDestination = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var drive in drives
            .Where(d => sourceDrives.All(sd => sd != d))
            .Where(d => wp.IsMatch(d.NameColon))
            .ExcludeByWildcards(d => d?.NameColon, wpDestination)
            .Where(d => wp.IsMatch(d.NameColon)))
        {
            string tiphelp = drive.DisplayRoot;
            if (!string.IsNullOrEmpty(drive.Description))
                tiphelp += $" ({drive.Description})";
            yield return new CompletionResult(PathTools.EscapePSText(drive.NameColon), drive.NameColon, CompletionResultType.ParameterValue, tiphelp);
        }
    }
}

internal class TmDriveCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = SessionState.EnumAllTmDrives();

        // Exclude drives already selected via the parameter
        var wpPath = CreateSelfExclusionList(commandAst, parameterName, wordToComplete);
        var matchingDrives = drives.ExcludeByWildcards(d => d?.NameColon, wpPath);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var drive in matchingDrives
            .Where(d => wp.IsMatch(d.NameColon)))
        {
            string tiphelp = drive.DisplayRoot;
            if (!string.IsNullOrEmpty(drive.Description))
                tiphelp += $" ({drive.Description})";
            yield return new CompletionResult(PathTools.EscapePSText(drive.NameColon), drive.NameColon, CompletionResultType.ParameterValue, tiphelp);
        }
    }
}

internal class DuNameCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesProjects = SessionState.EnumDuFolders(paramPath, recurse);

        // Exclude Names already selected via the parameter
        var wpName = GetFakeBoundParameters(fakeBoundParameters, "Name").ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.GroupBy(drivesProjects, dp => dp.drive.GetDuUsers(dp.project));

        foreach (var result in results)
        {
            foreach (var user in result
                .Where(u => wp.IsMatch(u.Name))
                .ExcludeByWildcards(u => u?.Name, wpName)
                .OrderBy(u => u.Name))
            {
                string tiphelp = user.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(user.Name), user.Name, CompletionResultType.Text, tiphelp);
            }
        }
    }
}

// Request from Mishima-san (KDDI): To allow specifying a User Principal Name for Add-DuUser,
// the following would be needed, but I cannot think of a good implementation.
// Either sacrifice performance or add complex parameters.
// Personally, neither option is acceptable.
//internal class DuUserNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IStaticCandidates
//{
//    public override IEnumerable<CompletionResult> CompleteArgument(
//        string commandName,
//        string parameterName,
//        string wordToComplete,
//        CommandAst commandAst,
//        IDictionary fakeBoundParameters)
//    {
//        var recurse = ResolveSwitchParameter(fakeBoundParameters, "Recurse");

//        // Extract path from the parameter. If not specified, target the current directory.
//        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
//        var drivesProjects = OrchDuDriveInfo.EnumFolders(paramPath, recurse);

//        // Exclude Names already selected via the parameter
//        var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Items, wordToComplete);

//        var wp = CreateWPFromWordToComplete(wordToComplete);

//        var results = ParallelResults3.GroupBy(drivesProjects, dp => dp.drive.GetDuUsers(dp.project));

//        foreach (var result in results)
//        {
//            if (result.Result is null) continue;

//            foreach (var user in result.Result
//                .Where(u => wp.IsMatch(u.UserName))
//                .ExcludeByWildcards(u => u?.UserName, wpUserName)
//                .OrderBy(u => u.UserName))
//            {
//                string tiphelp = user.GetPSPath();
//                yield return new CompletionResult(PathTools.EscapePSText(user.UserName), user.UserName, CompletionResultType.Text, tiphelp);
//            }
//        }
//    }
//}

public class EncodingCompleter : OrchArgumentCompleter
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wp = CreateWPFromWordToComplete(wordToComplete);

        //System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        EncodingInfo[] encodings = System.Text.Encoding.GetEncodings();

        foreach (EncodingInfo ei in encodings
            .Where(e => wp.IsMatch(e.Name)))
        {
            string tooltip = $"CodePage:{ei.CodePage}  {ei.DisplayName}";
            yield return new CompletionResult(PathTools.EscapePSText($"{ei.Name}"), ei.Name, CompletionResultType.Text, tooltip);
        }
    }
}
