using System.Management.Automation;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Core;

public partial class OrchProvider
{
    // How many folders the current Copy-Item has entered, for the folder bar's numerator.
    // On the provider next to ExcludeEntities and _linkReport, because the recursion has no
    // single frame that could hold it. It spans the whole INVOCATION, not one call: see the
    // token below.
    private int _copiedFolderIndex;

    // The dynamic-parameters object of the Copy-Item in progress, used only for its identity.
    // PowerShell makes one per invocation and hands it to every per-path CopyItem call, which
    // is the only thing distinguishing "the next of this command's paths" from "a new
    // command". Held, so the reference cannot be reused by a later allocation.
    private object? _copyInvocationToken;

    // Whether the walk's folder total is knowable. True while the invocation has produced a
    // single resolved path -- then the subtree count is the real total. False from the moment
    // a second path arrives: a wildcard's full reach never reaches the provider, and no call
    // says it is the last.
    private bool _copyFolderTotalKnown;

    // What the folder bar is a fraction OF, fixed for the life of one bar. See where it is
    // assigned, in CopyItemRecurse.
    private bool _folderBarCountsFolders;

    private int? FolderBarTotal(OrchDriveInfo srcDrive, Folder srcFolder, bool recurse)
        => _copyFolderTotalKnown ? CountFoldersInWalk(srcDrive, srcFolder, recurse) : null;

