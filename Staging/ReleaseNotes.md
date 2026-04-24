# Version: 0.9.17.0
## New Features
- Secret-typed assets (Orchestrator v20+): added `Set-OrchSecretAsset`, `Get-OrchSecretAsset`, and a type-agnostic `Remove-OrchAssetUserValue`. `Set-OrchSecretAsset` treats an empty `-SecretValue` as a silent no-op so CSV round-trip is safe (the API masks `SecretValue` on GET). Removing a per-robot entry on a Secret uses `Remove-OrchAssetUserValue` (the empty-delete convention from Text/Bool/Integer does not apply to Secret).

- `Get-OrchCredentialAsset`: new cmdlet returning only Credential-type assets, with its own `-ExportCsv`. The existing `Get-OrchAsset -ExportCredentialCsv` is unchanged.

- `Format-OrchQueueItem`: new pipeline formatter that groups queue items by `QueueDefinitionId` and emits one `Format-Table` per queue, flattening each item's `SpecificContent` keys into columns. Needed because `Format-Table` locks columns on the first object, silently hiding keys unique to later queues. Paired with a new `Expanded` ScriptProperty (ETS) on `QueueItem` — `Get-OrchQueueItem Q | ForEach-Object Expanded | Format-Table` is the single-queue shortcut.

- `Format-OrchTestDataQueueItem`: new pipeline formatter for test data queues, parsing `ContentJson` and promoting its top-level properties to columns. Groups by queue path; malformed JSON falls back to a raw column rather than aborting.

## Improvements
- `Set-OrchAsset`: `-ValueType Credential` and `-ValueType Secret` are now silently skipped (previously `Credential` was skipped but `Secret` would error). Use the type-specific `Set-OrchCredentialAsset` / `Set-OrchSecretAsset`.

- `Get-OrchAsset -ExportCsv`: excludes `Credential` and `Secret` assets (same policy as before, extended to Secret).

- `Copy-OrchAsset`: now supports all five asset types. Secret was previously rejected by the server with "asset secret value cannot be null"; the copy now inserts a `!!!PLEASE UPDATE!!!` placeholder and emits a warning (same pattern as Credential). When the source asset has an `ExternalName` (vault reference), the placeholder is skipped so the vault link is preserved verbatim. This applies to both Global and per-robot UserValue paths.

- `Get-OrchQueueItem`: the 600ms inter-page rate-limit wait is now cancellable (`Ctrl+C` no longer blocks up to 600ms per iteration) and is skipped on the final page (a partial short page means no further API call is coming). Removed 60 lines of commented-out multi-threaded implementation and an unused `WriteQueryUnavailableWarning` method.

- `Get-OrchQueueItem` help: added examples and notes for the `Expanded` ScriptProperty and `Format-OrchQueueItem`.

## Bug Fixes
- v20+ asset retrieval: `/odata/Assets` silently drops Secret-typed assets, while `/odata/Assets/.../GetFiltered` returns `CredentialUsername` as empty for Credential assets. Neither endpoint alone is sufficient. `GetAssets` now merges non-Secret from `/odata/Assets` with Secret-only from `GetFiltered?$filter=ValueType eq 'Secret'` on v20+ servers.

- `Get-OrchSecretAsset -ExpandUserValues`: the Global row was being filtered by `!string.IsNullOrEmpty(asset.Value)` but Secret's `Value` is always null (server masks it), so Global-scope Secret assets were never emitted in the expanded view. Switched the check to `HasDefaultValue`.

- `Copy-OrchAsset` UserValue Credential: the "Please update credential asset passwords" warning only fired for Global-level Credential copies. PerRobot-only Credential copies silently left `!!!PLEASE UPDATE!!!` without any indication. Now warns consistently for both scopes.

- `Copy-OrchAsset` UserValue machine-not-assigned: replaced the duplicate warning + error pair with a single warning (matching other `FindDstMachine` call sites).

## Breaking Changes
- Retired `Get-OrchTestDataQueueItemTable`. Use `Get-OrchTestDataQueueItem | Format-OrchTestDataQueueItem` instead. The old function misused the `Get-` verb (emitted format records, not data). Assumed unused; no aliases provided.


# Version: 0.9.16.6
## Bug Fixes
- Install-Module of 0.9.16.5 failed with "authenticode signature of the file 'UiPathOrch.psd1' is not valid" on any machine where the self-signed code-signing certificate was not pre-trusted. Root cause: 0.9.16.5 signed `UiPathOrch.psd1` / `.psm1` / `.ps1xml` / `Functions/*.ps1` in addition to the DLL, and Install-Module verifies Authenticode signatures on script files. Fixed by narrowing the signing scope to `UiPathOrch.dll` only; script files are published unsigned. No functional change.


# Version: 0.9.16.5
## Bug Fixes
- Get-PmLicenseInventory: JSON deserialization failed on tenants with consumable SKUs that report fractional allocation (e.g. AIU `allocated: 1451.8`). Widened `ProductAllocation.total` / `.allocated` from `int?` to `double?`.

## Improvements
- Get-PmLicenseInventory / Get-PmLicenseContract: the list-view hint for nested collections no longer suggests `| Format-Table` — e.g. `ProductAllocations : 51 item(s) — use $_.productAllocations` and `Products : N item(s) — use $_.products`.


# Version: 0.9.16.4
## New Features
- Get-PmLicenseAllocation: New cmdlet that returns per-tenant robot / runtime license allocations for a UiPath Automation Cloud organization (Robots & Services tab of Admin / Licenses). Parameters: -Tenant (wildcards, tab completion), -Path. Organization-scoped; Automation Cloud only.

- Get-PmLicenseInventory: New cmdlet that returns the organization-level license inventory dashboard, bundling five collections into one object: productAllocations, userLicensingBundles, entitlementUsages, availableServices, mlKeys. Parameters: -Path. Organization-scoped; Automation Cloud only.

- Get-PmLicenseContract: New cmdlet that returns the full license contract for an organization including purchased products, bundle templates, entitlements, ML service keys, and the embedded license payload (preserved verbatim). Parameters: -Path. Organization-scoped; Automation Cloud only.

## Improvements
- Added Get-Help documentation for Get-PmLicense and the three new Pm license cmdlets.


# Version: 0.9.16.3
## New Features
- Get-PmLicense: New cmdlet that returns tenant-level license inventory (allocated / inUse / total per UserBundle). Parameters: -License / -Code (wildcards, tab completion), -Path, -HasCapacity (allocated < total). Includes a formatted view with a usage bar.

## Improvements
- Rewrote about_UiPathOrch help topic to reflect the current feature set.

- Added -UseDefaultEditor switch to Edit-OrchConfig.

## Bug Fixes
- Suppress spurious PipelineStoppedException in WriteError when using Select-Object -First.

- Fixed Import-OrchQueueItem double-serialization bug.

- NavigationCmdletProvider review fixes: GetChildNames now writes the correct name / full-path / isContainer values, GetChildItems / GetChildNames honor Stopping mid-loop, GetItem has null guards, empty RenameItem / RemoveItem overrides were removed so the base class throws PSNotSupportedException, and several cmdlets gained [OutputType(typeof(void))].

## Internal
- Pinned .NET SDK to 8 via global.json.


# Version: 0.9.16.2
## New Features
- Get-OrchPSDrive: IdentityUrl is now automatically derived from Root when not explicitly configured, so it is always available in Get-OrchPSDrive output. Previously it was null unless specified in the settings file.

- Get-OrchPSDrive: Added Claims property containing decoded JWT access token claims as a PSObject. Access individual claims via `$drive.Claims.prt_id`, `$drive.Claims.email`, etc. Timestamp claims (exp, iat, nbf, auth_time) are converted to local DateTime.

## Improvements
- Refactored Update-OrchMachine to use HTTP PATCH with minimal payloads instead of DeepCopy + PUT. Only specified parameters are sent. Machine slots (UnattendedSlots, NonProductionSlots, TestAutomationSlots) can now be set to 0.

- Refactored Update-OrchUser to use per-property dirty flags instead of IEquatable comparison.

- Refactored Set-OrchAsset and Set-OrchCredentialAsset: extracted helper methods, replaced manual DeepCopy with shared utility, and improved PerRobot value lookup performance with Dictionary-based indexing.

- Removed IEquatable implementations from 14 entity types, no longer needed after the Update cmdlet refactoring.

- Major internal refactoring of the tab completion engine and parallel execution infrastructure.

## Bug Fixes
- Fixed Set-OrchAsset Integer parse error message incorrectly saying "bool" instead of "integer".

- Fixed Set-OrchCredentialAsset: empty CredentialPassword no longer silently deletes the Global credential value. Previously, re-importing a CSV exported by Get-OrchAsset -ExportCredentialCsv (which contains empty password fields) could destroy existing credentials. Use Remove-OrchAsset to delete credential assets instead.

## Breaking Changes
- Set-OrchCredentialAsset: `-CredentialPassword ''` no longer deletes the Global credential value. This prevents accidental credential loss when re-importing exported CSVs. PerRobot entry deletion via `-UserName <user> -CredentialPassword ''` is unchanged.

- Update-OrchMachine: Machine slot parameters (UnattendedSlots, NonProductionSlots, TestAutomationSlots) now accept 0 as a valid value. Previously, 0 was treated as "not specified".


# Version: 0.9.16.1
## New Features
- Added Set-OrchSetting cmdlet for updating tenant settings, with a Value completer that shows the current value.

- Added Getting Started guide (Docs/00-GettingStarted.md).

- Added CSV Export & Import guide (Docs/03-CsvExportImport.md).

- Added Pester integration tests (119 tests) covering Machine/Queue/Bucket/Asset CRUD, CSV export/import, cross-tenant copy, folder provider operations, error handling, wildcard support, and large-scale PerRobot asset operations.

## Improvements
- Entra ID warning now only fires when the tenant has Entra ID integration and the user is not signed in via Entra ID. Previously it triggered for all non-Entra ID users regardless of tenant configuration.

- Improved cross-version migration compatibility: copy and create operations for processes, machines, webhooks, queues, assets, buckets, schedules, and roles now work with older API versions (including On-prem 20.10 / API v11).

- Improved Update cmdlets: Update-OrchQueue, Update-OrchTrigger, and Update-OrchProcessVersion now detect changes before making API calls, skipping no-op updates.

- Refactored Update-OrchMachine to use HTTP PATCH with minimal payloads instead of DeepCopy + PUT. Only specified parameters are sent. Machine slots (UnattendedSlots, NonProductionSlots, TestAutomationSlots) can now be set to 0.

- Refactored Update-OrchUser to use per-property dirty flags instead of IEquatable comparison.

- Refactored Set-OrchAsset and Set-OrchCredentialAsset: extracted helper methods, replaced manual DeepCopy with shared utility, and improved PerRobot value lookup performance with Dictionary-based indexing.

- Replaced undocumented EditRelease API with public PATCH and PutReleaseRetention endpoints.

- Sort target processes by name in Update-OrchProcess.

- Improved organization ID resolution by extracting it directly from the JWT token, avoiding extra API calls.

- Removed unused IEquatable implementations from 14 entity types.

## Bug Fixes
- Fixed Entra ID warning appearing during tab completion by deferring display to cmdlet execution.

- Fixed Set-OrchAsset Integer parse error message incorrectly saying "bool" instead of "integer".

- Fixed Set-OrchCredentialAsset: empty CredentialPassword no longer silently deletes the Global credential value. Previously, re-importing a CSV exported by Get-OrchAsset -ExportCredentialCsv (which contains empty password fields) could destroy existing credentials. Use Remove-OrchAsset to delete credential assets instead.

- Fixed cache and retention comparison issues in Update cmdlets.

- Fixed Queue ReleaseId and Webhook Name for cross-version copy.

- Fixed role cache not clearing on error in CopyRoles, causing stale data.

- Fixed HTTP response body stream race between caller and async logger.

- Fixed minor resource disposal and thread pool issues.

## Breaking Changes
- Set-OrchCredentialAsset: `-CredentialPassword ''` no longer deletes the Global credential value. This prevents accidental credential loss when re-importing exported CSVs. PerRobot entry deletion via `-UserName <user> -CredentialPassword ''` is unchanged.

- Update-OrchMachine: Machine slot parameters (UnattendedSlots, NonProductionSlots, TestAutomationSlots) now accept 0 as a valid value. Previously, 0 was treated as "not specified".

## Documentation
- Reorganized documentation: renamed MigrationGuide to 04-MigrationGuide.md, added cross-version migration guidance, library feed settings, Queue SLA auto-assignment, and user investigation procedure.


# Version: 0.9.16.0
## New Features
- Added Import-OrchConfig cmdlet. Imports the configuration file and creates PSDrives for all enabled tenants. Use this to apply configuration changes without restarting the PowerShell session.

- Added New-OrchPSDrive cmdlet. Creates a new PSDrive in the current session without using the configuration file.

- Added Get-OrchConfigPath cmdlet. Returns the path to the UiPathOrch configuration file, allowing AI and scripts to directly read or edit it.

- Added Switch-OrchCurrentUser cmdlet. Opens an InPrivate browser window to re-authenticate with a different account. Useful when SSO auto-login prevents account switching.

- Added Entra ID login warning. When connecting to a drive, UiPathOrch checks the JWT token and displays a warning if the user is not signed in via Entra ID. For AD-integrated organizations, UiPathOrch automatically directs the user to the Entra ID login page during PKCE authentication.

