using System.Collections;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Linux 環境では、うまくエディタが起動できない。
// current location を移動して、編集してくれとメッセージを出すだけにしておくか。。
[Cmdlet(VerbsData.Edit, "OrchConfig")]
public class EditConfigCommand : PSCmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(EditorTypeCompleter))]
    //[ValidateSet("Default", "Notepad", "XdgOpen", "Vi", "Nano", IgnoreCase = true)]
    public string? EditorType { get; set; }

    internal class EditorTypeCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Default"));
                yield return new CompletionResult(PathTools.EscapePSText("Notepad"));
            }
            else
            {
                yield break;
                //yield return new CompletionResult(PathTools.EscapePSText("XdgOpen"));
                //yield return new CompletionResult(PathTools.EscapePSText("Vi"));
                //yield return new CompletionResult(PathTools.EscapePSText("Nano"));
            }
        }
    }

    private static Exception? TryLaunchEditors(string?[] editors, string configFilePath)
    {
        Exception ret = null;
        foreach (var ed in editors)
        {
            try
            {
                if (ed is null)
                {
                    // Windows: ファイルの拡張子に関連付けられたエディタで起動
                    Process.Start(new ProcessStartInfo(configFilePath) { UseShellExecute = true });
                    return null;
                }

                string candidate = ed.ToLowerInvariant();

                Process.Start(new ProcessStartInfo(candidate, configFilePath)
                {
                    UseShellExecute = true
                });

                //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                //{
                //}
                //else
                //{
                    //string folder = System.IO.Path.GetDirectoryName(configFilePath);
                    //string fileName = Path.GetFileName(configFilePath);

                    //if (candidate == "xdg-open")
                    //{
                    //    Process.Start(new ProcessStartInfo(candidate, configFilePath)
                    //    {
                    //        UseShellExecute = true
                    //    });
                    //}
                    //else
                    //{
                    //    Process.Start(new ProcessStartInfo("/usr/bin/script")
                    //    {
                    //        UseShellExecute = true,
                    //        ArgumentList = { "-q", "-c", $"env TERM=xterm {candidate} {configFilePath}", "/dev/null" }
                    //    });
                    //}
                //}

                return null;
            }
            catch (Exception ex)
            {
                ret = ex;
                // 次の候補へフォールバック
            }
        }
        return ret;
    }

    protected override void ProcessRecord()
    {
        Core.OrchProvider.EnsureDefaultConfigFileExists();
        string configFilePath = Core.OrchProvider.GetConfigFilePath();

        string?[] candidates;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string candidate = string.IsNullOrEmpty(EditorType) ? "notepad" : EditorType.ToLowerInvariant();
            if (candidate == "notepad")
            {
                candidates = ["notepad.exe", null];
            }
            else
            {
                candidates = [null, "notepad.exe"];
            }
            var ex = TryLaunchEditors(candidates, configFilePath);

            if (ex is not null)
            {
                WriteError(new ErrorRecord(ex, "LaunchEditorFailed", ErrorCategory.ResourceUnavailable, configFilePath));
            }

            WriteWarning($"Please edit '{configFilePath}'. After saving your changes, run `Mount-OrchPSDrive` to reload the configuration.");
            return;
        }

        // Linux 環境ではエディタをうまく起動できない。。
        // ディレクトリを移動して、編集を促すメッセージを出力する。元のディレクトリに戻るには popd を実行する。
        string folder = Path.GetDirectoryName(configFilePath);
        string fileName = Path.GetFileName(configFilePath);

        // 現在のロケーションをデフォルトスタックにプッシュ
        SessionState.Path.PushCurrentLocation("default");

        // 設定ファイルがあるパスに移動
        SessionState.Path.SetLocation(folder);

        WriteWarning($"Please edit './{fileName}'. After saving your changes, run Mount-OrchPSDrive to reload the configuration. Use `popd` to return to the previous location.");


        //string candidate = string.IsNullOrEmpty(EditorType) ? "nano" : EditorType.ToLowerInvariant();
        //if (candidate == "xdgopen")
        //{
        //    candidates = ["xdg-open", "vi", "nano"];
        //}
        //else if (candidate == "vi")
        //{
        //    candidates = ["vi", "nano", "xdg-open"];
        //}
        //else
        //{
        //    candidates = ["nano", "vi", "xdg-open"];
        //}
        //}
    }
}
