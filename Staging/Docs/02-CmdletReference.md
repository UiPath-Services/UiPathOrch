# UiPathOrch Module - Cmdlet Reference (239)

1. [Utility & Configuration (10)](#1-utility--configuration-10-cmdlets)
2. [User Management (19)](#2-user-management-19-cmdlets)
3. [Role Management (7)](#3-role-management-7-cmdlets)
4. [Folder Management (16)](#4-folder-management-16-cmdlets)
5. [Package & Library Management (12)](#5-package--library-management-12-cmdlets)
6. [Process Management (9)](#6-process-management-9-cmdlets)
7. [Job Management (9)](#7-job-management-9-cmdlets)
8. [Trigger Management (16)](#8-trigger-management-16-cmdlets)
9. [Asset Management (10)](#9-asset-management-10-cmdlets)
10. [Queue Management (10)](#10-queue-management-10-cmdlets)
11. [Storage Bucket Management (8)](#11-storage-bucket-management-8-cmdlets)
12. [Machine Management (9)](#12-machine-management-9-cmdlets)
13. [Calendar & Webhook (10)](#13-calendar--webhook-10-cmdlets)
14. [Action Catalog (3)](#14-action-catalog-3-cmdlets)
15. [Testing (21)](#15-testing-21-cmdlets)
16. [License Management (6)](#16-license-management-6-cmdlets)
17. [Monitoring & Logs (5)](#17-monitoring--logs-5-cmdlets)
18. [Settings (9)](#18-settings-9-cmdlets)
19. [PM - Users (5)](#19-platform-management---users-5-cmdlets)
20. [PM - Groups (8)](#20-platform-management---groups-8-cmdlets)
21. [PM - Robot Accounts (4)](#21-platform-management---robot-accounts-4-cmdlets)
22. [PM - Licenses (6)](#22-platform-management---licenses-6-cmdlets)
23. [PM - External Apps & Other (8)](#23-platform-management---external-apps--other-8-cmdlets)
24. [Document Understanding (7)](#24-document-understanding-7-cmdlets)
25. [Test Manager (11)](#25-test-manager-11-cmdlets)
26. [Bulk Copy Shortcut](#bulk-copy-shortcut)

This document provides a systematically categorized reference of all 239
cmdlets in the UiPathOrch module. Use Get-Help <CmdletName> -Examples for
detailed usage of each cmdlet.

API Prefixes:
  Orch  = Orchestrator OData API (189 cmdlets)
  Pm    = Platform Management / Identity Server API (32 cmdlets)
  Du    = Document Understanding API (7 cmdlets)
  Tm    = Test Manager API (11 cmdlets)

## 1. Utility & Configuration (10 cmdlets)

Module setup, connection management, and troubleshooting.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchHelp | Show module documentation |
| Get-OrchPSDrive | List connected drives and their scopes |
| Get-OrchConfigPath | Show configuration file path |
| Edit-OrchConfig | Open configuration file in editor |
| Clear-OrchCache | Clear cached data (force refresh) |
| Set-OrchLocation | Set the current folder location |
| Get-OrchCurrentUser | Get current authenticated user info |
| Get-OrchRobot | Get robots (legacy, replaced by Machine) |
| Get-OrchClassicEnvironment | Get classic environments (legacy) |
| Get-OrchClassicRobot | Get classic robots (legacy) |

## 2. User Management (19 cmdlets)

Tenant-level user accounts, authentication, sessions, and personal workspaces.

### Users

| Cmdlet | Description |
|--------|-------------|
| Get-OrchUser | List tenant users |
| Add-OrchUser | Add a user to the tenant |
| Copy-OrchUser | Copy users to another tenant |
| Update-OrchUser | Update user properties |
| Remove-OrchUser | Remove a user from the tenant |

### User Status & Sessions

| Cmdlet | Description |
|--------|-------------|
| Enable-OrchUserAttended | Enable attended mode for a user |
| Disable-OrchUserAttended | Disable attended mode for a user |
| Get-OrchUserPrivilege | Get effective permissions of a user |
| Get-OrchUserSession | Get active user sessions |
| Get-OrchUnattendedSession | Get unattended robot sessions |
| Update-OrchCurrentUserURPassword | Change current user's UR password |

### Personal Workspaces

| Cmdlet | Description |
|--------|-------------|
| Get-OrchPersonalWorkspace | List personal workspaces |
| Enable-OrchPersonalWorkspace | Enable a user's personal workspace |
| Disable-OrchPersonalWorkspace | Disable a user's personal workspace |
| Remove-OrchPersonalWorkspace | Remove a personal workspace |

### Directory & User Mapping

| Cmdlet | Description |
|--------|-------------|
| Search-OrchDirectory | Search the organization directory |
| New-OrchUserMappingCsv | Generate user mapping CSV for migration |
| Test-OrchUserMappingCsv | Validate a user mapping CSV |

## 3. Role Management (7 cmdlets)

Tenant-level roles and permission assignment.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchRole | List roles (use -ExpandPermission for details) |
| Copy-OrchRole | Copy roles to another tenant |
| Set-OrchRole | Update role permissions |
| Remove-OrchRole | Remove a role |
| Add-OrchRoleToFolderUser | Add a role to a folder user assignment |
| Remove-OrchRoleFromFolderUser | Remove a role from a folder user assignment |
| Remove-OrchRoleFromUser | Remove a tenant role from a user |

## 4. Folder Management (16 cmdlets)

Folder structure, user assignments, and machine assignments.
Folders are navigated using cd/dir (Set-Location/Get-ChildItem).

### Folder User Assignments

| Cmdlet | Description |
|--------|-------------|
| Get-OrchFolderUser | List folder user assignments |
| Add-OrchFolderUser | Assign a user to a folder with roles |
| Copy-OrchFolderUser | Copy folder user assignments to another tenant |
| Move-OrchFolderUser | Move a user assignment to another folder |
| Remove-OrchFolderUser | Remove a user from a folder |
| Find-OrchFolderNoUserAssigned | Find folders with no users assigned |

### Folder Machine Assignments

| Cmdlet | Description |
|--------|-------------|
| Get-OrchFolderMachine | List folder machine assignments |
| Add-OrchFolderMachine | Assign a machine to a folder |
| Copy-OrchFolderMachine | Copy folder machine assignments |
| Remove-OrchFolderMachine | Remove a machine from a folder |

### Folder Machine Settings

| Cmdlet | Description |
|--------|-------------|
| Get-OrchFolderMachineAccountMapping | Get machine-account mappings |
| Enable-OrchFolderMachineAccountMapping | Enable a machine-account mapping |
| Disable-OrchFolderMachineAccountMapping | Disable a machine-account mapping |
| Enable-OrchFolderMachineInherit | Enable machine inheritance from parent |
| Disable-OrchFolderMachineInherit | Disable machine inheritance from parent |
| Get-OrchFolderUsage | Get folder resource usage statistics |

## 5. Package & Library Management (12 cmdlets)

NuGet packages (automation projects) and libraries (shared components).

### Packages (Automation Projects)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchPackage | List packages |
| Get-OrchPackageVersion | List package versions |
| Copy-OrchPackage | Copy packages to another tenant/folder |
| Import-OrchPackage | Upload a .nupkg file |
| Export-OrchPackage | Download a package as .nupkg file |
| Remove-OrchPackage | Remove a package |

### Libraries (Shared Components)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchLibrary | List libraries |
| Get-OrchLibraryVersion | List library versions |
| Copy-OrchLibrary | Copy libraries to another tenant |
| Import-OrchLibrary | Upload a library .nupkg file |
| Export-OrchLibrary | Download a library as .nupkg file |
| Remove-OrchLibrary | Remove a library |

## 6. Process Management (9 cmdlets)

Processes (package deployments to folders).

| Cmdlet | Description |
|--------|-------------|
| Get-OrchProcess | List processes in a folder |
| New-OrchProcess | Create a process from a package |
| Copy-OrchProcess | Copy processes to another tenant/folder |
| Edit-OrchProcess | Edit process properties interactively |
| Update-OrchProcess | Update process properties |
| Update-OrchProcessVersion | Update a process to a specific package version |
| Reset-OrchProcessVersion | Reset a process to the latest package version |
| Get-OrchProcessRequirement | Get process runtime requirements |
| Remove-OrchProcess | Remove a process from a folder |

## 7. Job Management (9 cmdlets)

Job execution, monitoring, and execution media (video recordings).

### Jobs

| Cmdlet | Description |
|--------|-------------|
| Get-OrchJob | List jobs (supports filtering) |
| Start-OrchJob | Start a job |
| Stop-OrchJob | Stop/kill a running job |
| Open-OrchJob | Open a job in the browser |
| Get-OrchJobStats | Get job execution statistics |

### Execution Media (Video Recordings)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchJobMedia | List job execution media |
| Export-OrchJobMedia | Download execution media files |
| Remove-OrchJobMedia | Remove execution media |
| Get-OrchJobVideo | Get job video recording URL |

## 8. Trigger Management (16 cmdlets)

Time triggers, API triggers, and event triggers.

### Time Triggers (Schedule-based)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchTrigger | List time triggers |
| New-OrchTrigger | Create a time trigger |
| Copy-OrchTrigger | Copy triggers to another tenant/folder |
| Update-OrchTrigger | Update trigger properties |
| Enable-OrchTrigger | Enable a disabled trigger |
| Disable-OrchTrigger | Disable a trigger |
| Remove-OrchTrigger | Remove a trigger |

### API Triggers (HTTP-based)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchApiTrigger | List API triggers |
| Copy-OrchApiTrigger | Copy API triggers |
| Enable-OrchApiTrigger | Enable an API trigger |
| Disable-OrchApiTrigger | Disable an API trigger |
| Remove-OrchApiTrigger | Remove an API trigger |

### Event Triggers (Queue/event-based)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchEventTrigger | List event triggers |
| Enable-OrchEventTrigger | Enable an event trigger |
| Disable-OrchEventTrigger | Disable an event trigger |
| Remove-OrchEventTrigger | Remove an event trigger |

## 9. Asset Management (10 cmdlets)

Assets (key-value configuration), credential stores, and asset links.

### Assets

| Cmdlet | Description |
|--------|-------------|
| Get-OrchAsset | List assets |
| Copy-OrchAsset | Copy assets to another tenant/folder |
| Set-OrchAsset | Update asset values |
| Set-OrchCredentialAsset | Update credential asset values |
| Remove-OrchAsset | Remove an asset |

### Credential Stores

| Cmdlet | Description |
|--------|-------------|
| Get-OrchCredentialStore | List credential stores |
| Copy-OrchCredentialStore | Copy credential stores |
| Remove-OrchCredentialStore | Remove a credential store |

### Asset Links (Multi-folder Asset Sharing)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchAssetLink | List asset links |
| Add-OrchAssetLink | Create an asset link to share across folders |

## 10. Queue Management (10 cmdlets)

Queues and queue items for work distribution.

### Queues

| Cmdlet | Description |
|--------|-------------|
| Get-OrchQueue | List queues |
| New-OrchQueue | Create a queue |
| Copy-OrchQueue | Copy queues to another tenant/folder |
| Update-OrchQueue | Update queue properties |
| Remove-OrchQueue | Remove a queue |

### Queue Items

| Cmdlet | Description |
|--------|-------------|
| Get-OrchQueueItem | List queue items (supports filtering) |
| Copy-OrchQueueItem | Copy queue items to another tenant/folder |
| Import-OrchQueueItem | Bulk import queue items from CSV/JSON |
| Redo-OrchQueueItem | Retry a failed queue item |
| Remove-OrchQueueItem | Remove a queue item |

## 11. Storage Bucket Management (8 cmdlets)

Cloud/local storage buckets and their contents.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchBucket | List storage buckets |
| New-OrchBucket | Create a storage bucket |
| Copy-OrchBucket | Copy buckets to another tenant/folder |
| Remove-OrchBucket | Remove a storage bucket |
| Get-OrchBucketItem | List files in a storage bucket |
| Import-OrchBucketItem | Upload a file to a storage bucket |
| Export-OrchBucketItem | Download a file from a storage bucket |
| Remove-OrchBucketItem | Remove a file from a storage bucket |

## 12. Machine Management (9 cmdlets)

Machine templates, sessions, and client secrets.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchMachine | List machine templates |
| New-OrchMachine | Create a machine template |
| Copy-OrchMachine | Copy machines to another tenant |
| Update-OrchMachine | Update machine properties |
| Remove-OrchMachine | Remove a machine template |
| Get-OrchMachineSession | Get active machine sessions |
| Get-OrchMachineClientSecretId | List machine client secret IDs |
| Add-OrchMachineClientSecret | Add a client secret to a machine |
| Remove-OrchMachineClientSecret | Remove a client secret from a machine |

## 13. Calendar & Webhook (10 cmdlets)

### Calendars (for Trigger Scheduling)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchCalendar | List calendars |
| Copy-OrchCalendar | Copy calendars to another tenant |
| Remove-OrchCalendar | Remove a calendar |
| Add-OrchCalendarDate | Add dates to a calendar |
| Remove-OrchCalendarDate | Remove dates from a calendar |

### Webhooks

| Cmdlet | Description |
|--------|-------------|
| Get-OrchWebhook | List webhooks |
| Copy-OrchWebhook | Copy webhooks to another tenant |
| Enable-OrchWebhook | Enable a disabled webhook |
| Disable-OrchWebhook | Disable a webhook |
| Remove-OrchWebhook | Remove a webhook |

## 14. Action Catalog (3 cmdlets)

Action catalogs for long-running workflow actions.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchActionCatalog | List action catalogs |
| Copy-OrchActionCatalog | Copy action catalogs |
| Remove-OrchActionCatalog | Remove an action catalog |

## 15. Testing (21 cmdlets)

Test automation management (Orchestrator-side).

### Test Cases

| Cmdlet | Description |
|--------|-------------|
| Get-OrchTestCase | List test cases |
| Get-OrchTestCaseAssertion | Get test case assertions |
| Get-OrchTestCaseExecution | Get test case execution results |
| Remove-OrchTestCase | Remove a test case |

### Test Sets

| Cmdlet | Description |
|--------|-------------|
| Get-OrchTestSet | List test sets |
| Copy-OrchTestSet | Copy test sets to another tenant/folder |
| Start-OrchTestSet | Execute a test set |
| Get-OrchTestSetExecution | Get test set execution results |
| Stop-OrchTestSetExecution | Stop a running test set execution |
| Remove-OrchTestSet | Remove a test set |

### Test Set Schedules

| Cmdlet | Description |
|--------|-------------|
| Get-OrchTestSetSchedule | List test set schedules |
| Copy-OrchTestSetSchedule | Copy test set schedules |
| Enable-OrchTestSetSchedule | Enable a test set schedule |
| Disable-OrchTestSetSchedule | Disable a test set schedule |
| Remove-OrchTestSetSchedule | Remove a test set schedule |

### Test Data Queues

| Cmdlet | Description |
|--------|-------------|
| Get-OrchTestDataQueue | List test data queues |
| Copy-OrchTestDataQueue | Copy test data queues |
| Get-OrchTestDataQueueItem | Get items in a test data queue |
| Get-OrchTestDataQueueItemTable | Get test data queue items as a table |
| Reset-OrchTestDataQueueItem | Reset consumed test data queue items |
| Remove-OrchTestDataQueue | Remove a test data queue |

## 16. License Management (6 cmdlets)

License allocation and usage statistics.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchLicense | Get tenant license information |
| Get-OrchLicenseNamedUser | List named user license allocations |
| Get-OrchLicenseRuntime | List runtime license allocations |
| Enable-OrchLicenseRuntime | Enable a runtime license |
| Disable-OrchLicenseRuntime | Disable a runtime license |
| Get-OrchLicenseStats | Get license usage statistics |

## 17. Monitoring & Logs (5 cmdlets)

Logs, audit trails, and alerts.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchLog | Get robot execution logs (supports filtering) |
| Get-OrchLogLocation | Get log storage location info |
| Open-OrchLogLocation | Open log storage in browser/explorer |
| Get-OrchAuditLog | Get audit log entries (supports filtering) |
| Get-OrchAlert | Get system alerts |

## 18. Settings (9 cmdlets)

Tenant and system configuration settings.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchSetting | Get general tenant settings |
| Get-OrchWebSetting | Get web application settings |
| Get-OrchExecutionSetting | Get execution settings |
| Get-OrchActivitySetting | Get activity restriction settings |
| Get-OrchAuthenticationSetting | Get authentication settings |
| Get-OrchUpdateSetting | Get update server settings |
| Get-OrchConnectionString | Get connection string information |
| Enable-OrchMaintenanceMode | Enable maintenance mode |
| Disable-OrchMaintenanceMode | Disable maintenance mode |

## 19. Platform Management - Users (5 cmdlets)

Organization-level user management (Identity Server API).

| Cmdlet | Description |
|--------|-------------|
| Get-PmUser | List organization users |
| New-PmUser | Create a local user in the organization |
| Copy-PmUser | Copy users to another organization |
| Update-PmUser | Update user properties |
| Remove-PmUser | Remove a user from the organization |

## 20. Platform Management - Groups (8 cmdlets)

Organization-level group management.

| Cmdlet | Description |
|--------|-------------|
| Get-PmGroup | List groups |
| New-PmGroup | Create a group |
| Copy-PmGroup | Copy groups to another organization |
| Remove-PmGroup | Remove a group |
| Get-PmGroupMember | List group members |
| Add-PmGroupMember | Add a member to a group |
| Move-PmGroupMember | Move a member between groups |
| Remove-PmGroupMember | Remove a member from a group |

## 21. Platform Management - Robot Accounts (4 cmdlets)

Organization-level robot account management.

| Cmdlet | Description |
|--------|-------------|
| Get-PmRobotAccount | List robot accounts |
| Copy-PmRobotAccount | Copy robot accounts to another organization |
| Set-PmRobotAccount | Update robot account properties |
| Remove-PmRobotAccount | Remove a robot account |

## 22. Platform Management - Licenses (6 cmdlets)

Organization-level license group management.

| Cmdlet | Description |
|--------|-------------|
| Get-PmLicensedGroup | List licensed groups |
| Get-PmLicensedUser | List licensed users |
| Add-PmLicenseToPmLicensedGroup | Add a license to a licensed group |
| Remove-PmLicenseFromPmLicensedGroup | Remove a license from a licensed group |
| Remove-PmAllocationFromPmLicensedGroup | Remove allocation from a licensed group |
| Remove-PmLicensedGroup | Remove a licensed group |

## 23. Platform Management - External Apps & Other (8 cmdlets)

External applications, directory search, audit, and settings.

### External Applications

| Cmdlet | Description |
|--------|-------------|
| Get-PmExternalApplication | List external applications |
| Get-PmExternalApiResource | List available API resources/scopes |
| Copy-PmExternalApplication | Copy external applications |
| Remove-PmExternalApplication | Remove an external application |

### Directory

| Cmdlet | Description |
|--------|-------------|
| Search-PmDirectory | Search the organization directory |
| Resolve-PmDirectoryNameBulk | Bulk resolve directory usernames |

### Audit & Settings

| Cmdlet | Description |
|--------|-------------|
| Get-PmAuditLog | Get organization-level audit logs |
| Get-PmAuthenticationSetting | Get authentication settings |
| Get-PmAccessAllowedMember | Get access-allowed members |

## 24. Document Understanding (7 cmdlets)

Document Understanding project and user management (DU API).

| Cmdlet | Description |
|--------|-------------|
| Get-DuClassifier | List DU classifiers |
| Get-DuDocumentType | List DU document types |
| Get-DuExtractor | List DU extractors |
| Get-DuRole | List DU roles |
| Get-DuUser | List DU users |
| Add-DuUser | Add a user to a DU project |
| Remove-DuRoleFromDuUser | Remove a role from a DU user |

## 25. Test Manager (11 cmdlets)

Test Manager project management (TM API, separate from Orchestrator testing).

| Cmdlet | Description |
|--------|-------------|
| Get-TmServerInfo | Get Test Manager server information |
| Get-TmConfiguration | Get Test Manager configuration |
| Get-TmProjectSetting | Get project settings |
| Get-TmProjectPermission | Get project permissions |
| Get-TmRequirement | List requirements |
| Remove-TmRequirement | Remove a requirement |
| Get-TmTestCase | List Test Manager test cases |
| Remove-TmTestCase | Remove a Test Manager test case |
| Get-TmTestSet | List Test Manager test sets |
| Remove-TmTestSet | Remove a Test Manager test set |
| Get-TmTestExecution | Get test execution results |

## Bulk Copy Shortcut

The copy command (Copy-Item alias) copies all tenant and folder entities
at once in the correct dependency order:

  copy -Recurse Source:\ Destination:\

For cross-organization migration with username mapping:

  copy -Recurse Source:\ Destination:\ -UserMappingCsv mapping.csv

See Get-Help Copy-Item for details on -WhatIf, -ExcludeEntities, and
other parameters specific to UiPathOrch drives.

Version: 1.0 | Last Updated: March 2026 | Total Cmdlets: 239
