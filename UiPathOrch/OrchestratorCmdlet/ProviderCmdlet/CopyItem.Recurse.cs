using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public partial class OrchProvider
{
    private bool CopyItemRecurse(
        OrchDriveInfo srcDrive,
        Folder srcFolder,
        OrchDriveInfo dstDrive,
        Folder dstFolder,
        bool recurse,
        CancellationToken cancelToken,
        Dictionary<string, string>? userMapping = null)
    {
        if (srcFolder == dstFolder)
        {
            WriteError(new ErrorRecord(new OrchException(srcFolder.GetPSPath(),
                "Cannot copy a folder to itself."),
                "CopyFolderError", ErrorCategory.InvalidOperation, srcFolder));
            return false;
        }

        Folder? destinationWorkspace = null;
        if (srcFolder.FolderType == "Personal")
        {
            if (ExcludeEntities) return false;
            if (dstFolder != dstDrive.RootFolder)
            {
                // If dst is directly specified, copy contents directly without creating a subfolder
                destinationWorkspace = dstFolder;
            }

            // If UserMappingCsv is provided, find the corresponding dst workspace based on OwnerId
            // (only if dst is not explicitly specified)
            if (destinationWorkspace is null && userMapping is not null)
            {
                // Determine the source user name from the src workspace's OwnerId
                var srcWorkspace = srcDrive.PersonalWorkspaces.Get().FirstOrDefault(pw => pw.Id == srcFolder.Id);
                string? srcOwnerUserName = null;
                if (srcWorkspace?.OwnerId is not null)
                {
                    srcOwnerUserName = srcDrive.Users.Get().FirstOrDefault(u => u.Id == srcWorkspace.OwnerId)?.UserName;
                }
                // If OwnerId is empty, infer from the folder name
                if (string.IsNullOrEmpty(srcOwnerUserName) && srcFolder.DisplayName?.EndsWith("'s workspace") == true)
                {
                    srcOwnerUserName = srcFolder.DisplayName[..^"'s workspace".Length];
                }

                if (!string.IsNullOrEmpty(srcOwnerUserName)
                    && userMapping.TryGetValue(srcOwnerUserName, out var dstUserName)
                    && !string.IsNullOrEmpty(dstUserName))
                {
                    // Get the dst user's Id and search PersonalWorkspaces by OwnerId
                    var dstUser = dstDrive.Users.Get().FirstOrDefault(u => string.Compare(u.UserName, dstUserName, StringComparison.OrdinalIgnoreCase) == 0);
                    if (dstUser?.Id is not null)
                    {
                        var dstWorkspace = dstDrive.PersonalWorkspaces.Get().FirstOrDefault(pw => pw.OwnerId == dstUser.Id);
                        if (dstWorkspace is not null)
                        {
                            destinationWorkspace = dstDrive.GetFolders().FirstOrDefault(f => f.Id == dstWorkspace.Id);
                        }
                    }
                }
            }

            // If not found via mapping, fall back to searching for a folder with the same name
            destinationWorkspace ??= dstDrive.GetFolder(srcFolder.DisplayName!);

            if (destinationWorkspace is null)
            {
                // A personal workspace folder can't be discovered through the API until
                // it has been opened ("start exploring") in the Orchestrator web UI — and
                // that has to happen on both tenants. This isn't a failure of the copy, so
                // surface it as guidance (not an error), then skip this workspace.
                WriteWarning(
                    $"To copy the personal workspace '{srcFolder.GetPSPath()}', the corresponding personal workspace folder must first be opened (start exploring) in the Orchestrator web UI on both the source ({srcDrive.NameColonSeparator}) and destination ({dstDrive.NameColonSeparator}) tenants. This is not supported through the API and must be done manually. Once both are explored, run `Clear-OrchCache` and copy again.");
                return false;
            }
        }
        // Even if src is a regular folder, if dst is a Personal workspace,
        // copy contents directly without creating a subfolder
        else if (dstFolder.FolderType == "Personal")
        {
            destinationWorkspace = dstFolder;
            WriteWarning($"Destination is a personal workspace. Contents of \"{srcFolder.GetPSPath()}\" will be copied directly into \"{dstFolder.GetPSPath()}\" without creating a subfolder.");
        }

        string target = $"Item: '{srcFolder.GetPSPath()}' Destination: '{dstFolder.GetPSPath()}'";

        // Use the reason-returning overload so that under -WhatIf we can still descend
        // into subfolders below (each emits its own "Copy Folder" line) while a declined
        // -Confirm (reason != WhatIf) stops here. The two strings reproduce the output of
        // ShouldProcess(target, "Copy Folder") verbatim — the -WhatIf line is identical.
        bool proceed = ShouldProcess(
            $"Performing the operation \"Copy Folder\" on target \"{target}\".",
            $"Are you sure you want to perform this action?\nPerforming the operation \"Copy Folder\" on target \"{target}\".",
            "Confirm",
            out ShouldProcessReason shouldProcessReason);

        if (proceed)
        {
            // totalNum: folder itself, users, machines, packages, processes, assets, 
            // queues, triggers, API triggers, buckets, testsets, testschedules, testdataqueues
            int totalStageNum = 13;
            if (srcFolder.FolderType == "Personal") totalStageNum = 9;
            // Can Apps be copied?

            try
            {
                // When srcFolder is not directly under root and dstFolder is not root,
                // copy without the feed
                string feedType;
                if (srcFolder.ParentId is not null && dstFolder != dstDrive.RootFolder)
                {
                    feedType = "Processes";
                }
                else
                {
                    feedType = srcFolder.FeedType;
                }

                Folder newFolder;
                using ProgressReporter reporter = new(this, 1, totalStageNum, "Copying folder");
                // The scope starting below was introduced so that child reporters are disposed of in a timely manner.
                {
                    // #0 Copy the folder itself (no folder creation needed for personal workspaces)
                    if (destinationWorkspace is not null)
                    {
                        newFolder = destinationWorkspace;
                        reporter.WriteProgress(0, $"\"{srcFolder.GetPSPath()}\" to \"{newFolder.GetPSPath()}\"");
                    }
                    else
                    {
                        reporter.WriteProgress(0, $"\"{srcFolder.GetPSPath()}\" to \"{dstFolder.GetPSPath()}\"");
                        reporter.WriteProgress(0);
                        newFolder = CopyFolder(srcDrive, srcFolder, dstDrive, dstFolder, feedType!, cancelToken);
                        if (newFolder is null) return false;
                    }

                    srcDrive.Releases.ClearCache(srcFolder);
                    dstDrive.Releases.ClearCache(dstFolder);
                    dstDrive.FolderMachinesAssigned.ClearCache(dstFolder);

                    if (!ExcludeEntities)
                    {
                        int rootIndex = 0;

                        // Each stage bumps the parent progress bar, optionally clears a
                        // src-side cache, then runs its per-entity copy under a child
                        // ProgressReporter. Ordering is significant — buckets before
                        // processes, packages before triggers — so keep this list in
                        // dependency order. Base numbers (100..1300) match the original
                        // per-stage reporter offsets. (Test cases are intentionally absent:
                        // they are created automatically when packages/processes copy.)
                        var stages = new (string Label, int Base, Action? PreStep, Action<ProgressReporter> Run)[]
                        {
                            ("Copying folder users...      ", 100,
                                () => { srcDrive.FolderUsersWithInherited.ClearCache(srcFolder); srcDrive.FolderUsersWithNoInherited.ClearCache(srcFolder); },
                                r => CopyFolderUsers(this, srcDrive, srcFolder, null, null, dstDrive, newFolder, r, true, cancelToken, userMapping)),
                            ("Copying folder machines...   ", 200,
                                () => srcDrive.FolderMachinesAssigned.ClearCache(srcFolder),
                                r => CopyFolderMachines(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying buckets...           ", 300, null,
                                r => CopyBuckets(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying packages...          ", 400, null,
                                r => CopyPackages(this, srcDrive, srcFolder, dstDrive, newFolder, r, cancelToken)),
                            ("Copying processes...         ", 500, null,
                                r => CopyProcesses(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying assets...            ", 600,
                                () => srcDrive.Assets.ClearCache(srcFolder),
                                r => CopyAssets(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken, userMapping)),
                            ("Copying queues...            ", 700, null,
                                r => CopyQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying triggers...          ", 800,
                                () => srcDrive.Triggers.ClearCache(srcFolder),
                                r => CopyTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying API triggers...      ", 900,
                                () => srcDrive.ApiTriggers.ClearCache(srcFolder),
                                r => CopyApiTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying test sets...         ", 1000, null,
                                r => CopyTestSets(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying test schedules...    ", 1100, null,
                                r => CopyTestSetSchedules(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying test data queues...  ", 1200, null,
                                r => CopyTestDataQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Copying action catalogs...   ", 1300, null,
                                r => CopyActionCatalogs(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                        };

                        // Child reporters are kept alive for the whole sequence and disposed
                        // together at the end (reverse order, like the original stacked
                        // `using var` declarations) so the progress display timing is unchanged.
                        var childReporters = new List<ProgressReporter>(stages.Length);
                        try
                        {
                            foreach (var stage in stages)
                            {
                                reporter.WriteProgress(++rootIndex);
                                stage.PreStep?.Invoke();
                                var childReporter = new ProgressReporter(this, stage.Base, Int32.MaxValue, stage.Label);
                                childReporters.Add(childReporter);
                                stage.Run(childReporter);
                                cancelToken.ThrowIfCancellationRequested();
                            }
                        }
                        finally
                        {
                            for (int i = childReporters.Count - 1; i >= 0; i--)
                            {
                                childReporters[i].Dispose();
                            }
                        }
                    }
                }

                if (recurse)
                {
                    var subfolders = GetDirectChildFolders(srcDrive.GetFolders(), srcFolder);
                    if (newFolder.FolderType == "Personal" && subfolders.Count > 0)
                    {
                        WriteWarning($"Subfolders of \"{srcFolder.GetPSPath()}\" cannot be copied into a personal workspace. Skipping {subfolders.Count} subfolder(s).");
                    }
                    else
                    {
                        foreach (var subfolder in subfolders)
                        {
                            CopyItemRecurse(srcDrive, subfolder, dstDrive, newFolder, true, cancelToken, userMapping);
                            cancelToken.ThrowIfCancellationRequested();
                        }
                    }
                }

                // If the current user is not assigned to the source folder,
                // unassign them from the destination folder
                // But if we unassign, links cannot be copied..
                // UnassignMyselfAtNewFolder(srcDrive, srcFolder, dstDrive, newFolder);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(target, ex), "CopyFolderError", ErrorCategory.InvalidOperation, srcFolder));
            }

            return true;
        }

        // -WhatIf only: the destination folder is not actually created, so we recurse
        // with a "would-be" folder whose PSPath is "<dstFolder>\<srcName>" — that's where
        // this folder's copy would land — so every subfolder still emits its own
        // "Copy Folder" -WhatIf line. A declined -Confirm (reason != WhatIf) stops here.
        if (shouldProcessReason == ShouldProcessReason.WhatIf && recurse)
        {
            Folder wouldBeNewFolder = destinationWorkspace ?? new Folder
            {
                FullName = System.IO.Path.Combine(dstFolder.GetPSPath(), srcFolder.DisplayName ?? ""),
                DisplayName = srcFolder.DisplayName,
                FolderType = srcFolder.FolderType,
                ParentId = dstFolder.Id,
            };
            if (wouldBeNewFolder.FolderType != "Personal")
            {
                foreach (var subfolder in GetDirectChildFolders(srcDrive.GetFolders(), srcFolder))
                {
                    CopyItemRecurse(srcDrive, subfolder, dstDrive, wouldBeNewFolder, true, cancelToken, userMapping);
                    cancelToken.ThrowIfCancellationRequested();
                }
            }
        }
        return false;
    }

    protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
    {
        return new CopyItem_DynamicParameters();
    }

    private bool ShouldCopyTenantEntities<T>(string kind, OrchDriveInfo srcDrive, IEnumerable<T>? srcEntities, OrchDriveInfo dstDrive)
    {
        // Include the source count so the -WhatIf / -Confirm line shows how many of each
        // kind would be copied (e.g. "Item: 'Orch1:\* (5)'") without enumerating every
        // name — magnitude at a glance while keeping the per-type overview to one line.
        int count = srcEntities?.Count() ?? 0;
        if (count > 0)
        {
            return ShouldProcess($"Item: '{srcDrive.NameColonSeparator}* ({count})' Destination: '{dstDrive.NameColonSeparator}'", $"Copy {kind}");
        }
        return false;
    }

    protected override void CopyItem(string path, string copyPath, bool recurse)
    {
        var dynamicParameters = DynamicParameters as CopyItem_DynamicParameters;
        // Assign unconditionally (don't only set it to true) so the flag resets every
        // call. The field carries -ExcludeEntities down through CopyItemRecurse; relying
        // on a fresh provider instance per Copy-Item to clear it would silently leak the
        // flag into a later un-flagged copy if instances were ever pooled/reused.
        ExcludeEntities = dynamicParameters?.ExcludeEntities.IsPresent ?? false;

        OrchDriveInfo srcDrive = ExtractOrchDriveInfo(path);
        OrchDriveInfo dstDrive = ExtractOrchDriveInfo(copyPath);

        if (srcDrive is null || dstDrive is null)
        {
            return;
        }

        var userMapping = SessionState?.LoadUserMappingCsv(this, srcDrive, dstDrive, dynamicParameters?.UserMappingCsv);

        // This parent reporter should avoid flickering, so place it in a wide scope.
        using var cancelHandler = new ConsoleCancelHandler();

        srcDrive.OrchAPISession.EnsureAuthenticated();
        dstDrive.OrchAPISession.EnsureAuthenticated();

        // cache the folders
        Parallel.ForEach(Enumerable.Range(0, 2), index =>
        {
            switch (index)
            {
                case 0: srcDrive.GetFolders(); break;
                case 1: dstDrive.GetFolders(); break;
            }
        });

        var srcFolder = srcDrive.GetFolder(OrchDriveInfo.PSPathToOrchPath(path));
        if (srcFolder is null)
        {
            WriteError(new ErrorRecord(new OrchException(copyPath, $"{srcDrive.NameColon} does not have folder '{path}'."), "CopyFolderError", ErrorCategory.InvalidOperation, copyPath));
            return;
        }

        var dstFolder = dstDrive.GetFolder(OrchDriveInfo.PSPathToOrchPath(copyPath));
        if (dstFolder is null) // The destination specified was a non-existent folder name
        {
            WriteError(new ErrorRecord(new OrchException(path, $"{dstDrive.NameColon} does not have folder '{copyPath}'."), "CopyFolderError", ErrorCategory.InvalidOperation, path));
            return;
        }

        // First, when copying from root to root, copy all tenant entities.
        if (!ExcludeEntities && srcFolder == srcDrive.RootFolder && dstFolder == dstDrive.RootFolder)
        {
            if (ShouldCopyTenantEntities("Library", srcDrive, srcDrive.LibrariesInTenant.Get(), dstDrive))
            {
                CopyLibraryCmdlet.CopyLibraries(this, [srcDrive], null, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Package", srcDrive, srcDrive.GetPackages(srcDrive.RootFolder), dstDrive))
            {
                CopyPackageCmdlet.CopyPackages(this, [(srcDrive, srcDrive.RootFolder)], srcDrive.RootFolder, null, null, [(dstDrive, dstDrive.RootFolder)], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("CredentialStore", srcDrive, srcDrive.CredentialStores.Get(), dstDrive))
            {
                CopyCredentialStoreCmdlet.CopyCredentialStores(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Role", srcDrive, srcDrive.Roles.Get(), dstDrive))
            {
                CopyRoleCmdlet.CopyRoles(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("User", srcDrive, srcDrive.Users.Get(), dstDrive))
            {
                CopyUserCmdlet.CopyUsers(this, srcDrive, null, null, null, [dstDrive], true, cancelHandler.Token, userMapping);
            }

            if (ShouldCopyTenantEntities("Machine", srcDrive, srcDrive.Machines.Get(), dstDrive))
            {
                CopyMachineCmdlet.CopyMachines(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Calendar", srcDrive, srcDrive.Calendars.Get(), dstDrive))
            {
                CopyCalendarCmdlet.CopyCalendars(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }

            if (ShouldCopyTenantEntities("Webhook", srcDrive, srcDrive.Webhooks.Get(), dstDrive))
            {
                CopyWebhookCmdlet.CopyWebhooks(this, srcDrive, null, [dstDrive], true, cancelHandler.Token);
            }
        }

        // We don't want to call ShouldProcess("/") on the root folder,
        // so handle recursive copy of the root folder as a special case
        if (srcFolder == srcDrive.RootFolder)
        {
            bool isDirty = false;
            if (recurse)
            {
                // Enumerate all personal workspaces and root-level folders.
                // Personal workspace folders sometimes have a ParentId for some reason, but GetFolders() masks this.
                var foldersToBeCopied = srcDrive.GetFolders().Where((f => f.ParentId is null && f != srcDrive.RootFolder));
                foreach (var folderToBeCopied in foldersToBeCopied)
                {
                    // Accumulate: if ANY top-level folder was actually copied the dst
                    // folder cache must be invalidated below. Plain '=' would keep only
                    // the last folder's result, skipping the reset when the final folder
                    // returns false (e.g. a personal workspace) despite earlier copies.
                    isDirty |= CopyItemRecurse(srcDrive, folderToBeCopied, dstDrive, dstFolder ?? dstDrive.RootFolder!, true, cancelHandler.Token, userMapping);
                }
            }
            else if (!ExcludeEntities)
            {
                // A root-to-root copy without -Recurse copies the tenant-level entities
                // above but no folders. Warn (in both real and -WhatIf runs) so the
                // missing folders aren't mistaken for an empty tenant — -Recurse would
                // also copy every folder and its entities. Personal workspaces are
                // excluded from the count since they are never copied by -Recurse anyway.
                int skipped = srcDrive.GetFolders().Count(f => f != srcDrive.RootFolder && f.FolderType != "Personal");
                if (skipped > 0)
                {
                    WriteWarning($"Copying tenant-level entities only. {skipped} folder(s) and their entities are not copied without -Recurse.");
                }
            }
            if (isDirty)
            {
                dstDrive._dicFolders = null;
                dstDrive._dicFoldersForEnumFolders = null;
            }
            return;
        }

        bool bDirty = false;
        try
        {
            bDirty = CopyItemRecurse(srcDrive, srcFolder, dstDrive, dstFolder ?? dstDrive.RootFolder!, recurse, cancelHandler.Token, userMapping);
        }
        catch (Exception)
        {
            // If an exception leaked, we don't know whether the folder was created or not..
            // So clear the folder cache.
            dstDrive._dicFolders = null;
            dstDrive._dicFoldersForEnumFolders = null;
            throw;
        }
        finally
        {
            if (bDirty)
            {
                dstDrive._dicFolders = null;
                dstDrive._dicFoldersForEnumFolders = null;
            }
        }
    }
}