    /// <summary>
    /// How many folders the walk about to start will enter -- the folder bar's denominator.
    ///
    /// Counted from the folder list the drive already holds, so it costs nothing. It is the
    /// subtree of <paramref name="srcFolder"/>, or every folder on the drive when that is the
    /// root. An upper bound rather than an exact figure: a folder can still be skipped (a
    /// subfolder of a personal workspace, a declined -Confirm), which leaves the bar short of
    /// its total at the end. Overstating by a folder is a better failure than understating,
    /// which would drive the bar past 100%.
    /// </summary>
    private static int CountFoldersInWalk(OrchDriveInfo srcDrive, Folder srcFolder, bool recurse)
    {
        if (!recurse) return 1;

        var all = srcDrive.GetFolders();
        if (srcFolder == srcDrive.RootFolder)
        {
            return all.Count(f => f != srcDrive.RootFolder);
        }

        string prefix = srcFolder.GetPSPath();
        if (prefix.Length > 0 && prefix[^1] != System.IO.Path.DirectorySeparatorChar)
        {
            prefix += System.IO.Path.DirectorySeparatorChar;
        }
        return 1 + all.Count(f => f.GetPSPath().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private bool CopyItemRecurse(
        OrchDriveInfo srcDrive,
        Folder srcFolder,
        OrchDriveInfo dstDrive,
        Folder dstFolder,
        bool recurse,
        CancellationToken cancelToken,
        Dictionary<string, string>? userMapping = null,
        // The folder bar, owned by the OUTERMOST call and handed down. Null from the entry
        // points, which is what makes that call the owner; see where it is created below.
        // The per-entity bars underneath it are NOT shared: each folder gets its own set and
        // closes them when it is done, which is what shows the operator what that folder
        // carried.
        ProgressReporter? folderReporter = null)
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

        // Event triggers are carried by none of the copy stages below, and there is no
        // Copy-OrchEventTrigger: an ApiTrigger points at an Integration Service connection whose
        // id differs at the destination, and a connection cannot be created through an API at all
        // (authorising one is interactive). Whatever we decide about copying them, losing them
        // *silently* is the defect -- the operator's first signal is a process that never starts
        // in the new tenant. Warn wherever a copy would otherwise have carried this folder's
        // contents, including under -WhatIf (which descends without copying), but not when a
        // -Confirm prompt was declined.
        if (!ExcludeEntities && (proceed || shouldProcessReason == ShouldProcessReason.WhatIf))
        {
            WarnAboutUncopiedEventTriggers(srcDrive, srcFolder);
        }

        if (proceed)
        {
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

                // One reporter for the whole recursion, not one per folder. Dispose writes a
                // Completed record, so a reporter owned by each folder took the bar off the
                // screen and put it back for every folder of a -Recurse. Only the outermost
                // call owns it -- `using` on the null the inner calls get is a no-op.
                using ProgressReporter? ownedReporter = folderReporter is null
                    ? new ProgressReporter(this, FolderBarTotal(srcDrive, srcFolder, recurse), "Folders")
                    : null;
                ProgressReporter reporter = folderReporter ?? ownedReporter!;

                // The bar counts FOLDERS when the walk's folder total is knowable, which is
                // every run but a wildcard source, and is the number the operator is actually
                // waiting on. When it is not knowable it counts this folder's STAGES instead,
                // out of thirteen -- the old behaviour, and the only other whole that is
                // always known. A bar has to be a fraction of something; the choice is which
                // something, not whether to have one.
                //
                // Decided per bar, and a bar is created per resolved path, so the mode holds
                // for the length of one. A wildcard that matched several folders therefore
                // shows folders while walking the first (the total was right for it) and
                // stages from the second on, by which point that total is known to be short.
                if (folderReporter is null) _folderBarCountsFolders = _copyFolderTotalKnown;

                int folderIndex = ++_copiedFolderIndex;
                int stageIndex = 0;
                if (!_folderBarCountsFolders)
                {
                    // Per folder, because a personal workspace runs fewer of them.
                    reporter.TotalNum = srcFolder.FolderType == "Personal" ? 9 : 13;
                }
                // The scope starting below was introduced so that child reporters are disposed of in a timely manner.
                {
                    // #0 Copy the folder itself (no folder creation needed for personal workspaces)
                    //
                    // Both paths in full: "<source folder> -> <folder being created>". Neither
                    // can be inferred from the other -- how much of the source path is
                    // reproduced under the destination depends on where the walk started, so
                    // naming only the destination root would leave it unsaid whether this is
                    // creating Orch2:\TestFixture_Base\Development or Orch2:\Development. Long
                    // enough that the host may cut the line short; the whole destination is
                    // worth that risk, an ambiguous one is not. The child bars below name the
                    // entity only (see the stage loop), so no path is repeated per bar.
                    //
                    // The destination half can only be filled in once the folder exists, so
                    // the bar first goes up with the source alone -- that write is what shows
                    // the operator which folder the creation about to happen belongs to.
                    //
                    // It goes in Context rather than the activity because the activity is a
                    // fixed-width label; a path there would move the bar on every folder.
                    reporter.Context = srcFolder.GetPSPath();
                    reporter.WriteProgress(_folderBarCountsFolders ? folderIndex : stageIndex);
                    if (destinationWorkspace is not null)
                    {
                        newFolder = destinationWorkspace;
                    }
                    else
                    {
                        newFolder = CopyFolder(srcDrive, srcFolder, dstDrive, dstFolder, feedType!, cancelToken);
                        if (newFolder is null) return false;
                    }
                    reporter.Context = $"{srcFolder.GetPSPath()} -> {newFolder.GetPSPath()}";

                    srcDrive.Releases.ClearCache(srcFolder);
                    dstDrive.Releases.ClearCache(dstFolder);
                    dstDrive.FolderMachinesAssigned.ClearCache(dstFolder);

                    if (!ExcludeEntities)
                    {
                        // Each stage re-asserts the parent progress bar, optionally clears a
                        // src-side cache, then runs its per-entity copy under a child
                        // ProgressReporter. Ordering is significant — buckets before
                        // processes, packages before triggers — so keep this list in
                        // dependency order. (Test cases are intentionally absent: they are
                        // created automatically when packages/processes copy.)
                        //
                        // Each stage used to carry a hand-picked activity id (100..1300).
                        // Those are gone: the bars are allocated ids and name the folder bar
                        // as their parent by reference, so no two can be made to collide by
                        // editing one of them.
                        //
                        // Buckets / assets / queues also clear newFolder's dst-side list
                        // AFTER copying, exactly as the standalone Copy-OrchBucket / -OrchAsset
                        // / -OrchQueue cmdlets do (this loop was the one create path that
                        // didn't). These three are the only entities read back on the
                        // destination side during a copy — FindDstBucket from the process and
                        // queue stages, FindDstQueue from the trigger stage — and a list
                        // cached for newFolder before those creates (an earlier folder's link
                        // lookup does exactly that) otherwise leaves the entity invisible for
                        // the rest of the session, so the queue trigger that needs it is
                        // silently skipped.
                        //
                        // Every Label is padded to 16, the length of the longest ("Test data
                        // queues"), so these bars start their "[" in the same column. Written
                        // out rather than computed, so what is here is what is on screen --
                        // the trailing spaces are load-bearing. A longer stage name means
                        // re-padding the whole list. Anything that varies, such as the
                        // destination folder, goes in ProgressReporter.Context instead.
                        //
                        // The label is given here, when the bar is made, and the Copy* methods
                        // never touch it. They are shared with the standalone Copy-Orch*
                        // cmdlets, whose single bar wants the same noun unpadded; a method
                        // that set the label itself would be right for one caller only, and
                        // it used to be -- every Copy-OrchQueue run showed this padded label,
                        // and the "Copying queues..." it was constructed with never appeared.
                        //
                        // The parent's "Folders" is NOT padded to match: the host
                        // indents a child bar two columns under its parent, so the parent's
                        // label never shares a column with these and padding it only pushed
                        // its bar needlessly to the right.
                        var stages = new (string Label, Action? PreStep, Action<ProgressReporter> Run)[]
                        {
                            ("Folder users    ",
                                () => { srcDrive.FolderUsersWithInherited.ClearCache(srcFolder); srcDrive.FolderUsersWithNoInherited.ClearCache(srcFolder); },
                                r => CopyFolderUsers(this, srcDrive, srcFolder, null, null, dstDrive, newFolder, r, true, cancelToken, userMapping)),
                            ("Folder machines ",
                                () => srcDrive.FolderMachinesAssigned.ClearCache(srcFolder),
                                r => CopyFolderMachines(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Buckets         ", null,
                                r => { CopyBuckets(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken, _linkReport); dstDrive.Buckets.ClearCache(newFolder); }),
                            ("Packages        ", null,
                                r => CopyPackages(this, srcDrive, srcFolder, dstDrive, newFolder, r, cancelToken)),
                            ("Processes       ", null,
                                r => CopyProcesses(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Assets          ",
                                () => srcDrive.Assets.ClearCache(srcFolder),
                                r => { CopyAssets(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken, userMapping, _linkReport); dstDrive.Assets.ClearCache(newFolder); }),
                            ("Queues          ", null,
                                r => { CopyQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken, _linkReport); dstDrive.Queues.ClearCache(newFolder); }),
                            ("Triggers        ",
                                () => srcDrive.Triggers.ClearCache(srcFolder),
                                r => CopyTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("API triggers    ",
                                () => srcDrive.ApiTriggers.ClearCache(srcFolder),
                                r => CopyApiTriggers(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Test sets       ", null,
                                r => CopyTestSets(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Test schedules  ", null,
                                r => CopyTestSetSchedules(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Test data queues", null,
                                r => CopyTestDataQueues(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                            ("Action catalogs ", null,
                                r => CopyActionCatalogs(this, srcDrive, srcFolder, null, dstDrive, newFolder, r, true, cancelToken)),
                        };

                        // One bar per entity type, each left standing once its stage is done so
                        // the folder reads as a list of what was copied, and all of them closed
                        // together when the folder is (see the finally). They are nested under
                        // the folder bar rather than put beside it: a stage IS a step of the
                        // folder copy the parent bar is counting, and the host draws the set as
                        // one indented tree instead of fourteen unrelated activities.
                        var childReporters = new List<ProgressReporter>(stages.Length);
                        try
                        {
                            foreach (var stage in stages)
                            {
                                // Counting folders, the number does not change here -- this
                                // folder is still the one in hand -- but it is written per
                                // stage all the same: a bar nobody writes to can sit hidden
                                // behind ordinary console output for as long as a slow stage
                                // takes, and the stage boundaries are the cheap moments to
                                // put it back on screen. Counting stages, this IS the move.
                                reporter.WriteProgress(_folderBarCountsFolders ? folderIndex : ++stageIndex);
                                stage.PreStep?.Invoke();
                                var childReporter = new ProgressReporter(this, null, stage.Label, reporter);
                                childReporters.Add(childReporter);
                                stage.Run(childReporter);
                                // These bars stay up for the rest of the folder, so each one
                                // has to say it is finished. Left alone, a full bar still
                                // reads "Queues [2/2 queue-staging]" long after the queues
                                // are done, as though that one were still being copied.
                                childReporter.WriteCompleted();
                                cancelToken.ThrowIfCancellationRequested();
                            }
                        }
                        finally
                        {
                            // One bar per entity type, left standing for the whole folder so
                            // the run reads as a list of what was copied, then closed together
                            // when the folder is done.
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
                            CopyItemRecurse(srcDrive, subfolder, dstDrive, newFolder, true, cancelToken, userMapping, reporter);
                            cancelToken.ThrowIfCancellationRequested();
                        }
                    }
                }

                // If the current user is not assigned to the source folder,
                // unassign them from the destination folder
                // But if we unassign, links cannot be copied..
                // UnassignMyselfAtNewFolder(srcDrive, srcFolder, dstDrive, newFolder);
            }
            // Ctrl+C is not a failure of THIS folder, so it must not be reported as one. The
            // catch below wraps all thirteen stages and the subfolder recursion, and turning a
            // cancellation into a non-terminating error there made one Ctrl+C stop only the
            // entity in flight: the folder returned "copied", and the caller -- the subfolder
            // loop above, or the root-level loop in CopyItem -- moved on to the next folder
            // and started its thirteen stages over. Rethrowing unwinds the whole walk on the
            // first press, which is what the operator asked for by pressing it.
            catch (OperationCanceledException)
            {
                throw;
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

    /// <summary>
    /// Advisory only -- it must never disturb the copy it annotates, so every failure is
    /// swallowed. ListCachePerFolder.Get returns an empty list below the entity's API-version
    /// floor instead of throwing, so an Orchestrator that predates event triggers stays quiet
    /// on its own.
    /// </summary>
    private void WarnAboutUncopiedEventTriggers(OrchDriveInfo srcDrive, Folder srcFolder)
    {
        try
        {
            int count = srcDrive.EventTriggers.Get(srcFolder).Count;
            if (count > 0)
            {
                WriteWarning(
                    $"\"{srcFolder.GetPSPath()}\": {count} event trigger(s) are not copied. They reference an " +
                    "Integration Service connection, which cannot be created through an API, so they must be " +
                    "recreated at the destination. List them with `Get-OrchEventTrigger`.");
            }
        }
        catch { } // An advisory is never worth failing a copy over.
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
        var linkReport = _linkReport = new LinkCopyReport();

        // A wildcard source is resolved by PowerShell BEFORE the provider is called, and this
        // method is then called once per matched path. Resetting the folder counter here made
        // the bar restart on every one of them -- "copy orch1:\s* orch2:\" over four folders
        // read 1/1 four times. The dynamic-parameters object is created once per Copy-Item
        // and handed to each of those calls, so its identity says which calls belong to one
        // invocation; the counter is reset only when it changes.
        if (!ReferenceEquals(DynamicParameters, _copyInvocationToken))
        {
            _copyInvocationToken = DynamicParameters;
            _copiedFolderIndex = 0;
            // The first path's subtree is a real total. See the field.
            _copyFolderTotalKnown = true;
        }
        else
        {
            // A second path has arrived, so the total shown while walking the first one was
            // only that path's. How many more are coming is not knowable from here: the
            // pattern never reaches the provider and nothing says which call is the last.
            _copyFolderTotalKnown = false;
        }

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

            // try/finally, like the non-root path below: Ctrl+C now unwinds this loop instead
            // of being swallowed per folder, and the cache invalidation and the link report
            // are exactly what must still happen when it does. Half a walk leaves the folder
            // cache describing a destination that no longer matches the server, and that
            // stale view would then be used for the rest of the session.
            try
            {
                if (recurse)
                {
                    // Enumerate all personal workspaces and root-level folders.
                    // Personal workspace folders sometimes have a ParentId for some reason, but GetFolders() masks this.
                    var foldersToBeCopied = srcDrive.GetFolders().Where((f => f.ParentId is null && f != srcDrive.RootFolder));

                    // Owned here, not left to the first call, so the bar survives the whole
                    // walk: this loop makes one outermost call per top-level folder, and a bar
                    // owned by the first of them would be taken down when that folder finished.
                    // Owning it also means setting the mode, which CopyItemRecurse only does
                    // for a bar it creates itself.
                    _folderBarCountsFolders = _copyFolderTotalKnown;
                    using var folderReporter = new ProgressReporter(
                        this, FolderBarTotal(srcDrive, srcDrive.RootFolder!, true), "Folders");

                    // WithCancellation, so Ctrl+C stops the walk between top-level folders too.
                    // The subfolder loop inside CopyItemRecurse already checks after each child;
                    // this loop is the one level that did not, and it is the outermost one.
                    foreach (var folderToBeCopied in foldersToBeCopied.WithCancellation(cancelHandler.Token))
                    {
                        // Accumulate: if ANY top-level folder was actually copied the dst
                        // folder cache must be invalidated below. Plain '=' would keep only
                        // the last folder's result, skipping the reset when the final folder
                        // returns false (e.g. a personal workspace) despite earlier copies.
                        isDirty |= CopyItemRecurse(srcDrive, folderToBeCopied, dstDrive, dstFolder ?? dstDrive.RootFolder!, true, cancelHandler.Token, userMapping, folderReporter);
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
            }
            catch (Exception)
            {
                // The walk stopped partway, so we do not know what reached the destination.
                dstDrive.ClearFolders();
                throw;
            }
            finally
            {
                if (isDirty)
                {
                    dstDrive.ClearFolders();
                }
                linkReport.Flush(this);
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
            dstDrive.ClearFolders();
            throw;
        }
        finally
        {
            if (bDirty)
            {
                dstDrive.ClearFolders();
            }
            linkReport.Flush(this);
        }
    }
}
