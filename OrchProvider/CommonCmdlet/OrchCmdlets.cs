using System.Data;
using System.Diagnostics;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Entities.JsonConverter;
using Path = System.IO.Path;

namespace UiPath.PowerShell.Commands;

public class EncodingArgumentTransformationAttribute : ArgumentTransformationAttribute
{
    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        if (inputData is string encodingName)
        {
            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Invalid encoding: {encodingName}");
            }
        }

        throw new ArgumentException("Input data is not a valid encoding name.");
    }
}

public abstract class OrchestratorPSCmdlet : PSCmdlet, IWritableHost
{
    internal static string ConvertToUnsecureString(SecureString securePassword)
    {
        IntPtr unmanagedString = IntPtr.Zero;
        try
        {
            unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
            return Marshal.PtrToStringUni(unmanagedString)!;
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
        }
    }

    internal static string EscapeCsvValue(string? value, bool escapeWildcard = false)
    {
        if (value is null) return "";

        // PowerShell のワイルドカード文字をエスケープ
        if (escapeWildcard)
        {
            value = WildcardPattern.Escape(value);
        }

        // CSV特有のエスケープ処理
        if (value.IndexOfAny([',', '"', '\n', '\r']) >= 0)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    internal static string EscapeCsvValue(int? value)
    {
        return value?.ToString() ?? "";
    }

    internal static string EscapeCsvValue(long? value)
    {
        return value?.ToString() ?? "";
    }

    internal static string EscapeCsvValue(bool? value)
    {
        return value?.ToString().ToUpper() ?? "";
    }

    internal static string EscapeCsvValue(DateTime? value)
    {
        return value?.ToString() ?? "";
    }

    internal static string EscapeCsvValue(IEnumerable<string?>? values, bool escapeWildcard = false)
    {
        if (values is null) return "";
        if (escapeWildcard)
        {
            return EscapeCsvValue(string.Join(',', values
                .Where(r => !string.IsNullOrEmpty(r))
                .Select(r => WildcardPattern.Escape(r))
                .OrderBy(r => r)));
        }
        else
        {
            return EscapeCsvValue(string.Join(',', values.OrderBy(r => r)));
        }
    }

    internal static string EscapeCsvValue(Tag[]? value)
    {
        return EscapeCsvValue(value?.ConvertToString());
    }

    internal static string? FormatDateTimeWithKind(DateTime? dateTime)
    {
        if (dateTime is null) return null;

        string format = "yyyy-MM-ddTHH:mm:ss";

        // Utcの場合、末尾にZを付加
        if (dateTime.Value.Kind == DateTimeKind.Utc)
        {
            return dateTime.Value.ToString(format) + "Z";
        }
        else
        {
            // LocalまたはUnspecifiedの場合
            return dateTime.Value.ToString(format);
        }
    }

    // returns (physicalFilePath, psFilePath)
    internal static (string?, string?) GenerateCsvFilePath(string? paramExportCsv, SessionState state, string defaultFileName)
    {
        if (string.IsNullOrEmpty(paramExportCsv)) return (null, null);

        ICollection<PathInfo> resolvedPaths = null;

        try
        {
            resolvedPaths = state.Path.GetResolvedPSPathFromPSPath(paramExportCsv);
        }
        catch // (ディレクトリパスではなく) ファイルパスを指定した場合は、存在しないファイル名を除外して親フォルダパスを探し直す
        {
            string parentFolder = Path.GetDirectoryName(paramExportCsv);
            defaultFileName = Path.GetFileName(paramExportCsv);
            try
            {
                resolvedPaths = state.Path.GetResolvedPSPathFromPSPath(parentFolder);
            }
            catch
            {
                throw new ItemNotFoundException($"Cannot find path '{paramExportCsv}' because it does not exist.'");
            }
        }

        Debug.Assert(resolvedPaths.Count != 0);

        string psPath;
        string physicalPath;
        switch (resolvedPaths.Count)
        {
            case 1:
                var resolvedPath = resolvedPaths.First();
                psPath = resolvedPath.Path;
                physicalPath = resolvedPath.ProviderPath;
                break;
            //case 0: // GetResolvedPSPathFromPSPath() が例外をスローするはずなので、これはない
                //var parentFolder = Path.GetDirectoryName(paramExportCsv);
                //var fileName = Path.GetFileName(paramExportCsv);
                //resolvedPaths = state.Path.GetResolvedPSPathFromPSPath(parentFolder);
                //physicalPath = resolvedPaths.Count switch
                //{
                //    1 => Path.Combine(resolvedPaths.First().ProviderPath, fileName),
                //    0 => throw new FileNotFoundException($"The specified path '{paramExportCsv}' does not exist."),
                //    _ => throw new InvalidOperationException($"The specified path '{paramExportCsv}' resolves to multiple locations."),
                //};
                //break;
            default:
                throw new InvalidOperationException($"The specified path '{paramExportCsv}' resolves to multiple locations.");
        }

        if (Directory.Exists(physicalPath))
        {
            return (Path.Combine(physicalPath, defaultFileName), Path.Combine(psPath, defaultFileName));
        }
        else
        {
            return (physicalPath, psPath);
        }
    }

    internal static StreamWriter? WriteCsvHeader(string? filePath, Encoding? encoding, string[] headers)
    {
        if (string.IsNullOrEmpty(filePath)) return null;

        encoding ??= Encoding.Default;

        // utf-8 の場合には、BOM を追加する
        if (encoding is UTF8Encoding)
        {
            encoding = new UTF8Encoding(true);
        }
        // utf-16 or utf-16BE が指定された場合にも BOM を追加する
        else if (encoding is UnicodeEncoding unicodeEncoding)
        {
            // エンディアンをバイト配列から確認する
            byte[] testBytes = unicodeEncoding.GetBytes("A");

            // ビッグエンディアンの場合、"A" (U+0041) のバイト配列は [0x00, 0x41] になる
            bool isBigEndian = testBytes[0] == 0x00 && testBytes[1] == 0x41;

            // BOM 付きの UnicodeEncoding に変換
            encoding = new UnicodeEncoding(isBigEndian, true);
        }
        else if (encoding is UTF32Encoding utf32Encoding)
        {
            // UTF-32 エンコーディングの場合、エンディアンをバイト配列から確認する
            byte[] testBytes = utf32Encoding.GetBytes("A");

            // ビッグエンディアンの場合、"A" (U+0041) のバイト配列は [0x00, 0x00, 0x00, 0x41] になる
            bool isBigEndian = testBytes[0] == 0x00 && testBytes[1] == 0x00 && testBytes[2] == 0x00 && testBytes[3] == 0x41;

            // BOM 付きの UTF32Encoding に変換
            encoding = new UTF32Encoding(isBigEndian, true);
        }

        var writer = new StreamWriter(filePath, false, encoding);
        writer.WriteCsvLine(headers);

        return writer;
    }

    protected static int? ConvertPriorityToSpecificPriorityValue(string? specificPriorityValue)
    {
        return specificPriorityValue switch
        {
            "Critical" => 95,
            "Highest" => 85,
            "VeryHigh" => 75,
            "High" => 65,
            "MediumHigh" => 55,
            "Medium" => 45,
            "MediumLow" => 35,
            "Low" => 25,
            "VeryLow" => 15,
            "Lowest" => 5,
            _ => null
        };
    }

    internal static string? SerializeExecutorRobotArray(OrchDriveInfo drive, RobotExecutor[]? robotExecutors)
    {
        if (robotExecutors is null || robotExecutors.Length == 0) return null;

        var robots = drive.Robots.Get();
        if (robots is null) return null;

        var robotsById = robots.ToDictionary(m => m.Id!.Value);

        var targetRobots = new List<Robot>();
        foreach (var re in robotExecutors)
        {
            if (robotsById.TryGetValue(re.Id!.Value, out var robot))
            {
                targetRobots.Add(robot);
            }
        }

        if (targetRobots.Count == 0) return null;
        return string.Join(',', targetRobots.Select(r => r.Name).Order());
    }

    internal static string? SerializeMachineRobotSessions(IWritableHost? _this, OrchDriveInfo drive, Folder folder, string? target, IEnumerable<MachineRobotSession>? machineRobots)
    {
        if (machineRobots is null || !machineRobots.Any()) return null;

        List<MachineRobotSessionForSerialize> mrss = [];

        // drive.RobotsFromFolder.Get(folder) を使う方が、どんなタイミングでも期待通り動きそうだけど
        // completer の実装においては、パフォーマンスも大事かな。。
        // すでに登録済みの内容をシリアライズするのだし、drive.Robots.Get() でも動きそうな気がする
        var robots = drive.Robots.Get();
        foreach (var mr in machineRobots)
        {
            Robot robot = null;
            if (mr.RobotId is not null && mr.RobotId != 0)
            {
                robot = robots.Where(r => r.Id == mr.RobotId).FirstOrDefault();
            }
            else if (!string.IsNullOrEmpty(mr.RobotUserName))
            {
                robot = robots.Where(r => string.Compare(r.User?.UserName, mr.RobotUserName, StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
            }

            string machineName = mr.MachineName;
            if (string.IsNullOrEmpty(machineName))
            {
                machineName = drive.Machines.Get().Where(m => m.Id == mr.MachineId).FirstOrDefault()?.Name;
            }

            string sessionName = mr.SessionName;
            if (string.IsNullOrEmpty(sessionName) && (mr.SessionId.GetValueOrDefault() != 0))
            {
                var session = drive.MachineSessionRuntimesByFolder.Get(folder)
                    //.Where(s => s.RuntimeType == "Unattended") // これ不要。RuntimeType が違っても SessionId は同じになるため。
                    .Where(s => s.SessionId == mr.SessionId)
                    .FirstOrDefault();
                sessionName = session?.HostMachineName + " - " + session?.ServiceUserName;
            }

            mrss.Add(new MachineRobotSessionForSerialize()
            {
                UserName = robot?.Username,
                MachineName = machineName,
                SessionName = sessionName
            });
        }

        //return string.Join(',', mrss.Select(e => JsonSerializer.Serialize(e, OrchAPISession.jsoWhenWritingNull))).Replace("\\u0027", "'");
        return JsonSerializer.Serialize(mrss, JsonTools.jsoWhenWritingNull).Replace("\\u0027", "'");
    }

    private readonly Lazy<HashSet<string>> ValidScopes = new(() =>
        ["Default", "Shared", "PersonalWorkspace", "Cloud", "AutomationCloudRobot", "ElasticRobot"]);

    // executorRobots は RobotName の列挙を渡す
    // SelectMany() の結果を連結しているから、内部で List<RobotExecutor> を構築する必要はない。その方が効率的。
    internal static RobotExecutor[]? DeserializeExecutorRobots(IWritableHost? _this, OrchDriveInfo drive, Folder folder, string target, IEnumerable<string>? executorRobots)
    {
        if (executorRobots is null || executorRobots.All(string.IsNullOrEmpty)) return null;

        try
        {
            var robots = drive.Robots.Get();
            var result = executorRobots
                .SelectMany(executorRobot =>
                {
                    // 合致するロボット一覧を抽出
                    var wpRobotName = new WildcardPattern(executorRobot, WildcardOptions.IgnoreCase);
                    var targetRobots = robots.Where(r => wpRobotName.IsMatch(r.Name));

                    if (!targetRobots.Any())
                    {
                        _this?.WriteWarning($"'{target}': The robot with name '{executorRobot}' is not found.");
                    }

                    return targetRobots;
                })
                .DistinctBy(r => r.Id)
                .Select(robot => new RobotExecutor { Id = robot.Id })
                .ToArray();

            return (result.Length == 0) ? null : result;
        }
        catch (Exception ex)
        {
            _this?.WriteError(new ErrorRecord(new OrchException(target, "Failed to deserialize ExecutorRobots.", ex), "GetRobotsFromFolderError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    internal MachineRobotSession[]? DeserializeMachineRobotSessions(IWritableHost? _this, OrchDriveInfo drive, Folder folder, string target, string[]? machineRobots)
    {
        if (machineRobots is null || machineRobots.All(string.IsNullOrEmpty)) return null;

        try
        {
            var tenantUsers = drive.GetUsers();

            // これを呼び出しておかないと、Orchestrator がロボットの検索に失敗してしまう
            // んだけど、GetUsers() で置き換えたからもう呼ばなくても良いな。
            //_ = drive.RobotsFromFolder.Get(folder);

            // この中にはワイルドカードが入っている可能性があるので、すべて展開していく
            IEnumerable<MachineRobotSessionForSerialize?> mrss = null;
            if (machineRobots.Length == 1 && machineRobots[0].StartsWith('[')) // && machineRobots[0].EndsWith(']'))
            {
                // CSV からインポートした場合は配列としてデシリアライズ
                mrss = JsonSerializer.Deserialize<MachineRobotSessionForSerialize[]>(machineRobots[0]);
            }
            else
            {
                mrss = machineRobots.Select(mr => JsonSerializer.Deserialize<MachineRobotSessionForSerialize>(mr));
            }

            List<MachineRobotSession> targets = [];

            foreach (var mrs in mrss ?? [])
            {
                // UserName を適切な Id に変換
                // ワイルドカードをサポートするため、複数の User が出てくる場合がある
                List<Entities.User> users = null;
                if (!string.IsNullOrEmpty(mrs?.UserName))
                {
                    var wpUserName = new WildcardPattern(mrs.UserName, WildcardOptions.IgnoreCase);
                    users = tenantUsers.Where(u => wpUserName.IsMatch(u.UnattendedRobot?.UserName)).ToList();
                    if (users.Count == 0)
                    {
                        WriteWarning($"'{target}': The user name '{mrs.UserName}' is not configured as Unattended Robot in '{drive.NameColonSeparator}'.");
                    }
                }

                // MachineName を適切な Id に変換
                // ワイルドカードをサポートするため、複数の Machine が出てくる場合がある
                List<MachineFolder> machines = null;
                if (!string.IsNullOrEmpty(mrs?.MachineName))
                {
                    var wpMachineName = new WildcardPattern(mrs.MachineName, WildcardOptions.IgnoreCase);
                    machines = drive.FolderMachines.Get(folder)
                        .Where(m => wpMachineName.IsMatch(m.Name))
                        .Where(m => ValidScopes.Value.Contains(m.Scope!))
                        .ToList();
                    if (machines.Count == 0)
                    {
                        WriteWarning($"'{target}': The machine name '{mrs.MachineName}' does not match any in '{folder.GetPSPath()}'.");
                    }
                }

                // user と machine の両方がなければスキップ
                if ((users is null || users.Count == 0) && (machines is null || machines.Count == 0)) continue;

                // 便宜上、要素をひとつだけ入れておく
                if (users is null || users.Count == 0) users = [null];
                if (machines is null || machines.Count == 0) machines = [null];

                // SessionName を適切な Id に変換
                // ワイルドカードをサポートするため、複数の Session が出てくる場合がある
                List<MachineSessionRuntime> sessions = null;
                if (!string.IsNullOrEmpty(mrs?.SessionName))
                {
                    var wpSessionName = new WildcardPattern(mrs.SessionName, WildcardOptions.IgnoreCase);
                    sessions = drive.MachineSessionRuntimesByFolder.Get(folder)
                        //.Where(s => s.RuntimeType == "Unattended") // これ不要。RuntimeType が違っても SessionId は同じになるため。
                        //.Where(s => wpMachineName.IsMatch(s.MachineName)) // この条件はあとで判断する
                        .Where(s =>
                        {
                            var sessionName = s.HostMachineName;
                            if (!string.IsNullOrEmpty(s.ServiceUserName))
                            {
                                sessionName += (" - " + s.ServiceUserName);
                            }
                            return wpSessionName.IsMatch(sessionName);
                        })
                        .DistinctBy(s => s.SessionId)
                        .ToList();

                    if (sessions is null || sessions.Count == 0)
                    {
                        WriteWarning($"'{target}': The session name '{mrs.SessionName}' does not match any in '{folder.GetPSPath()}'.");
                    }
                }
                // 便宜上、要素をひとつだけ入れておく
                if (sessions is null || sessions.Count == 0) sessions = [null];

                // すべての組み合わせを生成して処理
                var combinations = users
                    .SelectMany(user => machines, (user, machine) => new { user, machine })
                    .SelectMany(pair => sessions, (pair, session) => new { pair.user, pair.machine, session });

                foreach (var c in combinations)
                {
                    // セッションの MachineId が不一致ならスキップ
                    if (c.session is not null && c.machine?.Id != c.session.MachineId) continue;

                    targets.Add(new MachineRobotSession()
                    {
                        RobotId = c.user?.UnattendedRobot?.RobotId,
                        MachineId = c.machine?.Id,
                        SessionId = c.session?.SessionId
                    });
                }
            }
            return targets.ToArray();
        }
        catch (Exception ex)
        {
            _this?.WriteError(new ErrorRecord(new OrchException(target, "Failed to deserialize MachineRobots.", ex), "GetUsersError", ErrorCategory.InvalidOperation, target));
            return null;
        }
    }

    protected static DirectoryObject? ResolveDirectoryName(IWritableHost _host, OrchDriveInfo drive, string name, int type)
    {
        string strType = type switch
        {
            0 => "users",
            1 => "groups",
            2 => "machines",
            3 => "robots",
            4 => "applications",
            _ => throw new InvalidOperationException()
        };

        var resolved = drive.SearchDirectory(name).Where(g => g.type == type).ToList();

        if (resolved.Count == 0)
        {
            _host.WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, $"No {strType} found for '{name}'."), "SearchForUsersAndGroupsError", ErrorCategory.InvalidOperation, drive));
            return null;
        }
        if (resolved.Count > 1)
        {
            _host.WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, $"Duplicated {strType} found for '{name}'."), "SearchForUsersAndGroupsError", ErrorCategory.InvalidOperation, drive));
            return null;
        }
        return resolved.First();
    }

    internal static (string?, string?) ExtractPackageIdVersionFromFilePath(string fullPath)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(fullPath);
        var match = Regex.Match(fileName, @"^(.*)\.(\d+)\.(\d+)\.(\d+)$");

        if (match.Success)
        {
            // 前の部分
            string id = match.Groups[1].Value;
            // 3つの数字
            string version = $"{match.Groups[2].Value}.{match.Groups[3].Value}.{match.Groups[4].Value}";

            return (id, version);
        }
        return (null, null);
    }