- Added handling for deprecated Alerts API: returns an error on API v18+.

- (Experimental) Added New-OrchUserMappingCsv and Test-OrchUserMappingCsv cmdlets for cross-organization tenant migration with username mapping. New-OrchUserMappingCsv generates a CSV that maps source directory users to destination directory users by searching the destination directory. Test-OrchUserMappingCsv validates the mapping. All copy cmdlets now accept the -UserMappingCsv parameter to translate user references during migration.

## Bug Fixes
- Fixed Cloud PKCE authorize URL to use the updated UiPath Identity Server endpoint with acr_values for organization routing.

- Fixed New-OrchProcess bugs: triple enumeration of packages, Description property leaking between calls, and nullable handling.

- Fixed parameter mutation bugs and ErrorId usage across 47 files.

- Fixed Pm cmdlets race condition by converting from parallel to sequential execution when sharing cache.

- Fixed Personal workspace exclusion across FolderUser, FolderMachine, and Test cmdlets.

- Fixed OutputType attributes: corrected ActionCatalog type, removed incorrect OutputType from Copy cmdlets.

- Fixed ShouldProcess messages: typos and naming consistency.

- Removed uipath.com domain check from Document Understanding and Test Manager drive creation, enabling Automation Suite environments.

## Documentation
- Rewrote all cmdlet help documentation using PlatyPS v1. 238 markdown help files with standardized descriptions, 1030+ examples, parameter documentation.

- Removed legacy PDF documentation and Japanese-language help files. All documentation is now in English markdown format.

- Added AI-oriented guide documents (Docs/01-Essentials.md, 02-CmdletReference.md, 03-MigrationGuide.md) for module usage, cmdlet reference, and tenant migration procedures.

## Breaking Changes
- Update cmdlets (Set-Orch*) now treat empty string "" as an intentional value clear. Previously, empty strings were silently ignored.


# Version: 0.9.15.5
- Added Get-OrchTestCaseAssertion cmdlet. Retrieves test case assertions for a given test case execution. Supports pipeline input from Get-OrchTestCaseExecution and can download screenshots using the -ScreenshotPath parameter.

- Added Get-OrchLogLocation cmdlet. Returns the log folder path for a specified drive. Useful for scripting access to log files without opening File Explorer.

- Enhanced Get-OrchTestCaseExecution with new filter parameters: -Last, -StartTimeAfter, and -StartTimeBefore. Also added -TestSetExecutionName filter, -Skip and -First pagination parameters. When -PathTestSetExecutionName is specified, the corresponding TestSetExecution is now fetched automatically if not cached.

- Fixed a bug where Get-OrchJob, Get-OrchQueueItem, and Get-OrchTestCaseExecution could fail with HTTP 400 errors when filtering by a large number of robots or test set executions. The OData filter is now automatically split into batches to stay within API limits.

- Fixed on-premises OAuth authentication failing due to an extra tenant path segment being included in the identity server URL.

- Fixed Get-OrchTestCaseExecution returning duplicate results when used as pipeline input to itself.

- Get-OrchTestSetExecution now outputs cached results with a warning message when no filter parameters are specified, consistent with other cmdlets such as Get-OrchJob and Get-OrchLog.

- Fixed Get-OrchJob compatibility with older on-premises Orchestrator versions. On API versions earlier than 12, the Machine field is excluded from the $expand clause and the ProcessType filter is omitted, preventing request errors on unsupported versions. The threshold has been confirmed up through API version 13, and will be re-evaluated when version 14 is available.

- Fixed on-premises API requests to use X-UIPATH-TenantName header instead of embedding the tenant in the URL path. Also improved Cloud base URL calculation to derive the org path dynamically instead of relying on a hardcoded domain match.

- Tab completion and path resolution now canonicalize folder and project name casing from cache across all providers (Orchestrator, Document Understanding, Test Manager).

- Fixed SpecificPriorityValue being sent to Orchestrator API versions that reject it. The threshold has been raised from ApiVersion 12 to 14, and the value is now cleared after converting to JobPriority.

- Fixed RateLimiter resource leak by adding Dispose and removing unused Release method.


# Version: 0.9.15.4
- Fixed Get-OrchProcess -ExportCsv not retrieving process entry points correctly.


# Version: 0.9.15.3
- Added Get-PmAccessAllowedMember cmdlet to retrieve partition access policy members.


# Version: 0.9.15.2
- Added Remove-OrchEventTrigger cmdlet.

- Added -EntryPointPath alias to -Name parameter of Get-OrchTestCaseExecution.

- Fixed Get-OrchTestCaseExecution filtering by -Name.

- Fixed Get-ChildItem not working properly when there are no folders or projects.


# Version: 0.9.15.1
- DateTime properties now return local time instead of UTC, even when not using the default view. Previously, local time was only displayed in the default view. This is a breaking change if you were relying on UTC timestamps.

- Improved performance by removing ScriptBlock for timezone conversion from format views.


# Version: 0.9.14.19
- Added Get-TmTestExecution cmdlet. Note that this cmdlet is only available on the UiPathOrchTm drive. The UiPathOrchTm drive becomes automatically available when you add the TM.* scope. You can verify this with the Get-PSDrive cmdlet.

- Adjusted the default view for Get-TmRequirement to better align with the Automation Cloud display.

- Changed the default view for Get-TmProjectPermission from list view to table view.


# Version: 0.9.14.18
- Added Get-OrchEventTrigger, Enable-OrchEventTrigger, and Disable-OrchEventTrigger cmdlets to manage event triggers.


# Version: 0.9.14.17
- Added Get-PmAuthenticationSetting cmdlet to retrieve organization authentication settings.


# Version: 0.9.14.16
- Added Get-OrchProcessRequirement cmdlet.


# Version: 0.9.14.15
- Added Remove-OrchBucketItem cmdlet.

- Fixed Import-OrchBucketItem not escaping file names, which caused incorrect names when special characters were used.

- Improved Import-OrchBucketItem to set Content-Type based on file extension.

- Enhanced Import-OrchBucketItem and Export-OrchBucketItem cmdlets to support cancellation with Ctrl+C.

- Updated Get-OrchBucketItem default view to right-align the Size column for better readability.


# Version: 0.9.14.14
- Added Import-OrchBucketItem cmdlet. Upload multiple files at once with wildcards or comma-separated values. You can also re-upload files downloaded with Export-OrchBucketItem while preserving the original folder structure and bucket names.


# Version: 0.9.14.13
- In the Add-OrchFolderUser cmdlet, the API called to search for the user specified with the -UserName parameter has been switched. This change enables usernames containing hyphens (-) to be resolved correctly and improves overall stability. As a result of this update, group names specified with -UserName are now case-sensitive.
  - From: GET /api/DirectoryService/SearchForUsersAndGroups
  - To: POST /api/Directory/BulkResolveByName/{partitionGlobalId}

- Accordingly, in the Get-OrchFolderUser cmdlet, the CSV file output with the -ExportCsv parameter now records group UserName values with case sensitivity preserved.


# Version: 0.9.14.12
- Added the -DisplayName parameter to the Update-PmUser cmdlet, allowing the displayName property to be updated.


# Version: 0.9.14.11
- Added the Export-OrchBucketItem cmdlet. This cmdlet exports files contained in storage buckets to a local folder.

  ```powershell
  # Exports all files from all buckets in the current folder to the current directory on drive c:
  Export-OrchBucketItem

  # Exports all files from the specified bucket in the current folder
  Export-OrchBucketItem MyBucket

  # Exports the specified files from all buckets in the current folder
  Export-OrchBucketItem * MyFile*, YourPic*

  # Exports all files from all buckets across the tenant to the specified local folder
  Export-OrchBucketItem -Path Orch1:\ -Recurse -Destination c:\tmp
  ```


# Version: 0.9.14.10
- Improved the behavior of Risk Management parameters (-WhatIf, -Confirm, -Verbose) in the Add-OrchFolderMachine cmdlet. Previously, when multiple machines were specified for the -Name parameter, these parameters applied collectively. They now apply individually to each machine.

- Added the PropagateToSubFolders column to the default view of the Get-OrchFolderMachine cmdlet.


# Version: 0.9.14.9
- In the previous release, the validator for the -Type parameter of the Add-OrchUser cmdlet was not functioning as expected.

- Improved the implementation of certain validators by replacing the use of runtime type information with generic type parameters.


# Version: 0.9.14.8
- Fixed an issue where the -Recurse parameter of the Add-DuUser cmdlet was not functioning. You can now add users to all Document Understanding projects at once, as shown below:

  Orch1Du:\> Add-DuUser -Recurse DirectoryUser user1@uipath.com, user2@uipath.com 'DU Data Annotator'

- Note: The following usage, which has always worked as intended, achieves the same result:

  Orch1Du:\> Add-DuUser -Path * DirectoryUser user1@uipath.com, user2@uipath.com 'DU Data Annotator'

- Added validators to the following parameters to ensure that errors are raised for invalid values. This is especially helpful when using UiPathOrch via PowerShell.MCP, where LLMs cannot use completers:
  - The -Type parameter of cmdlets such as Get-OrchUser and Add-OrchUser
  - The -Last parameter of cmdlets such as Get-OrchJob and Get-OrchAuditLog


# Version: 0.9.14.7
- The GroupName column was missing from the CSV file output by Get-PmGroupMember -ExportCsv.

- Added -Name as an alias for the -GroupName parameter in cmdlets that handle PmGroup entities. Since the group name is included in the "name" property of the entity returned from the API, this change makes it easier to work with these cmdlets in pipelines. Affected cmdlets:
  - Get-PmGroup
  - Get-PmGroupMember
  - New-PmGroup 
  - Add-PmGroupMember
  - Remove-PmGroup
  - Remove-PmGroupMember
  - Copy-PmGroup
  - Move-PmGroupMember
  - Get-PmLicensedGroup
  - Add-PmLicenseToPmLicensedGroup
  - Remove-PmLicensedGroup
  - Remove-PmLicenseFromPmLicensedGroup
  - Remove-PmAllocationFromPmLicensedGroup

# Version: 0.9.14.6
- Some API version checks were inadvertently left in the test entity copy cmdlets in version 0.9.14.5. These checks have now been removed.


# Version: 0.9.14.5
- Previously, when connecting to a tenant whose Orchestrator API version was below 18, cmdlets for managing test entities would perform no actions (i.e., no API calls were made). This behavior was incorrect. These cmdlets now function normally regardless of the API version. The affected entities include:
  - OrchTestCase
  - OrchTestSet
  - OrchTestCaseExecution
  - OrchTestSetExecution
  - OrchTestSetSchedule
  - OrchTestDataQueue
  - OrchTestDataQueueItem

  If test entities are not enabled for the tenant, running these cmdlets results in errors. These errors are harmless and can be safely ignored. In the future, if a reliable way to detect whether test entities are enabled becomes available, we plan to restore the behavior that disables these cmdlets accordingly.

- The -Name parameter was not set as mandatory for the Enable-OrchFolderMachineInherit and Disable-OrchFolderMachineInherit cmdlets. This has been corrected.


# Version: 0.9.14.4
- The list of personal workspace folders is no longer retrieved when connecting to a tenant configured with a confidential application. This improves connection speed.

# Version: 0.9.14.3
- When searching Active Directory integrated with a tenant, it is now possible to search by values other than identityName. This improves the behavior of the following cmdlets:
  - Add-OrchUser
  - Add-OrchFolderUser
  - Add-PmLicenseToPmUser
  - Search-OrchDirectory

- For the following cmdlets, completion suggestions did not work as expected when pressing Ctrl+Space or Tab with input enclosed in double quotes:
  - Add-OrchFolderUser
  - Add-PmGroupMember
  - Add-PmLicenseToPmUser
  - Add-PmLicenseToPmLicensedGroup
  - Search-OrchDirectory


# Version: 0.9.14.2
- Calls to the GET /api/DirectoryService/SearchForUsersAndGroups endpoint are now made with appropriate rate limiting to respect the API quota. As a result, CSV files containing a large number of users can now be successfully imported using the following cmdlets:
  - Add-OrchUser
  - Add-OrchFolderUser
  - Copy-OrchUser
  - Add-PmLicenseToPmUser
  - Copy-Item


# Version: 0.9.14.1
- The *-Du* and *-Tm* cmdlets now also use the SessionState of their PSCmdlet context. This improves robustness.


# Version: 0.9.14.0
- When used together with PowerShell.MCP, the current location could sometimes not be resolved correctly. This issue was caused by using a cached SessionState in both the context of the PSCmdlet and external components such as parameter completers. The implementation has been modified so that the PSCmdlet context now refers to its own SessionState, resolving the issue.


# Version: 0.9.13.6
- When copying a trigger from a classic folder using the Copy-OrchTrigger cmdlet, the associated machine is now also taken into consideration. Robot accounts were already handled as of version 0.9.13.4, and this change complements that enhancement.


# Version: 0.9.13.5
- The Export-OrchLibrary cmdlet did not correctly resolve the current location when only a drive name was specified for the -Destination parameter. The following usage is now supported:

  ```powershell
  # Exports to the current location of the C: drive:
  Export-OrchLibrary -Path Orch1: YourLibrary 1.0.1 c:

  # Paths on FileSystem PSDrives, such as Temp:, are now handled correctly
  Export-OrchLibrary -Path Orch1: YourLibrary 1.0.1 temp:

  # If -Destination is omitted, exports to the current location of the last-used FileSystem drive:
  Export-OrchLibrary -Path Orch1: YourLibrary 1.0.1
  ```

  Note: This issue had already been addressed for Export-OrchPackage in version 0.9.9.1, but the fix for Export-OrchLibrary had been unintentionally omitted until now.


