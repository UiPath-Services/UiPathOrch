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
    // Drives this cmdlet is about to operate on -- only their PendingWarning
    // gets flushed in BeginProcessing. The legacy code walked every registered
    // drive, which surfaced unrelated drives' pending warnings during the next
    // cmdlet on an unrelated drive ("Orch1:\> Get-OrchAsset" emitting WARNINGs
    // about 'local:\' because local: had touched HTTP earlier).
    //
    // Smart default covers the two dominant conventions:
    //   1) the current location's drive (no -Path required), and
    //   2) any string / string[] -Path parameter values, drive name parsed out.
    //
    // Subclasses whose drive-targeting parameter is named differently
    // (-SourcePath / -DestinationPath / -Folder / ...) override this method
    // so their target drives flush in the same cmdlet call rather than the
    // next one. Drives that don't make it into the set keep their warning
    // pending; it surfaces the next time they're current or named on -Path.
    protected virtual IEnumerable<string> GetTargetDriveNames()
    {
        // (1) Current location -- map Du / Tm shadow drives back to their parent
        // because PendingWarning lives on the parent's OrchAPISession.
        switch (SessionState.Drive.Current)
        {
            case OrchDriveInfo od: yield return od.Name; break;
            case OrchDuDriveInfo du: yield return du.ParentDrive.Name; break;
            case OrchTmDriveInfo tm: yield return tm.ParentDrive.Name; break;
        }

        // (2)(3) Bound -Path / -Destination values -- delegated to a static
        // helper so the keys covered by the smart default can be locked in
        // by the test suite without spinning up a PSCmdlet runtime.
        foreach (var name in GetTargetDriveNamesFromBoundParameters(MyInvocation.BoundParameters))
            yield return name;
    }

    // Smart-default's bound-parameter half: extracts drive names from -Path
    // (covers ~88% of cmdlets) and -Destination (Copy-Orch* convention -- 25
    // cmdlets share the exact name, all as Orch drive paths). Export-Orch*
    // 4 cmdlets also expose -Destination but as a local filesystem path
    // (C:\Downloads, etc.); the BeginProcessing loop intersects against
    // EnumAllOrchDrives so non-Orch names like "C" filter out as no-ops.
    // No false positives in practice.
    internal static IEnumerable<string> GetTargetDriveNamesFromBoundParameters(
        IDictionary<string, object> boundParameters)
    {
        if (boundParameters.TryGetValue("Path", out var pathObj))
            foreach (var n in ExtractDriveNamesFromBoundPath(pathObj)) yield return n;

        if (boundParameters.TryGetValue("Destination", out var destObj))
            foreach (var n in ExtractDriveNamesFromBoundPath(destObj)) yield return n;
    }

    // Coerces a BoundParameters["Path"] value (which the PowerShell binder
    // may deliver as string[], string, or other IEnumerable<string> shapes)
    // into the drive names it references. Empty / null entries and entries
    // without a parseable drive prefix are silently skipped so the caller's
    // target-drive set stays clean. Pure helper so the test suite can cover
    // the shape combinations without a real PSCmdlet runtime.
    internal static IEnumerable<string> ExtractDriveNamesFromBoundPath(object? pathObj)
    {
        IEnumerable<string>? paths = pathObj switch
        {
            IEnumerable<string> arr => arr,
            string s => [s],
            _ => null,
        };
        if (paths is null) yield break;

        foreach (var p in paths)
        {
            var name = OrchDriveInfo.ExtractDriveName(p);
            if (!string.IsNullOrEmpty(name)) yield return name;
        }
    }

    // Flush deferred warnings for the drives this cmdlet will touch (see
    // GetTargetDriveNames). Deferring rather than emitting at the producer
    // site is still required because some producers (OrchProvider.GetChildItems)
    // run during tab completion where Console output would corrupt the prompt.
    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        var targets = new HashSet<string>(
            GetTargetDriveNames().Where(n => !string.IsNullOrEmpty(n)),
            StringComparer.OrdinalIgnoreCase);
        if (targets.Count == 0) return;

        foreach (var drive in SessionState.EnumAllOrchDrives())
        {
            if (!targets.Contains(drive.Name)) continue;

            var warning = drive.OrchAPISession.PendingWarning;
            if (warning is null) continue;

            drive.OrchAPISession.ClearPendingWarning();
            drive.OrchAPISession.EntraIdWarningChecked = true;
            // Producers concatenate multiple warnings with "\n\n". A single
            // WriteWarning() with embedded newlines would render the blank
            // separator as a stray "WARNING:" line, so emit each paragraph
            // as its own WriteWarning call.
            foreach (var segment in warning.Split(["\n\n"], StringSplitOptions.RemoveEmptyEntries))
            {
                WriteWarning(segment);
            }
        }
    }

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

        // Escape PowerShell wildcard characters
        if (escapeWildcard)
        {
            value = WildcardPattern.Escape(value);
        }

        // CSV-specific escaping
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

        // If UTC, append "Z" suffix
        if (dateTime.Value.Kind == DateTimeKind.Utc)
        {
            return dateTime.Value.ToString(format) + "Z";
        }
        else
        {
            // Local or Unspecified
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
        catch // If a file path (not a directory path) was specified, strip the non-existent file name and re-resolve the parent folder path
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
            //case 0: // This should not happen because GetResolvedPSPathFromPSPath() would throw an exception
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

        // For UTF-8, add a BOM
        if (encoding is UTF8Encoding)
        {
            encoding = new UTF8Encoding(true);
        }
        // For UTF-16 or UTF-16BE, also add a BOM
        else if (encoding is UnicodeEncoding unicodeEncoding)
        {
            // Determine endianness from the byte array
            byte[] testBytes = unicodeEncoding.GetBytes("A");

            // For big-endian, "A" (U+0041) is encoded as [0x00, 0x41]
            bool isBigEndian = testBytes[0] == 0x00 && testBytes[1] == 0x41;

            // Convert to a UnicodeEncoding with BOM
            encoding = new UnicodeEncoding(isBigEndian, true);
        }
        else if (encoding is UTF32Encoding utf32Encoding)
        {
            // For UTF-32 encoding, determine endianness from the byte array
            byte[] testBytes = utf32Encoding.GetBytes("A");

            // For big-endian, "A" (U+0041) is encoded as [0x00, 0x00, 0x00, 0x41]
            bool isBigEndian = testBytes[0] == 0x00 && testBytes[1] == 0x00 && testBytes[2] == 0x00 && testBytes[3] == 0x41;

            // Convert to a UTF32Encoding with BOM
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

        // Using drive.RobotsFromFolder.Get(folder) would be more reliable in all scenarios,
        // but performance matters for the completer implementation.
        // Since we are serializing already-registered content, drive.Robots.Get() should work fine.
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
                var session = drive.MachineSessionRuntimesByFolder.Fetch(folder)
                    //.Where(s => s.RuntimeType == "Unattended") // Not needed. SessionId is the same regardless of RuntimeType.
                    .Where(s => s.SessionId == mr.SessionId)
                    .FirstOrDefault();
                sessionName = session?.HostMachineName + " - " + session?.ServiceUserName;
            }

            // Robot.Username is populated only for Classic robots; Modern
            // (folder-user-bound) robots leave Username null and carry the
            // actual user name on Robot.User.UserName. The mr.RobotUserName
            // server field is sometimes also populated on the binding
            // record itself — use whichever source first yields a non-empty
            // value so CSV exports retain the binding identity. Without
            // this fallback, a user-bound HttpTrigger round-trips as
            // `[{}]` on the CSV side and the Update pass wipes the binding.
            string? resolvedUserName = robot?.Username;
            if (string.IsNullOrEmpty(resolvedUserName)) resolvedUserName = robot?.User?.UserName;
            if (string.IsNullOrEmpty(resolvedUserName)) resolvedUserName = mr.RobotUserName;

            mrss.Add(new MachineRobotSessionForSerialize()
            {
                UserName = resolvedUserName,
                MachineName = machineName,
                SessionName = sessionName
            });
        }

        //return string.Join(',', mrss.Select(e => JsonSerializer.Serialize(e, OrchAPISession.jsoWhenWritingNull))).Replace("\\u0027", "'");
        return JsonSerializer.Serialize(mrss, JsonTools.jsoWhenWritingNull).Replace("\\u0027", "'");
    }

    private readonly Lazy<HashSet<string>> ValidScopes = new(() =>
        ["Default", "Shared", "PersonalWorkspace", "Cloud", "AutomationCloudRobot", "ElasticRobot"]);

    // executorRobots receives an enumeration of RobotName values.
    // Since we concatenate SelectMany() results, there is no need to build an internal List<RobotExecutor>. This is more efficient.
    internal static RobotExecutor[]? DeserializeExecutorRobots(IWritableHost? _this, OrchDriveInfo drive, Folder folder, string target, IEnumerable<string>? executorRobots)
    {
        if (executorRobots is null || executorRobots.All(string.IsNullOrEmpty)) return null;

        try
        {
            var robots = drive.Robots.Get();
            var result = executorRobots
                .SelectMany(executorRobot =>
                {
                    // Extract matching robots
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
            var tenantUsers = drive.Users.Get();

            // This call was previously needed to prevent Orchestrator from failing to find robots,
            // but since we replaced it with GetUsers(), it is no longer necessary.
            //_ = drive.RobotsFromFolder.Get(folder);

            // These may contain wildcards, so expand all of them
            IEnumerable<MachineRobotSessionForSerialize?> mrss = null;
            if (machineRobots.Length == 1 && machineRobots[0].StartsWith('[')) // && machineRobots[0].EndsWith(']'))
            {
                // When imported from CSV, deserialize as an array
                mrss = JsonSerializer.Deserialize<MachineRobotSessionForSerialize[]>(machineRobots[0]);
            }
            else
            {
                mrss = machineRobots.Select(mr => JsonSerializer.Deserialize<MachineRobotSessionForSerialize>(mr));
            }

            List<MachineRobotSession> targets = [];

            foreach (var mrs in mrss ?? [])
            {
                // Resolve UserName to the appropriate Id
                // Multiple Users may match because wildcards are supported
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

                // Resolve MachineName to the appropriate Id
                // Multiple Machines may match because wildcards are supported
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

                // Skip if neither user nor machine is available
                if ((users is null || users.Count == 0) && (machines is null || machines.Count == 0)) continue;

                // For convenience, insert a single null element as a placeholder
                if (users is null || users.Count == 0) users = [null!];
                if (machines is null || machines.Count == 0) machines = [null!];

                // Resolve SessionName to the appropriate Id
                // Multiple Sessions may match because wildcards are supported
                List<MachineSessionRuntime> sessions = null;
                if (!string.IsNullOrEmpty(mrs?.SessionName))
                {
                    var wpSessionName = new WildcardPattern(mrs.SessionName, WildcardOptions.IgnoreCase);
                    sessions = drive.MachineSessionRuntimesByFolder.Fetch(folder)
                        //.Where(s => s.RuntimeType == "Unattended") // Not needed. SessionId is the same regardless of RuntimeType.
                        //.Where(s => wpMachineName.IsMatch(s.MachineName)) // This condition is evaluated later
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
                // For convenience, insert a single null element as a placeholder
                if (sessions is null || sessions.Count == 0) sessions = [null!];

                // Generate and process all combinations
                var combinations = users
                    .SelectMany(user => machines, (user, machine) => new { user, machine })
                    .SelectMany(pair => sessions, (pair, session) => new { pair.user, pair.machine, session });

                foreach (var c in combinations)
                {
                    // Skip if the session's MachineId does not match
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

    protected static DirectoryObject? ResolveDirectoryName(IWritableHost _host, OrchDriveInfo drive, string name, int type, string? domain = null)
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

        var resolved = drive.SearchDirectory(name, domain).Where(g => g.type == type).ToList();

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
            // The preceding part
            string id = match.Groups[1].Value;
            // The three numeric segments
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

    // When Select-Object -First N (or similar) stops the pipeline, PowerShell throws
    // PipelineStoppedException from WriteObject(). Our catch (Exception ex) blocks wrap
    // it in OrchException and call WriteError(), which surfaces it as a spurious error.
    // By re-throwing PipelineStoppedException here, we let PowerShell handle pipeline
    // termination cleanly without showing an error message to the user.
    protected new void WriteError(ErrorRecord errorRecord)
    {
        var ex = errorRecord.Exception;
        while (ex is not null)
        {
            if (ex is PipelineStoppedException pse)
                throw pse;
            ex = ex.InnerException;
        }
        base.WriteError(errorRecord);
    }
}
