using System.Management.Automation;
using System.Text;

namespace UiPath.PowerShell.OrchProvider
{
    [Cmdlet(VerbsCommon.Get, "OrchHelp")]
    [OutputType(typeof(string))]
    public class GetOrchDocumentationCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "UiPath.PowerShell.OrchProvider");
            var modulePath = assembly != null ? Path.GetDirectoryName(assembly.Location) :
                @"C:\Program Files\PowerShell\7\Modules\UiPathOrch";

            var docsPath = Path.Combine(modulePath!, "Docs");

            var docFiles = new StringBuilder();
            if (Directory.Exists(docsPath))
            {
                foreach (var file in Directory.GetFiles(docsPath, "*.md").OrderBy(Path.GetFileName))
                {
                    var fi = new FileInfo(file);
                    var sizeKB = Math.Round(fi.Length / 1024.0, 1);

                    // Read first line as title
                    var firstLine = File.ReadLines(file).FirstOrDefault() ?? "";
                    var title = firstLine.TrimStart('#', ' ');

                    docFiles.AppendLine($"  {fi.Name,-30} {title}");
                }
            }

            WriteObject($@"UiPathOrch Module Documentation
===============================

Docs Path: {docsPath}

{docFiles}
Essential Commands:
  Get-OrchPSDrive                 Verify connected drives
  Get-OrchCurrentUser             Verify connection
  Clear-OrchCache                 Reset on errors
  Get-Help <CmdletName> -Examples Cmdlet usage examples
");
        }
    }
}
