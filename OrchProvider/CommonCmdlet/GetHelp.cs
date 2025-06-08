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

            var txtFiles = new StringBuilder();
            if (Directory.Exists(docsPath))
            {
                foreach (var file in Directory.GetFiles(docsPath, "*.txt").OrderBy(Path.GetFileName))
                {
                    var fi = new FileInfo(file);
                    var sizeKB = Math.Round(fi.Length / 1024.0, 1);
                    txtFiles.AppendLine($"  📄 {fi.Name} ({sizeKB}KB)");
                }
            }

            var pdfFiles = new StringBuilder();
            if (Directory.Exists(docsPath))
            {
                foreach (var file in Directory.GetFiles(docsPath, "*.pdf"))
                {
                    var fi = new FileInfo(file);
                    var sizeMB = Math.Round(fi.Length / (1024.0 * 1024.0), 1);
                    pdfFiles.AppendLine($"  📄 {fi.Name} ({sizeMB}MB)");
                }
            }

            WriteObject($@"UiPathOrch Module Documentation
===============================

📁 Module Path: {modulePath}

📚 LLM Documentation ({docsPath}):
{txtFiles}
📖 PDF Manuals (Human Reference):
{pdfFiles}
🤖 LLM Quick Start:
Get-Content ""{Path.Combine(docsPath, "01-Essentials.txt")}""

🚀 Essential Commands:
Get-OrchPSDrive                       # ALWAYS start here
Get-OrchCurrentUser                   # Verify connection  
Clear-OrchCache                       # Reset on errors

💡 LLM Tip: Read 01-Essentials.txt first for mandatory execution rules
");
 
        }
    }
}
