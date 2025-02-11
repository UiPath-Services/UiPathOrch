using System.Collections;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsData.Edit, "OrchConfig")]
public class EditConfigCommand : Cmdlet
{
    [Parameter(Position = 0)]
    [ArgumentCompleter(typeof(EditorTypeCompleter))]
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
                yield return new CompletionResult(PathTools.EscapePSText("XdgOpen"));
                yield return new CompletionResult(PathTools.EscapePSText("Vi"));
                yield return new CompletionResult(PathTools.EscapePSText("Nano"));
            }
        }
    }

    private Exception? TryLaunchEditors(string?[] editors, string configFilePath)
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

                bool useShellExecute;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    useShellExecute = true;
                }
                else
                {
                    // Linux: "xdg-open" はシェル経由、それ以外は直接起動
                    useShellExecute = candidate == "xdg-open";
                }

                Process.Start(new ProcessStartInfo(candidate, configFilePath)
                {
                    UseShellExecute = useShellExecute
                });

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

        string?[] candidates = [];

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
        }
        else
        {
            string candidate = string.IsNullOrEmpty(EditorType) ? "nano" : EditorType.ToLowerInvariant();
            if (candidate == "xdgopen")
            {
                candidates = ["xdg-open", "vi", "nano"];
            }
            else if (candidate == "vi")
            {
                candidates = ["vi", "nano", "xdg-open"];
            }
            else
            {
                candidates = ["nano", "vi", "xdg-open"];
            }
        }

        var ex = TryLaunchEditors(candidates, configFilePath);
        if (ex is not null)
        {
            WriteError(new ErrorRecord(ex, "LaunchEditorFailed", ErrorCategory.ResourceUnavailable, configFilePath));
        }
        else
        {
            WriteWarning($"Please edit {configFilePath}. Once edited, launch a new PS session and `Import-Module UiPathOrch` to mount your Orchestrator tenants as PSDrives.");
        }
    }
}
