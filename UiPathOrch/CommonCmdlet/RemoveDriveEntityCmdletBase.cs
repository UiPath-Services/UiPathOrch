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
public abstract class RemoveDriveEntityCmdletBase<TEntity> : OrchestratorPSCmdlet
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

    protected abstract string EntityNoun { get; }
    protected abstract IEnumerable<TEntity> GetEntities(OrchDriveInfo drive);
    protected abstract void Remove(OrchDriveInfo drive, TEntity entity);
    protected abstract Func<TEntity?, string?> GetName { get; }
    protected abstract Func<TEntity, string> GetPSPath { get; }

    protected virtual Func<IEnumerable<TEntity>, IEnumerable<TEntity>>? PreFilter => null;
    protected virtual ErrorCategory ErrorCategory => ErrorCategory.InvalidOperation;

    protected sealed override void ProcessRecord()
    {
        var drives = SessionState.EnumOrchDrives(EffectivePath(Path, LiteralPath));
        var getName = GetName;
        var getPSPath = GetPSPath;
        var preFilter = PreFilter;
        var errorCategory = ErrorCategory;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var drive in drives)
        {
            try
            {
                IEnumerable<TEntity> entities = GetEntities(drive);
                if (preFilter is not null) entities = preFilter(entities);

                foreach (var entity in entities
                    .FilterByNames(getName, Name)
                    .OrderBy(getName).WithCancellation(cancelHandler.Token))
                {
                    if (ShouldProcess(getPSPath(entity), $"Remove {EntityNoun}"))
                    {
                        try
                        {
                            Remove(drive, entity);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(getPSPath(entity), ex), $"Remove{EntityNoun}Error", errorCategory, entity));
                        }
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), $"Get{EntityNoun}Error", errorCategory, drive));
            }
        }
    }
}
