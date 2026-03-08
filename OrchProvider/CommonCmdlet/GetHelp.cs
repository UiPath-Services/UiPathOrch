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

            // Collect .md and .txt doc files with TOC line counts
            var docFiles = new StringBuilder();
            var tocCommands = new StringBuilder();
            if (Directory.Exists(docsPath))
            {
                var docExtensions = new[] { "*.md", "*.txt" };
                foreach (var ext in docExtensions)
                {
                    foreach (var file in Directory.GetFiles(docsPath, ext).OrderBy(Path.GetFileName))
                    {
                        var fi = new FileInfo(file);
                        // Find TOC end line (line starting with "Use Show-TextFiles")
                        var lines = File.ReadAllLines(file);
                        var tocEnd = 0;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].StartsWith("Use Show-TextFiles"))
                            {
                                tocEnd = i + 1;
                                break;
                            }
                        }
                        var sizeKB = Math.Round(fi.Length / 1024.0, 1);
                        docFiles.AppendLine($"  📄 {fi.Name} ({sizeKB}KB)");
                        if (tocEnd > 0)
                        {
                            var filePath = Path.Combine(docsPath, fi.Name);
                            tocCommands.AppendLine($"Show-TextFiles \"{filePath}\" -LineRange 1,{tocEnd}");
                        }
                    }
                }
            }

            WriteObject($@"UiPathOrch Module Documentation
===============================

📁 Docs Path: {docsPath}

📚 Documentation:
{docFiles}
🤖 How to read docs (show TOC of each document):
{tocCommands}
🚀 Essential Commands:
Get-OrchPSDrive                       # ALWAYS start here
Get-OrchCurrentUser                   # Verify connection
Clear-OrchCache                       # Reset on errors

💡 Read TOC first, then use -LineRange to read specific sections
");
 
        }
    }
}