    internal CredentialStore? FindCredentialStoreId(string target, OrchDriveInfo drive, WildcardPattern? wpCredentialStore)
    {
        var credentialStores = drive.CredentialStores.Get();
        if (wpCredentialStore is not null)
        {
            var matchingCredentialStores = credentialStores.Where(cs => wpCredentialStore.IsMatch(cs.Name)).Take(2).ToList();
            if (matchingCredentialStores.Count == 0)
            {
                Exception e = new($"CredentialStore '{wpCredentialStore}' does not exist.");
                WriteError(new ErrorRecord(new OrchException(target, e), "ResolveCredentialStoreError", ErrorCategory.InvalidOperation, target));
                return null;
            }
            if (matchingCredentialStores.Count == 2)
            {
                Exception e = new($"CredentialStore '{wpCredentialStore}' resolved to multiple credential stores. Ignored.");
                WriteError(new ErrorRecord(new OrchException(target, e), "ResolveCredentialStoreError", ErrorCategory.InvalidOperation, target));
                return null;
            }
            // assert(matchingCredentialStores.Count == 1)
            return matchingCredentialStores[0];
        }
        return null;
    }

    internal void WriteCSVExportedMessage(IWritableHost _this, string? filePath)
    {
        if (filePath is not null)
        {
            WriteObject($"CSV has been exported as '{filePath}'.");
        }
    }
}
