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
public abstract class RemoveFolderEntityCmdletBase<TEntity> : RemoveEntityCmdletBase<TEntity>
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

    protected abstract IEnumerable<TEntity> GetEntities(OrchDriveInfo drive, Folder folder);
    protected abstract void Remove(OrchDriveInfo drive, Folder folder, TEntity entity);

    protected virtual bool ExcludePersonalWorkspace => false;

    protected sealed override void ProcessRecord()
    {
        var drivesFolders = ExcludePersonalWorkspace
            ? SessionState.EnumFoldersWithoutPersonalWorkspace(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth)
            : SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);
        var wpName = Name.ConvertToWildcardPatternList();
        var preFilter = PreFilter;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                IEnumerable<TEntity> entities = GetEntities(drive, folder);
                if (preFilter is not null) entities = preFilter(entities);

                RemoveMatching(entities, wpName, folder.GetPSPath(), entity => Remove(drive, folder, entity), cancelHandler.Token);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), $"Get{EntityNoun}Error", ErrorCategory, folder));
            }
        }
    }
}
