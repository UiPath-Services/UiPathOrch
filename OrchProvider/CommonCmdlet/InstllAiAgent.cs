using System.Reflection;
using System.Management.Automation;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsLifecycle.Install, "OrchAiShellAgent")]
public class InstallAiShellAgentCommand : PSCmdlet
{
    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    protected override void ProcessRecord()
    {
        // ローカルアプリケーションデータフォルダを基点に agents フォルダのパスを構築
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string agentsFolder = System.IO.Path.Combine(localAppData, "Programs", "AIShell", "agents");

        // agents フォルダが存在しない場合、警告を表示して処理を中断
        if (!Directory.Exists(agentsFolder))
        {
            //WriteWarning();
            //var ex = new Exception($"必要なフォルダが見つかりません: {agentsFolder}");
            //var errorRecord = new ErrorRecord(ex, "FolderNotFound", ErrorCategory.ResourceUnavailable, null);
            throw new DirectoryNotFoundException($"{agentsFolder} doesn't exist. Install AIShell first.");
        }

        // コピー先フォルダのパスを構築 (agents フォルダ内のサブフォルダ)
        string destinationFolder = System.IO.Path.Combine(agentsFolder, "UiPath.UiPathOrch.Agent");
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
            WriteVerbose($"Created directory: {destinationFolder}");
        }

        // 共通の埋め込みリソースの前半部分
        string prefix = "OrchProvider.Resources.AIShell.";

        // ファイル名のみ管理（これらを前半部分と連結して完全なリソース名となる）
        string[] fileNames =
        [
            //"Microsoft.Extensions.Logging.Abstractions.dll",
            //"System.ClientModel.dll",
            //"System.CommandLine.dll",
            //"System.Memory.Data.dll",
            //"UiPath.UiPathOrch.Agent.dll"
        ];

        Assembly assembly = Assembly.GetExecutingAssembly();

        foreach (string fileName in fileNames)
        {
            string resourceName = prefix + fileName;
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    WriteWarning($"Resource not found: {resourceName}");
                    continue;
                }

                string destinationFilePath = System.IO.Path.Combine(destinationFolder, fileName);
                using (FileStream fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
                WriteVerbose($"Copied {resourceName} to {destinationFilePath}");
            }
        }
    }
}
