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

    // Maximum number of pending entries spelled out in the aggregated warning before the
    // rest collapse into a "+N more" tail.
    internal const int PendingAssignmentListLimit = 20;

    // Aggregated once per run instead of per row: on a fresh cross-org destination most
    // directory users are not tenant users yet, so per-row warnings would drown the report
    // in expected noise. Pending = resolves in the destination directory but no tenant user
    // record yet; folder-user copy creates that record automatically on assignment.
    internal static string FormatPendingAssignmentWarning(IReadOnlyList<string> entries, string dstName)
    {
        var listed = string.Join(", ", entries.Take(PendingAssignmentListLimit));
        string more = entries.Count > PendingAssignmentListLimit ? $", ... (+{entries.Count - PendingAssignmentListLimit} more)" : "";
        return $"{entries.Count} DestinationUserName(s) resolve in the destination directory but are not yet tenant users in '{dstName}': {listed}{more}. This is expected before folder users are copied — folder-user copy (Copy-OrchFolderUser with -UserMappingCsv, or copy -Recurse) assigns directory users and creates the tenant user automatically. Investigate only if per-user asset values still drop after folder users are copied.";
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
        var pendingAssignment = new List<string>();

        // Per-row work can be slow (source rows missing from the tenant user list fall
        // back to scanning every folder's users; unresolved destinations hit the
        // directory search), so show per-entry progress.
        foreach (var entry in entries.OrderBy(e => e.SourceUserName)
            .WithProgressBar(this, "Validating user mapping CSV...", e => e.SourceUserName, entries.Count))
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

            // Reachable as a destination tenant user? This is the check that predicts copy
            // behavior — per-user values are re-homed against the tenant user list — and it
            // also covers robot accounts, which the directory search below may not return.
            if (IsDestinationTenantUser(destinationUserName, srcUser?.EmailAddress, dstUsers))
            {
                okCount++;
                continue;
            }

            // Not a tenant user yet — check the destination directory: a directory user
            // becomes a tenant user automatically when folder users are copied (Pending),
            // while a name found in neither place needs fixing.
            try
            {
                var resolved = dstDrive.SearchDirectory(destinationUserName)?
                    .Where(u => string.Compare(u.identityName, destinationUserName, StringComparison.OrdinalIgnoreCase) == 0)
                    .ToList();

                if (resolved is not null && resolved.Count > 0)
                {
                    pendingAssignment.Add($"'{destinationUserName}' (mapped from '{sourceUserName}')");
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

        if (pendingAssignment.Count > 0)
        {
            WriteWarning(FormatPendingAssignmentWarning(pendingAssignment, dstDrive.NameColon));
        }

        // Output summary
        string pendingPart = pendingAssignment.Count > 0 ? $", {pendingAssignment.Count} Pending (not yet tenant users)" : "";
        WriteObject($"Validation complete: {okCount} OK{pendingPart}, {warningCount} Warning(s), {errorCount} Error(s) out of {entries.Count} entries.");

        if (errorCount == 0 && warningCount == 0)
        {
            if (pendingAssignment.Count == 0)
            {
                WriteObject("All mappings are valid. The CSV is ready to use.");
            }
            else
            {
                WriteObject("All mappings resolve. Copy folder users before assets so the pending users are assigned automatically (see the warning above).");
            }
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
