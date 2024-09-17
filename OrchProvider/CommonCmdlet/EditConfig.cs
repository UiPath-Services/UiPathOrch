using System.Diagnostics;
using System.Management.Automation;

namespace UiPath.PowerShell.Commands
{
    [Cmdlet(VerbsData.Edit, "OrchConfig")]
    public class EditConfigCommand : Cmdlet
    {
        public enum EditorType { Default, Notepad }

        [Parameter(Position = 0)]
        public EditorType? Editor { get; set; }

        protected override void ProcessRecord()
        {
            Core.OrchProvider.EnsureDefaultConfigFileExists();

            string configFilePath = Core.OrchProvider.GetConfigFilePath();
            switch (Editor)
            {
                case EditorType.Default:
                    try
                    {
                        var startInfo = new ProcessStartInfo(configFilePath)
                        {
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                    }
                    catch (Exception)
                    {
                        var startInfo = new ProcessStartInfo("notepad.exe")
                        {
                            Arguments = configFilePath,
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                    }
                    break;

                default:
                    var startInfo2 = new ProcessStartInfo("notepad.exe")
                    {
                        Arguments = configFilePath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo2);
                    break;
            }
            WriteWarning($"Please edit {configFilePath}. Once edited, launch a new PS session and `Import-Module UiPathOrch` to mount your Orchestrator tenants as PSDrives.");
        }
    }
}
