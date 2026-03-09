using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
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

        var matches = MyRegex().Matches(input);
        foreach (Match match in matches.Cast<Match>())
        {
            string value = match.Value.Trim();
            if (!string.IsNullOrEmpty(value))
                yield return PathTools.UnescapePSText(value);
        }
    }

    public static string ExtractValueFromCommandElement(CommandElementAst element)
    {
        string ret = element switch
        {
            StringConstantExpressionAst stringAst => stringAst.Value,
            VariableExpressionAst variableAst => variableAst.VariablePath.UserPath,
            ErrorExpressionAst errorAst => errorAst.ToString(),// For ErrorExpressionAst, retrieve appropriate info or return an error message
            _ => element.ToString(),// For unknown CommandElementAst subtypes, retrieve info or
                                    // define appropriate default behavior.
        };
        return ret.Trim().TrimEnd(',');
    }

    public static bool GetSwitchParameterValue(CommandAst commandAst, string parameterName)
    {
        string[] knownSwitchParameters = {
            "AllDrives",
            "ExcludeEntities",
            "ExpandAllocation",
            "ExpandDetails",
            "Expanded",
            "ExpandEntity",
            "ExpandExcludedDate",
            "ExpandGroup",
            "ExpandPermission",
            "ExpandRobotUser",
            "ExpandUserValues",
            "Force",
            "GenerateTemplateCsv",
            "HostFeed",
            "IncludeInherited",
            "IncludePastDate",
            "IsConsumed",
            "License",
            "NoMatchWarning",
            "OrderAscending",
            "Recurse",
            "Reload",
            "WarnOnNoMatch",
            "Verbose",
            "Confirm",
            "WhatIf"
        };

        if (!knownSwitchParameters.Contains(parameterName))
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

    public static IEnumerable<string> GetParameterValues(CommandAst commandAst, string paramName, string[]? precedingPositionalParameterNames = null, string? wordToComplete = null)
    {
        string value = GetParameterValue(commandAst, paramName, precedingPositionalParameterNames);
        foreach (var i in SplitCommaSeparatedText(value?.RemoveEnd(wordToComplete)))
        {
            yield return i;
        }
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
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        _ = uint.TryParse(paramDepth, out uint depth);

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return SessionState.EnumFolders(paramPath, recurse, depth, includeRoot);
    }

    protected static List<(OrchDuDriveInfo drive, DuProject project)> ResolveDuPath(CommandAst commandAst, IDictionary fakeBoundParameters) //, bool includeRoot = false)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return SessionState.EnumDuFolders(paramPath, recurse);
    }

    protected static List<(OrchDriveInfo drive, Folder folder)> ResolvePathWithoutPersonalWorkspace(CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        _ = uint.TryParse(paramDepth, out uint depth);

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);
    }

    protected static List<WildcardPattern>? CreateWPListFromParameter(CommandAst commandAst, string parameterName, string[]? positionalParams, string? wordToComplete)
    {
        var param = GetParameterValues(commandAst, parameterName, positionalParams, wordToComplete);
        return param.ConvertToWildcardPatternList();
    }

    protected static List<WildcardPattern>? CreateWPListFromOtherParameters(CommandAst commandAst, string parameterName, string[] positionalParams)
    {
        var param = GetParameterValues(commandAst, parameterName, positionalParams);
        return param.ConvertToWildcardPatternList();
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
            if ((specifiedNames?.Any(e => string.Compare(e, newName, true) == 0) ?? false) ||
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
        var results = ParallelResults3.GroupBy(drives, drive =>
        {
            var groups = drive.PmGroups.Get()
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name);
            return ParallelResults3.ForEach(groups, group => drive.PmGroups.Get(group?.id));
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

    // Experimental implementation to retrieve the list of positional parameters via reflection.
    // Using this would eliminate the need to pass the positional parameter list as a template parameter to the completer class.
    // It seems to work, but using this prevents the completer from being used from .ps1 scripts.
    // That's unfortunate.
    // It would be better if the completer could also be used from .ps1 functions, right?
    // So we still have to pass the positional parameter list as a template parameter to the completer class.
    //private static readonly Dictionary<string, Dictionary<string, string[]>> _cache = new();

    // Calling this from the completer's CompleteArgument() retrieves the list of positional parameters as an array.
    // The implementation for handling parameter sets is incomplete, but it successfully retrieves the positional parameter list.
    // Note that for reflection to work correctly, the cmdlet class name must match the cmdlet name
    // with hyphens removed.
    //protected string[]? GetPositionalParameterList(string commandName, IDictionary fakeBoundParameters)
    //{
    //    // Check the cache
    //    if (_cache.TryGetValue(commandName, out var cachedParameterSets))
    //    {
    //        // If there is only one parameter set, use it as the default
    //        if (cachedParameterSets.Count == 1)
    //        {
    //            return cachedParameterSets.Values.First();
    //        }

    //        // Infer the current parameter set
    //        string? currentParameterSet = DetermineCurrentParameterSet(cachedParameterSets.Keys, fakeBoundParameters);
    //        return currentParameterSet is not null && cachedParameterSets.ContainsKey(currentParameterSet)
    //            ? cachedParameterSets[currentParameterSet]
    //            : null;
    //    }

    //    // Get the command class type
    //    string className = $"UiPath.PowerShell.Commands.{commandName.Replace("-", "")}Command, UiPath.PowerShell.OrchProvider";
    //    Type commandType = Type.GetType(className);

    //    if (commandType is not null)
    //    {
    //        var parameterSets = new Dictionary<string, List<string>>();

    //        // Get parameter properties and extract positional parameters, considering parameter sets
    //        foreach (var property in commandType.GetProperties())
    //        {
    //            var parameterAttributes = property.GetCustomAttributes<ParameterAttribute>();

    //            foreach (var parameterAttribute in parameterAttributes)
    //            {
    //                if (parameterAttribute.Position >= 0)
    //                {
    //                    string parameterSetName = string.IsNullOrEmpty(parameterAttribute.ParameterSetName)
    //                        ? "__DefaultParameterSet__"
    //                        : parameterAttribute.ParameterSetName;

    //                    if (!parameterSets.ContainsKey(parameterSetName))
    //                    {
    //                        parameterSets[parameterSetName] = new List<string>();
    //                    }
    //                    parameterSets[parameterSetName].Add(property.Name);
    //                }
    //            }
    //        }

    //        var positionalParameterSets = parameterSets.ToDictionary(
    //            kvp => kvp.Key,
    //            kvp => kvp.Value.OrderBy(paramName =>
    //                commandType.GetProperty(paramName)?
    //                    .GetCustomAttributes<ParameterAttribute>()
    //                    .First(attr => attr.ParameterSetName == kvp.Key).Position)
    //                .ToArray()
    //        );

    //        _cache[commandName] = positionalParameterSets;

    //        // If there is only one parameter set, return it
    //        if (positionalParameterSets.Count == 1)
    //        {
    //            return positionalParameterSets.Values.First();
    //        }

    //        string? currentSet = DetermineCurrentParameterSet(positionalParameterSets.Keys, fakeBoundParameters);
    //        return currentSet is not null && positionalParameterSets.ContainsKey(currentSet)
    //            ? positionalParameterSets[currentSet]
    //            : null;
    //    }

    //    return null;
    //}

    //private string? DetermineCurrentParameterSet(IEnumerable<string> parameterSetNames, IDictionary fakeBoundParameters)
    //{
    //    // Check parameter set candidates in order and identify the one matching fakeBoundParameters
    //    foreach (var setName in parameterSetNames)
    //    {
    //        // Prioritize returning "__DefaultParameterSet__"
    //        if (setName == "__DefaultParameterSet__")
    //        {
    //            return setName;
    //        }

    //        bool matches = fakeBoundParameters.Keys.Cast<string>()
    //            .All(param => parameterSetNames.Contains(param));

    //        if (matches)
    //        {
    //            return setName;
    //        }
    //    }

    //    return null; // No matching parameter set found
    //}

    protected static string TipHelp(OrchDriveInfo drive)
    {
        string tiphelp = drive.DisplayRoot;
        if (!string.IsNullOrEmpty(drive.Description))
            tiphelp += $" ({drive.Description})";
        return tiphelp;
    }

    protected static string TipHelp(NuLicensedGroupMember member)
    {
        string tiphelp = member.GetPSPath();
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

    protected static string TipHelp(PmUser entity)
    {
        string tiphelp = entity.GetPSPath();
        string username = (string.Join(" ", [entity.name, entity.surname])).Trim();
        if (!string.IsNullOrEmpty(username))
            tiphelp += $" ({username})";
        return tiphelp;
    }

    protected static string TipHelp(PmRobotAccount entity)
    {
        string tiphelp = entity.GetPSPath();
        return tiphelp;
    }

    protected static string TipHelp(DirectoryUser entity)
    {
        string tiphelp = entity.GetPSPath();
        if (!string.IsNullOrEmpty(entity.displayName))
            tiphelp += $" ({entity.displayName})";
        return tiphelp;
    }

    [GeneratedRegex(@"(?:[^',]+|'[^']*')+")]
    private static partial Regex MyRegex();
}

internal class ActionCatalogNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.ActionCatalogs.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var catalog in result
                .Where(b => wp.IsMatch(b.Name))
                .ExcludeByWildcards(e => e?.Name, wpName)
                .OrderBy(e => e.Name))
            {
                string tooltip = catalog.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(catalog.Name), catalog.Name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class ApiTriggerNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.ApiTriggers.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var trigger in result
                .Where(t => wp.IsMatch(t.Name))
                .ExcludeByWildcards(t => t?.Name, wpName))
            {
                string tooltip = trigger.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(trigger.Name), trigger.Name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class EventTriggerNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.EventTriggers.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var trigger in result
                .Where(t => wp.IsMatch(t.Name))
                .ExcludeByWildcards(t => t?.Name, wpName))
            {
                string tooltip = trigger.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(trigger.Name), trigger.Name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class AssetNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        // Only target the ValueType selected via the parameter
        var wpValueType = CreateWPListFromOtherParameters(commandAst, "ValueType", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

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

internal class AssetValueTypeCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

        var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);
        var wpValueType = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Assets.Get(df.folder));

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

internal class BucketNameCompleter<TPositional, WritableOnly> : OrchArgumentCompleter where TPositional : IPositionalParameters where WritableOnly : IBoolParameter
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Buckets.Get(df.folder));

        bool bFound = false;
        foreach (var result in results)
        {
            foreach (var bucket in result
                .Where(b => !WritableOnly.Value || !(b.Options?.Contains("ReadOnly") ?? false))
                .Where(b => wp.IsMatch(b.Name))
                .ExcludeByWildcards(b => b?.Name, wpName)
                .OrderBy(b => b.Name))
            {
                bFound = true;
                string tooltip = bucket.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(bucket.Name), bucket.Name, CompletionResultType.Text, tooltip);
            }
        }

        if (!bFound)
        {
            yield return new CompletionResult($@"""(No buckets found for '{RemoveEnclosingQuotes(wordToComplete)}')""");
        }
    }
}