# Version: 0.9.13.4
- When copying a trigger using the Copy-OrchTrigger cmdlet, if the source folder is a classic folder, the ExecutorRobots property of the destination trigger is now constructed by referencing the classic robot from the source.


# Version: 0.9.13.3
- Improved the behavior of the New-OrchProcess cmdlet.
  - When the -EntryPoint parameter is specified, execution could fail in folders with a feed.
  - When the -Version parameter is not specified, the latest version is now selected automatically.

- The Remove-OrchPackage cmdlet did not previously clear the package cache after execution.

- In cmdlets that update entities, such as Update-OrchTrigger, even for parameters that require resolving names to IDs, omitted parameters no longer overwrite existing values with null. Existing values are now preserved instead.


# Version: 0.9.13.2
- The caches for PmLicensedUser and PmLicensedGroup are now also shared across tenants (PSDrives) that belong to the same organization.


# Version: 0.9.13.1
- The cache for PmGroup is now also shared across tenants (PSDrives) that belong to the same organization.


# Version: 0.9.13.0
- The caches for PmUser, PmRobotAccounts, PmExternalClients, and PmExternalApiResources are now shared across tenants (PSDrives) that belong to the same organization. This change improves performance, reduces memory usage, and ensures consistent behavior when working with multiple tenants within the same organization. Note that caches for other organization-level entities, such as PmGroup, are still managed separately per PSDrive.

- A ReleaseNotes.txt file has been added to the module installation folder. You can navigate to this folder using the Set-OrchLocation cmdlet.


# Version: 0.9.12.12
- The completer for the -EntryPoint parameter of New-OrchProcess did not work as expected when a non-latest version was specified for the -Version parameter.


# Version: 0.9.12.11
- Automation Cloud organization-user creation and deletion API has been deprecated, causing the Get-PmUser, Remove-PmUser, Update-PmUser and Copy-PmUser cmdlets to stop working. These cmdlets now invoke the new private API (version 19.0 remains unchanged), restoring correct operation.

- The New-PmUser, Remove-PmUser, Update-PmUser and Copy-PmUser cmdlets now clear the PmUser and PmGroup caches after execution to ensure data consistency.


# Version: 0.9.12.10
- Made HTTP log output asynchronous, improving performance.

- Updated the module manifest (UiPathOrch.psd1) so that RootModule now points to the .dll, slightly reducing Import-Module time.

- Changed the Get-OrchJob cmdlet to no longer support wildcards for the -State parameter; specifying an invalid state now throws a runtime error to prevent LLMs from passing unsupported values.


# Version: 0.9.12.9
- In entities returned from the Web API, fields that should have been Guids were sometimes returned in a non-Guid format, causing JSON deserialization to fail. Therefore, all of those fields have been changed to the string type. As a result, the reliability of JSON deserialization has been improved.


# Version: 0.9.12.8
- Corrected the CSV output of Get-OrchTrigger -ExportCsv, which had invalid values in the ExecutorRobots column.

- Enabled specifying machines inherited from a parent folder with the -MachineRobots parameter of Update-OrchTrigger.

- The validity check for tenant URLs specified in the configuration file was incorrect, causing URLs such as https://orchestrator.local/ to be mistakenly treated as invalid. MSI Orchestrator URLs can indeed take this form.

- Introduced a Get-OrchHelp cmdlet for LLM with MCP scenarios, intended to allow an LLM to learn how to use the UiPathOrch module. Please try the PowerShell.MCP module: https://www.powershellgallery.com/packages/PowerShell.MCP


# Version: 0.9.12.7
- In Get-OrchUser -ExportCsv, if an Exp* property (for example, ExplicitMayHaveUserSession or ExplicitMayHaveRobotSession) is null, the corresponding May* column (for example, MayHaveUserSession or MayHaveRobotSession) now falls back to its original value.


# Version: 0.9.12.6
- When copying queues using the Copy-Item or Copy-OrchQueue cmdlet, if the source queue does not have StaleRetention policy configured, the destination queue is now automatically set to Delete as the StaleRetentionAction and 180 as the StaleRetentionPeriod.

- The Update-OrchUser cmdlet could fail with an error when ExecutionSetting parameters (-ES_*) were specified. This was because the ExecutionSetting values were applied to both the UnattendedRobot and RobotProvision properties of the user, regardless of whether those properties were present. This behavior has been adjusted: -ES_* parameters are now only applied to UnattendedRobot and RobotProvision if they are not null.


# Version: 0.9.12.5
- Added support for the -Name (alias: -FirstName) and -Surname (alias: -LastName) parameters to the Update-OrchUser cmdlet, enabling user name updates. Please note that these parameters are effective only in MSI Orchestrator. In Automation Cloud, specifying these parameters has no effect.


# Version: 0.9.12.4
- The Add-OrchUser cmdlet could previously fail when attempting to add robot accounts. This issue has been resolved by explicitly setting the following property values when adding a robot account:

  - "MayHavePersonalWorkspace": false
  - "MayHaveRobotSession": false
  - "MayHaveUnattendedSession": true
  - "MayHaveUserSession": false


# Version: 0.9.12.3
- Folder cache was not properly cleared in some cases after executing Clear-OrchCache, or after creating or deleting folders. This issue was introduced in version 0.9.12.2.

- Copy-Item and Copy-OrchQueue failed to copy queues from Automation Cloud to MSI Orchestrator. This issue was introduced in version 0.9.10.7.

- New cmdlets Copy-OrchQueueItem and Remove-OrchQueueItem have been added.
  - Copy-OrchQueueItem copies all queue items with the status New to a queue with the same name in a different folder, and outputs the items that were successfully copied. The output can be piped to Remove-OrchQueueItem to remove those items from the source queue, preventing duplicate transaction processing.

  - Remove-OrchQueueItem outputs any items that failed to be removed. These can be exported to a CSV file using Export-Csv, and later re-imported to retry deletion. To use Remove-OrchQueueItem independently, specify the queue name and the IDs of the items to remove.

- Below is an example of using the Copy-OrchQueueItem and Remove-OrchQueueItem cmdlets:
  - To move items from the MyQueue queue in the \Shared folder to a queue with the same name in the \dst folder:
    PS Orch1\Shared> Copy-OrchQueueItem MyQueue \dst | Remove-OrchQueueItem | Export-Csv c:itemsToRemove.csv -Encoding utf8BOM

  - To move items from all queues in one tenant to the corresponding queues in another tenant with the same folder structure:
    PS Orch1:\> Copy-OrchQueueItem -Recurse * Orch2:\ | Remove-OrchQueueItem | Export-Csv c:itemsToRemove.csv -Encoding utf8BOM

- Note: The Copy-OrchQueueItem cmdlet does not copy the queues themselves. If the destination folder does not contain a queue with the same name, you need to copy the queues beforehand using the Copy-OrchQueue cmdlet. Keep in mind that Copy-OrchQueue copies only the queue definitions. It does not copy the items contained in the queues.
  - To copy all queues in the \Shared folder to the \dst folder:
    PS Orch1\Shared> Copy-OrchQueue * \dst

  - To copy all queues in a tenant to another tenant that has the same folder structure:
    PS Orch1:\> Copy-OrchQueue -Recurse * Orch2:\

- To ensure safe operation, it is strongly recommended to disable triggers and stop automation processes in both source and destination folders before copying queue items.
  - To disable all triggers in both Orch1: and Orch2::
    PS> Disable-OrchTrigger -Path Orch1:\,Orch2:\ -Recurse *


# Version: 0.9.12.2
- Fixed an issue where CSV files generated by Get-OrchAsset -ExportCsv became invalid if the asset description contained commas.

- Fixed an issue where the folder output order was incorrect when using Get-ChildItem -Recurse or specifying the -Recurse parameter with other cmdlets (such as Get-OrchAsset or Get-OrchProcess), if the target was a Personal Workspace containing subfolders.
  - Note: The Personal Workspace can contain subfolders if a Solution has been deployed within it.

- You can now use the proxy settings configured in Internet Options. To enable this, update the configuration file as follows:

  "Proxy": {
    "UseDefaultWebProxy": true,
    "Enabled": true
  },

  - Other properties within the "Proxy" section, such as "Url" and "BypassProxyOnLocal", can remain unchanged. These properties will be ignored when "UseDefaultWebProxy" is set to true.


# Version: 0.9.12.1
- Added Copy-PmGroup and Copy-PmExternalApplication cmdlets.

- The following cmdlets are now available for copying organization entities. When all of these cmdlets are executed, the final set of entities created in the destination will be the same, regardless of the order in which the cmdlets are run:
  - Copy-PmUser
  - Copy-PmRobotAccount
  - Copy-PmExternalApplication 
  - Copy-PmGroup

- All of these cmdlets share the same usage pattern. For example, the following command copies all local users from Orch1: to Orch2:

  PS Orch1:\> Copy-PmUser * Orch2: -WhatIf

  - If the output looks correct, remove the -WhatIf switch and run the command again.

  - Note: If Orch1: and Orch2: belong to the same organization, no action will be taken.

- Copy-PmUser, Copy-PmRobotAccount, and Copy-PmExternalApplication automatically add copied entities to groups with the same name in the destination. If no such group exists, one is automatically created.

- Copy-PmGroup automatically adds local users, robot accounts, external applications, directory users, and directory groups with the same name to the copied group. However, if such entities with the same names do not exist in the destination, they will not be created automatically.

- Added Remove-PmExternalApplication cmdlet.
  - Note: External applications currently in use for the UiPathOrch connection cannot be deleted unless the -Force switch is specified.

- Updated Remove-PmUser to prevent deletion of users currently used for connecting via non-confidential apps in UiPathOrch.

- Fixed an issue where New-OrchProcess did not work in environments using API versions earlier than 19. This issue was introduced in version 0.9.10.9.
  - Note: You can check the Web API version of the target Orchestrator using the Get-OrchPSDrive cmdlet.

- Triggers copied with Copy-OrchTrigger or Copy-Item are now always created as disabled, even if the source trigger is enabled.

- When using the Orch-UpdateUser cmdlet with parameters that modify ExecutionSettings (such as -ES_TracingLevel or -ES_StudioNotifyServer), the settings were previously applied only to the user's UnattendedRobot property. Now, the cmdlet also applies the same values to the user's RobotProvision property. Additionally, these property updates are no longer applied to users whose Type is DirectoryExternalApplication.


# Version: 0.9.12.0
- Cmdlets whose noun names begin with OrchPm (such as Get-OrchPmUser) can now also be used with PSDrives provided by the UiPathOrchDu and UiPathOrchTm providers. These drives are automatically mounted when the corresponding scopes (Document Understanding and Test Manager, respectively) are configured in the settings file. After importing the UiPathOrch module, you can verify the mounted drives using the Get-PSDrive cmdlet.

- Along with this update, the cmdlet names with the OrchPm noun prefix have been renamed to use the shorter Pm prefix instead. For example, Get-OrchPmUser is now renamed to Get-PmUser. We apologize for any inconvenience caused by this change.


# Version: 0.9.11.4
- The Copy-OrchPmRobotAccount cmdlet now automatically creates groups with the same names in the target organization if the groups to which the robots being copied belong do not already exist there.


# Version: 0.9.11.3
- Starting with version 0.9.9.7, you can add a UiPathOrch PSDrive using the New-PSDrive cmdlet. Version 0.9.11.3 (this release) adds support for using a personal access token (PAT) via the new -AccessToken parameter. This allows you to mount a PSDrive without a configuration file or an external app registration:

  PS> New-PSDrive -PSProvider UiPathOrch -Name Orch1 -Root https://cloud.uipath.com/YOUR_ORGANIZATION/YOUR_TENANT -AccessToken YOUR_ACCESS_TOKEN -OAuthScope "OR.Folders OR.Users OR.Settings"

  - For the -AccessToken parameter, specify either a personal access token or a bearer token obtained through another external application connection.

  - Please note that the parameter name for specifying the scopes is -OAuthScope, not -Scope.

  - You must run Import-Module UiPathOrch before executing the New-PSDrive cmdlet.

  - If no configuration file exists when the module is imported, one is automatically created and opened in Notepad. To suppress this behavior, set the environment variable UIPATHORCH_SUPPRESS_CONFIG_CREATION to 1 before importing UiPathOrch. This feature was introduced in UiPathOrch 0.9.9.7.

- Added the -Robot filter parameter to the Get-OrchJob cmdlet. Wildcards are supported.
  - The robot names specified in the -Robot parameter of Get-OrchJob can also be constructed from the output of Get-OrchRobot. For example:

    PS Orch1:\Shared> Get-OrchJob -Robot (Get-OrchRobot | ? Type -eq Unattended | select -ExpandProperty Name) -State Successful -OrderBy EndTime

- Included the robot Name in the default view of the Get-OrchRobot cmdlet.

- The roleAssignmentDtos property, which belongs to the DuUser entity, was not properly expanded when the output of the Get-DuUser cmdlet was piped to ConvertTo-Json.

- Operations on the Document Understanding entity cache were not thread-safe.


# Version: 0.9.11.2
- You can now connect to a tenant using a personal access token. Please configure the PSDrive in your settings file as follows:

