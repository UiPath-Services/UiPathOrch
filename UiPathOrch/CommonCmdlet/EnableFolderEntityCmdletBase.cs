using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

// Generic base for Enable-*/Disable-* cmdlets that toggle a folder-scoped entity by Name wildcard.
// Mirrors RemoveFolderEntityCmdletBase<TEntity>, but the per-item operation is a state toggle
// (driven by the static TEnable.Value) instead of a delete, and only entities whose current state
// differs from the target are acted on.
//
// Derived classes are one thin per-family base each:
//   - Provide the family's NameCompleter (it can't be generic — it fetches family-specific entities)
//   - Implement EntityNoun, GetEntities, SetEnabled, GetName, GetPSPath, IsEnabled
//   - Optionally override OuterErrorContext
// Concrete Enable*/Disable* cmdlets derive from that per-family base as <True>/<False> and attach
// the completer to Name.
public abstract class EnableFolderEntityCmdletBase<TEntity, TEnable> : OrchestratorPSCmdlet
    where TEnable : IBoolParameter
{
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
    protected abstract void SetEnabled(OrchDriveInfo drive, Folder folder, TEntity entity, bool enabled);
    protected abstract Func<TEntity?, string?> GetName { get; }
    protected abstract Func<TEntity, string> GetPSPath { get; }
    protected abstract Func<TEntity, bool> IsEnabled { get; }

    protected sealed override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        bool target = TEnable.Value;
        string action = $"{(target ? "Enable" : "Disable")} {EntityNoun}";
        var getName = GetName;
        var getPSPath = GetPSPath;
        var isEnabled = IsEnabled;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                // Only act on entities whose current state differs from the target — matches what
                // the family completer offers and skips a needless call on an already-in-state item.
                foreach (var entity in GetEntities(drive, folder)
                    .Where(e => isEnabled(e) != target)
                    .FilterByWildcards(getName, wpName)
                    .OrderBy(getName).WithCancellation(cancelHandler.Token))
                {
                    if (ShouldProcess(getPSPath(entity), action))
                    {
                        try
                        {
                            SetEnabled(drive, folder, entity, target);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(getPSPath(entity), ex), $"{(target ? "Enable" : "Disable")}{EntityNoun}Error", ErrorCategory.InvalidOperation, entity));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), $"Get{EntityNoun}Error", ErrorCategory.InvalidOperation, folder));
            }
        }
    }
}
