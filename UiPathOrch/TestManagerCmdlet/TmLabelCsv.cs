using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// The Labels column of the Tm* CSV files. Labels are not on the entities: each object's labels are
// read with one call (the bulk objectlabels/{type}/objects returns only the union of the names, not
// which object has which). A cell holds the names joined by commas, a comma or backtick in a name
// escaped with a backtick -- the form the module's other list columns (-Tags) use.
internal static class TmLabelCsv
{
    // The -Labels parameter of New-/Update-Tm*: every element is split on unescaped commas, so a
    // CSV cell "a,b`,c" gives "a" and "b,c", and a native array -Labels a,b stays two labels.
    internal static string[]? Parse(string[]? labels)
        => labels.SplitValuesByUnescapedCommas()?.Where(l => !string.IsNullOrEmpty(l)).ToArray();

    internal static string Format(IEnumerable<string>? labels)
        => labels is null ? "" : string.Join(',', labels.Order(StringComparer.Ordinal).Select(l => l.Replace("`", "``").Replace(",", "`,")));

    // Reads the labels of each entity in parallel and calls write with the entity and its formatted
    // labels, in the entities' order. An entity whose labels cannot be read is reported and left
    // out, so the CSV never holds a row that silently lost its labels.
    internal static void ForEachWithLabels<T>(OrchestratorPSCmdlet cmdlet, OrchTmDriveInfo drive, TmProject project,
        string objectType, IEnumerable<T> entities, Func<T, string?> getId, Func<T, string> getPath, Action<T, string> write,
        ProgressReporter parent, CancellationToken token)
        where T : class
    {
        using var results = OrchThreadPool.RunForEach(entities,
            getPath,
            e => e,
            e => drive.OrchAPISession.GetTmObjectLabels(project.id!, objectType, getId(e)!));

        using var reporter = new ProgressReporter(cmdlet, results.Count, $"Getting labels in {project.GetPSPath()}", parent);
        foreach (var result in results)
        {
            try
            {
                var labels = results.GetResultWithProgress(result, reporter, token);
                write(result.Source, Format(labels?.Select(l => l.name!).Where(n => !string.IsNullOrEmpty(n))));
            }
            catch (OrchException ex)
            {
                cmdlet.WriteError(new ErrorRecord(ex, "GetTmObjectLabelError", ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
