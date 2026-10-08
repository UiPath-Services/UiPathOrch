using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using User = UiPath.PowerShell.Entities.User;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.New, "OrchUserMappingCsv")]
public class NewUserMappingCsvCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? SourceTenant { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? DestinationTenant { get; set; }

    [Parameter(Position = 2, Mandatory = true)]
    public string? ExportCsv { get; set; }

    [Parameter]
    [ArgumentCompleter(typeof(EncodingCompleter))]
    [EncodingArgumentTransformation]
    public Encoding? CsvEncoding { get; set; }

    // Within one tenant (SourceTenant = DestinationTenant): the domain the users and groups move
    // from, and the one they move to. OLD\taro becomes NEW\taro, taro@old.example becomes
    // taro@new.example.
    [Parameter]
    public string? SourceDomain { get; set; }

    [Parameter]
    public string? DestinationDomain { get; set; }

    private static readonly string DefaultCsvName = "UserMapping.csv";
    private static readonly string[] CsvHeaders = ["SourceUserName", "SourceEmail", "SourceDisplayName", "SourceSource", "DestinationUserName", "Name", "SurName", "DisplayName"];

    class MappingCsvLine
    {
        public string? SourceUserName { get; set; }
        public string? SourceEmail { get; set; }
        public string? SourceDisplayName { get; set; }
        public string? SourceSource { get; set; }
        public string? DestinationUserName { get; set; }
        // New-PmUser compatible columns (for creating local users at the destination)
        public string? Name { get; set; }
        public string? SurName { get; set; }
        public string? DisplayName { get; set; }
        // Not a CSV column. Routes destination resolution: robot accounts are matched
        // against the destination TENANT user list (the directory search may not return
        // them), mirroring how the copy cmdlets re-home per-user asset values.
        public bool IsRobot { get; set; }
    }

    // Pure core of the robot-row destination resolution, separated for unit testing.
    // Name match only, no type filter — mirrors the copy-time policy (ResolveDstUserPure),
    // which matches any tenant user by UserName. Returns the destination tenant's own
    // spelling of the name (casing may differ from the source).
    internal static string? ResolveRobotDestination(string? sourceUserName, IEnumerable<User> dstUsers) =>
        dstUsers.FirstOrDefault(u =>
            string.Compare(u.UserName, sourceUserName, StringComparison.OrdinalIgnoreCase) == 0)?.UserName;

    private static void WriteCsvContent(StreamWriter writer, Dictionary<string, MappingCsvLine> userMapping)
    {
        // Directory users first, then robot accounts, each block sorted by name —
        // the two kinds are filled in differently (users resolve via the directory,
        // robots via the tenant user list / manual mapping), so keeping them grouped
        // makes the manual-edit pass much easier than a single name sort.
        foreach (var user in userMapping.OrderBy(l => l.Value.IsRobot).ThenBy(l => l.Key).Select(u => u.Value))
        {
            string[] line = [
                EscapeCsvValue(user.SourceUserName),
                EscapeCsvValue(user.SourceEmail),
                EscapeCsvValue(user.SourceDisplayName),
                EscapeCsvValue(user.SourceSource),
                EscapeCsvValue(user.DestinationUserName),
                EscapeCsvValue(user.Name),
                EscapeCsvValue(user.SurName),
                EscapeCsvValue(user.DisplayName)
            ];
            writer.WriteCsvLine(line);
        }
    }

    private void EnumeratePmGroupMembers(
        OrchDriveInfo srcDrive,
        Dictionary<string, MappingCsvLine> userMappings,
        ProgressReporter reporter, CancellationToken cancelToken)
    {
        ICollection<PmGroup> srcGroups = null;
        try
        {
            srcGroups = srcDrive.PmGroups.Get().ToList();
            reporter.TotalNum = srcGroups.Count;
        }
        catch
        {
            WriteWarning("Failed to get PmGroups. Skipping.");
        }
        if (srcGroups is null) return;

        int index = 0;
        foreach (var group in srcGroups)
        {
            cancelToken.ThrowIfCancellationRequested();

            try
            {
                reporter.WriteProgress(++index, group.displayName);
                var detailedSrcGroup = srcDrive.PmGroups.Get(group.id);

                if (detailedSrcGroup is null || detailedSrcGroup.members is null) continue;

                foreach (var member in detailedSrcGroup.members.Where(m => m.objectType == "DirectoryUser"))
                {
                    cancelToken.ThrowIfCancellationRequested();

                    if (!userMappings.ContainsKey(member.name!))
                    {
                        MappingCsvLine l = new()
                        {
                            SourceUserName = member.name,
                            SourceEmail = member.email,
                            SourceDisplayName = member.displayName,
                            SourceSource = member.source
                        };
                        userMappings[member.name!] = l;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(group.GetPSPath(srcDrive.NameColonSeparator), ex), "GetPmGroupError", ErrorCategory.InvalidOperation, group));
            }
        }
    }