internal class BucketFullPathCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromOtherParameters(commandAst, "Name", TPositional.Parameters);

        // Exclude FullPaths already selected via the parameter
        var wpFullPath = CreateWPListFromParameter(commandAst, "FullPath", TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df =>
        {
            var buckets = df.drive.Buckets.Get(df.folder).FilterByWildcards(e => e?.Name, wpName);
            return ParallelResults3.GroupBy(buckets, bucket =>
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

internal class CalendarNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        // Extract path from the parameter. If not specified, target the current directory.
        var drives = ResolveOrchDrives(fakeBoundParameters);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.GetCalendars());

        bool bFound = false;
        foreach (var result in results)
        {
            foreach (var calendar in result
                .Where(c => wp.IsMatch(c.Name))
                .ExcludeByWildcards(c => c?.Name, wpName)
                .OrderBy(b => b.Name))
            {
                bFound = true;
                string tooltip = calendar.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(calendar.Name), calendar.Name, CompletionResultType.Text, tooltip);
            }
        }
        if (!bFound)
        {
            yield return new CompletionResult($@"""(No calendars found for '{RemoveEnclosingQuotes(wordToComplete)}')""");
        }
    }
}

internal class CredentialStoreNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.CredentialStores.Get());

        bool bFound = false;
        foreach (var result in results)
        {
            foreach (var credentialStore in result
                .Where(c => wp.IsMatch(c.Name))
                .ExcludeByWildcards(c => c?.Name, wpName)
                .OrderBy(c => c.Name!))
            {
                bFound = true;
                string tiphelp = TipHelp(credentialStore);
                yield return new CompletionResult(PathTools.EscapePSText(credentialStore.Name), credentialStore.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
        if (!bFound)
        {
            yield return new CompletionResult($@"""(No credential stores found for '{RemoveEnclosingQuotes(wordToComplete)}')""");
        }
    }
}

internal class FolderMachineNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var machine in result
                .Where(m => wp.IsMatch(m.Name))
                .ExcludeByWildcards(m => m?.Name, wpName)
                .OrderBy(m => m.Name))
            {
                yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, TipHelp(machine));
            }
        }
    }
}