"PSDrives": [
  {
    "Name": "Orch1",
    "Root": "https://cloud.uipath.com/YOUR_ORGANIZATION/YOUR_TENANT",
    "AccessToken": "YOUR_PERSONAL_ACCESS_TOKEN",
    "Scope": "OR.Folders.Read OR.Settings.Read OR.Users.Read",
    "Enabled": true
  }
]

- The following changes were made to prevent meaningless errors from appearing when copying entities:
  - In the Copy-OrchRole and Copy-Item cmdlets, if the source role is static and a role with the same name already exists in the destination tenant, the role is now skipped.  

  - In the Copy-OrchCredentialStore cmdlet, if the source credential store is "Orchestrator Database" and a credential store with the same name exists in the destination, the credential store is now skipped.


# Version: 0.9.11.1
- Improved the behavior of the Add-OrchPmGroupMember cmdlet.
  - When more than 21 usernames were specified simultaneously with the -UserName parameter, the operation would fail. Since this cmdlet aggregates multiple rows from the imported CSV file before calling the API, this issue could occur in practice.

  - This issue was due to restrictions of the following endpoint: POST /api/Directory/BulkResolveByName/{partitionGlobalId}

  - When calling this endpoint with a large number of usernames, the call is now automatically split into batches of 20 users to work around this limitation.

- In the provider for UiPathOrchDu (the Document Understanding PS drive), outputs from Get-ChildItem and from other cmdlets that use the -Recurse parameter (such as Get-DuUser) are now sorted by project name.

- For the -Name parameter of the Remove-DuRoleFromDuUser cmdlet, it was previously required to pass the displayName. However, in the Name column of Get-DuUser -ExportCsv, the email is output for users while groups and apps output the displayName. To maintain consistent behavior, when removing a user from a project using Remove-DuRoleFromDuUser, the -Name parameter now requires specifying the user's email.


# Version: 0.9.11.0
- The Copy-Item cmdlet (alias: copy) now copies not only folder entites but also tenant entities. With this change, a lift & shift tenant migration can be completed with a single command:

  PS> copy src:\ dst:\ -Recurse

- When copying the root folder of source tenant to the root folder of destination tenant, Copy-Item copies all tenant entities. These tenant entities include:
  - Libraries
  - Packages
  - Credential Stores
  - Roles
  - Users
  - Machines
  - Calendars
  - Webhooks

  Note that the process for copying the tenant entities mentioned above shares its implementation with cmdlets such as Copy-OrchLibrary and Copy-OrchPackage.

- Below are some examples of executing the Copy-Item cmdlet (copy is an alias for Copy-Item):
  - To copy all tenant entities and all folders:
    PS> copy src:\ dst:\ -Recurse

  - Select the types of tenant-level entities you want to copy, and the folders you wish to include:
    PS> copy src:\ dst:\ -Recurse -Confirm

  - To copy all tenant entities (without copying folders):
    PS> copy src:\ dst:\

  - To copy all folders (without copying tenant entities) Note that in this example, the src:\ root folder itself is not included in 'src:\*':
    PS> copy src:\* dst:\ -Recurse

  - Copy only all folders, without copying any tenant-level entities and the entities contained in the folders:
    PS> copy Orch1:\ Orch2:\ -Recurse -ExcludeEntities

- For detailed instructions on tenant migration and how to use the Copy-Item cmdlet, please refer to the user guide pdf around page 42 in the module's installation directory. To navigate to the installation directory, run the Set-OrchLocation cmdlet after importing this module.

- The Copy-OrchLibrary cmdlet incorrectly copied all libraries regardless of the names specified with the -Id parameter. This bug was introduced in version 0.9.7.10.


# Version: 0.9.10.9
- Two parameters, -A4R_Enabled and -A4R_HealingEnabled, have been added to the New-OrchProcess and Update-OrchProcess cmdlets.

- Additionally, two columns, A4R_Enabled and A4R_HealingEnabled, have been added to the CSV file output by Get-OrchProcess -ExportCsv. This CSV file can be imported using the New-OrchProcess and Update-OrchProcess cmdlets.


# Version: 0.9.10.8
- The New-OrchPmUserBulk cmdlet has been renamed to New-OrchPmUser. We apologize for any inconvenience this change may cause.

- The New-OrchPmUser cmdlet now automatically creates groups with the specified name if the name provided to the -GroupName parameter does not contain wildcard characters and no existing group matches the name.

- The Get-OrchPmUser cmdlet now supports the -ExportCsv parameter. The CSV file generated with this parameter can be imported using the New-OrchPmUser cmdlet.

- The -Name parameter completer now works as expected for the following cmdlets:
  - New-OrchBucket
  - New-OrchMachine
  - New-OrchQueue
  - New-OrchTrigger
  - New-OrchPmGroup


# Version: 0.9.10.7
- Properties have been added to several entity types to align with Automation Cloud API version 19.0.

- Modified the Copy-Item, Copy-OrchProcess, Copy-OrchQueue, New-OrchProcess, Update-OrchProcess, New-OrchQueue, and Update-OrchQueue cmdlets:
  - In Automation Cloud tenants, processes and queues can no longer be created with RetentionAction set to "None". As a result, these cmdlets will now automatically replace "None" with "Delete" if specified.

- The Get-OrchProcess -ExportCsv and Get-OrchQueue -ExportCsv now include the following three columns in the generated CSV files:
  - StaleRetentionAction
  - StaleRetentionPeriod
  - StaleRetentionBucket

  - The New-OrchProcess, Update-OrchProcess, New-OrchQueue, and Update-OrchQueue cmdlets now support parameters with the same names as the newly added CSV columns.

- Improvements to the Update-OrchProcess cmdlet:
  - When specifying -EntryPoint parameter, its EntryPointId was incorrectly being set to RetentionBucketId.

  - The -RetentionBucket parameter's completer no longer displays read-only storage buckets.

- Fix for Get-OrchQueue -ExportCsv:
  - Previously, retention policies were not included in the exported CSV.

- Configuration validation improvement:
  - A warning is now displayed if the AppId specified in the configuration file is invalid (e.g., an empty string or not in GUID format).


# Version: 0.9.10.6
- Some parameters of update-related cmdlets did not support CSV file imports, such as the -Path parameter of the Set-OrchAsset cmdlet.

- Additionally, almost all Get-related cmdlets did not support CSV file imports. All affected cmdlets, including Get-OrchAsset and Get-OrchPackage, have been updated to allow imports from CSV files.

- Please note that, at this time, switch parameters do not support CSV imports.