    private void EnumerateTenantUsers(
        OrchDriveInfo srcDrive,
        Dictionary<string, MappingCsvLine> userMappings,
        ProgressReporter reporter, CancellationToken cancelToken)
    {
        List<User> users = null;
        try
        {
            // DirectoryRobot too: robot accounts own most per-user asset values in practice,
            // and their names routinely differ between tenants — leaving them out of the CSV
            // meant the rows had to be discovered by watching the copy's drop warnings.
            // Filtered before counting, so the total is what the loop below walks.
            users = srcDrive.Users.Get()
                .Where(u => u is not null && (u.Type == "DirectoryUser" || u.Type == "DirectoryRobot"))
                .ToList();
            reporter.TotalNum = users.Count;
        }
        catch (Exception ex)
        {
            WriteWarning($"'{srcDrive.NameColonSeparator}': Failed to get Tenant Users. Skipping. {ex.Message}");
        }
        if (users is null) return;

        int index = 0;
        foreach (var user in users)
        {
            cancelToken.ThrowIfCancellationRequested();

            // Written per user although nothing here calls the server: it is the only write this
            // bar gets, and a bar never written never appears -- the stage used to run with no
            // bar on screen at all.
            reporter.WriteProgress(++index, user.UserName);

            if (string.IsNullOrEmpty(user.UserName)) continue;

            if (!userMappings.ContainsKey(user.UserName))
            {
                bool isRobot = user.Type == "DirectoryRobot";
                MappingCsvLine l = new()
                {
                    SourceUserName = user.UserName,
                    SourceEmail = user.EmailAddress,
                    // Informational: lets the operator spot robot rows in the CSV.
                    SourceSource = isRobot ? "robot" : null,
                    IsRobot = isRobot
                };
                userMappings[user.UserName] = l;
            }
        }
    }