internal class MachineNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.Machines.Get());

        bool bFound = false;
        foreach (var result in results)
        {
            foreach (var machine in result
                .Where(m => wp.IsMatch(m.Name))
                .ExcludeByWildcards(m => m?.Name, wpName)
                .OrderBy(m => m.Name!))
            {
                bFound = true;
                string tiphelp = TipHelp(machine);
                yield return new CompletionResult(PathTools.EscapePSText(machine.Name), machine.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
        if (!bFound)
        {
            yield return new CompletionResult($@"""(No machines found for '{RemoveEnclosingQuotes(wordToComplete)}')""");
        }
    }
}

internal class MachineRobotUsersCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpRobotUsers = CreateWPListFromParameter(commandAst, "RobotUsers", TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.AllRobotsAcrossFolders.Get());

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

internal class LibraryIdCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolveOrchDrives(fakeBoundParameters);
        var hostFeed = GetSwitchParameterValue(commandAst, "HostFeed");

        // Exclude Ids already selected via the parameter
        var wpId = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        // Only target Versions selected via the parameter
        var wpVersion = CreateWPListFromOtherParameters(commandAst, "Version", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => hostFeed ? drive.LibrariesInHost.Get() : drive.LibrariesInTenant.Get());

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

internal class LibraryVersionCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolveOrchDrives(fakeBoundParameters);
        var hostFeed = GetSwitchParameterValue(commandAst, "HostFeed");

        // Exclude Ids already selected via the parameter
        var wpId = CreateWPListFromOtherParameters(commandAst, "Id", TPositional.Parameters);

        // Exclude Versions already selected via the parameter
        var wpVersion = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive =>
        {
            var libraries = hostFeed ?
                drive.LibrariesInHost.Get().FilterByWildcards(l => l?.Id, wpId) :
                drive.LibrariesInTenant.Get().FilterByWildcards(l => l?.Id, wpId);

            return ParallelResults3.GroupBy(libraries, library => hostFeed ?
                drive.GetLibraryVersionsInHostFeed(library.Id!) :
                drive.GetLibraryVersions(library.Id!));
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

internal class PackageIdCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse); ////////////////////////TODO★

        // Exclude Ids already selected via the parameter
        var wpId = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetPackages(df.folder));

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

