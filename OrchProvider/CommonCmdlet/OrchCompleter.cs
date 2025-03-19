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
            ErrorExpressionAst errorAst => errorAst.ToString(),// ErrorExpressionAstの場合、適切な情報を取得するか、エラーメッセージを返す
            _ => element.ToString(),// 未知のCommandElementAstのサブタイプの場合、その情報を取得するか、
                                    // 適切なデフォルトの動作を定義します。
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

        // コマンドの各要素を反復処理し、指定されたパラメータ名を探す
        foreach (var element in commandAst.CommandElements)
        {
            // パラメータ名をチェック
            if (element is CommandParameterAst param &&
                param.ParameterName.Equals(parameterName, StringComparison.OrdinalIgnoreCase) &&
                param.Argument is null) // スイッチパラメータには引数がない
            {
                return true;
            }
        }

        return false;
    }

    // TODO: スイッチパラメータに値が指定されていた場合の処理が正しくない気がする
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
                    // 現在のパラメータが取り出したいパラメータの場合の処理
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
                    // スイッチパラメータの場合、単に削除する
                    elements.RemoveAt(i);
                    i--;
                    if (isPositionalParam && positionIndex > i)
                    {
                        positionIndex--;
                    }
                }
                else
                {
                    // 通常のパラメータの場合、パラメータ名と値を削除
                    elements.RemoveAt(i); // パラメータ名を削除
                    if (i < elements.Count && !elements[i].StartsWith("-"))
                    {
                        elements.RemoveAt(i); // パラメータ値を削除
                    }
                    i--;
                }
            }
        }

        // 残った要素から位置パラメータの値を取得
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

    protected static List<OrchDriveInfo> ResolveDrives(IDictionary fakeBoundParameters)
    {
        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return OrchDriveInfo.EnumOrchDrives(paramPath);
    }

    protected static List<OrchDuDriveInfo> ResolveDuDrives(IDictionary fakeBoundParameters)
    {
        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return OrchDriveInfo.EnumDuDrives(paramPath);
    }

    protected static List<(OrchDriveInfo drive, Folder folder)> ResolvePath(CommandAst commandAst, IDictionary fakeBoundParameters, bool includeRoot = false)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        _ = uint.TryParse(paramDepth, out uint depth);

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return OrchDriveInfo.EnumFolders(paramPath, recurse, depth, includeRoot);
    }

    protected static List<(OrchDuDriveInfo drive, DuProject project)> ResolveDuPath(CommandAst commandAst, IDictionary fakeBoundParameters) //, bool includeRoot = false)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return OrchDuDriveInfo.EnumFolders(paramPath, recurse);
    }

    protected static List<(OrchDriveInfo drive, Folder folder)> ResolvePathWithoutPersonalWorkspace(CommandAst commandAst, IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");
        var paramDepth = GetParameterValue(commandAst, "Depth");
        _ = uint.TryParse(paramDepth, out uint depth);

        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        return OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);
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

    protected static WildcardPattern CreateWPFromWordToComplete(string? wordToComplete)
    {
        if (!string.IsNullOrEmpty(wordToComplete))
        {
            if (wordToComplete.StartsWith('\'') && wordToComplete.EndsWith('\''))
            {
                wordToComplete = wordToComplete.Substring(1, wordToComplete.Length - 2).Replace("''", "'");
            }
        }

        wordToComplete ??= "*";

        string checker = wordToComplete.Replace("`*", "").Replace("`+", "");
        if (!checker.Contains('*') && !checker.Contains('?'))
            wordToComplete += '*';

        return new WildcardPattern(wordToComplete, WildcardOptions.IgnoreCase);
    }

    protected static List<PmGroupMember> GetExistingMembers(List<OrchDriveInfo> drives, List<WildcardPattern>? wpGroupName)
    {
        var results = ParallelResults.ForEach(drives, drive =>
        {
            var groups = drive.GetPmGroups().Values
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name);
            return ParallelResults.ForEach(groups, group => drive.GetPmGroup(group?.id));
        });

        List<PmGroupMember> existingMembers = [];
        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var group in entities!)
            {
                if (!group.TryGetValue(out var detailedGroup)) continue;

                existingMembers.AddRange(detailedGroup?.members ?? []);
            }
        }
        return existingMembers;
    }

    // positional parameters の一覧を、リフレクションで取得する試験的な実装
    // これを使えば、positinal parameter の一覧はテンプレートパラメータで completer class に渡す必要がなくなる
    // うまく動きそうだけど、これを使うと completer を .ps1 から使うことができなくなる。。
    // つらい
    // .ps1 の function からも completer を使えた方がいいよね？
    // すると、やはり positinal parameter の一覧はテンプレートパラメータで completer class に渡すしかない
    //private static readonly Dictionary<string, Dictionary<string, string[]>> _cache = new();

    // これを completer の CompleteArgument() から呼び出すと、positional parameters の一覧を配列で取得できる
    // parameter set を考慮する実装が不完全だが、positional parameters の一覧を取得することはできている
    // ただし、リフレクションを適切に動作させるには、cmdlet class の名前が cmdlet name のハイフンを除去したものと同一に
    // なっている必要があることに注意。
    //protected string[]? GetPositionalParameterList(string commandName, IDictionary fakeBoundParameters)
    //{
    //    // キャッシュを確認
    //    if (_cache.TryGetValue(commandName, out var cachedParameterSets))
    //    {
    //        // パラメータセットが一つしかない場合はそれをデフォルトとして使用
    //        if (cachedParameterSets.Count == 1)
    //        {
    //            return cachedParameterSets.Values.First();
    //        }

    //        // 現在のパラメータセットを推測
    //        string? currentParameterSet = DetermineCurrentParameterSet(cachedParameterSets.Keys, fakeBoundParameters);
    //        return currentParameterSet is not null && cachedParameterSets.ContainsKey(currentParameterSet)
    //            ? cachedParameterSets[currentParameterSet]
    //            : null;
    //    }

    //    // コマンドクラスの型を取得
    //    string className = $"UiPath.PowerShell.Commands.{commandName.Replace("-", "")}Command, UiPath.PowerShell.OrchProvider";
    //    Type commandType = Type.GetType(className);

    //    if (commandType is not null)
    //    {
    //        var parameterSets = new Dictionary<string, List<string>>();

    //        // パラメータプロパティを取得し、パラメータセットを考慮して positional parameter を抽出
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

    //        // パラメータセットが一つしかない場合はそれを返す
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
    //    // パラメータセットの候補を順にチェックして、fakeBoundParameters に一致するものを特定
    //    foreach (var setName in parameterSetNames)
    //    {
    //        // "__DefaultParameterSet__" を優先して返す
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

    //    return null; // 一致するパラメータセットが見つからない場合
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
            2 => "DirectoryMachine: ", // これ正しい？
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

    protected static string TipHelp(Library library)
    {
        string tiphelp = library.GetPSPath();
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

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.ActionCatalogs.Get(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var catalog in entities!
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

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.ApiTriggers.Get(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var trigger in entities!
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

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        // パラメータで選択された ValueType のみ対象とする
        var wpValueType = CreateWPListFromOtherParameters(commandAst, "ValueType", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Assets.Get(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var asset in entities!
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

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Assets.Get(df.folder));

        HashSet<string> valueTypes = [];
        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;
            foreach (var asset in entities!.FilterByWildcards(a => a?.Name, wpName))
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

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Buckets.Get(df.folder));

        bool bFound = false;
        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var bucket in entities!
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
            yield return new CompletionResult("'(No buckets found)'");
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
        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.GetCalendars());

        bool bFound = false;
        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var calendar in entities!
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
            yield return new CompletionResult("'(No calendars found)'");
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.CredentialStores.Get());

        bool bFound = false;
        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            if (entities?.Count != 0) bFound = true;

            foreach (var credentialStore in entities!
                .Where(c => wp.IsMatch(c.Name))
                .ExcludeByWildcards(c => c?.Name, wpName)
                .OrderBy(c => c.Name!))
            {
                string tiphelp = TipHelp(credentialStore);
                yield return new CompletionResult(PathTools.EscapePSText(credentialStore.Name), credentialStore.Name, CompletionResultType.ParameterValue, tiphelp);
            }
        }
        if (!bFound)
        {
            yield return new CompletionResult("'(No credential stores found)'");
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

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.FolderMachinesAssigned.Get(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var machine in entities!
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.Machines.Get());

        bool bFound = false;
        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var machine in entities!
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
            yield return new CompletionResult("'(No machines found)'");
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの Name は、候補から除外する
        var wpRobotUsers = CreateWPListFromParameter(commandAst, "RobotUsers", TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.AllRobotsAcrossFolders.Get());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var robot in entities!
                .Where(r => wp.IsMatch(r?.User?.FullName))
                .ExcludeByWildcards(p => p?.User?.FullName, wpRobotUsers)
                .OrderBy(p => p.User?.FullName))
            {
                string tiphelp = robot.GetPSPath();
                if (!string.IsNullOrEmpty(robot.Username))
                    tiphelp += $" ({robot.Username})";
                yield return new CompletionResult(PathTools.EscapePSText(robot.User?.FullName), robot.User?.FullName, CompletionResultType.Text, tiphelp);
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
        var drives = ResolveDrives(fakeBoundParameters);
        var hostFeed = GetSwitchParameterValue(commandAst, "HostFeed");

        // パラメータで選択済みの Id は、候補から除外する
        var wpId = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        // パラメータで選択された Version のみ対象とする
        var wpVersion = CreateWPListFromOtherParameters(commandAst, "Version", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => hostFeed ? drive.LibrariesInHost.Get() : drive.LibrariesInTenant.Get());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var library in entities!
                .Where(l => wp.IsMatch(l.Id))
                .ExcludeByWildcards(l => l?.Id, wpId)
                .FilterByWildcards(l => l?.Version, wpVersion)
                .OrderBy(l => l.Id))
            {
                string tiphelp = TipHelp(library);
                yield return new CompletionResult(PathTools.EscapePSText(library.Id), library.Id, CompletionResultType.ParameterValue, tiphelp);
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
        var drives = ResolveDrives(fakeBoundParameters);
        var hostFeed = GetSwitchParameterValue(commandAst, "HostFeed");

        // パラメータで選択済みの Id は、候補から除外する
        var wpId = CreateWPListFromOtherParameters(commandAst, "Id", TPositional.Parameters);

        // パラメータで選択済みの Version は、候補から除外する
        var wpVersion = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive =>
        {
            var libraries = hostFeed ?
                drive.LibrariesInHost.Get().FilterByWildcards(l => l?.Id, wpId) :
                drive.LibrariesInTenant.Get().FilterByWildcards(l => l?.Id, wpId);

            return ParallelResults.ForEach(libraries, library => hostFeed ?
                drive.GetLibraryVersionsInHostFeed(library.Id!) :
                drive.GetLibraryVersions(library.Id!));
        });

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var library in entities!)
            {
                if (!library.TryGetValue(out var versions)) continue;

                foreach (var version in versions!
                    .Where(v => wp.IsMatch(v.Version))
                    .ExcludeByWildcards(v => v?.Version, wpVersion))
                //.OrderBy(v => v.Version!, VersionComparer.Instance))
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

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(paramPath, recurse); ////////////////////////TODO★

        // パラメータで選択済みの Id は、候補から除外する
        var wpId = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetPackages(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(m => wp.IsMatch(m.Id))
                .ExcludeByWildcards(p => p?.Id, wpId)
                .OrderBy(l => l.Id))
            {
                string tiphelp = TipHelp(e);
                yield return new CompletionResult(PathTools.EscapePSText(e.Id), e.Id, CompletionResultType.ParameterValue, tiphelp);
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

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = OrchDriveInfo.EnumPackageFeedFolders(paramPath, recurse);

        // パラメータで選択された Id のみ対象とする
        var wpId = CreateWPListFromOtherParameters(commandAst, "Id", TPositional.Parameters);

        // パラメータで選択済みの Version は、候補から除外する
        var wpVersion = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df =>
        {
            var packages = df.drive.GetPackages(df.folder).FilterByWildcards(p => p?.Id, wpId); ;
            return ParallelResults.ForEach(packages, package => df.drive.GetPackageVersions(df.folder, package.Id!));
        });

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var packages)) continue;

            foreach (var results2 in packages!)
            {
                if (!results2.TryGetValue(out var versions)) continue;

                foreach (var version in versions!
                    .Where(v => wp.IsMatch(v.Version))
                    .ExcludeByWildcards(v => v?.Version, wpVersion))
                    //.OrderBy(v => v.Version!, VersionComparer.Instance))
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

        // パラメータで選択済みのライブラリ名は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetReleases(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var release in entities!
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

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.Queues.Get(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(q => wp.IsMatch(q.Name))
                .ExcludeByWildcards(q => q?.Name, wpName)
                .OrderBy(q => q.Name))
            {
                string tiphelp = TipHelp(e);
                yield return new CompletionResult(PathTools.EscapePSText(e.Name), e.Name, CompletionResultType.ParameterValue, tiphelp);
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

        // パラメータで選択済みの Process は、候補から除外する
        var paramName = GetParameterValues(commandAst, parameterName, TPositional.Parameters, wordToComplete);
        var wpName = paramName.ConvertToWildcardPatternList();

        var wp = CreateWPFromWordToComplete(wordToComplete);


        // ListReleases は非公開の API のようだから、GetReleases を使った方が良さそうだ。
        // ApiVersion == 13 のとき、ListReleases は動作しなかった。
        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetReleases(df.folder));

        // こっちの方が、Orchestrator に対する負荷が少ないとか、処理が早いとか、あるのかもしれない。。
        // var results = ParallelResults.ForEach(drivesFolders, df => df.drive.ListReleases(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var release in entities!
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みのライブラリ名は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.Roles.Get());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var role in entities!
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みのユーザー名は、候補から除外する
        var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        // パラメータで選択された FullName のみ対象とする
        var wpFullName = CreateWPListFromOtherParameters(commandAst, "FullName", TPositional.Parameters);

        var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(u => wp.IsMatch(u.UserName))
                .ExcludeByWildcards(u => u?.UserName, wpUserName)
                .FilterByWildcards(u => u?.FullName, wpFullName)
                .FilterByWildcards(u => u?.Type, wpType)
                .OrderBy(u => u.UserName))
            {
                string tiphelp = TipHelp2(e);
                yield return new CompletionResult(PathTools.EscapePSText(e.UserName), e.UserName, CompletionResultType.ParameterValue, tiphelp);
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択された UserName のみ対象とする
        var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);

        // パラメータで選択済みのユーザー名は、候補から除外する
        var wpFullName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.GetUsers());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
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

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.GetTriggers(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(t => wp.IsMatch(t.Name))
                .ExcludeByWildcards(t => t?.Name, wpName)
                .OrderBy(t => t.Name))
            {
                string tiphelp = TipHelp(e);
                yield return new CompletionResult(PathTools.EscapePSText(e.Name), e.Name, CompletionResultType.Text, tiphelp);
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
        var drives = OrchDriveInfo.EnumOrchDrives(paramPath);

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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.Webhooks.Get());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(e => wp.IsMatch(e.Name))
                .ExcludeByWildcards(e => e?.Name, wpName)
                .OrderBy(e => e.Name!))
            {
                string tiphelp = TipHelp(e);
                yield return new CompletionResult(PathTools.EscapePSText(e.Name), e.Name, CompletionResultType.ParameterValue, tiphelp);
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

        var drives = ResolveDrives(fakeBoundParameters);
        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.SearchPmDirectory(wordToComplete));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;
            if (entities is null) continue;

            var drive = result.Source;

            foreach (var s in entities
                .Where(s => !names.Contains(s.identityName)) // 入力済みのものを除く
                .Where(s => s.objectType == kind)
                .OrderBy(s => s.identityName))
            {
                string tiphelp = drive.NameColonSeparator + s.identityName;
                yield return new CompletionResult(PathTools.EscapePSText(s.identityName), s.identityName, CompletionResultType.ParameterValue, tiphelp);
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

        var results = ParallelResults.ForEach(drives, drive => drive.ParentDrive.SearchPmDirectory(wordToComplete));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;
            if (entities is null) continue;

            var drive = result.Source;

            foreach (var s in entities
                .Where(s => !names.Contains(s.identityName)) // 入力済みのものを除く
                .Where(s => types.Contains(s?.objectType))
                .OrderBy(s => s.identityName))
            {
                string tiphelp = drive.NameColonSeparator + s.identityName;
                string name = !string.IsNullOrEmpty(s.email) ? s.email : s.identityName;
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの UserName は、候補から除外する
        var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);
        var wpType = CreateWPListFromOtherParameters(commandAst, "Type", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        // 指定されたグループの既存のメンバーを取得する
        var wpGroupName = CreateWPListFromOtherParameters(commandAst, "GroupName", TPositional.Parameters);
        var existingMemberIds = GetExistingMembers(drives, wpGroupName);

        // 各グループの詳細を取得する
        var results = ParallelResults.ForEach(drives, drive =>
        {
            var groups = drive.GetPmGroups().Values
                .FilterByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name);
            return ParallelResults.ForEach(groups, group => drive.GetPmGroup(group?.id));
        });

        // グループのメンバーとなっている DirectoryUser を収集する
        List<PmGroupMember> users = [];
        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!)
            {
                if (!e.TryGetValue(out var detailedGroup)) continue;

                foreach (var member in detailedGroup?.members?
                    .FilterByWildcards(m => m?.objectType, wpType) ?? [])
                {
                    users.Add(member);
                }
            }
        }

        // 条件に合致する DirectoryUser を候補として表示する
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの UserName は、候補から除外する
        var wpType = CreateWPListFromParameter(commandAst, "Type", TPositional.Parameters, wordToComplete);
        var wpUserName = CreateWPListFromOtherParameters(commandAst, "UserName", TPositional.Parameters);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        // 指定されたグループの既存のメンバーを取得する
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

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth); ///////// ★TODO
        
        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.TestCases.Get(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var testCase in entities!
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

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.TestDataQueues.Get(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var testDataQueue in entities!
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

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.TestSetSchedules.Get(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var testSet in entities!
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

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = OrchDriveInfo.EnumFoldersWithoutPersonalWorkspace(paramPath, recurse, depth);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, df => df.drive.TestSets.Get(df.folder));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var testSet in entities!
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.GetPmGroups());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!.Values
                .Where(g => wp.IsMatch(g?.name))
                .ExcludeByWildcards(g => g?.name!, wpName)
                .OrderBy(g => g?.name))
            {
                string tiphelp = e?.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(e?.name), e?.name, CompletionResultType.Text, tiphelp);
            }
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.PmRobotAccounts.Get());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(r => r is not null)
                .Where(r => wp.IsMatch(r!.name!))
                .ExcludeByWildcards(r => r!.name!, wpName)
                .OrderBy(r => r!.name))
            {
                string tiphelp = e.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(e.name), e.name, CompletionResultType.Text, tiphelp);
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの Name は、候補から除外する
        var wpEmail = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.PmUsers.Get());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var user in entities!
                .Where(g => !string.IsNullOrEmpty(g?.email))
                .Where(g => wp.IsMatch(g?.email))
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
        var drives = ResolveDrives(fakeBoundParameters);

        // パラメータで選択済みの Name は、候補から除外する
        var wpGroupName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drives, drive => drive.PmLicensedGroups.Get());

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(g => wp.IsMatch(g?.name))
                .ExcludeByWildcards(g => g?.name!, wpGroupName)
                .OrderBy(g => g?.name))
            {
                string tiphelp = e?.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(e?.name), e?.name, CompletionResultType.Text, tiphelp);
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

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = OrchTmDriveInfo.EnumFolders(paramPath, recurse);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, dp => dp.drive.GetTmRequirements(dp.project));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(e => wp.IsMatch(e.name))
                .ExcludeByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.name))
            {
                string tooltip = e.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(e.name), e.name, CompletionResultType.Text, tooltip);
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

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = OrchTmDriveInfo.EnumFolders(paramPath, recurse);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, dp => dp.drive.GetTmTestSets(dp.project));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(e => wp.IsMatch(e.name))
                .ExcludeByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.name))
            {
                string tooltip = e.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(e.name), e.name, CompletionResultType.Text, tooltip);
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

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesFolders = OrchTmDriveInfo.EnumFolders(paramPath, recurse);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesFolders, dp => dp.drive.GetTmTestCases(dp.project));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var e in entities!
                .Where(e => wp.IsMatch(e.name))
                .ExcludeByWildcards(e => e?.name, wpName)
                .OrderBy(e => e.name))
            {
                string tooltip = e.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(e.name), e.name, CompletionResultType.Text, tooltip);
            }
        }
    }
}

