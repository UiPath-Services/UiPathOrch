using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Generic base class for Remove-* cmdlets that delete folder-scoped entities by Name wildcard.
// Derived classes:
//   - Add [Cmdlet(VerbsCommon.Remove, "OrchXxx", SupportsShouldProcess = true)]
//   - Override Name to attach [ArgumentCompleter(typeof(XxxNameCompleter))]
//   - Implement EntityNoun, GetEntities, Remove, GetName, GetPSPath
//   - Optionally override PreFilter, ExcludePersonalWorkspace, ErrorCategory
public abstract class RemoveFolderEntityCmdletBase<TEntity> : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public virtual string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected abstract string EntityNoun { get; }
    protected abstract IEnumerable<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract void Remove(OrchDriveInfo drive, Folder folder, TEntity entity);
    protected abstract Func<TEntity?, string?> GetName { get; }
    protected abstract Func<TEntity, string> GetPSPath { get; }

    protected virtual Func<IEnumerable<TEntity>, IEnumerable<TEntity>>? PreFilter => null;
    protected virtual bool ExcludePersonalWorkspace => false;
    protected virtual ErrorCategory ErrorCategory => ErrorCategory.InvalidOperation;

    protected sealed override void ProcessRecord()
    {
        var drivesFolders = ExcludePersonalWorkspace
            ? SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth)
            : SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        var getName = GetName;
        var getPSPath = GetPSPath;
        var preFilter = PreFilter;
        var errorCategory = ErrorCategory;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                IEnumerable<TEntity> entities = GetEntities(drive, folder);
                if (preFilter is not null) entities = preFilter(entities);

                foreach (var entity in entities
                    .FilterByWildcards(getName, wpName)
                    .OrderBy(getName).WithCancellation(cancelHandler.Token))
                {
                    if (ShouldProcess(getPSPath(entity), $"Remove {EntityNoun}"))
                    {
                        try
                        {
                            Remove(drive, folder, entity);
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
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), $"Get{EntityNoun}Error", errorCategory, folder));
            }
        }
    }
}
