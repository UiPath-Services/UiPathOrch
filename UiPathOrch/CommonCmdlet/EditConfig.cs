using System.Collections;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// On Linux, we cannot reliably launch an editor.
// Instead, just change the current location and display a message prompting the user to edit manually.
[Cmdlet(VerbsData.Edit, "OrchConfig")]
public class EditConfigCommand : PSCmdlet
{
    [Parameter]
    public SwitchParameter UseDefaultEditor { get; set; }

    [Parameter(Position = 0, DontShow = true)]
    [Obsolete("Use -UseDefaultEditor instead.")]
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
                    // Windows: launch with the editor associated with the file extension
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
                // Fall back to the next candidate
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
            bool useDefault = UseDefaultEditor.IsPresent
                || string.Equals(EditorType, "Default", StringComparison.OrdinalIgnoreCase);
            if (useDefault)
            {
                candidates = [null, "notepad.exe"];
            }
            else
            {
                candidates = ["notepad.exe", null];
            }
            var ex = TryLaunchEditors(candidates, configFilePath);

            if (ex is not null)
            {
                WriteError(new ErrorRecord(ex, "LaunchEditorFailed", ErrorCategory.ResourceUnavailable, configFilePath));
            }

            WriteWarning($"Please edit '{configFilePath}'. After saving your changes, run `Import-OrchConfig` to reload the configuration.");
            return;
        }

        // On Linux, we cannot reliably launch an editor.
        // Change to the config directory and display a message prompting the user to edit. Use popd to return to the previous directory.
        string folder = Path.GetDirectoryName(configFilePath);
        string fileName = Path.GetFileName(configFilePath);

        // Push the current location onto the default stack
        SessionState.Path.PushCurrentLocation("default");

        // Change to the directory containing the configuration file
        SessionState.Path.SetLocation(folder);

        WriteWarning($"Please edit './{fileName}'. After saving your changes, run Import-OrchConfig to reload the configuration. Use `popd` to return to the previous location.");


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
