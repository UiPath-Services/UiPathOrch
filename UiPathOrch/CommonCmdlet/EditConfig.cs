using System.Diagnostics;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace UiPath.PowerShell.Commands;

// On Linux, we cannot reliably launch an editor.
// Instead, just change the current location and display a message prompting the user to edit manually.
[Cmdlet(VerbsData.Edit, "OrchConfig")]
public class EditConfigCmdlet : PSCmdlet
{
    [Parameter]
    public SwitchParameter UseDefaultEditor { get; set; }

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
            if (UseDefaultEditor.IsPresent)
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
    }
}
