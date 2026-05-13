using System.Management.Automation;
using System.Text;

namespace UiPath.PowerShell.OrchProvider
{
    [Cmdlet(VerbsCommon.Get, "OrchHelp")]
    [OutputType(typeof(string))]
    public class GetOrchDocumentationCmdlet : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            var modulePath = MyInvocation.MyCommand.Module?.ModuleBase
                ?? Path.GetDirectoryName(typeof(GetOrchDocumentationCmdlet).Assembly.Location);

            var docsPath = GetTrueCasePath(Path.Combine(modulePath!, "Docs"));

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
  Get-Help <CmdletName> -Online   Open online help in browser
  TabExpansion2 '<partial cmd>'   Get parameter value completions programmatically
");
        }

        private static string GetTrueCasePath(string path)
        {
            var di = new DirectoryInfo(path);
            if (di.Parent is null) return di.Root.FullName;
            var parent = GetTrueCasePath(di.Parent.FullName);
            var match = Directory.GetFileSystemEntries(parent, di.Name);
            return match.Length > 0 ? match[0] : path;
        }
    }
}
