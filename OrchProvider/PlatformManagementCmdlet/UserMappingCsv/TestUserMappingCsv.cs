using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsDiagnostic.Test, "OrchUserMappingCsv")]
public class TestUserMappingCsvCommand : OrchestratorPSCmdlet
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

    private List<MappingEntry> ReadMappingCsv(string physicalPath)
    {
        var entries = new List<MappingEntry>();

        using var enumerator = File.ReadLines(physicalPath).GetEnumerator();

        if (!enumerator.MoveNext())
            throw new InvalidOperationException("The CSV file is empty.");

        var headers = enumerator.Current.Split(',');
        int Col(string name) => Array.FindIndex(headers, h => h.Trim().Trim('"').Equals(name, StringComparison.OrdinalIgnoreCase));

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

            var columns = line.Split(',');
            string Val(int idx) => idx >= 0 && idx < columns.Length ? columns[idx].Trim().Trim('"') : "";

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

        // CSV を読み込む
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

        // ソーステナントのユーザー一覧を取得
        var srcUsers = srcDrive.GetUsers();

        int okCount = 0;
        int warningCount = 0;
        int errorCount = 0;

        foreach (var entry in entries.OrderBy(e => e.SourceUserName))
        {
            string sourceUserName = entry.SourceUserName;
            string? destinationUserName = entry.DestinationUserName;

            // SourceUserName がソーステナントに存在するか確認
            var srcUser = srcUsers.FirstOrDefault(u => string.Compare(u.UserName, sourceUserName, StringComparison.OrdinalIgnoreCase) == 0);

            bool srcFound = srcUser is not null;
            if (!srcFound)
            {
                // フォルダユーザーとしても探す
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
                    // フォルダユーザーの検索に失敗しても続行
                }
            }

            if (!srcFound)
            {
                WriteWarning($"[WARNING] SourceUserName '{sourceUserName}' was not found in source tenant '{srcDrive.NameColon}'.");
                warningCount++;
            }

            // DestinationUserName が空でないか確認
            if (string.IsNullOrEmpty(destinationUserName))
            {
                WriteWarning($"[WARNING] SourceUserName '{sourceUserName}' has empty DestinationUserName.");
                warningCount++;
                continue;
            }

            // DestinationUserName がデスティネーションディレクトリに存在するか確認
            try
            {
                var resolved = dstDrive.SearchDirectory(destinationUserName)?
                    .Where(u => string.Compare(u.identityName, destinationUserName, StringComparison.OrdinalIgnoreCase) == 0)
                    .ToList();

                if (resolved is not null && resolved.Count > 0)
                {
                    okCount++;
                }
                else
                {
                    // ディレクトリに見つからない場合、ローカルユーザー作成に必要な情報があるか確認
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

        // サマリを出力
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