internal class PackageVersionCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumPackageFeedFolders(paramPath, recurse);

        // Only target Ids selected via the parameter
        var wpId = CreateWPListFromOtherParameters(commandAst, "Id", TPositional.Parameters);

        // Exclude Versions already selected via the parameter
        var wpVersion = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df =>
        {
            var packages = df.drive.GetPackages(df.folder).FilterByWildcards(p => p?.Id, wpId); ;
            return ParallelResults3.GroupBy(packages, package => df.drive.GetPackageVersions(df.folder, package.Id!));
        });

        foreach (var result in results)
        {
            foreach (var versions in result)
            {
                foreach (var version in versions
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

internal class ProcessNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

        // Exclude library names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetReleases(df.folder));

        foreach (var result in results)
        {
            foreach (var release in result
                .Where(p => wp.IsMatch(p.Name))
                .ExcludeByWildcards(p => p?.Name, wpName)
                .OrderBy(p => p.Name))
            {
                string tiphelp = TipHelp(release);
                yield return new CompletionResult(PathTools.EscapePSText(release.Name), release.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

internal class QueueNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.Queues.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var queue in result
                .Where(q => wp.IsMatch(q.Name))
                .ExcludeByWildcards(q => q?.Name, wpName)
                .OrderBy(q => q.Name))
            {
                string tiphelp = TipHelp(queue);
                yield return new CompletionResult(PathTools.EscapePSText(queue.Name), queue.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

internal class ListReleasesCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drivesFolders = ResolvePath(commandAst, fakeBoundParameters);

        // Exclude Processes already selected via the parameter
        var paramName = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);
        var wpName = paramName.ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);


        // ListReleases appears to be a private API, so using GetReleases is probably better.
        // ListReleases did not work when ApiVersion == 13.
        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetReleases(df.folder));

        // This might have less load on Orchestrator or be faster to process.
        // var results = ParallelResults.ForEach(drivesFolders, df => df.drive.ListReleases(df.folder));

        foreach (var result in results)
        {
            foreach (var release in result
                .Where(r => wp.IsMatch(r.Name))
                .ExcludeByWildcards(r => r?.Name, wpName)
                .OrderBy(r => r.Name))
            {
                string tooltip = TipHelp(release);
                yield return new CompletionResult(PathTools.EscapePSText(release.Name), release.Name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class RoleNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolveOrchDrives(fakeBoundParameters);

        // Exclude library names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.Roles.Get());

        foreach (var result in results)
        {
            foreach (var role in result
                .Where(r => wp.IsMatch(r.Name))
                .ExcludeByWildcards(r => r?.Name, wpName)
                .OrderBy(role => role.Name))
            {
                string tiphelp = TipHelp(role);
                yield return new CompletionResult(PathTools.EscapePSText(role.Name), role.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

public class TenantUserUserNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolveOrchDrives(fakeBoundParameters);

        // Exclude user names already selected via the parameter
        var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        // Only target FullNames selected via the parameter
        var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);

        var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.GetUsers());

        foreach (var result in results)
        {
            foreach (var user in result
                .Where(u => wp.IsMatch(u.UserName))
                .ExcludeByWildcards(u => u?.UserName, wpUserName)
                .FilterByWildcards(u => u?.FullName, wpFullName)
                .FilterByWildcards(u => u?.Type, wpType)
                .OrderBy(u => u.UserName))
            {
                string tiphelp = TipHelp2(user);
                yield return new CompletionResult(PathTools.EscapePSText(user.UserName), user.UserName, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

public class TenantUserFullNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var drives = ResolveOrchDrives(fakeBoundParameters);

        // Only target UserNames selected via the parameter
        var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);

        // Exclude user names already selected via the parameter
        var wpFullName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.GetUsers());

        foreach (var result in results)
        {
            foreach (var e in result
                .Where(u => wp.IsMatch(u.FullName))
                .FilterByWildcards(u => u?.UserName, wpUserName)
                .ExcludeByWildcards(u => u?.FullName, wpFullName)
                .FilterByWildcards(u => u?.Type, wpType)
                .OrderBy(u => u.FullName))
            {
                string tiphelp = TipHelp2(e);
                yield return new CompletionResult(PathTools.EscapePSText(e.FullName), e.FullName, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
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

internal class TriggerNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.GetTriggers(df.folder));

        foreach (var result in results)
        {
            foreach (var trigger in result
                .Where(t => wp.IsMatch(t.Name))
                .ExcludeByWildcards(t => t?.Name, wpName)
                .OrderBy(t => t.Name))
            {
                string tiphelp = TipHelp(trigger);
                yield return new CompletionResult(PathTools.EscapePSText(trigger.Name), trigger.Name, CompletionResultType.Text, tiphelp);
            }
        }
    }
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

internal class WebhookNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.Webhooks.Get());

        foreach (var result in results)
        {
            foreach (var webhook in result
                .Where(e => wp.IsMatch(e.Name))
                .ExcludeByWildcards(e => e?.Name, wpName)
                .OrderBy(e => e.Name!))
            {
                string tiphelp = TipHelp(webhook);
                yield return new CompletionResult(PathTools.EscapePSText(webhook.Name), webhook.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

internal class PmDirectoryNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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

        var entityType = GetParameterValue(commandAst, "EntityType", TPositional.Parameters);
        string kind = entityType?.ToLower() switch
        {
            "user" => "DirectoryUser",
            "group" => "DirectoryGroup",
            "application" => "Application",
            _ => null
        };
        if (kind is null) yield break;

        var names = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var drives = ResolvePmDrives(fakeBoundParameters);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        wordToComplete = RemoveEnclosingQuotes(wordToComplete);
        var results = ParallelResults3.GroupBy(drives, drive => drive.SearchPmDirectory(RemoveEnclosingQuotes(wordToComplete)));

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

internal class PmDirectoryNameCompleter4Du<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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

        var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);
        var types = DirectoryTypes.Parameters.FilterByWildcards(p => p, wpType);
        types = types.Select(t => t == "DirectoryApplication" ? "Application" : t);

        var names = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var drives = ResolveDuDrives(fakeBoundParameters);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        wordToComplete = RemoveEnclosingQuotes(wordToComplete);
        var results = ParallelResults3.GroupBy(drives, drive => drive.ParentDrive.SearchPmDirectory(wordToComplete));

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

internal class UserNameInPmGroupCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);
        var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        // Get existing members of the specified group
        var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", TPositional.Parameters);
        var existingMemberIds = GetExistingMembers(drives, wpGroupName);

        // Get details for each group
        var results = ParallelResults3.GroupBy(drives, drive =>
        {
            var groups = drive.PmGroups.Get()
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name);
            return ParallelResults3.ForEach(groups, group => drive.PmGroups.Get(group?.id));
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

internal class TypeInPmGroupCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpType = CreateWPListFromParameter(commandAst, "Type", TPositional.Parameters, wordToComplete);
        var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        // Get existing members of the specified group
        var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", TPositional.Parameters);
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

internal class ExternalApplicationNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.PmExternalClients.Get());

        foreach (var result in results)
        {
            foreach (var client in result
                .Where(a => wp.IsMatch(a?.name))
                .ExcludeByWildcards(a => a?.name!, wpName)
                .OrderBy(a => a?.name))
            {
                string tooltip = client?.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(client?.name), client?.name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class TestCaseNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        uint.TryParse(paramDepth, out uint depth);

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth); ///////// ★TODO
        
        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.TestCases.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var testCase in result
                .Where(tc => wp.IsMatch(tc.Name!))
                .ExcludeByWildcards(tc => tc?.Name, wpName)
                .OrderBy(tc => tc.Name))
            {
                string tiphelp = TipHelp(testCase);
                yield return new CompletionResult(PathTools.EscapePSText(testCase.Name), testCase.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

internal class TestDataQueueNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        uint.TryParse(paramDepth, out uint depth);

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.TestDataQueues.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var testDataQueue in result
                .Where(e => wp.IsMatch(e.Name!))
                .ExcludeByWildcards(e => e?.Name, wpName)
                .OrderBy(e => e.Name))
            {
                string tiphelp = TipHelp(testDataQueue);
                yield return new CompletionResult(PathTools.EscapePSText(testDataQueue.Name), testDataQueue.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

internal class TestScheduleNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        uint.TryParse(paramDepth, out uint depth);

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.TestSetSchedules.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var testSet in result
                .Where(tc => wp.IsMatch(tc.Name!))
                .ExcludeByWildcards(tc => tc?.Name, wpName)
                .OrderBy(tc => tc.Name))
            {
                string tiphelp = TipHelp(testSet);
                yield return new CompletionResult(PathTools.EscapePSText(testSet.Name), testSet.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
}

internal class TestSetNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        uint.TryParse(paramDepth, out uint depth);

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, df => df.drive.TestSets.Get(df.folder));

        foreach (var result in results)
        {
            foreach (var testSet in result
                .Where(te => wp.IsMatch(te.Name!))
                .ExcludeByWildcards(te => te?.Name, wpName)
                .OrderBy(te => te.Name))
            {
                string tiphelp = TipHelp(testSet);
                yield return new CompletionResult(PathTools.EscapePSText(testSet!.Name), testSet.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
    }
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
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        uint.TryParse(paramDepth, out uint depth);

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var (drive, folder) in drivesFolders)
        {
            // Get TestSetExecution name list from cache (deduplicated by name)
            if (drive._dicTestSetExecutions?.TryGetValue(folder.Id ?? 0, out var cached) ?? false)
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
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        uint.TryParse(paramDepth, out uint depth);

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        var wp = CreateWPFromWordToComplete(wordToComplete);
        var yielded = new HashSet<Int64>();

        foreach (var (drive, folder) in drivesFolders)
        {
            Int64 folderId = folder.Id ?? 0;

            // Get Ids from the TestCaseExecution cache
            if (drive._dicTestCaseExecutions?.TryGetValue(folderId, out var tceCache) ?? false)
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
            if (drive._dicTestCaseAssertions?.TryGetValue(folderId, out var tcaCache) ?? false)
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
internal class TestCaseExecutionEntryPointCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        uint.TryParse(paramDepth, out uint depth);

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);
        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (drive, folder) in drivesFolders)
        {
            if (drive._dicTestCaseExecutions?.TryGetValue(folder.Id ?? 0, out var cached) ?? false)
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
public class PmGroupNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.PmGroups.Get());

        bool bFound = false;
        foreach (var result in results)
        {
            foreach (var pmGroup in result
                .Where(g => wp.IsMatch(g.name))
                .ExcludeByWildcards(g => g?.name, wpName)
                .OrderBy(g => g.name))
            {
                bFound = true;
                string tiphelp = pmGroup.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(pmGroup?.name), pmGroup?.name, CompletionResultType.Text, tiphelp);
            }
        }
        if (!bFound)
        {
            yield return new CompletionResult($@"""(No groups found for '{RemoveEnclosingQuotes(wordToComplete)}')""");
        }
    }
}

internal class PmRobotAccountNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.PmRobotAccounts.Get());

        foreach (var result in results)
        {
            foreach (var pmRobotAccount in result
                .Where(r => r is not null)
                .Where(r => wp.IsMatch(r!.name!))
                .ExcludeByWildcards(r => r!.name!, wpName)
                .OrderBy(r => r!.name))
            {
                string tiphelp = pmRobotAccount.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(pmRobotAccount.name), pmRobotAccount.name, CompletionResultType.Text, tiphelp);
            }
        }
    }
}

internal class PmUserEmailCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpEmail = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.PmUsers.Get());

        foreach (var result in results)
        {
            foreach (var user in result
                .Where(g => !string.IsNullOrEmpty(g.email))
                .Where(g => wp.IsMatch(g.email))
                .ExcludeByWildcards(u => u?.email!, wpEmail)
                .OrderBy(u => u?.email))
            {
                string tooltip = user.GetPSPath();
                if (!string.IsNullOrEmpty(user.displayName))
                    tooltip += $" ({user.displayName})";
                yield return new CompletionResult(PathTools.EscapePSText(user?.email), user?.email, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class PmLicensedGroupNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpGroupName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drives, drive => drive.PmLicensedGroups.Get());

        foreach (var result in results)
        {
            foreach (var licensedGroup in result
                .Where(g => wp.IsMatch(g?.name))
                .ExcludeByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name))
            {
                string tiphelp = licensedGroup?.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(licensedGroup?.name), licensedGroup?.name, CompletionResultType.Text, tiphelp);
            }
        }
    }
}

#endregion

#region Completers for Test Manager cmdlets
internal class TmRequirementNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumTmFolders(paramPath, recurse);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, dp => dp.drive.TmRequirements.Get(dp.project));

        foreach (var result in results)
        {
            foreach (var requirement in result
                .Where(e => wp.IsMatch(e.name))
                .ExcludeByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.name))
            {
                string tooltip = requirement.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(requirement.name), requirement.name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class TmTestSetNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumTmFolders(paramPath, recurse);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, dp => dp.drive.TmTestSets.Get(dp.project));

        foreach (var result in results)
        {
            foreach (var testSet in result
                .Where(e => wp.IsMatch(e.name))
                .ExcludeByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.name))
            {
                string tooltip = testSet.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(testSet.name), testSet.name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class TmTestCaseNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumTmFolders(paramPath, recurse);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, dp => dp.drive.TmTestCases.Get(dp.project));

        foreach (var result in results)
        {
            foreach (var testCase in result
                .Where(e => wp.IsMatch(e.name))
                .ExcludeByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.name))
            {
                string tooltip = testCase.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(testCase.name), testCase.name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

internal class TmTestExecutionNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = SessionState.EnumTmFolders(paramPath, recurse);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesFolders, dp => dp.drive.TmTestExecutions.Get(dp.project));

        foreach (var result in results)
        {
            foreach (var testExecution in result
                .Where(e => wp.IsMatch(e.name))
                .ExcludeByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.name))
            {
                string tooltip = testExecution.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(testExecution.name), testExecution.name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

#endregion

// This parameter only accepts a single value, so there is no need to consider positional parameters.

internal class StaticTextsCompleter<TItems> : OrchArgumentCompleter where TItems : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var wpParam = CreateWPListFromParameter(commandAst, parameterName, null, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var candidate in TItems.Parameters
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
        var wpParam = CreateWPListFromParameter(commandAst, parameterName, null, wordToComplete);

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
        var wpParam = CreateWPListFromParameter(commandAst, parameterName, null, wordToComplete);

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

public class DriveCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpPath = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

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

// Drive name completer for the -Path parameter that is not a positional parameter
public class DriveCompleter : DriveCompleter<Positional.Empty>
{
}

// Similar to DriveCompleter, but this has the ability to exclude the source drive.
internal class DestinationDriveCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
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
        var wpDestination = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

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

internal class TmDriveCompleter<T> : OrchArgumentCompleter where T : IPositionalParameters
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
        var wpPath = CreateWPListFromParameter(commandAst, parameterName, T.Parameters, wordToComplete);
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

internal class DuNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        // Extract path from the parameter. If not specified, target the current directory.
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesProjects = SessionState.EnumDuFolders(paramPath, recurse);

        // Exclude Names already selected via the parameter
        var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults3.GroupBy(drivesProjects, dp => dp.drive.GetDuUsers(dp.project));

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
//internal class DuUserNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
//{
//    public override IEnumerable<CompletionResult> CompleteArgument(
//        string commandName,
//        string parameterName,
//        string wordToComplete,
//        CommandAst commandAst,
//        IDictionary fakeBoundParameters)
//    {
//        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

//        // Extract path from the parameter. If not specified, target the current directory.
//        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
//        var drivesProjects = OrchDuDriveInfo.EnumFolders(paramPath, recurse);

//        // Exclude Names already selected via the parameter
//        var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

//        var wp = CreateWPFromWordToComplete(wordToComplete);

//        var results = ParallelResults.ForEach(drivesProjects, dp => dp.drive.GetDuUsers(dp.project));

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
