using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UiPath.OrchAPI;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Path = System.IO.Path;
using Session = UiPath.PowerShell.Entities.Session;
using User = UiPath.PowerShell.Entities.User;

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

            var writer = new StreamWriter(filePath, false, encoding);
            writer.WriteLine(string.Join(",", headers));

            return writer;
        }

        protected Folder? GetRelativeDstFolder(Folder srcRootFolder, Folder srcFolder, OrchDriveInfo dstDrive, Folder dstRootFolder)
        {
            var strDstRootFolder = dstRootFolder.FullyQualifiedName;
            //if (strDstRootFolder != "") strDstRootFolder += '/';

            // srcFolder の、srcRootFolder からの相対パスを取得
            string relativePath = srcFolder.FullyQualifiedName![srcRootFolder.FullyQualifiedName!.Length..];
            relativePath = relativePath.TrimStart('/').TrimEnd('/');

            string strDstFolder = null;
            if (strDstRootFolder == "")
            {
                if (relativePath == "")
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

            var dstFolder = dstDrive._dicFolders!.FirstOrDefault(f => string.Compare(f.FullyQualifiedName, strDstFolder, StringComparison.OrdinalIgnoreCase) == 0);
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
                    var robots2 = drive!.GetRobots();
                    mrs.RobotName = robots2.FirstOrDefault(r => r.Id == elem.RobotId)?.Name;
                }

                // MachineId を変換
                if (elem.MachineId != null)
                {
                    var machines = drive!.GetMachines();
                    mrs.MachineName = machines.FirstOrDefault(m => m.Id == elem.MachineId)?.Name;
                }

                // SessionId を変換
                if (elem.SessionId != null)
                {
                    var sessions = drive.GetMachineSessionRuntimesByFolderId(folder!);
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

        internal void WriteCSVExportedMessage(IWritableHost _this, string? filePath)
        {
            if (filePath != null)
            {
                WriteObject($"CSV has been exported as '{filePath}'.");
            }
        }
    }
}
