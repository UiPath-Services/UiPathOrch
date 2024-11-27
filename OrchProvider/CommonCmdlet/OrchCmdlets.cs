using System.Data;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Path = System.IO.Path;

namespace UiPath.PowerShell.Commands
{
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
        // TODO
        //public void EnsureLocationInOrchestratorFolder()
        //{
        //    if (CurrentLocation == null || CurrentLocation == "" || CurrentLocation == "/")
        //    {
        //        throw new Exception("Set-Location to Orchestrator folder with cd command first.");
        //    }
        //}

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
            if (value == null) return "";

            // PowerShell のワイルドカード文字をエスケープ
            if (escapeWildcard)
            {
                value = WildcardPattern.Escape(value);
            }

            // CSV特有のエスケープ処理
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
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
            return value?.ToString() ?? "";
        }

        internal static string EscapeCsvValue(DateTime? value)
        {
            return value?.ToString() ?? "";
        }

        internal static string EscapeCsvValue(IEnumerable<string>? values, bool escapeWildcard = false)
        {
            if (values == null) return "";
            if (escapeWildcard)
            {
                return EscapeCsvValue(string.Join(',', values
                    .OrderBy(r => r)
                    .Select(r => WildcardPattern.Escape(r)) ?? []));
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
            if (dateTime == null) return null;

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

        internal static string? GenerateCsvFilePath(string? paramExportCsv, SessionState state, string defaultFileName)
        {
            if (string.IsNullOrEmpty(paramExportCsv)) return null;

            ICollection<PathInfo> resolvedPaths = null;
            
            try
            {
                resolvedPaths = state.Path.GetResolvedPSPathFromPSPath(paramExportCsv);
            }
            catch
            {
                string parentFolder = Path.GetDirectoryName(paramExportCsv);
                defaultFileName = Path.GetFileName(paramExportCsv);
                try
                {
                    resolvedPaths = state.Path.GetResolvedPSPathFromPSPath(parentFolder);
                }
                catch
                {
                    throw new FileNotFoundException($"The specified path '{paramExportCsv}' does not exist.");
                }
            }

            string resolvedPath;
            switch (resolvedPaths.Count)
            {
                case 1:
                    resolvedPath = resolvedPaths.First().ProviderPath;
                    break;
                case 0:
                    var parentFolder = Path.GetDirectoryName(paramExportCsv);
                    var fileName = Path.GetFileName(paramExportCsv);
                    resolvedPaths = state.Path.GetResolvedPSPathFromPSPath(parentFolder);
                    resolvedPath = resolvedPaths.Count switch
                    {
                        1 => Path.Combine(resolvedPaths.First().ProviderPath, fileName),
                        0 => throw new FileNotFoundException($"The specified path '{paramExportCsv}' does not exist."),
                        _ => throw new InvalidOperationException($"The specified path '{paramExportCsv}' resolves to multiple locations."),
                    };
                    break;
                default:
                    throw new InvalidOperationException($"The specified path '{paramExportCsv}' resolves to multiple locations.");
            }

            if (Directory.Exists(resolvedPath))
            {
                return Path.Combine(resolvedPath, defaultFileName);
            }
            else
            {
                return resolvedPath!;
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
            WriteCsvLine(writer, headers);

            return writer;
        }

        // writer.WriteLine(string.Join(',', values) とすると、内部で string を連結してしまう。
        // 逐次 writer.Write() を呼ぶ方が効率的だ。
        internal static void WriteCsvLine(TextWriter writer, string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                writer.Write(values[i]); // 各値を直接書き込む
                if (i < values.Length - 1)
                {
                    writer.Write(','); // フィールド間のカンマを挿入
                }
            }
            writer.WriteLine(); // 最後に改行を追加
        }

        protected Folder? GetRelativeDstFolder(Folder srcRootFolder, Folder srcFolder, OrchDriveInfo dstDrive, Folder dstRootFolder, bool includeRoot = false)
        {
            var strDstRootFolder = dstRootFolder.FullyQualifiedName;
            //if (strDstRootFolder != "") strDstRootFolder += '/';

            // srcFolder の、srcRootFolder からの相対パスを取得
            string relativePath = srcFolder.FullyQualifiedName![srcRootFolder.FullyQualifiedName!.Length..];
            relativePath = relativePath.TrimStart('/').TrimEnd('/');

            string strDstFolder = null;
            if (strDstRootFolder == "")
            {
                if (!includeRoot && relativePath == "")
                {
                    WriteError(new ErrorRecord(
                        new OrchException(dstDrive.NameColonSeparator, $"Folder entities cannot be copied to {dstDrive.NameColonSeparator}."),
                        "CopyFolderEntityToRootFolderError",
                        ErrorCategory.InvalidOperation, 
                        dstDrive));
                    return null;
                }
                strDstFolder = relativePath;
            }
            else
            {
                strDstFolder = (strDstRootFolder + '/' + relativePath).Trim('/');
            }

            if (string.IsNullOrEmpty(strDstFolder))
            {
                return dstDrive.RootFolder;
            }

            var dstFolder = dstDrive.GetFolders().FirstOrDefault(f => string.Compare(f.FullyQualifiedName, strDstFolder, StringComparison.OrdinalIgnoreCase) == 0);
            if (dstFolder == null)
            {
                strDstFolder = strDstFolder.Replace('/', '\\');
                WriteError(new ErrorRecord(
                    new OrchException(srcFolder.GetPSPath(), $"{dstDrive.NameColonSeparator}{strDstFolder} does not exist."),
                    "NoCorrespondingDstFolderError",
                    ErrorCategory.InvalidOperation,
                    dstDrive));
                return null;
            }

            return dstFolder;
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

        internal static string? SerializeMachineRobotSessionArray(OrchDriveInfo drive, Folder folder, MachineRobotSession[]? machineRobots)
        {
            if (machineRobots == null || machineRobots.Length == 0) return null;

            List<MachineRobotSessionForSerialize> mrss = [];

            foreach (var elem in machineRobots)
            {
                MachineRobotSessionForSerialize mrs = new();

                // RobotId を変換
                if (elem.RobotId != null)
                {
                    var robots2 = drive.Robots.Get();
                    mrs.RobotName = robots2.FirstOrDefault(r => r.Id == elem.RobotId)?.Name;
                }

                // MachineId を変換
                if (elem.MachineId != null)
                {
                    var machines = drive!.Machines.Get();
                    mrs.MachineName = machines.FirstOrDefault(m => m.Id == elem.MachineId)?.Name;
                }

                // SessionId を変換
                if (elem.SessionId != null)
                {
                    var sessions = drive.MachineSessionRuntimesByFolder.Get(folder);
                    mrs.HostMachineName = sessions.FirstOrDefault(s => s.SessionId == elem.SessionId)?.HostMachineName;
                }
                mrss.Add(mrs);
            }

            return JsonSerializer.Serialize(mrss, OrchAPISession.jsoWhenWritingNull);
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
            if (wpCredentialStore != null)
            {
                var matchingCredentialStores = credentialStores.Where(cs => wpCredentialStore.IsMatch(cs.Name));
                if (!matchingCredentialStores.Any())
                {
                    Exception e = new($"CredentialStore '{wpCredentialStore}' does not exist.");
                    WriteError(new ErrorRecord(new OrchException(target, e), "ResolveCredentialStoreError", ErrorCategory.InvalidOperation, target));
                    return null;
                }
                if (matchingCredentialStores.Take(2).Count() == 2)
                {
                    Exception e = new($"CredentialStore '{wpCredentialStore}' resolved to multiple credential stores. Ignored.");
                    WriteError(new ErrorRecord(new OrchException(target, e), "ResolveCredentialStoreError", ErrorCategory.InvalidOperation, target));
                    return null;
                }
                // assert(matchingCredentialStores.Couint() == 1)
                return matchingCredentialStores.First();
            }
            return null;
        }

        internal void WriteCSVExportedMessage(IWritableHost _this, string? filePath)
        {
            if (filePath != null)
            {
                WriteObject($"CSV has been exported as '{filePath}'.");
            }
        }
    }
}
