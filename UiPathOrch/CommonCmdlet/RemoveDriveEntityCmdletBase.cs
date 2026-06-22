using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base class for Remove-* cmdlets that delete drive-scoped (tenant-level) entities by Name wildcard.
// Derived classes:
//   - Add [Cmdlet(VerbsCommon.Remove, "OrchXxx", SupportsShouldProcess = true)]
//   - Override Name to attach [ArgumentCompleter(typeof(XxxNameCompleter))]
//   - Implement EntityNoun, GetEntities, Remove, GetName, GetPSPath
//   - Optionally override PreFilter, ErrorCategory
public abstract class RemoveDriveEntityCmdletBase<TEntity> : RemoveEntityCmdletBase<TEntity>
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    protected abstract IEnumerable<TEntity> GetEntities(OrchDriveInfo drive);
    protected abstract void Remove(OrchDriveInfo drive, TEntity entity);

    protected sealed override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var wpName = Name.ConvertToWildcardPatternList();
        var preFilter = PreFilter;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                IEnumerable<TEntity> entities = GetEntities(drive);
                if (preFilter is not null) entities = preFilter(entities);

                RemoveMatching(entities, wpName, drive.NameColonSeparator, entity => Remove(drive, entity), cancelHandler.Token);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), $"Get{EntityNoun}Error", ErrorCategory, drive));
            }
        }
    }
}
