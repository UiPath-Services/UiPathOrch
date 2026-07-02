using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsDiagnostic.Test, "OrchUserMappingCsv")]
public class TestUserMappingCsvCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true)]
    public string? CsvFile { get; set; }

    [Parameter(Position = 1, Mandatory = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? SourceTenant { get; set; }

    [Parameter(Position = 2, Mandatory = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? DestinationTenant { get; set; }

    private class MappingEntry
    {
        public string SourceUserName { get; set; } = "";
        public string? DestinationUserName { get; set; }
        public string? Name { get; set; }
        public string? SurName { get; set; }
        public string? DisplayName { get; set; }
    }

    // Mirrors the tenant-user matching ResolveDstUserPure applies during the asset copy
    // (mapped-name match first, then source-email fallback) minus the folder-assignment
    // check, which is folder-scoped and cannot be judged at CSV level.
    internal static bool IsDestinationTenantUser(string destinationUserName, string? sourceEmail,
        IEnumerable<UiPath.PowerShell.Entities.User> dstUsers)
    {
        return dstUsers.Any(u =>
            string.Compare(u.UserName, destinationUserName, StringComparison.OrdinalIgnoreCase) == 0
            || (!string.IsNullOrEmpty(sourceEmail)
                && string.Compare(u.UserName, sourceEmail, StringComparison.OrdinalIgnoreCase) == 0));
    }

    private List<MappingEntry> ReadMappingCsv(string physicalPath)
    {
        var entries = new List<MappingEntry>();

        using var enumerator = File.ReadLines(physicalPath).GetEnumerator();

        if (!enumerator.MoveNext())
            throw new InvalidOperationException("The CSV file is empty.");

        var headers = UiPath.PowerShell.Commands.CsvHelper.CsvLine.Split(enumerator.Current);
        int Col(string name) => headers.FindIndex(h => h.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));

        int iSource = Col("SourceUserName");
        int iDest = Col("DestinationUserName");
        int iName = Col("Name");
        int iSurName = Col("SurName");
        int iDisplayName = Col("DisplayName");

        if (iSource == -1 || iDest == -1)
            throw new InvalidOperationException("The CSV file does not contain the required columns: 'SourceUserName' and 'DestinationUserName'.");

        while (enumerator.MoveNext())
        {
            var line = enumerator.Current;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var columns = UiPath.PowerShell.Commands.CsvHelper.CsvLine.Split(line);
            string Val(int idx) => idx >= 0 && idx < columns.Count ? columns[idx].Trim() : "";

            string sourceUserName = Val(iSource);
            if (string.IsNullOrEmpty(sourceUserName)) continue;

            entries.Add(new MappingEntry
            {
                SourceUserName = sourceUserName,
                DestinationUserName = Val(iDest),
                Name = Val(iName),
                SurName = Val(iSurName),
                DisplayName = Val(iDisplayName)
            });
        }

        return entries;
    }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetOrchDrive(SourceTenant);
        var dstDrive = SessionState.GetOrchDrive(DestinationTenant);

        // Read the CSV
        string physicalPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(CsvFile!);
        List<MappingEntry> entries;
        try
        {
            entries = ReadMappingCsv(physicalPath);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, "ReadCsvError", ErrorCategory.ReadError, CsvFile));
            return;
        }

        if (entries.Count == 0)
        {
            WriteWarning("No user mapping entries found in the CSV.");
            return;
        }

        // Get the user list from the source tenant
        var srcUsers = srcDrive.Users.Get();

        // Tenant-level user list from the destination. Directory resolution alone is not
        // enough: per-user asset values are re-homed against the destination *tenant* user
        // list (ResolveDstUserPure), so a name that resolves in the directory but has no
        // tenant user yet still drops during the asset copy.
        var dstUsers = dstDrive.Users.Get();

        int okCount = 0;
        int warningCount = 0;
        int errorCount = 0;

        foreach (var entry in entries.OrderBy(e => e.SourceUserName))
        {
            string sourceUserName = entry.SourceUserName;
            string? destinationUserName = entry.DestinationUserName;

            // Check if SourceUserName exists in the source tenant
            var srcUser = srcUsers.FirstOrDefault(u => string.Compare(u.UserName, sourceUserName, StringComparison.OrdinalIgnoreCase) == 0);

            bool srcFound = srcUser is not null;
            if (!srcFound)
            {
                // Also search as a folder user
                try
                {
                    var folders = srcDrive.GetFolders();
                    foreach (var folder in folders)
                    {
                        var folderUsers = srcDrive.FolderUsersWithNoInherited.Get(folder);
                        if (folderUsers?.Any(fu => string.Compare(fu.UserEntity?.UserName, sourceUserName, StringComparison.OrdinalIgnoreCase) == 0) == true)
                        {
                            srcFound = true;
                            break;
                        }
                    }
                }
                catch
                {
                    // Continue even if folder user search fails
                }
            }

            if (!srcFound)
            {
                WriteWarning($"[WARNING] SourceUserName '{sourceUserName}' was not found in source tenant '{srcDrive.NameColon}'.");
                warningCount++;
            }

            // Check if DestinationUserName is not empty
            if (string.IsNullOrEmpty(destinationUserName))
            {
                WriteWarning($"[WARNING] SourceUserName '{sourceUserName}' has empty DestinationUserName.");
                warningCount++;
                continue;
            }

            // Check if DestinationUserName exists in the destination directory
            try
            {
                var resolved = dstDrive.SearchDirectory(destinationUserName)?
                    .Where(u => string.Compare(u.identityName, destinationUserName, StringComparison.OrdinalIgnoreCase) == 0)
                    .ToList();

                if (resolved is not null && resolved.Count > 0)
                {
                    if (IsDestinationTenantUser(destinationUserName, srcUser?.EmailAddress, dstUsers))
                    {
                        okCount++;
                    }
                    else
                    {
                        WriteWarning($"[WARNING] DestinationUserName '{destinationUserName}' (mapped from '{sourceUserName}') resolves in the destination directory but is not yet a tenant user in '{dstDrive.NameColon}'. Per-user asset values referencing this user will be dropped unless the user is assigned to the destination folder before assets are copied (folder-user copy assigns directory users automatically, e.g.: Copy-OrchFolderUser ... -UserMappingCsv).");
                        warningCount++;
                    }
                }
                else
                {
                    // If not found in the directory, check if the necessary information for local user creation is available
                    bool hasName = !string.IsNullOrEmpty(entry.Name) || !string.IsNullOrEmpty(entry.DisplayName);

                    if (hasName)
                    {
                        WriteWarning($"[WARNING] DestinationUserName '{destinationUserName}' (mapped from '{sourceUserName}') was not found in destination directory, but Name/DisplayName are provided. A local user can be created via New-PmUser.");
                        warningCount++;
                    }
                    else
                    {
                        WriteError(new ErrorRecord(
                            new OrchException(dstDrive.NameColonSeparator, $"DestinationUserName '{destinationUserName}' (mapped from '{sourceUserName}') was not found in destination directory, and Name/DisplayName columns are empty. Please fill in user details or ensure the user exists in the destination."),
                            "DestinationUserNotFoundError",
                            ErrorCategory.ObjectNotFound,
                            dstDrive));
                        errorCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    new OrchException(dstDrive.NameColonSeparator, $"Failed to search for DestinationUserName '{destinationUserName}'", ex),
                    "SearchDirectoryError",
                    ErrorCategory.InvalidOperation,
                    dstDrive));
                errorCount++;
            }
        }

        // Output summary
        WriteObject($"Validation complete: {okCount} OK, {warningCount} Warning(s), {errorCount} Error(s) out of {entries.Count} entries.");

        if (errorCount == 0 && warningCount == 0)
        {
            WriteObject("All mappings are valid. The CSV is ready to use.");
        }
        else if (errorCount == 0)
        {
            WriteWarning("No errors found, but there are warnings. Please review the output above.");
        }
        else
        {
            WriteWarning("Errors found. Please fix the CSV and re-run this cmdlet.");
        }
    }
}
