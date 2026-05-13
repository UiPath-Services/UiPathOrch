using System.Management.Automation;
using System.Text;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
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
    }

    private static void WriteCsvContent(StreamWriter writer, Dictionary<string, MappingCsvLine> userMapping)
    {
        foreach (var user in userMapping.OrderBy(l => l.Key).Select(u => u.Value))
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
                reporter.WriteProgress(++index);
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
        ICollection<User> users = null;
        try
        {
            users = srcDrive.Users.Get();
            reporter.TotalNum = users.Count;
        }
        catch (Exception ex)
        {
            WriteWarning($"'{srcDrive.NameColonSeparator}': Failed to get Tenant Users. Skipping. {ex.Message}");
        }
        if (users is null) return;

        foreach (var user in users.Where(u => u.Type == "DirectoryUser"))
        {
            cancelToken.ThrowIfCancellationRequested();

            if (user is null || string.IsNullOrEmpty(user.UserName)) continue;

            if (!userMappings.ContainsKey(user.UserName))
            {
                MappingCsvLine l = new()
                {
                    SourceUserName = user.UserName,
                    SourceEmail = user.EmailAddress
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
                reporter.WriteProgress(++index);
                var folderUsers = srcDrive.FolderUsersWithNoInherited.Get(folder);

                if (folderUsers is null) continue;

                foreach (var folderUser in folderUsers.Where(u => u.UserEntity?.Type == "DirectoryUser"))
                {
                    cancelToken.ThrowIfCancellationRequested();

                    if (folderUser.UserEntity is null || string.IsNullOrEmpty(folderUser.UserEntity.UserName)) continue;

                    if (!userMappings.ContainsKey(folderUser.UserEntity.UserName))
                    {
                        MappingCsvLine l = new()
                        {
                            SourceUserName = folderUser.UserEntity.UserName
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

    protected override void ProcessRecord()
    {
        var (physicalCsvPath, providerCsvPath) = GenerateCsvFilePath(ExportCsv, SessionState, DefaultCsvName);
        using var writer = WriteCsvHeader(physicalCsvPath, CsvEncoding, CsvHeaders);

        var srcDrive = SessionState.GetOrchDrive(SourceTenant);
        var dstDrive = SessionState.GetOrchDrive(DestinationTenant);

        if (srcDrive == dstDrive)
        {
            WriteWarning("The specified SourceTenant and DestinationTenant drives are the same. Please ensure that they are different and try again.");
            return;
        }

        if (srcDrive.GetPartitionGlobalId() == dstDrive.GetPartitionGlobalId())
        {
            WriteWarning("The specified SourceTenant and DestinationTenant belong to the same organization. User migration can proceed without a UserMapping CSV.");
            return;
        }

        // key: SourceUserName
        var userMappings = new Dictionary<string, MappingCsvLine>(StringComparer.OrdinalIgnoreCase);

        // totalNum: PmGroups, folders, assets
        int totalStageNum = 3;

        string msg = "Generating user mapping csv...";
        using var cancelHandler = new ConsoleCancelHandler();
        using ProgressReporter reporter = new(this, 1, totalStageNum, msg);
        try
        {
            msg = "Enumerating PmGroup Members...          ";
            using ProgressReporter reporterPmGroups = new(this, 100, Int32.MaxValue, msg);
            EnumeratePmGroupMembers(srcDrive, userMappings, reporterPmGroups, cancelHandler.Token);

            msg = "Enumerating Tenant Users...             ";
            using ProgressReporter reporterUsers = new(this, 200, Int32.MaxValue, msg);
            EnumerateTenantUsers(srcDrive, userMappings, reporterUsers, cancelHandler.Token);

            // This is necessary because directory users (not just tenant users) can be assigned to folders.
            msg = "Enumerating Users assigned in Folders...";
            using ProgressReporter reporterFolderUsers = new(this, 300, Int32.MaxValue, msg);
            EnumerateFolderUsers(srcDrive, userMappings, reporterFolderUsers, cancelHandler.Token);

            // On second thought, there's no need to search assets.
            // Only users assigned to a folder should be assignable to an asset.
            // What happens if you unassign a user after creating the asset? But we don't need to worry about that.
            //msg = "Enumerating Users assigned in Assets... ";
            //using ProgressReporter reporterAssets = new(this, 400, Int32.MaxValue, msg);
            //EnumerateAssetUsers(srcDrive, userMappings, reporterAssets, cancelHandler.Token);

            msg = $"Searching Source directory...          ";
            var srcResolvedUsers = srcDrive.PmBulkResolveByName("user",
                userMappings.Where(u => string.IsNullOrEmpty(u.Value.SourceEmail) || string.IsNullOrEmpty(u.Value.SourceSource)),
                u => u.Key);

            foreach (var srcResolvedUser in srcResolvedUsers)
            {
                var line = userMappings[srcResolvedUser.Key];
                line.SourceEmail = srcResolvedUser.Value?.email;
                line.SourceSource = srcResolvedUser.Value?.source;
            }

            msg = $"Searching Destination directory...     ";
            #region First, search by UserName
            var dstResolvedUsersByUserName = dstDrive.PmBulkResolveByName("user",
                userMappings.Where(u => string.IsNullOrEmpty(u.Value.DestinationUserName)),
                u => u.Key);

            foreach (var dstResolvedUser in dstResolvedUsersByUserName)
            {
                userMappings[dstResolvedUser.Key].DestinationUserName = dstResolvedUser.Value?.name;
            }
            #endregion

            #region Search by Email for users not found
            var dstResolvedUsersByEmail = dstDrive.PmBulkResolveByName("user",
                userMappings.Where(u => string.IsNullOrEmpty(u.Value.DestinationUserName) && !string.IsNullOrEmpty(u.Value.SourceEmail)),
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
