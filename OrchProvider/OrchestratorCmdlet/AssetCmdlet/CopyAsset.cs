using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.Name_Destination;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Copy, "OrchAsset", SupportsShouldProcess = true)]
    [OutputType(typeof(Entities.Asset))]
    public class CopyAssetCommand : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(AssetNameCompleter<Positional.Name_Destination>))]
        public string[]? Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string? Destination { get; set; }

        //[Parameter]
        //[ArgumentCompleter(typeof(AssetValueTypeCompleter<Positional>))]
        //[SupportsWildcards]
        //public string[]? ValueType { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SupportsWildcards]
        public string? Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public uint Depth { get; set; }

        protected override void ProcessRecord()
        {
            var (srcDrive, srcRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Path);
            var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path, Recurse.IsPresent, Depth);

            var (dstDrive, dstRootFolder) = OrchDriveInfo.ResolveToSingleFolder(Destination);

            // コピー元とコピー先が同じなら、何もしない
            if (srcDrive == dstDrive && srcRootFolder.FullyQualifiedName == dstRootFolder.FullyQualifiedName) return;

            var wpName = Name.ConvertToWildcardPatternList();

            // キャッシュは暗黙にクリアしない方が良いか。。
            //srcDrive._dicExtendedMachines = null;
            //srcDrive._dicAssetLinks = null;
            //srcDrive._dicCredentialStores = null;
            //dstDrive._dicCredentialStores = null;
            //srcDrive._dicUsers = null; // TODO: AD ユーザーに対応する必要があるのではないか？
            //dstDrive._dicUsers = null;
            //srcDrive._dicExtendedMachines = null; // dstDrive のキャッシュはクリア不要。フォルダマシンを取得するため。

            string msg = "Copying assets...";
            using var reporterAssets = new ProgressReporter(this, 600, Int32.MaxValue, msg, msg);
            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var (_, srcFolder) in srcDrivesFolders)
            {
                // コピー対象のエンティティがひとつもなければ、dstFolder を検索する必要はない
                //srcDrive._dicAssets?.TryRemove(srcFolder.Id ?? 0, out _);
                var srcEntities = srcDrive.GetAssets(srcFolder).FilterByWildcards(e => e?.Name, wpName);
                if (!srcEntities.Any()) continue;

                Folder? dstFolder = GetRelativeDstFolder(srcRootFolder, srcFolder, dstDrive, dstRootFolder);
                if (dstFolder == null) continue;

                try
                {
                    Core.OrchProvider.CopyAssets(this,
                        srcDrive, srcFolder, wpName,
                        dstDrive, dstFolder, reporterAssets,
                        cancelHandler.Token, false);
                    dstDrive._dicAssets?.TryRemove(dstFolder.Id ?? 0, out _);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string target = dstFolder.GetPSPath();
                    WriteError(new ErrorRecord(new OrchException(target, ex), "CopyAssetError", ErrorCategory.InvalidOperation, dstFolder));
                }
            }





            //var srcDrives = OrchDriveInfo.EnumOrchDrives(Path);
            //var srcDrivesFolders = OrchDriveInfo.EnumFolders(Path);
            //var dstDrivesFolders = OrchDriveInfo.EnumFolders(Destination);
            //var wpName = Name.ConvertToWildcardPatternList();

            //foreach (var srcDrive in srcDrives)
            //{
            //    srcDrive._dicAssetLinks = null;
            //}

            //// コピーの直前でキャッシュを削除するので、ここで取得しておくのは意味がない

            //string msg = "Copying assets...";
            //using var reporterAssets = new ProgressReporter(this, 600, Int32.MaxValue, msg, msg);
            //foreach (var dstDriveFolder in dstDrivesFolders)
            //{
            //    var (dstDrive, dstFolder) = dstDriveFolder;
            //    foreach (var srcDriveFolder in srcDrivesFolders)
            //    {
            //        var (srcDrive, srcFolder) = srcDriveFolder;

            //        try
            //        {
            //            Core.OrchProvider.CopyAssets(this,
            //                srcDrive, srcFolder, wpName,
            //                dstDrive, dstFolder, reporterAssets, false);
            //            dstDrive._dicAssets?.TryRemove(dstFolder.Id ?? 0, out _);
            //        }
            //        catch (OperationCanceledException)
            //        {
            //            throw;
            //        }
            //        catch (Exception ex)
            //        {
            //            string target = dstFolder.GetPSPath();
            //            WriteError(new ErrorRecord(new OrchException(target, ex), "CopyAssetError", ErrorCategory.InvalidOperation, dstFolder));
            //        }
            //    }
            //}
        }
    }
}