#endregion

// このパラメータは、どうせ値をひとつしか受け入れないから、positional param を考慮する必要もない。。

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

// key は必ず string
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

// value は必ず string
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

// このパラメータは、どうせ値をひとつしか受け入れないから、positional param を考慮する必要もない。。
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

// このパラメータは、どうせ値をひとつしか受け入れないから、positional param を考慮する必要もない。。
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
        var drives = OrchDriveInfo.EnumAllOrchDrives();

        // パラメータで選択済みのドライブは、候補から除外する
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

// positional parameter となっていない -Path パラメータのための drive name completer
public class DriveCompleter : DriveCompleter<Positional.Empty>
{
}

// DriveCompleter と良く似ているのだけど、これはコピー元のドライブを除外する機能がある。
internal class DestinationDriveCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var sourceDrives = ResolveDrives(fakeBoundParameters);
        var drives = OrchDriveInfo.EnumAllOrchDrives();

        // パラメータで選択済みのドライブは、候補から除外する
        var wpDestination = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        foreach (var drive in drives
            .Where(d => sourceDrives.All(sd => sd != d))
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
        var drives = OrchTmDriveInfo.EnumAllOrchDrives();

        // パラメータで選択済みのドライブは、候補から除外する
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

internal class DuUserNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
{
    public override IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters)
    {
        var recurse = GetSwitchParameterValue(commandAst, "Recurse");

        // パラメータからパスを抽出する。指定がなければ、カレントディレクトリを対象にする
        var paramPath = GetFakeBoundParameters(fakeBoundParameters, "Path");
        var drivesProjects = OrchDuDriveInfo.EnumFolders(paramPath, recurse);

        // パラメータで選択済みの Name は、候補から除外する
        var wpName = CreateWPListFromParameter(commandAst, "Name", TPositional.Parameters, wordToComplete);

        var wp = CreateWPFromWordToComplete(wordToComplete);

        var results = ParallelResults.ForEach(drivesProjects, dp => dp.drive.GetDuUsers(dp.project));

        foreach (var result in results)
        {
            if (!result.TryGetValue(out var entities)) continue;

            foreach (var user in entities!
                .Where(e => wp.IsMatch(e?.displayName))
                .ExcludeByWildcards(e => e?.displayName!, wpName)
                .OrderBy(e => e?.displayName))
            {
                string tiphelp = user.GetPSPath();
                yield return new CompletionResult(PathTools.EscapePSText(user.displayName), user.displayName, CompletionResultType.Text, tiphelp);
            }
        }
    }
}

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
