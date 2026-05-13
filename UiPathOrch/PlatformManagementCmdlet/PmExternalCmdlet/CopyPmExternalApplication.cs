using System.Management.Automation;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Copy, "PmExternalApplication", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.ExternalClientCreated))]
public class CopyPmExternalApplicationCommand : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(ExternalApplicationNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DestinationDriveCompleter))]
    public string[]? Destination { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string? Path { get; set; }

    protected override void ProcessRecord()
    {
        var srcDrive = SessionState.GetPmDrive(Path);
        var dstDrives = SessionState.EnumPmDrives(Destination.Split1stValueByUnescapedCommas());
        var wpName = Name.ConvertToWildcardPatternList();

        var srcClients = srcDrive.PmExternalClients.Get();
        var srcPartitionGlobalId = srcDrive.GetPartitionGlobalId();
        if (string.IsNullOrEmpty(srcPartitionGlobalId)) return;

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var srcApp in srcClients
            .FilterByWildcards(app => app?.name, wpName)
            .OrderBy(app => app.name).WithCancellation(cancelHandler.Token))
        {
            string target = srcApp.GetPSPath(srcDrive.NameColonSeparator);
            foreach (var dstDrive in dstDrives.WithCancellation(cancelHandler.Token))
            {
                var dstPartitionGlobalId = dstDrive.GetPartitionGlobalId();
                if (string.IsNullOrEmpty(dstPartitionGlobalId)) continue;
                if (srcPartitionGlobalId == dstPartitionGlobalId) continue;

                if (ShouldProcess(target, "Copy PmExternalApplication"))
                {
                    try
                    {
                        #region Skip if an app with the same name exists in the destination organization
                        // The API doesn't return an error, so let's check just in case.
                        var dstApps = dstDrive.PmExternalClients.Get();
                        var dstApp = dstApps.FirstOrDefault(src => string.Compare(src.name, srcApp.name, StringComparison.OrdinalIgnoreCase) == 0);
                        if (dstApp is not null)
                        {
                            WriteWarning($"\"{srcApp.GetPSPath(srcDrive.NameColonSeparator)}\": An external application named '{srcApp.name}' already exists in \"{dstDrive.NameColonSeparator}\".");
                            continue;
                        }
                        #endregion

                        ExternalClient detailedClient = srcDrive.OrchAPISession.GetPmExternalClient(srcPartitionGlobalId, srcApp.id ?? "");
                        if (detailedClient is null) continue;

                        CreateExternalClientCommand cmd = new()
                        {
                            partitionGlobalId = dstDrive.GetPartitionGlobalId(),
                            name = detailedClient.name,
                            isConfidential = detailedClient.isConfidential,
                            redirectUri = detailedClient.redirectUri,
                            scopes = detailedClient.resources?.SelectMany(r => r.scopes!)?.Select(s => new ExternalScope()
                            {
                                name = s.name,
                                type = s.type
                            }).ToArray()
                        };

                        var newApp = dstDrive.OrchAPISession.PostPmExternalClient(cmd);
                        if (newApp is null)
                        {
                            WriteWarning($"\"{dstDrive.NameColonSeparator}\": Failed to create an external application '{srcApp.name}'.");
                            continue;
                        }

                        WriteObject(newApp.WithPath(dstDrive.NameColonSeparator));
                        dstDrive.PmExternalClients.ClearCache();

                        // Non-confidential apps cannot belong to groups, so no further processing is needed
                        if (!newApp.isConfidential.GetValueOrDefault())
                        {
                            continue;
                        }

                        // Need to find and add to groups.
                        var dirEntries = dstDrive.PmBulkResolveByName("application", [newApp], app => app.name!);
                        var newAppDirEntry = dirEntries.Values.FirstOrDefault(e => string.Compare(e?.name, newApp.name, StringComparison.OrdinalIgnoreCase) == 0);
                        if (newAppDirEntry is null) continue;
                        var srcGroups = srcDrive.PmGroups.Get();
                        foreach (var srcGroup in srcGroups.WithCancellation(cancelHandler.Token))
                        {
                            try
                            {
                                var detailedSrcGroup = srcDrive.PmGroups.Get(srcGroup.id);
                                if (detailedSrcGroup?.members?.Any(m => string.Compare(m.name, srcApp.name, StringComparison.OrdinalIgnoreCase) == 0) ?? false)
                                {
                                    // srcApp belongs to srcGroup.
                                    // Search for a group with the same name in dstDrive; if not found, create one.
                                    var dstGroups = dstDrive.PmGroups.Get();
                                    var dstGroup = dstGroups.FirstOrDefault(g => string.Compare(g.name, srcGroup.name, StringComparison.OrdinalIgnoreCase) == 0);
                                    if (dstGroup is null) // Create a new group in dstDrive and add newApp.id
                                    {
                                        dstGroup = dstDrive.CreatePmGroup(srcGroup.name, [newAppDirEntry.identifier!]);
                                    }
                                    else // Add newApp to the existing group in dstDrive
                                    {
                                        dstDrive.AddMemberToPmGroup(dstGroup.id, dstGroup.name, [newAppDirEntry.identifier]);
                                    }
                                }
                            }
                            catch (OrchException ex)
                            {
                                WriteWarning($"\"{dstDrive.NameColonSeparator}\": Failed to add {newApp.name} to PmGroup {srcGroup.name}. {ex.Message}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(dstDrive, ex), "PostPmExternalApplicationError", ErrorCategory.InvalidOperation, dstDrive));
                    }
                }
            }
        }
    }
}
