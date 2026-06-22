using System.Management.Automation;
using UiPath.PowerShell.Core;

namespace UiPath.PowerShell.Commands;

// Shared machinery for Remove-* cmdlets, independent of whether the entity is folder-scoped
// (RemoveFolderEntityCmdletBase) or drive/tenant-scoped (RemoveDriveEntityCmdletBase). The scope
// enumeration differs per subclass; this base owns the part they have in common: the per-entity
// loop that filters by the -Name wildcards, orders, shows a progress bar, gates each delete behind
// ShouldProcess, and reports per-item failures as non-terminating errors.
public abstract class RemoveEntityCmdletBase<TEntity> : OrchestratorPSCmdlet
{
    protected abstract string EntityNoun { get; }
    protected abstract Func<TEntity?, string?> GetName { get; }
    protected abstract Func<TEntity, string> GetPSPath { get; }

    protected virtual Func<IEnumerable<TEntity>, IEnumerable<TEntity>>? PreFilter => null;
    protected virtual ErrorCategory ErrorCategory => ErrorCategory.InvalidOperation;

    // The per-entity removal loop shared by both scopes. `scopeLabel` is the human-readable
    // container the progress bar names (a folder PSPath or a drive "Name:"); `remove` deletes one
    // entity (the subclass closes over its drive/folder). Behaviour matches the two former inline
    // copies exactly: same filter/order/progress/ShouldProcess wording and the same per-item
    // OrchException error (id "Remove{Noun}Error", target = the entity).
    protected void RemoveMatching(
        IEnumerable<TEntity> entities,
        IReadOnlyList<WildcardPattern>? wpName,
        string scopeLabel,
        Action<TEntity> remove,
        CancellationToken token)
    {
        var getName = GetName;
        var getPSPath = GetPSPath;
        var entityNoun = EntityNoun;
        var errorCategory = ErrorCategory;

        foreach (var entity in entities
            .FilterByWildcards(getName, wpName)
            .OrderBy(getName)
            .WithProgressBar(this, $"Removing {entityNoun} in {scopeLabel}", getName)
            .WithCancellation(token))
        {
            if (ShouldProcess(getPSPath(entity), $"Remove {entityNoun}"))
            {
                try
                {
                    remove(entity);
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(new OrchException(getPSPath(entity), ex), $"Remove{entityNoun}Error", errorCategory, entity));
                }
            }
        }
    }
}