    private void EnumerateFolderUsers(
        OrchDriveInfo srcDrive,
        Dictionary<string, MappingCsvLine> userMappings,
        ProgressReporter reporter, CancellationToken cancelToken)
    {
        var folders = srcDrive.GetFolders();
        reporter.TotalNum = folders.Count;

        int index = 0;
        foreach (var folder in folders)
        {
            cancelToken.ThrowIfCancellationRequested();

            try
            {
                reporter.WriteProgress(++index, folder.GetPSPath());
                var folderUsers = srcDrive.FolderUsersWithNoInherited.Get(folder);

                if (folderUsers is null) continue;

                foreach (var folderUser in folderUsers.Where(u => u.UserEntity?.Type == "DirectoryUser" || u.UserEntity?.Type == "DirectoryRobot"))
                {
                    cancelToken.ThrowIfCancellationRequested();

                    if (folderUser.UserEntity is null || string.IsNullOrEmpty(folderUser.UserEntity.UserName)) continue;

                    if (!userMappings.ContainsKey(folderUser.UserEntity.UserName))
                    {
                        bool isRobot = folderUser.UserEntity.Type == "DirectoryRobot";
                        MappingCsvLine l = new()
                        {
                            SourceUserName = folderUser.UserEntity.UserName,
                            SourceSource = isRobot ? "robot" : null,
                            IsRobot = isRobot
                        };
                        userMappings[folderUser.UserEntity.UserName] = l;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetFolderUserError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }

    //private void EnumerateAssetUsers(
    //    OrchDriveInfo srcDrive,
    //    Dictionary<string, MappingCsvLine> userMappings,
    //    ProgressReporter reporter, CancellationToken cancelToken)
    //{
    //    var folders = srcDrive.GetFolders();
    //    reporter.TotalNum = folders.Count;

    //    reporter.TotalNum = folders.Count;

    //    using var results = OrchThreadPool.RunForEach(folders,
    //        folder => folder.GetPSPath(),
    //        folder => folder,
    //        folder => srcDrive.Assets.Get(folder)
    //    );

    //    int index = 0;
    //    foreach (var result in results)
    //    {
    //        cancelToken.ThrowIfCancellationRequested();

    //        try
    //        {
    //            reporter.WriteProgress(++index, $"{index:D}/{folders.Count}");
    //            var assets = result.GetResult(cancelToken);

    //            if (assets is null) continue;

    //            foreach (var asset in assets)
    //            {
    //                cancelToken.ThrowIfCancellationRequested();

    //                foreach (var user in asset.UserValues ?? [])
    //                {
    //                    if (string.IsNullOrEmpty(user.UserName)) continue;

    //                    if (!userMappings.ContainsKey(user.UserName))
    //                    {
    //                        MappingCsvLine l = new()
    //                        {
    //                            SourceUserName = user.UserName
    //                        };
    //                        userMappings[user.UserName] = l;
    //                    }
    //                }
    //            }
    //        }
    //        catch (OrchException ex)
    //        {

    //        }
    //    }
    //}

    // The name a user or group of sourceDomain has in destinationDomain, or null when the name
    // does not carry sourceDomain: DOMAIN\name and name@domain are rewritten; a bare name is not,
    // since nothing in it says which domain it belongs to.
    internal static string? RewriteDomain(string name, string sourceDomain, string destinationDomain)
    {
        int bs = name.IndexOf('\\');
        if (bs > 0)
        {
            return string.Equals(name[..bs], sourceDomain, StringComparison.OrdinalIgnoreCase)
                ? $"{destinationDomain}\\{name[(bs + 1)..]}" : null;
        }
        int at = name.LastIndexOf('@');
        if (at > 0)
        {
            return string.Equals(name[(at + 1)..], sourceDomain, StringComparison.OrdinalIgnoreCase)
                ? $"{name[..at]}@{destinationDomain}" : null;
        }
        return null;
    }

    // Within one tenant: one row per user and group of -SourceDomain (tenant users and folder
    // assignments; robot accounts belong to no domain). DestinationUserName is the rewritten name
    // when the directory knows it, otherwise empty for the operator to fill or delete.
    private void WriteSameTenantMapping(OrchDriveInfo drive, StreamWriter? writer, string? providerCsvPath)
    {
        string[] types = ["DirectoryUser", "DirectoryGroup"];
        var candidates = new Dictionary<string, (string type, string? email)>(StringComparer.OrdinalIgnoreCase);
        void Add(string? name, string? type, string? email)
        {
            if (string.IsNullOrEmpty(name) || type is null || !types.Contains(type)) return;
            if (RewriteDomain(name, SourceDomain!, DestinationDomain!) is null) return;
            candidates.TryAdd(name, (type, email));
        }

        using var cancelHandler = new ConsoleCancelHandler();
        try
        {
            foreach (var user in drive.Users.Get()) Add(user.UserName, user.Type, user.EmailAddress);
        }
        catch (Exception ex)
        {
            WriteWarning($"'{drive.NameColonSeparator}': Failed to get Tenant Users. Skipping. {ex.Message}");
        }

        foreach (var folder in drive.GetFolders().WithProgressBar(this, "Folder users", f => f.GetPSPath()))
        {
            cancelHandler.Token.ThrowIfCancellationRequested();
            try
            {
                foreach (var fu in drive.FolderUsersWithNoInherited.Get(folder)) Add(fu.UserEntity?.UserName, fu.UserEntity?.Type, null);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetFolderUserError", ErrorCategory.InvalidOperation, folder));
            }
        }

        var rows = new Dictionary<string, MappingCsvLine>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, (type, email)) in candidates.OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
            .WithProgressBar(this, "Destination directory", c => c.Key, candidates.Count))
        {
            cancelHandler.Token.ThrowIfCancellationRequested();
            string rewritten = RewriteDomain(name, SourceDomain!, DestinationDomain!)!;
            string? found = null;
            try
            {
                var (resolved, result) = Core.OrchProvider.ResolveDstDirectoryUserPure(
                    drive.SearchDirectory(rewritten), rewritten, DirectoryTypeItems.Items[type]);
                if (result == Core.OrchProvider.FindDstDirectoryUserResult.Resolved) found = resolved!.identityName;
            }
            catch (Exception ex)
            {
                WriteWarning($"'{rewritten}': Failed to search the directory. {ex.Message}");
            }

            rows[name] = new MappingCsvLine
            {
                SourceUserName = name,
                SourceEmail = email,
                SourceSource = type == "DirectoryGroup" ? "group" : null,
                DestinationUserName = found,
            };
        }

        if (writer is null) return;
        WriteCsvContent(writer, rows);
        WriteCSVExportedMessage(this, providerCsvPath);
        if (rows.Count == 0)
        {
            WriteWarning($"No user or group of '{SourceDomain}' was found in '{drive.NameColon}'.");
        }
        else if (rows.Values.All(r => !string.IsNullOrEmpty(r.DestinationUserName)))
        {
            WriteWarning($"Every user and group of '{SourceDomain}' has a counterpart in '{DestinationDomain}'. Copy-OrchUser and Copy-OrchFolderUser can use this CSV within '{drive.NameColon}'.");
        }
        else
        {
            WriteWarning($"Some users or groups of '{SourceDomain}' were not found in '{DestinationDomain}'. Fill out their 'DestinationUserName', or delete the rows, and verify the file with Test-OrchUserMappingCsv.");
        }
    }

    protected override void ProcessRecord()
    {
        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        var srcDrive = SessionState.GetOrchDrive(SourceTenant);
        var dstDrive = SessionState.GetOrchDrive(DestinationTenant);

        if (srcDrive == dstDrive)
        {
            if (string.IsNullOrWhiteSpace(SourceDomain) || string.IsNullOrWhiteSpace(DestinationDomain))
            {
                WriteWarning("The specified SourceTenant and DestinationTenant drives are the same. To map the users of one domain onto another within this tenant, specify -SourceDomain and -DestinationDomain.");
                return;
            }
            WriteSameTenantMapping(srcDrive, writer, providerCsvPath);
            return;
        }

        if (srcDrive.IsSameOrganization(dstDrive))
        {
            WriteWarning("The specified SourceTenant and DestinationTenant belong to the same organization. User migration can proceed without a UserMapping CSV.");
            return;
        }

        // key: SourceUserName
        var userMappings = new Dictionary<string, MappingCsvLine>(StringComparer.OrdinalIgnoreCase);

        using var cancelHandler = new ConsoleCancelHandler();

        // One bar for the run, counting its steps, with a child bar under it for each of the
        // three enumerations -- the shape Copy-Item gives a folder and its entity types. The
        // parent is written at the start of every step: a child names its parent by id, and a
        // parent never written leaves the host holding children of an activity it was never
        // shown. The last three steps have no child bar, so the parent's status names the step;
        // without them the bar would sit at its end through the directory searches, which are
        // the slow part on a large tenant.
        //
        // The child labels share one width (13) so their "[" line up; the parent's is not
        // padded, being the only bar at its indent. Each child stays up once its stage is done,
        // saying so, and all are taken down together at the end of the try.
        const int totalStepNum = 6;
        using ProgressReporter reporter = new(this, totalStepNum, "User mapping");
        reporter.Context = $"{srcDrive.NameColonSeparator} -> {dstDrive.NameColonSeparator}";
        try
        {
            reporter.WriteProgress(1, "group members");
            using ProgressReporter reporterPmGroups = new(this, null, "Group members", reporter);
            EnumeratePmGroupMembers(srcDrive, userMappings, reporterPmGroups, cancelHandler.Token);
            reporterPmGroups.WriteCompleted();

            reporter.WriteProgress(2, "tenant users");
            using ProgressReporter reporterUsers = new(this, null, "Tenant users ", reporter);
            EnumerateTenantUsers(srcDrive, userMappings, reporterUsers, cancelHandler.Token);
            reporterUsers.WriteCompleted();

            // This is necessary because directory users (not just tenant users) can be assigned to folders.
            reporter.WriteProgress(3, "folder users");
            using ProgressReporter reporterFolderUsers = new(this, null, "Folder users ", reporter);
            EnumerateFolderUsers(srcDrive, userMappings, reporterFolderUsers, cancelHandler.Token);
            reporterFolderUsers.WriteCompleted();

            // On second thought, there's no need to search assets.
            // Only users assigned to a folder should be assignable to an asset.
            // What happens if you unassign a user after creating the asset? But we don't need to worry about that.
            //using ProgressReporter reporterAssets = new(this, null, "Assets       ", reporter);
            //EnumerateAssetUsers(srcDrive, userMappings, reporterAssets, cancelHandler.Token);

            // Robot rows never go through the directory searches below — robot accounts
            // may not be returned by them. Resolve against the destination TENANT user
            // list instead, which is what the copy cmdlets match per-user values against
            // (robot accounts do appear there). Same-named robots auto-fill; the rest are
            // left empty for the operator to map (or delete when not needed).
            reporter.WriteProgress(4, "robot accounts");
            var robotRows = userMappings.Values.Where(l => l.IsRobot && string.IsNullOrEmpty(l.DestinationUserName)).ToList();
            if (robotRows.Count > 0)
            {
                List<User> dstUsers = [];
                try
                {
                    dstUsers = dstDrive.Users.Get();
                }
                catch (Exception ex)
                {
                    WriteWarning($"'{dstDrive.NameColonSeparator}': Failed to get destination tenant users; robot rows are left for manual mapping. {ex.Message}");
                }
                foreach (var row in robotRows)
                {
                    row.DestinationUserName = ResolveRobotDestination(row.SourceUserName, dstUsers);
                }
            }

            reporter.WriteProgress(5, "source directory");
            var srcResolvedUsers = srcDrive.PmBulkResolveByName("user",
                userMappings.Where(u => !u.Value.IsRobot && (string.IsNullOrEmpty(u.Value.SourceEmail) || string.IsNullOrEmpty(u.Value.SourceSource))),
                u => u.Key);

            foreach (var srcResolvedUser in srcResolvedUsers)
            {
                var line = userMappings[srcResolvedUser.Key];
                line.SourceEmail = srcResolvedUser.Value?.email;
                line.SourceSource = srcResolvedUser.Value?.source;
            }

            reporter.WriteProgress(6, "destination directory");
            #region First, search by UserName
            var dstResolvedUsersByUserName = dstDrive.PmBulkResolveByName("user",
                userMappings.Where(u => !u.Value.IsRobot && string.IsNullOrEmpty(u.Value.DestinationUserName)),
                u => u.Key);

            foreach (var dstResolvedUser in dstResolvedUsersByUserName)
            {
                userMappings[dstResolvedUser.Key].DestinationUserName = dstResolvedUser.Value?.name;
            }
            #endregion

            #region Search by Email for users not found
            var dstResolvedUsersByEmail = dstDrive.PmBulkResolveByName("user",
                userMappings.Where(u => !u.Value.IsRobot && string.IsNullOrEmpty(u.Value.DestinationUserName) && !string.IsNullOrEmpty(u.Value.SourceEmail)),
                u => u.Value.SourceEmail!);

            // Better to convert to a dictionary first.
            var dicUserMappingsByEmail = userMappings
                .Where(kv => !string.IsNullOrEmpty(kv.Value.SourceEmail))
                .ToDictionary(kv => kv.Value.SourceEmail!, kv => kv.Value);

            foreach (var dstResolvedUser in dstResolvedUsersByEmail)
            {
                if (dicUserMappingsByEmail.TryGetValue(dstResolvedUser.Key, out var line))
                {
                    line.DestinationUserName = dstResolvedUser.Value?.name;
                }
            }
            #endregion
        }
        finally
        {
            if (writer is not null)
            {
                WriteCsvContent(writer, userMappings);
                WriteCSVExportedMessage(this, providerCsvPath);
                if (userMappings.All(u => !string.IsNullOrEmpty(u.Value.DestinationUserName)))
                {
                    WriteWarning($"User migration from '{srcDrive.NameColon}' to '{dstDrive.NameColon}' with this CSV file is ready!");
                }
                else
                {
                    WriteWarning("User mapping is incomplete. Please fill out the 'DestinationUserName' column in the CSV file and verify it using the Test-OrchUserMappingCsv cmdlet.");
                }
            }
        }
    }
}