# Version: 0.9.10.5
- The -ExportCsv parameter of the Get-OrchUser cmdlet now outputs the values of Explicit* in the May* columns of the generated CSV file. It appears that Explicit* contains individual user settings, whereas the May* columns reflect the results of the union of privileges model (which includes both the user's permissions and those of the groups the user belongs to). Since the CSV file generated by the -ExportCsv parameter is intended for import using the Add-OrchUser / Update-OrchUser cmdlets, having the Explicit* values in the May* columns ensures consistent behavior.

- Added the Get-OrchUserPrivilege cmdlet. This cmdlet corresponds to the information displayed in the Summary card on the user editing screen after the introduction of the union of privileges model in the web interface. Please note that the Web API called by this cmdlet is currently private, so there is a possibility that this cmdlet may stop working in the future.


# Version: 0.9.10.4
- The Add-DuRoleToDuUser cmdlet has been renamed to Add-DuUser. Additionally, this cmdlet now allows adding directory users and groups to projects.

- The -ExportCsv parameter has been added to the Get-DuUser cmdlet. The CSV file generated using this parameter can be imported with the Add-DuUser cmdlet. Please note that the import destination is the project specified in the Path column of the CSV file. This can be overridden by specifying the -Path parameter in the command line. Use the following command:

  PS Orch1Du:\YourProj> Import-Csv c:ExportedDuUsers.csv | Add-DuUser -Path . -WhatIf

- The Path property values in entities output by the Get-DuUser, Get-DuDocumentType, Get-DuClassifier, and Get-DuExtractor cmdlets were incorrect.

- The output format of the Get-OrchPmGroupMember cmdlet could be inconsistent.

- The Add-OrchPmGroupMember cmdlet did not always function as intended.

- The Update-OrchUser cmdlet now allows unassigning all roles from users.
  - To unassign all roles from specified user:
    PS Orch1:\> Update-OrchUser <user name> -Roles ""

  - To unassign all roles from all users, robots, groups and applications:
    PS Orch1:\> Update-OrchUser * -Roles ""

  - To unassign all roles from users only (excluding robots, groups, and applications):
    PS Orch1:\> Get-OrchUser -Type DirectoryUser | select UserName | Update-OrchUser -Roles ""


# Version: 0.9.10.3
- Improved the behavior of the Add-OrchUser cmdlet.
  - Fixed an issue where the -MayHaveUserSession parameter was not functioning.

  - Added the -IsExternalLicensed parameter.

  - Previously, the following parameters were not included in the payload when not specified. This has been changed so that they are now automatically included as false when not specified. This change makes the default values more intuitive and user-friendly in the Union of privileges model, where false is a more natural default. Additionally, this change aligns the behavior with the web interface.
    - -MayHaveRobotSession
    - -MayHaveUnattendedSession
    - -MayHavePersonalWorkspace
    - -MayHaveUserSession
    - -RestrictToPersonalWorkspace
    - -IsExternalLicensed

- In line with the above changes, the -ExportCsv parameter of the Get-OrchUser cmdlet has been modified to include the IsExternalLicensed column in the output.

- Added the -IsExternalLicensed parameter to the Update-OrchUser cmdlet also.


# Version: 0.9.10.2
- HTTP logging is now available.
  - Add the following to the global settings in the configuration file. This can be overridden in each PSDrive setting.
    "Logging": {
      "Level": "Info",
      "Enabled": true
    }

  - The following values can be specified for Level:
    - Error: Outputs logs equivalent to Verbose only when the response status is not 200.
    - Info: Outputs a one-line summary for all requests and responses.
    - Trace: Outputs only request headers that contain "UIPATH" in their name, along with the request body and response body.
    - Verbose: Outputs all request headers, request bodies, response headers, and response bodies.

  - The folder where logs are output can be opened using the Open-OrchLogLocation cmdlet.

- The completer for the -InputArguments parameter of Start-OrchJob cmdlet now functions correctly.

- Fixed an issue where an incorrect message was displayed when an error occurred during library and package downloads.

- The Get-OrchPSDrive cmdlet did not output automatically configured HttpListener when HttpListener was not specified in the configuration file.


# Version: 0.9.10.1
- The Add-DuRoleToDuUser and Remove-DuRoleFromDuUser cmdlets have been added.  
  - These cmdlets are used to assign and remove roles for users in Document Understanding projects.

  - Please note that these cmdlets operate on folders (projects) in the PS drives of the UiPathOrchDu provider as their target.  

  - The PS drives of the UiPathOrchDu provider are automatically mounted when scopes starting with Du are specified in the configuration file.


# Version: 0.9.10.0
- It now works with PowerShell 7 on Linux. Its functionality was verified on Ubuntu.

- The configuration file template's newline characters were set to CRLF, but have been changed to LF.


# Version: 0.9.9.7
- The New-PSDrive cmdlet is now supported, allowing you to connect to a PSDrive for a tenant not listed in the configuration file. Use the following command:

  PS> New-PSDrive [-Name] <driveName> [-PSProvider] UiPathOrch [-Root] <tenant url> -AppId <appId> -AppSecret <appSecret> -OAuthScope "OR.Users OR.Folders"

  - Parameter names enclosed in square brackets can be omitted. Note that the New-PSDrive cmdlet has a built-in -Scope parameter, which only accepts Global, Local, or Script. Therefore, use the -OAuthScope parameter instead of -Scope to specify the Scope.

  - Additionally, parameters not specified in the New-PSDrive cmdlet will be automatically supplemented from the global settings in the configuration file, if available.

- When executing Import-Module UiPathOrch, if the configuration file does not exist, it is automatically created and opened in Notepad. However, this behavior can be inconvenient when running UiPathOrch scripts in unattended environments. To suppress this behavior, set the environment variable UIPATHORCH_SUPPRESS_CONFIG_CREATION to 1. In this case, use the New-PSDrive cmdlet to connect to a tenant.

- There were some items in the global settings of the configuration file that were not effective, such as Root and AppId. Now, all items that can be specified for a PSDrive can also be included in the global settings.


# Version: 0.9.9.6
- Comments can now be written in the configuration file.

- As a result, the explanations previously written in the `Description` field of the configuration file template have been moved to comments.

- The configuration file template has been localized. It currently supports the following seven languages:  
  - German
  - English
  - French
  - Japanese
  - Korean
  - Romanian
  - Turkish

- The configuration file must be encoded in UTF-8. To ensure this, a UTF-8 BOM (Byte Order Mark) has been added to the configuration file templates. This should help prevent character corruption when adding local characters to the configuration file.

- To obtain the configuration file template, delete (or rename for backup) the existing configuration file and then run Import-Module UiPathOrch. If no configuration file exists, it will be automatically created from the template.


# Version: 0.9.9.5
- If the Scope was too long in the configuration file, the system failed to connect to the tenant. To address this, we have added a process to automatically shorten the Scope appropriately.
  - If both OR.XXX.Read and OR.XXX.Write are present, they will be replaced with OR.XXX.

  - If OR.XXX is included, OR.XXX.Read and OR.XXX.Write will be excluded.

- Note that the shortened Scope is not automatically written back to the configuration file. To check the shortened Scope, run the following command:
  PS> Get-OrchPSDrive <drive name> | select -ExpandProperty Scope

- Additionally, Get-OrchPSDrive cmdlet outputs various drive-related information, such as BearerToken, TenantId, and PartitionGlobalId. You can retrieve this information with:
  PS> Get-OrchPSDrive | select *

  - This behavior remains unchanged from previous versions.

- If a candidate displayed by the completers contains a comma, it is now automatically enclosed in single quotes.


# Version: 0.9.9.4
- Added the -HostFeed switch parameter to the following cmdlets. When specified, it retrieves library packages from the host feed.
  - Get-OrchLibrary
  - Get-OrchLibraryVersion


# Version: 0.9.9.3
- The Get-OrchLog cmdlet now includes the -JobKey parameter, making it easier to retrieve logs for a specific job.
  - The completer for the -JobKey parameter suggests cached job Key values as candidates.

  - The -JobKey parameter also has an alias, -Key, so you can execute this cmdlet in the following way as well:

    PS Orch1:\Shared> Get-OrchJob -First 1 | Get-OrchLog

    This works because the Path and Key properties of the job entity are redirected to the parameters of Get-OrchLog with the same names.


# Version: 0.9.9.2
- Improved Get-OrchTrigger cmdlet:
  - Fixed an issue where the ExecutorRobots property of triggers could not be retrieved. This required calling an additional endpoint:
    GET /odata/ProcessSchedules/UiPath.Server.Configuration.OData.GetRobotIdsForSchedule(key={processScheduleId}) 

  - When the -ExpandDetails or -ExportCsv switch parameter is specified, the above endpoint is called along with following endpoint. The behavior of calling this endpoint remains unchanged from previous versions:
    GET /odata/ProcessSchedules({processScheduleId})

  - As part of this fix, the CSV file output by Get-OrchTrigger -ExportCsv now include an ExecutorRobots column. Additionally, the MachineRobots column is now serialized in a more appropriate format.

  - Triggers are no longer retrieved from personal workspace folders when the Orchestrator Web API version is below 12, as attempting to do so would result in an error.

- Improved New-OrchTrigger and Update-OrchTrigger cmdlets:
  - Added the -ExecutorRobots parameter, allowing better reflection of trigger dynamic allocation and account mapping information.

  - Enhanced the completers for the -ExecutorRobots and -MachineRobots parameters.
    - For New-OrchTrigger, the completers display available combinations of values, and multiple values can be specified using a comma-separated format.
    - For Update-OrchTrigger, the completers show the values configured for the specified triggers.

- Improved Copy-OrchTrigger and Copy-Item cmdlets:
  - Improved the ability to copy trigger dynamic allocation and account mapping information. However, to fully copy these, the destination folder must have the appropriate robots and sessions configured.

  - Fixed an issue where copying a trigger with a StopProcessDate in the past would fail. Now, if the date is in the past, it is removed, and the trigger is copied with Enabled set to false.

  - When copying queue triggers, the operation now fails if a queue with the same name does not exist in the destination folder.

  - The Copy-Item cmdlet now copies entities in alphabetical order by their names.

- Fixed an issue in the Copy-OrchCalendar cmdlet where all calendars were copied regardless of the value specified in the -Name parameter.

- Improved Add-OrchFolderMachine cmdlet:
  - When the target folder is a personal workspace folder, the cmdlet now skips processing. For example, when adding a machine to all folders directly under the root, unnecessary errors are no longer output:

    PS Orch1:\> Add-OrchFolderMachine -Path * MyMachine

- Improved Get-ChildItem cmdlet (alias: dir):
  - When the Orchestrator API version is outdated and does not return the FolderType property, this cmdlet now sets this property to either Personal or Standard (simulated). This is useful when writing .ps1 scripts to process folders.

- Addressed an issue where inappropriate warnings about OAuth scopes were output when connecting to Orchestrator without OAuth support using a username and password.

- Added three cmdlets for searching the directory service. Each cmdlet corresponds to a different endpoint:
  - Search-OrchDirectory cmdlet
    Endpoint: GET /api/DirectoryService/SearchForUsersAndGroups

  - Search-OrchPmDirectory cmdlet
    Endpoint: GET /api/Directory/Search/{partitionGlobalId}

  - Resolve-OrchPmDirectoryNameBulk cmdlet
    Endpoint: POST /api/Directory/BulkResolveByName/{partitionGlobalId}


# Version: 0.9.9.1
- The Get-OrchPmGroupMember cmdlet has been added.
  - This cmdlet outputs the same content as the existing Get-OrchPmGroup -ExpandMembers.

  - As part of this change, the -ExpandMembers parameter has been removed from the Get-OrchPmGroup cmdlet, and the functionality to expand members has been migrated to the Get-OrchPmGroupMember cmdlet.

  - This update enables more intuitive operations, as follows:
    - The output of Get-OrchPmGroup -ExportCsv can be imported into the New-OrchPmGroup cmdlet.
    - The output of Get-OrchPmGroupMember -ExportCsv can be imported into the Add-OrchPmGroupMember cmdlet.

- The Export-OrchPackage cmdlet has been improved.
  - When a relative path for the -Destination parameter is specified, the path was previously resolved using the process's current working directory, which could lead to unintended behavior.

  - The relative path is now correctly resolved using the current location of the PSDrive.

  - For example, the following command previously failed to write the packages to the correct location. This issue has now been resolved. The first wildcard represents the package name, and the second wildcard represents the version number:

    PS Orch1:\> Export-OrchPackage -Recurse * * c:

  - If the -Destination parameter is not specified, the packages are exported to the current location of the last-used FileSystem drive. This behavior remains unchanged from previous versions.

- The Import-OrchLibrary and Import-OrchPackage cmdlets have also been improved.
  - These cmdlets previously did not function correctly when a relative path was specified for the -Source parameter.


# Version: 0.9.9.0
- Several cmdlets with the verb Add have been renamed to use the verb New. When these cmdlets were initially created, the verb Add was chosen to align with the terminology used in the Orchestrator web interface, as it was considered more intuitive at the time. However, as the number of cmdlets increased, it was determined that New and Add should be used appropriately based on their functionality. We apologize for any inconvenience caused by this change.

- List of cmdlets renamed to use the verb New:
  - Add-OrchMachine -> New-OrchMachine
  - Add-OrchBucket -> New-OrchBucket
  - Add-OrchProcess -> New-OrchProcess
  - Add-OrchQueue -> New-OrchQueue
  - Add-OrchTrigger -> New-OrchTrigger
  - Add-OrchPmUserBulk -> New-OrchPmUserBulk
  - Add-OrchPmGroup -> New-OrchPmGroup

List of cmdlets where the verb was not changed:
  - Add-OrchUser 
  - Add-OrchCalendarDate
  - Add-OrchMachineClientSecret
  - Add-OrchFolderMachine
  - Add-OrchFolderUser
  - Add-OrchRoleToFolderUser
  - Add-OrchAssetLink
  - Add-OrchPmGroupMember
  - Add-OrchPmLicenseToPmLicensedGroup
  - Start-OrchJob

- The above changes are only renaming the cmdlets, and their functionality remains completely unchanged. If you wish to use the cmdlets with their old names, you can set aliases. Aliases can be configured as follows:

  PS> Set-Alias Add-OrchProcess New-OrchProcess

- To configure the alias automatically when the PowerShell console starts, you can add the above command to your profile script. To edit the profile script, execute the following in the PowerShell console:

  PS> notepad $profile


# Version: 0.9.8.24
- When an SSL error, such as the absence of an installed certificate, occurs, the process can now continue by ignoring the error. To ignore SSL errors, add the following to the PSDrive in the configuration file:
  "IgnoreSslErrors": true

- The Add-OrchFolderUser cmdlet now displays an appropriate message if it fails to retrieve roles.

- The Copy-Item and Copy-OrchFolderMachine cmdlets now display an appropriate message if a machine with the same name is already assigned to the destination folder.

- Some operations failed when the target Orchestrator version was outdated. These issues have been resolved, and the cmdlets now handle operations appropriately based on the API version. The correspondence between cmdlets and API versions is as follows:

  - Add-OrchQueue:
    - ApiVer <  16: Does not set RetentionAction and RetentionPeriod, and Calls POST /odata/QueueDefinitions
    - ApiVer >= 16: Sets RetentionAction and RetentionPeriod, and Calls POST /odata/QueueDefinitions/UiPath.Server.Configuration.OData.CreateQueue

  - Get-OrchProcess -ExpandDetails, Update-OrchProcess, Copy-OrchProcess, Copy-Item:
    - ApiVer <  17: Does not call GET /odata/ReleaseRetention({releaseId})
    - ApiVer >= 17: Calls GET /odata/ReleaseRetention({releaseId})

  - Add-OrchProcess, Copy-Item:
    - ApiVer <  17: Calls POST /odata/Releases
    - ApiVer >= 17: Calls POST /odata/Releases/UiPath.Server.Configuration.OData.CreateRelease

  - Get-OrchTestCase, Get-OrchTestSet, Get-OrchTestSetSchedule, Get-OrchTestSetExecution:
    - ApiVer <  18: No action is taken.
    - ApiVer >= 18: Retrieve entities.


# Version: 0.9.8.23
- Added the Redo-OrchQueueItem cmdlet, which retries transaction items by specifying the queue name and the item IDs. It retries only retryable items, defined as those with a Status of Failed and a Revision of either None or InReview.

  - To retry specified items in the queue, use the following command. The -Id parameter, which specifies item IDs, supports auto-completion:

    PS Orch1:\Shared> Redo-OrchQueueItem YourQueueName <item IDs>

  - To retry all failed items in a queue:

    PS Orch1:\Shared> Get-OrchQueueItem YourQueueName -Status Failed -Revision None,InReview | Redo-OrchQueueItem -Verbose

  - Please note that the Get-OrchQueueItem cmdlet can retrieve up to a maximum of 1000 items at a time. If there are more than 1000 retryable items, repeatedly execute following command until the -Verbose parameter no longer outputs anything:

    PS Orch1:\Shared> Get-OrchQueueItem YourQueueName -Status Failed -Revision None,InReview | Redo-OrchQueueItem -Verbose

  - To retry all retryable queue items across all queues in the tenant:

    PS Orch1:\> Get-OrchQueueItem -Recurse * -Status Failed -Revision None,InReview | Redo-OrchQueueItem

- Fixed an issue where the -UserName parameter completer for the Add-OrchPmGroupMember cmdlet was not functioning correctly.


# Version: 0.9.8.22
- In version 0.9.8.16, the parameter name of the Add-OrchProcess cmdlet was changed from -PackageId to -Id. However, the column name in the CSV file output by Get-OrchProcess -ExportCsv was not updated accordingly. As a result, this CSV file could not be imported using the Add-OrchProcess cmdlet.

- When the Get-OrchProcess cmdlet tried to deserialize ReleaseDto from the server, it failed if the ResourceOverwrite property had a value because its type was not documented in the Swagger doc. Based on the JSON returned by Automation Cloud, I defined the ResourceOverwrite type correctly to resolve the error. To account for other Orchestrator versions returning differently defined ResourceOverwrite, deserialization failures now set the value to null instead of throwing an exception. This may require future adjustments.

- A warning is now displayed if the required scope specifications for UiPathOrch are not listed in the configuration file.

- The authentication process can now be canceled with Ctrl+C.


# Version: 0.9.8.21
- The member names included in UpdateInfoDto were incorrect. As a result, information from UpdateInfo was not properly included in the output of cmdlets such as Get-OrchUserSession.

- When importing a CSV using the Add-OrchFolderUser cmdlet, if multiple roles were specified in the Roles column as comma-separated values, a warning stating "No matching role found" was incorrectly displayed, even though the processing completed successfully.


# Version: 0.9.8.20
- Under certain conditions, the Copy-Item cmdlet was failing. This issue has been present since UiPathOrch 0.9.8.11.

- Fixed an issue where the Add-OrchTrigger and Update-OrchTrigger cmdlets could not add multiple account-machine mappings. These mappings can be specified using the -MachineRobots parameter. The valid format for this parameter can be confirmed using auto-completion.

- The -MachineRobots parameter of the Add-OrchTrigger and Update-OrchTrigger cmdlets now supports wildcards for the internal RobotName, MachineName, and HostMachineName fields. While auto-completion is not available for these internal values, using wildcards allows you to perform the intended operation even if the exact RobotName or MachineName is unknown. For example, to add all combinations of RobotName, MachineName, and HostMachineName available in the current folder as account-machine mappings to <your triggers>, use the following command:

  PS Orch1:\Shared> Update-OrchTrigger <your trigger names> -MachineRobots '[{"RobotName":"*","MachineName":"*","HostMachineName":"*"}]'

- The completer for the -UserName parameter in the following functions stopped working in version 0.9.8.16:
  - Enable-OrchUserAttended
  - Enable-OrchPersonalWorkspace
  - Disable-OrchUserAttended
  - Disable-OrchPersonalWorkspace

  Note: Functions refer to cmdlets implemented in .ps1 files. The implementations of the above functions are located in the Functions directory under the UiPathOrch installation directory.

- Changed the output format of the Get-OrchActivitySetting cmdlet from table view to list view. Additionally, the members of SignalR are now expanded and displayed.


# Version: 0.9.8.19
- Added the following cmdlets:
  - Get-OrchFolderMachineAccountMapping
  - Enable-OrchFolderMachineAccountMapping
  - Disable-OrchFolderMachineAccountMapping

- Regarding the Enable and Disable cmdlets mentioned above:
  - The first positional parameter -Name specifies the machine name.
  - The second positional parameter -UserName specifies the account name.
  - These parameters support wildcards. To specify exact names, you can use auto-completion.
  - Additionally, like other cmdlets, the -WhatIf, -Verbose, and -Confirm switch parameters are supported.
  - For example, to remove all account mappings for all machines assigned to all folders, you can use the following command:
    PS Orch1:\> Disable-OrchFolderMachineAccountMapping -Recurse * *

- Added the -Scope parameter to the Add-OrchMachine cmdlet. In addition to Machine Templates, it is now possible to add Standard and Serverless machines.

- Fixed an issue where the header and values in the CSV file output by Get-OrchMachine -ExportCsv were misaligned. Additionally, the following columns are now included in the output. This CSV file can be imported using Add-OrchMachine and Update-OrchMachine cmdlets:
  - Scope
  - RobotUsers
  - UpdatePolicyType
  - UpdatePolicyVersion
  - MaintenanceCron
  - MaintenanceDuration
  - MaintenanceEnabled
  - MaintenanceTimezoneId


# Version: 0.9.8.18
- Added the following parameter to the Add-OrchMachine cmdlet:
  - -RobotUsers 

- Added the following parameters to the Update-OrchMachine cmdlet:
  - -RobotUsers 
  - -AutomationType
  - -TargetFramework


# Version: 0.9.8.17
- The Add-OrchUser cmdlet was unable to add robot accounts with the -UR_Password parameter.

- The -SourceRecurse switch parameter of the Import-OrchPackage cmdlet has been renamed to -Recurse to make it easier for users to find this parameter.

- The -WarnNoMatch parameter in the following cmdlets has been renamed to -NoMatchWarning. This change was made to reduce the risk of user errors, such as mistakenly specifying -WarnNoMatch instead of -WhatIf.
  - Remove-OrchUser
  - Remove-OrchFolderUser
  - Remove-OrchPmUser
  - Remove-OrchPmGroupMember
  - Remove-OrchPmAllocationFromPmLicensedGroup


# Version: 0.9.8.16
- The following cmdlets for managing users now support the -Type parameter:
  - Get-OrchUser
  - Copy-OrchUser
  - Remove-OrchUser
  - Remove-OrchRoleFromUser
  - Add-OrchRoleToFolderUser
  - Remove-OrchRoleFromFolderUser

- Fixed an issue where the completers for the Add-OrchProcess cmdlet were not functioning properly.

- Renamed the -JobId parameter in Get-OrchJob to -Id.

- Renamed the -PackageId parameter in Add-OrchProcess to -Id.

- Resolved an issue in the Add-OrchPmGroupMember cmdlet where the auto-completion for the -UserName parameter included local group names. Since local groups cannot be added to groups, local group names are no longer displayed as suggestions.

- Fixed an issue where the -UserName parameter in the Set-OrchPmRobotAccount cmdlet was unintentionally hidden.


# Version: 0.9.8.15
- When executing cmdlets with parameters instructing the retrieval of detailed information (e.g., Get-OrchProcess -ExpandDetails) across many folders (by specifying parameters such as -Recurse or -Path), an error stating "non-concurrent collections must have exclusive access" rarely occurred.

- When specifying the -Recurse switch parameter for any cmdlets, the output previously mixed personal workspace folders and other folders. This behavior has been corrected so that all personal workspace folders are output first, followed by other folders.

  - Please note that personal workspaces that have been started exploring via the Orchestrator web interface have been operable from earlier versions of UiPathOrch. However, since starting to explore personal workspaces is not supported via API, this operation must be performed manually through the web interface.

  - To reflect personal workspace explorations started on the web in a PowerShell session, please execute the Clear-OrchCache cmdlet to clear the folder cache.


# Version: 0.9.8.14
- The version number is now displayed on the screen after logging in with the browser using OAuth non-confidential settings.


# Version: 0.9.8.13
- The Add-OrchPmGroupMember cmdlet has been improved.
  - When the tenant is integrated with Active Directory, it is now possible to add AD groups to local groups.

  - When querying names in the directory, the queries are now performed in bulk whenever possible.


# Version: 0.9.8.12
- The ResumeVersion property of JobDto was incorrectly defined as a string, which caused an error when retrieving resumed jobs using the Get-OrchJob cmdlet. This has been fixed by changing the ResumeVersion property to int.

- Fixed an issue where the source property of users was not included in the output when executing Get-OrchPmGroup -ExpandMembers.

- Enhanced the processing speed of Get-OrchPmGroup -ExpandMembers.

- Added the following query parameters to the Get-OrchJob cmdlet:
  - -StartTimeAfter
  - -StartTimeBefore
  - -EndTimeAfter
  - -EndTimeBefore
  - -ResumeTimeAfter
  - -ResumeTimeBefore

- Updated the output of the Get-OrchUser cmdlet to include the following properties:
  - ExplicitMayHaveRobotSession
  - ExplicitMayHaveUserSession
  - ExplicitMayHavePersonalWorkspace
  - ExplicitRestrictToPersonalWorkspace

- Fixed an issue where the Directory of triggers did not display correctly immediately after triggers were created using the Add-OrchTrigger cmdlet.


# Version: 0.9.8.11
- When RedirectUrl was set at the root level of the configuration file, connections to confidential app settings could not be established.

- Previously, each entity used a distinct cache implementation, despite the code being nearly identical. To streamline maintenance, the common code has now been extracted and unified. This change improves code maintainability and slightly reduces the module's footprint. Additionally, several minor issues were fixed in the process.

- In the former implementation of Get-OrchLog, logs sorted by TimeStamp could retrieve over 10,000 rows by generating an OData query based on the TimeStamp value of the last row fetched. However, this approach sometimes caused log rows on page boundaries to be missed. Therefore, the automatic page splitting by TimeStamp values has been removed. This change is expected to improve functionality, though as a trade-off, it is no longer possible to retrieve over 10,000 log rows at once. If needed, please adjust the query parameters manually and execute the Get-OrchLog cmdlet in segments. Then, by running the Get-OrchLog cmdlet without parameters, the cached log rows can be combined and output.

- The Get-OrchQueueItem cmdlet has been enhanced.
  - In Automation Cloud, the number of queue items that can be retrieved at once is limited to 100. Previously, specifying -First 100 was required when executing the Get-OrchQueueItem cmdlet to obtain the expected results. This limitation has been removed by automatically paginating in increments of 100 rows.

  - The DateTime values returned by the Get-OrchQueueItem cmdlet (such as DueDate, DeferDate, etc.) are now output in the local time zone. Previously, the default view displayed DateTime values in the local time zone, but they appeared in UTC format when redirected via a pipeline.

  - Several query parameters (such as -DueDateAfter and -DueDateBefore) have been added.

  - The Get-OrchQueueItem cmdlet now displays a progress bar.


# Version: 0.9.8.10
- For cmdlets that copy folder entities (such as Copy-OrchAsset), the process has been modified so that even if reading entities from a folder fails, the operation continues with subsequent folders without interruption.

- The -Recurse switch parameter has been added to the following cmdlets, allowing -Recurse to be used with all cmdlets that copy folder entities:
  - Copy-OrchBucket
  - Copy-OrchProcess
  - Copy-OrchQueue
  - Copy-OrchTestDataQueue
  - Copy-OrchTestSet
  - Copy-OrchTestSetSchedule

- When the source and destination folders have an identical subfolder structure, specifying -Recurse on folder entity copy cmdlets will automatically copy each folder's entities to the corresponding destination folder. For example, to copy all storage buckets from one tenant to another with an identical folder structure, use the following command:
  PS Src:\> Copy-OrchBucket -Recurse * Dst:\

- For the Copy-OrchPmUser cmdlet, when the source user's email is empty, this will now set the destination user's username to the source user's username. Previously, the source user's email was always set as the destination user's username, even if it was empty.


# Version: 0.9.8.9
- When retrieving logs from multiple folders with the Get-OrchLog cmdlet and specifying the -First parameter, it was not handled correctly (the internal counter was not reset). As a result, logs could not be retrieved from the second folder onward.

- The -JobId parameter in the Get-OrchLog cmdlet was not functioning and has been removed.

- The Copy-OrchTrigger, Copy-OrchApiTrigger, and Copy-OrchTestSet cmdlets have been updated to change the method for searching robots in the destination tenant.  
  Previously, the following endpoint was used to search for robots in the destination tenant:
    - GET /odata/Robots/UiPath.Server.Configuration.OData.GetConfiguredRobots?$expand=User
  This has been modified to use the following endpoint for robot searches:
    - GET /odata/Robots/UiPath.Server.Configuration.OData.GetRobotsFromFolder(folderId={folderId})


# Version: 0.9.8.8
- The Move-OrchFolderUser cmdlet has been added. This cmdlet moves folder users within a tenant's folders.

- The Copy-OrchPmUser cmdlet has been added. This cmdlet copies organizational users between organizations. If the groups to which the users belong do not exist in the destination organization, they are automatically created.

- The parameter for cmdlets handling PmUser has been changed from -UserName to -Email. It was more appropriate to use -Email for uniquely identifying PmUser rather than -UserName. Note that the -Email parameter has an alias -UserName. The modified cmdlets are as follows:
  - Get-OrchPmUser
  - Update-OrchPmUser
  - Remove-OrchPmUser


# Version: 0.9.8.7
- When migrating from MSI Orchestrator to Automation Cloud, user identifiers often change from usernames to email addresses. As a result, user migration did not always function as expected. To address this, if a user is not found when searched by username in the destination tenant's directory, the directory is now also searched using the user's email address. This update ensures that the Copy-OrchUser, Copy-Item, Copy-OrchFolderUser, and Copy-OrchAsset cmdlets work as expected when migrating users from MSI Orchestrator to Automation Cloud.

- The following nodes have been added to the root of UiPathOrchConfig.json:
  - Description
  - RedirectUrl
  - HttpListener
  - Scope
  - Enabled

  The values set in these root nodes are shared across all PSDrives as child nodes. These values can be overridden (cascaded) by defining nodes with the same name within each PSDrive's node. This allows for a much more concise configuration file, especially when dealing with many tenants within the same organization. The RedirectUrl setting, in particular, can likely be shared among multiple PSDrive configurations.

- In line with the above changes, the template for UiPathOrchConfig.json has been updated.

- Values cascaded to each PSDrive can now be verified using the Get-OrchPSDrive cmdlet as follows:
  PS> Get-OrchPSDrive | select *

- The Update-OrchTrigger cmdlet has been updated so that passing an empty string to the -MachineRobots parameter now removes MachineRobots.


# Version: 0.9.8.6
- Added the Move-OrchPmGroupMember cmdlet. This cmdlet moves users between organizational groups within a tenant.


# Version: 0.9.8.5
- The Disable-OrchTrigger cmdlet had stopped working in version 0.9.7.14. This issue was introduced during the process of sharing implementation with the Enable-OrchTrigger cmdlet. We apologize for any inconvenience caused by this problem.

- These cmdlets have been renamed
  - From:
    - Add-OrchPmMemberToPmGroup
    - Remove-OrchPmMemberFromPmGroup
  - To:
    - Add-OrchPmGroupMember
    - Remove-OrchPmGroupMember 
  We apologize for any inconvenience caused by this change.


# Version: 0.9.8.4
- Configuration File-Related Updates:
  - The HttpListener in UiPathOrchConfig.json is now automatically configured from RedirectUrl. The value of HttpListener will be the same as RedirectUrl with a trailing slash added. This allows the configuration file to work without specifying HttpListener. However, if the port number specified in RedirectUrl is 1024 or below, you still need to include HttpListener in the configuration file as before.

  - Changed the value of RedirectUrl in the template file of UiPathOrchConfig.json to "http://localhost:8085/Temporary_Listen_Addresses" (in other words, changed the port from 80 to 8085). Also, removed HttpListener from this file. If the configuration file does not exist when Import-Module UiPathOrch is executed, a configuration file will be automatically created from this template.

- Role-Related Updates:
  - Added the -ExportCsv parameter to the Get-OrchRole cmdlet. Like other cmdlets, you can specify a folder or file path for exporting the CSV. If a folder path is specified, it will output with the default file name "ExportedRoles.csv". This CSV file can be imported using the Set-OrchRole cmdlet.

  - Added the Set-OrchRole cmdlet. If a role with the name specified by the -Name parameter (Name column in the CSV) already exists, it will be updated. If it does not exist, a new role with that name will be created.

- Other:
  - The execution result of Get-OrchRole -ExpandPermission was incorrect. I apologize for the inconvenience.


# Version: 0.9.8.3
- The Get-OrchPmAuditLog cmdlet has been added. Similar to Get-OrchJob and Get-OrchLog cmdlets, running it without specifying filter parameters will output the entire cache. Please note that the only filter parameters available for this cmdlet are -Skip and -First.

- The -ExportCsv parameter has been added to the Get-OrchFolderMachine cmdlet. Machines inherited from parent folders will not be included in this CSV file. This CSV file can be imported using the Add-OrchFolderMachine cmdlet. Please be aware that if you are importing it into a different tenant, you need to modify the drive name embedded in the Path column of the CSV file.

- The -PropagateToSubFolders parameter of the Add-OrchFolderMachine cmdlet was not functioning. When the parameter was added in an earlier version, the implementation was forgotten. My apologies for this oversight.


# Version: 0.9.8.2
- The behavior of the Get-OrchAuditLog cmdlet has been improved.  

  - This cmdlet can now call the /odata/AuditLogs/UiPath.Server.Configuration.OData.GetAuditLogDetails(auditLogId={auditLogId}) endpoint. When the -ExpandDetails switch parameter is specified, the output from this endpoint is cached in the Details member of each audit log entry. Additionally, JSON text stored in Details.CustomData is automatically expanded into Details.CustomDataExpanded.

  - When no filter parameters are specified, cached content is now output. Previously retrieved logs can be obtained quickly. However, even in this case, if the -ExpandDetails switch parameter is specified, an API call is made for log entries whose Details are not cached.

  - The Entities.CustomData returned from the API contains JSON text, which is now automatically expanded into Entities.CustomDataExpanded. This content can be easily checked by redirecting the output to the ConvertTo-Json cmdlet.  

  - The output is now sorted in descending order by ExecutionTime with odata query, making the -Skip and -First parameters easier to use.


# Version: 0.9.8.1
- The configuration file property name Proxy.Address has been changed to Proxy.Url for consistency. Apologies for any inconvenience.


# Version: 0.9.8.0
- Support for proxy connections has been added. To configure the proxy, place the following section at the beginning of the UiPathOrchConfig.json file, directly under the root (i.e., parallel to "PSDrives"). This "Proxy" section can also be defined inside each PSDrive, where it will override the root-level "Proxy" settings.

{
  "Proxy": {
    "Address": "http://proxy.example.com:8080",
    "BypassProxyOnLocal": true,
    "UseDefaultCredentials": false,
    "Credentials": {
      "Username": "PROXY_USERNAME",
      "Password": "PROXY_PASSWORD"
    },
    "Enabled": true
  },

  "PSDrives": [
    ...

- For example, to enable the proxy for all PSDrives but disable it for a specific PSDrive, you can include the following in the configuration of that PSDrive:

      "Proxy": {
        "Enabled": false
      }


# Version: 0.9.7.18
- The CSV import process in the Update-OrchUser cmdlet was not handled correctly.

- The -UserName parameter of the Update-OrchPmUser cmdlet did not support CSV imports.


# Version: 0.9.7.17
- Removed the -Email parameter from the Update-OrchPmUser cmdlet. This is because changing the email is not supported by the API, and it would not yield the expected results.

- Changed the endpoint called by the Get-OrchPmUser cmdlet for Automation Cloud:
  - Before: GET /api/User/users/{partitionGlobalId} 
  - After: GET /portal_/api/identity/UserPartition/licenses?partitionGlobalId={partitionGlobalId} 
  - When the target is MSI Orchestrator, the cmdlet will continue to call the following endpoint: GET /api/UserPartition/users/{partitionGlobalId} 

- Added the following Content-Type header to all HTTP GET requests. Generally, this header is not required when performing GET requests to Orchestrator Web API endpoints, but the endpoint called by the above Get-OrchPmUser cmdlet returned an error without it:
  Content-Type: application/json; charset=utf-8

- If the Orchestrator API version is below 16, the processing of the following cmdlets is now skipped. This change also helps prevent unnecessary errors when copying folders in older versions of Orchestrator:
  - Get-OrchApiTrigger
  - Get-OrchTestCase
  - Get-OrchTestCaseExecution
  - Get-OrchTestSet
  - Get-OrchTestSetExecution
  - Get-OrchTestDataQueue


# Version: 0.9.7.16
- The Update-OrchProcessVersion cmdlet now includes the -Id parameter. You can specify the target process using -Id instead of -Name. This allows the cmdlet to be executed without the need for the OR.Execution.Read scope in OAuth (however, OR.Execution.Write is still required).

- The exception handling in the Get-OrchProcess cmdlet was incomplete.


# Version: 0.9.7.15
- The Copy-Item and Copy-OrchFolderUser cmdlets were not copying robots and applications assigned to folders.

- The completer for the -UserName parameter in the Add-OrchFolderUser cmdlet was not functioning under certain conditions.

- After updating PmGroup and PmRobotAccount, the directory cache was not automatically cleared.

- Get-OrchUser -ExportCsv now outputs the UR_CredentialExternalName column.

- Added the -UR_CredentialExternalName parameter to the Add-OrchUser and Update-OrchUser cmdlets.

- The CSV file generated by the following command was inappropriate:
PS Orch1:\> Get-OrchRole -ExpandPermission | Export-Csv <filepath>


# Version: 0.9.7.14
- The Copy-OrchUser cmdlet now searches for a user with the same name as the source user in the destination tenant, and if found, sets that user's identifier.

- The Add-OrchUser cmdlet had recently stopped being able to add robots, but this has been fixed.


# Version: 0.9.7.13
- The Enable-OrchFolderMachineInherit and Disable-OrchFolderMachineInherit cmdlets have been added. These cmdlets replicate the functionality of the "Propagate machine to subfolders" menu in the Orchestrator web interface.

- The Copy-Item and Copy-OrchFolderMachine cmdlets now correctly copy the PropagateToSubFolders property of folder machines. Additionally, these cmdlets no longer copy folder machines where the IsAssigned property is set to False.

- There was an issue where the Copy-OrchMachine cmdlet failed to copy the Account-Machine Mappings of tenant machines under certain conditions. Additionally, if a robot with the same name is not found in the destination tenant, the copy process will now continue with a warning, whereas previously, the operation would fail.


# Version: 0.9.7.12
- The Get-DuRole and Get-DuUser cmdlets have been added. Please note that these cmdlets work on drives of the UiPathOrchDu provider.

- The Get-OrchPSDrive cmdlet now outputs drive information for UiPathOrchDu and UiPathOrchTm providers in addition to the UiPathOrch provider.


# Version: 0.9.7.11
- The Get-OrchMachineClientSecretId cmdlet has been added. By combining it with the Remove-OrchMachineClientSecret cmdlet, it is now easy to bulk delete client secrets issued before a specified date and time. Here's how:
PS Orch1:\> Get-OrchMachineClientSecretId | ? CreationTime -LT '2024/10/01' | Remove-OrchMachineClientSecret

- The Clear-OrchCache cmdlet was not clearing the cache for the Get-OrchLog cmdlet.


# Version: 0.9.7.10
- When outputting a CSV file using -ExportCsv, if -CsvEncoding is specified as UTF, a BOM (Byte Order Mark) is now added to the beginning of the file. This ensures that the CSV file can be opened in Excel without garbled characters in a Japanese environment, even if -CsvEncoding sjis is not specified.

- The Add-OrchMachineSecretKey cmdlet has been renamed to Add-OrchMachineClientSecret.

- The Remove-OrchMachineClientSecret cmdlet has been added.

- The Add-OrchUser and Copy-OrchUser cmdlets now output a warning when no matching roles are found in the destination tenant.

- When copying users with the Copy-OrchUser cmdlet, if the original user has folder roles assigned, those roles are now excluded before copying.

- The Add-OrchUser and Add-OrchFolderUser cmdlets could not add users if their UserName contained characters such as + or _.

- The Copy-OrchTrigger cmdlet now continues copying even if a robot with the same name is not found in the destination tenant, after outputting an error. Previously, the copy would fail if a robot with the same name was not found.


# Version: 0.9.7.9
- The Add-OrchMachineSecretKey cmdlet has been added.

- The Add-OrchFolderUser cmdlet now outputs a warning when no roles matching the wildcard specified in -Roles are found.

- The Copy-OrchRole cmdlet did not copy roles where IsStatic was set to True.

- In recent versions, some cmdlets that manage tenant users had stopped working under certain conditions.

- The Get-OrchUser cmdlet now includes the -ExportCsv parameter. It should export all parameters, including the Execution Settings of Unattended Robots. This CSV file can be imported using the Add-OrchUser and Update-OrchUser cmdlets.

- The Add-OrchUser and Update-OrchUser cmdlets now have many additional parameters. You should now be able to specify all the parameters that can be set through the web interface, including the Execution Settings of Unattended Robots.

- The processing speed of the Get-OrchUser cmdlet has been improved when the -ExpandDetail or -ExportCsv switch parameters are specified. Additionally, a progress bar is now displayed.

- The date and time data included in the entities output by the Get-OrchJob, Get-OrchLog, and Get-OrchAuditLog cmdlets are now output in the local timezone. Previously, they were displayed in the local timezone only when output to the console using the default view, but were output in UTC when piped to other cmdlets.


# Version: 0.9.7.8
- The name of the Add-OrchPmLicenseToPmLicensedGroup cmdlet has been changed to Add-OrchPmLicenseToPmGroup.

- The completer for the Add-OrchPmLicenseToPmGroup cmdlet was not functioning as expected. Additionally, it was not possible to assign licenses to groups that had not yet been assigned any licenses.

- The Remove-OrchPmLicensedGroup cmdlet has been added.


# Version: 0.9.7.7
- Updated the progress bars in the Copy-OrchLibrary and Copy-OrchPackage cmdlets to display overall progress.
- Added the -ProcessType filter parameter to the Get-OrchJob cmdlet.
- Removed the Get-OrchJobVideo cmdlet and replaced it with a function of the same name. The new function achieves the same functionality by calling the Get-OrchJob cmdlet, reducing code duplication and minimizing the module's footprint.


# Version: 0.9.7.6
- The Copy-OrchMachine cmdlet now outputs the LicenseKey and ClientSecret of the machine created in the destination tenant.


# Version: 0.9.7.5
- Added the -Recurse parameter to Copy-OrchPackage. When the feed folders have the same name in both the source and destination tenants, the following command will copy all packages from both the tenant feed and folder feeds. The first asterisk represents the package name, and the second asterisk represents the version number:
PS Src:\> Copy-OrchPackage * * Dst:\ -Recurse

- The Get-OrchProcess cmdlet was not retrieving retention info.

- The Import-OrchPackage cmdlet did not support CSV import.

- In addition to update-related cmdlets, the Get-OrchJob and Get-OrchLog cmdlets now also accept parameters from CSV imports. By specifying the -Path parameter via CSV, you can limit the target folders for processing. Going forward, I plan to modify other Get cmdlets to also accept parameters from CSV imports.


# Version: 0.9.7.4
- Bucket operations enhancements:
  - Added the -ExportCsv parameter to the Get-OrchBucket cmdlet.
  - Added the Add-OrchBucket cmdlet, which allows importing CSV files generated by the Get-OrchBucket cmdlet.

- Machine operations enhancements:
  - The Get-OrchMachine cmdlet now exports Tags along with other machine information.
  - Added the -Tags parameter to the Add-OrchMachine cmdlet for tags assignment during machine creation.

- Process operations enhancements:
  - Added the -ExpandDetails switch parameter in the Get-OrchProcess cmdlet for detailed information retrieval.
  - Fixed an issue where the Get-OrchProcess cmdlet previously output incomplete information when exporting to a CSV file.

- Format Change for the -Tags Parameter:
  The format of the -Tags parameter has changed from JSON format to simple comma-separated text in the following cmdlets:
  - Get-OrchBucket / Add-OrchBucket
  - Get-OrchProcess / Add-OrchProcess
  - Get-OrchQueue / Add-OrchQueue


# Version: 0.9.7.3
- Added the Get-OrchPmLicensedUser cmdlet.

- Added the -ExcludeEntities switch parameter to the Copy-Item cmdlet. When specified, only folders are copied, and folder entities (such as processes, assets, triggers, etc.) contained within them are not copied.

- When copying the root folder using Copy-Item with the -Recurse parameter, the sorting order of the folders at the destination became incorrect.

- The output of the Get-OrchJob cmdlet was incomplete.

- The Path property in the output of the Copy-OrchPmRobotAccount cmdlet was not set correctly.


# Version: 0.9.7.2
- Added the Update-OrchMachine cmdlet.


# Version: 0.9.7.1
- The Remove-OrchRoleFromUser cmdlet had stopped working in the recent version.

- Part of the output of the Get-OrchLicenseStats cmdlet was incorrect.


# Version: 0.9.7.0
- Renamed the following cmdlets from:
  - Get-OrchPmUserLicenseGroup
  - Remove-OrchPmAllocationFromPmUserLicenseGroup
to:
  - Get-OrchPmLicensedGroup
  - Remove-OrchPmAllocationFromPmLicensedGroup

- Added the following cmdlets:
  - Update-OrchPmUser
  - Add-OrchPmLicenseToPmLicensedGroup
  - Remove-OrchPmLicenseFromPmLicensedGroup

- Improvements:
  - Refactored the implementation of the Add-OrchPmMemberToPmGroup cmdlet.
  - Refactored the implementation of the Remove-OrchPmMemberFromPmGroup cmdlet and made the -Type parameter mandatory.

- Note on Get-OrchPmUser cmdlet:
  - For MSI Orchestrator, the Get-OrchPmUser cmdlet now calls the following endpoint (please note that this endpoint is deprecated):
  GET /api/UserPartition/users/{partitionGlobalId}
  - For Cloud Orchestrator, it continues to call the same endpoint as before:
  GET /api/User/users/{partitionGlobalId}


# Version: 0.9.6.23
- The Get-OrchLog cmdlet was unable to properly handle more than 10,000 log entries when the -OrderBy parameter was specified.

- When the -ExportCsv parameter was specified for the Get-OrchPmGroup cmdlet, the column name in the output .csv file was incorrect.


# Version: 0.9.6.22
- Like with other cmdlets, item names in the output of the Get-Item and Get-ChildItem cmdlets can now be displayed using auto-completion.

  ```powershell
  PS Orch1:\> dir | select FullName,FeedType,[press ctrl+space]
  ```

  Note: dir is an alias for Get-ChildItem, and select is an alias for Select-Object.

- In the UiPathOrchDu and UiPathOrchTm providers, the FullName property has been added to DuProject and TmProject. By specifying this in the -Path parameter, you can easily write PowerShell scripts to perform actions on all projects.

  ```powershell
  dir Orch1Du:\ | % {
      # $_ contains the project entity
      $doctype = Get-DuDocumentType -Path $_.FullName

      # Additional complex processing...
  }
  ```

  Note: % is an alias for ForEach-Object cmdlet.

- The Find-OrchFolderNoUserAssigned.ps1 script, which was located in the Examples folder, has been promoted to a function. This function lists all folders that have no users assigned to them. To output the paths of the relevant folders concisely, use the following command:

  ```powershell
  PS> Find-OrchFolderNoUserAssigned Orch1:\ -IncludeInherited | select -ExpandProperty FullName
  ```

- The following cmdlets previously failed when a package with the same name already existed in the destination. They have been modified to skip the API call when a package with the same name is found. This significantly improves processing speed when many packages with the same name already exist:
  - Copy-OrchLibrary
  - Copy-OrchPackage
  - Import-OrchLibrary
  - Import-OrchPackage

- The -ExpandDetails switch parameter has been added to the Get-OrchTrigger cmdlet. This instructs it to call GET /odata/ProcessSchedules({processScheduleId}).


# Version: 0.9.6.21
- The Get-OrchJob, Get-OrchLog, and Get-OrchQueueItem cmdlets now support specifying sorting fields and sorting order. You can specify these using the -OrderBy and -OrderAscending parameters.

- Some parts of the output of the Get-OrchQueueItem cmdlet were not being processed correctly.


# Version: 0.9.6.20
- Added the -ExpandDetails switch parameter to the Get-OrchUser cmdlet. This allows retrieving detailed user information, such as the UpdatePolicy property.

- Copy-OrchUser was not properly copying certain properties, such as the UpdatePolicy property.

- Added the Update-OrchUser cmdlet, which allows updating any user information. For example, to enable attended automation for a user, use the following command:
  PS> Update-OrchUser <username> -MayHaveRobotSession True

- To enable unattended automation for a user, use the following command:
  PS> Update-OrchUser <username> -MayHaveUnattendedSession True -UR_UserName <windows domain\user> -UR_Password <windows password>

- When executing the Enable-OrchUserAttended and Disable-OrchUserAttended cmdlets, some user properties (such as the UpdatePolicy property) were being lost.

- Deprecated the Enable-OrchUserAttended and Disable-OrchUserAttended cmdlets and replaced them with functions of the same names. These functions achieve the same functionality by calling the Update-OrchUser cmdlet, which reduces code duplication and minimizes the module's footprint.

- Similarly, added the Enable-OrchPersonalWorkspace and Disable-OrchPersonalWorkspace functions for enabling or disabling a user's personal workspace. These functions also call the Update-OrchUser cmdlet to achieve their functionality.

- The Remove-OrchPersonalWorkspace cmdlet now automatically disables the target personal workspace folder before removing it. This fix prevents the personal workspace from being recreated immediately after removal.

- When removing a personal workspace using Remove-Item, the target personal workspace folder is now automatically disabled before removal, just as with Remove-OrchPersonalWorkspace.


# Version: 0.9.6.19
- The Copy-OrchFolderUser cmdlet was unable to process directory users.


# Version: 0.9.6.18
- The endpoint called by the Get-OrchPmUser cmdlet has been changed from GET /api/UserPartition/users/{partitionGlobalId} to GET /api/User/users/{partitionGlobalId}.
- The Add-OrchPmUserBulk cmdlet has been added. This cmdlet wraps the Platform Management API endpoint POST /api/User/BulkCreate. It supports importing CSV files to create users in bulk. If there are rows in the CSV that can be aggregated, they will be processed in a single API call.
- The format of the CSV that can be imported is as follows. The only required parameter is Email:
  Path,Email,Name,SurName,DisplayName,Type,BypassBasicAuthRestriction,InvitationAccepted,GroupName
- To specify multiple group names in the GroupName field of a CSV file, enclose the comma-separated group names in double quotes. If you edit the GroupName field in Excel, Excel will automatically add the double quotes.
- For Type, please specify one of the following options. This list can also be viewed using auto-completion on the command line:
  user, robot, directoryUser, directoryGroup, robotAccount, application


# Version: 0.9.6.17
- There was an issue with the cache in the Get-OrchLog cmdlet, where the same log entry was being cached multiple times. Now, log entries that have already been added to the cache are no longer added again.


# Version: 0.9.6.16
- Added the FullName attribute to folder entities retrieved by the dir command. This makes it easier to write scripts that enumerate folders and execute cmdlets by specifying the FullName in the -Path parameter.
- The following script lists all folders in the specified tenant where no users are assigned:

dir Orch1:\ -Recurse | ForEach-Object {
    if (-not (Get-OrchFolderUser -Path $_.FullName)) {
        $_.FullName
    }
}

- The script Find-FoldersNoUserAssigned.ps1 has been added to the Examples folder.


# Version: 0.9.6.15
- When executing the Get-OrchLog cmdlet, an error occurred due to API limitations if the filter specified by the parameters matched more than 10,000 log entries. To address this, the API calls are now automatically divided to retrieve more than 10,000 log entries.
- Please note that the Get-OrchLog cmdlet caches the retrieved log entries. When executed without specifying filter parameters, it will output all cached log entries. This helps avoid repeatedly querying Orchestrator with the same filter, thereby reducing API usage and minimizing the load on Orchestrator.
- To avoid exceeding API rate limits, the Add-OrchFolderUser cmdlet now waits 600 milliseconds between API calls.


# Version: 0.9.6.14
- It is now possible to connect to an Orchestrator built on Azure App Service. Since the Identity Server URL will be different in this case, please specify it in the IdentityUrl key within the UiPathOrchConfig.json. For the other keys in the JSON file, use the same settings as for OAuth confidential or non-confidential configurations.
- The Get-OrchLog cmdlet now displays a progress bar.


# Version: 0.9.6.13
- The processing order of folders when the -Recurse switch parameter was specified was unnatural. It has been changed to process in a more natural order.
- Added the Remove-OrchCalendarDate cmdlet.
- Added the -WarnOnNoMatch switch parameter to the following cmdlets. This will output a warning if the specified user is not found:
  - Remove-OrchFolderUser
  - Remove-OrchPmUser
  - Remove-OrchPmAllocationFromPmUserLicenseGroup
- Removed the -WarnOnNoMatch switch parameter from the following cmdlets. Since these cmdlets do not support wildcards for usernames, it is more natural to output a warning by default if the specified user is not found:
  - Add-OrchFolderUser
  - Add-OrchPmMemberToPmGroup


# Version: 0.9.6.12
- When executing Import-Csv | Add-OrchUser, if the same user appears multiple times in the .csv file, those entries are now consolidated and processed with a single API call to add the user.
- The following cmdlets have had the -Roles parameter given an alias -TenantRoles:
  - Add-OrchUser
  - Remove-RoleFromUser
- The following cmdlets have had the -Roles parameter given an alias -FolderRoles:
  - Add-OrchFolderUser
  - Add-OrchRoleToFolderUser
  - RemoveRoleFromFolderUser
- These new aliases for the parameters allow you to consolidate your .csv files for user management.


# Version: 0.9.6.11
- Some parameter names in the Get-OrchJob and Get-OrchLog cmdlets were incorrect.
- When executing the Get-OrchLog cmdlet without specifying any filter parameters, the contents of the cache are now output.
- The Get-OrchJob cmdlet now displays a progress bar.


# Version: 0.9.6.10
- The Copy-OrchCalendar cmdlet stopped working in version 0.9.6.9. We sincerely apologize for this issue.


# Version: 0.9.6.9
- Added the Add-OrchCalendarDate cmdlet. You can specify multiple calendars using wildcards and commas. If a non-existent calendar name is specified, a new calendar will be created with that name. Multiple dates can be added at once. You can also import dates from a CSV file. To add dates up to yesterday, specify the -AllowPastDates switch parameter.
- Added the -ExportCsv parameter to the Get-OrchCalendar cmdlet. This CSV can be imported using the Add-OrchCalendarDate cmdlet.
- Renamed the Id parameter in the Get-OrchLog cmdlet to -JobId and made this parameter non-mandatory. Additionally, added the -Recurse parameter and several filter parameters. These changes make the cmdlet easier to use.
- Modified the Get-OrchJob and Get-OrchQueueItem cmdlets to process in a single thread to avoid API rate limits, with a 600-millisecond wait between each API call.
- Fixed an issue where the Add-OrchPmMemberToPmGroup cmdlet did not work under certain conditions.
- Updated Set-OrchAsset to prevent the creation of new assets with names containing wildcard characters. If you want to create an asset with a name containing wildcards, escape the name with a backtick.
- Added the -WarnOnNoMatch switch parameter to the following cmdlets. It outputs a warning if the specified user does not exist:
  - Add-OrchUser
  - Remove-OrchUser
  - Add-OrchPmMemberToPmGroup
  - Remove-OrchPmMemberFromPmGroup


# Version: 0.9.6.8
- The Get-OrchPmGroup cmdlet stopped working in version 0.9.6.7. We sincerely apologize for this issue.
- The -ExpandGroup parameter of the Get-OrchPmRobotAccount cmdlet was not functioning correctly.
- Cmdlets that accept the -ExportCsv parameter now escape Path and Name of entities containing wildcard characters when exporting to CSV.


# Version: 0.9.6.7


# Version: 0.9.6.6


# Version: 0.9.6.5


# Version: 0.9.6.4


# Version: 0.9.6.3


# Version: 0.9.6.2


# Version: 0.9.6.1


# Version: 0.9.6.0


# Version: 0.9.5.16


# Version: 0.9.5.15


# Version: 0.9.5.14


# Version: 0.9.5.13


# Version: 0.9.5.12


# Version: 0.9.5.11


# Version: 0.9.5.10


# Version: 0.9.5.9


# Version: 0.9.5.8


# Version: 0.9.5.7


# Version: 0.9.5.6


# Version: 0.9.5.5


# Version: 0.9.5.4


# Version: 0.9.5.3


# Version: 0.9.5.2


# Version: 0.9.5.1


# Version: 0.9.5.0


# Version: 0.9.4.0


# Version: 0.9.3.1


# Version: 0.9.3.0


# Version: 0.9.2.2


# Version: 0.9.2.1


# Version: 0.9.2.0


# Version: 0.9.1.1


# Version: 0.9.1.0


# Version: 0.9.0.1


# Version: 0.9.0.0


# Version: 0.8.11.1


# Version: 0.8.11.0


# Version: 0.8.10.13


# Version: 0.8.10.12


# Version: 0.8.10.11


# Version: 0.8.10.10


# Version: 0.8.10.9


# Version: 0.8.10.8


# Version: 0.8.10.7


# Version: 0.8.10.6


# Version: 0.8.10.5


# Version: 0.8.10.4


# Version: 0.8.10.3


# Version: 0.8.10.2


# Version: 0.8.10.1


# Version: 0.8.10.0


# Version: 0.8.9.8


# Version: 0.8.9.7


# Version: 0.8.9.6


# Version: 0.8.9.5


# Version: 0.8.9.4


# Version: 0.8.9.3


# Version: 0.8.9.2


# Version: 0.8.9.1


# Version: 0.8.9.0


# Version: 0.8.8.3


# Version: 0.8.8.2


# Version: 0.8.8.1


# Version: 0.8.8.0


# Version: 0.8.7.5


# Version: 0.8.7.4


# Version: 0.8.7.3


# Version: 0.8.7.2


# Version: 0.8.7.1


# Version: 0.8.7.0


# Version: 0.8.6.8


# Version: 0.8.6.7


# Version: 0.8.6.6


# Version: 0.8.6.5


# Version: 0.8.6.4


# Version: 0.8.6.3


# Version: 0.8.6.2


# Version: 0.8.6.1


# Version: 0.8.6.0


# Version: 0.8.5.5


# Version: 0.8.5.4


# Version: 0.8.5.3


# Version: 0.8.5.2


# Version: 0.8.5.1


# Version: 0.8.5.0


# Version: 0.8.4.0


# Version: 0.8.3.7


# Version: 0.8.3.6


# Version: 0.8.3.5


# Version: 0.8.3.4


# Version: 0.8.3.3


# Version: 0.8.3.2


# Version: 0.8.3.1


# Version: 0.8.3.0


# Version: 0.8.2.0


# Version: 0.8.1.5


# Version: 0.8.1.4


# Version: 0.8.1.3


# Version: 0.8.1.2


# Version: 0.8.1.1


# Version: 0.8.1.0


# Version: 0.8.0.10


# Version: 0.8.0.9


# Version: 0.8.0.8


# Version: 0.8.0.7


# Version: 0.8.0.6


# Version: 0.8.0.5


# Version: 0.8.0.4


# Version: 0.8.0.3


# Version: 0.8.0.2


# Version: 0.8.0.1


# Version: 0.8.0.0


