---
title: Cmdlets by Topic
nav_order: 4
permalink: /cmdlets-by-topic/
---

# UiPathOrch Module - Cmdlet Reference

1. [Utility and Configuration](#1-utility-and-configuration)
2. [User Management](#2-user-management)
3. [Role Management](#3-role-management)
4. [Folder Management](#4-folder-management)
5. [Package and Library Management](#5-package-and-library-management)
6. [Process Management](#6-process-management)
7. [Job Management](#7-job-management)
8. [Trigger Management](#8-trigger-management)
9. [Asset Management](#9-asset-management)
10. [Queue Management](#10-queue-management)
11. [Storage Bucket Management](#11-storage-bucket-management)
12. [Machine Management](#12-machine-management)
13. [Calendar and Webhook](#13-calendar-and-webhook)
14. [Action Center](#14-action-center)
15. [Testing](#15-testing)
16. [License Management](#16-license-management)
17. [Monitoring and Logs](#17-monitoring-and-logs)
18. [Settings](#18-settings)
19. [PM - Users](#19-platform-management-users)
20. [PM - Groups](#20-platform-management-groups)
21. [PM - Robot Accounts](#21-platform-management-robot-accounts)
22. [PM - Licenses](#22-platform-management-licenses)
23. [PM - External Apps & Other](#23-platform-management-external-apps-and-other)
24. [Document Understanding](#24-document-understanding)
25. [Test Manager](#25-test-manager)
26. [Bulk Copy Shortcut](#bulk-copy-shortcut)

This document provides a systematically categorized reference of all
cmdlets in the UiPathOrch module. Use Get-Help <CmdletName> -Examples
for detailed usage of each cmdlet.

API Prefixes:
  Orch  = Orchestrator OData API
  Pm    = Platform Management / Identity Server API
  Du    = Document Understanding API
  Tm    = Test Manager API

<a id="1-utility-and-configuration"></a>
## 1. Utility & Configuration

Module setup, connection management, and troubleshooting.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchHelp | Show module documentation |
| Import-OrchConfig | Import the configuration file and create PSDrives |
| Get-OrchPSDrive | List connected drives and their scopes |
| New-OrchPSDrive | Manually create a PSDrive |
| Get-OrchConfigPath | Show configuration file path |
| Edit-OrchConfig | Open configuration file in editor |
| Clear-OrchCache | Clear cached data (force refresh) |
| Set-OrchLocation | Set the current folder location |
| Get-OrchCurrentUser | Get current authenticated user info |
| Switch-OrchCurrentUser | Re-authenticate as a different user on a drive |
| Get-OrchProductVersion | Get the Orchestrator product version per drive |
| Resolve-OrchAuthError | Diagnose a sign-in failure from the browser URL |
| Invoke-OrchApi | Call an Orchestrator / Identity / Portal API endpoint directly |
| Get-OrchRobot | Get robots (legacy, replaced by Machine) |
| Get-OrchClassicEnvironment | Get classic environments (legacy) |
| Get-OrchClassicRobot | Get classic robots (legacy) |

## 2. User Management

Tenant-level user accounts, authentication, sessions, and personal workspaces.

### Users

| Cmdlet | Description |
|--------|-------------|
| Get-OrchUser | List tenant users |
| Get-OrchUserDetail | Get per-user detailed information |
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
| Clear-OrchInactiveSession | Bulk-delete Disconnected / Unresponsive unattended sessions |
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

## 3. Role Management

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

## 4. Folder Management

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

<a id="5-package-and-library-management"></a>
## 5. Package & Library Management

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

## 6. Process Management

Processes (package deployments to folders).

| Cmdlet | Description |
|--------|-------------|
| Get-OrchProcess | List processes in a folder |
| Get-OrchProcessDetail | Get per-process detailed information |
| New-OrchProcess | Create a process from a package |
| Copy-OrchProcess | Copy processes to another tenant/folder |
| Edit-OrchProcess | Edit process properties interactively |
| Update-OrchProcess | Update process properties |
| Update-OrchProcessVersion | Update a process to a specific package version |
| Reset-OrchProcessVersion | Reset a process to the latest package version |
| Get-OrchProcessRequirement | Get process runtime requirements |
| Remove-OrchProcess | Remove a process from a folder |

## 7. Job Management

Job execution, monitoring, and execution media (video recordings).

### Jobs

| Cmdlet | Description |
|--------|-------------|
| Get-OrchJob | List jobs (supports filtering) |
| Start-OrchJob | Start a job |
| Stop-OrchJob | Stop/kill a running job |
| Restart-OrchJob | Restart a Faulted job |
| Resume-OrchJob | Resume a Suspended job |
| Open-OrchJob | Open a job in the browser |
| Get-OrchJobStats | Get job execution statistics |

### Execution Media (Video Recordings)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchJobMedia | List job execution media |
| Export-OrchJobMedia | Download execution media files |
| Remove-OrchJobMedia | Remove execution media |
| Get-OrchJobVideo | Get job video recording URL |

## 8. Trigger Management

Time triggers, API triggers, and event triggers.

### Time Triggers (Schedule-based)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchTrigger | List time triggers |
| Get-OrchTriggerDetail | Get per-trigger detailed information |
| New-OrchTrigger | Create a time trigger |
| Copy-OrchTrigger | Copy triggers to another tenant/folder |
| Update-OrchTrigger | Update trigger properties |
| Enable-OrchTrigger | Enable a disabled trigger |
| Disable-OrchTrigger | Disable a trigger |
| Test-OrchTrigger | Validate a trigger's process schedule against the server |
| Remove-OrchTrigger | Remove a trigger |

### API Triggers (HTTP-based)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchApiTrigger | List API triggers |
| New-OrchApiTrigger | Create an API trigger |
| Update-OrchApiTrigger | Update API trigger properties |
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

## 9. Asset Management

Assets (key-value configuration), credential stores, and asset links.

### Assets

| Cmdlet | Description |
|--------|-------------|
| Get-OrchAsset | List assets |
| Copy-OrchAsset | Copy assets to another tenant/folder |
| Set-OrchAsset | Update asset values |
| Remove-OrchAsset | Remove an asset |
| Remove-OrchAssetUserValue | Remove per-robot (UserValue) entries from assets |

### Credential Assets

| Cmdlet | Description |
|--------|-------------|
| Get-OrchCredentialAsset | Get credential-type assets |
| Set-OrchCredentialAsset | Create or update credential assets |

### Secret Assets

| Cmdlet | Description |
|--------|-------------|
| Get-OrchSecretAsset | Get secret-type assets |
| Set-OrchSecretAsset | Create or update secret assets |

### Credential Stores

| Cmdlet | Description |
|--------|-------------|
| Get-OrchCredentialStore | List credential stores |
| Copy-OrchCredentialStore | Copy credential stores |
| Update-OrchCredentialStore | Update credential store properties |
| Remove-OrchCredentialStore | Remove a credential store |

### Asset Links (Multi-folder Asset Sharing)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchAssetLink | List asset links |
| Add-OrchAssetLink | Create an asset link to share across folders |
| Remove-OrchAssetLink | Remove a folder link from an asset |

## 10. Queue Management

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
| Format-OrchQueueItem | Format queue items as one table per queue, expanding SpecificContent |
| Remove-OrchQueueItem | Remove a queue item |

### Queue Links (Multi-folder Queue Sharing)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchQueueLink | List queue links |
| Add-OrchQueueLink | Link a queue to additional folders |
| Remove-OrchQueueLink | Remove a folder link from a queue |

## 11. Storage Bucket Management

Cloud/local storage buckets and their contents.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchBucket | List storage buckets |
| New-OrchBucket | Create a storage bucket |
| Update-OrchBucket | Update storage bucket properties |
| Copy-OrchBucket | Copy buckets to another tenant/folder |
| Remove-OrchBucket | Remove a storage bucket |
| Get-OrchBucketItem | List files in a storage bucket |
| Import-OrchBucketItem | Upload a file to a storage bucket |
| Export-OrchBucketItem | Download a file from a storage bucket |
| Remove-OrchBucketItem | Remove a file from a storage bucket |

### Bucket Links (Multi-folder Bucket Sharing)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchBucketLink | List bucket links |
| Add-OrchBucketLink | Link a bucket to additional folders |
| Remove-OrchBucketLink | Remove a folder link from a bucket |

## 12. Machine Management

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

<a id="13-calendar-and-webhook"></a>
## 13. Calendar & Webhook

### Calendars (for Trigger Scheduling)

| Cmdlet | Description |
|--------|-------------|
| Get-OrchCalendar | List calendars |
| Get-OrchCalendarDate | List a calendar's excluded dates |
| Copy-OrchCalendar | Copy calendars to another tenant |
| Remove-OrchCalendar | Remove a calendar |
| Add-OrchCalendarDate | Add dates to a calendar |
| Remove-OrchCalendarDate | Remove dates from a calendar |

### Webhooks

| Cmdlet | Description |
|--------|-------------|
| Get-OrchWebhook | List webhooks |
| New-OrchWebhook | Create a webhook |
| Update-OrchWebhook | Update an existing webhook |
| Copy-OrchWebhook | Copy webhooks to another tenant |
| Enable-OrchWebhook | Enable a disabled webhook |
| Disable-OrchWebhook | Disable a webhook |
| Get-OrchWebhookEventType | List the event types webhooks can subscribe to |
| Test-OrchWebhook | Send a Ping event to verify webhook connectivity |
| Remove-OrchWebhook | Remove a webhook |

## 14. Action Center

Long-running, human-in-the-loop workflows. Action catalogs define the form templates; tasks are the runtime instances awaiting human action.

### Action Catalogs

| Cmdlet | Description |
|--------|-------------|
| Get-OrchActionCatalog | List action catalogs |
| New-OrchActionCatalog | Create an action catalog |
| Copy-OrchActionCatalog | Copy action catalogs |
| Remove-OrchActionCatalog | Remove an action catalog |

### Tasks

| Cmdlet | Description |
|--------|-------------|
| Get-OrchTask | Get tasks from a folder |
| Get-OrchTaskAcrossFolder | Get tasks tenant-wide via the cross-folder endpoint |
| Set-OrchTask | Update task title, priority, note, or catalog association |
| Remove-OrchTask | Delete tasks by Id |

## 15. Testing

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
| Get-OrchTestSetDetail | Get test sets with their Packages and TestCases arrays populated |
| New-OrchTestSet | Create a test set |
| Copy-OrchTestSet | Copy test sets to another tenant/folder |
| Start-OrchTestSet | Execute a test set |
| Get-OrchTestSetExecution | Get test set execution results |
| Stop-OrchTestSetExecution | Stop a running test set execution |
| Remove-OrchTestSet | Remove a test set |

### Test Set Schedules

| Cmdlet | Description |
|--------|-------------|
| Get-OrchTestSetSchedule | List test set schedules |
| New-OrchTestSetSchedule | Create a test set schedule |
| Update-OrchTestSetSchedule | Update existing test set schedules |
| Copy-OrchTestSetSchedule | Copy test set schedules |
| Enable-OrchTestSetSchedule | Enable a test set schedule |
| Disable-OrchTestSetSchedule | Disable a test set schedule |
| Remove-OrchTestSetSchedule | Remove a test set schedule |

### Test Data Queues

| Cmdlet | Description |
|--------|-------------|
| Get-OrchTestDataQueue | List test data queues |
| New-OrchTestDataQueue | Create a test data queue |
| Copy-OrchTestDataQueue | Copy test data queues |
| Get-OrchTestDataQueueItem | Get items in a test data queue |
| Import-OrchTestDataQueueItem | Bulk-add items from a CSV (web "Upload Items" format) |
| Format-OrchTestDataQueueItem | Format test data queue items as one table per queue, expanding ContentJson |
| Reset-OrchTestDataQueueItem | Reset consumed test data queue items |
| Remove-OrchTestDataQueue | Remove a test data queue |

## 16. License Management

License allocation and usage statistics.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchLicense | Get tenant license information |
| Get-OrchLicenseNamedUser | List named user license allocations |
| Get-OrchLicenseRuntime | List runtime license allocations |
| Enable-OrchLicenseRuntime | Enable a runtime license |
| Disable-OrchLicenseRuntime | Disable a runtime license |
| Get-OrchLicenseStats | Get license usage statistics |

<a id="17-monitoring-and-logs"></a>
## 17. Monitoring & Logs

Logs, audit trails, and alerts.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchLog | Get robot execution logs (supports filtering) |
| Get-OrchLogLocation | Get log storage location info |
| Open-OrchLogLocation | Open log storage in browser/explorer |
| Get-OrchAuditLog | Get audit log entries (supports filtering) |
| Get-OrchAlert | Get system alerts |

## 18. Settings

Tenant and system configuration settings.

| Cmdlet | Description |
|--------|-------------|
| Get-OrchSetting | Get general tenant settings |
| Set-OrchSetting | Update a general tenant setting value |
| Get-OrchWebSetting | Get web application settings |
| Get-OrchExecutionSetting | Get execution settings |
| Get-OrchActivitySetting | Get activity restriction settings |
| Get-OrchAuthenticationSetting | Get authentication settings |
| Get-OrchUpdateSetting | Get update server settings |
| Get-OrchConnectionString | Get connection string information |
| Enable-OrchMaintenanceMode | Enable maintenance mode |
| Disable-OrchMaintenanceMode | Disable maintenance mode |

<a id="19-platform-management-users"></a>
## 19. Platform Management - Users

Organization-level user management (Identity Server API).

| Cmdlet | Description |
|--------|-------------|
| Get-PmUser | List organization users |
| New-PmUser | Create a local user in the organization |
| Copy-PmUser | Copy users to another organization |
| Update-PmUser | Update user properties |
| Remove-PmUser | Remove a user from the organization |

### User Preferences

Per-user portal preferences (theme, language, ...) via the identity Setting API, as generic Key/Value pairs.

| Cmdlet | Description |
|--------|-------------|
| Get-PmUserPreference | Read your own portal preferences (theme, language, ...) |
| Set-PmUserPreference | Set your own portal preferences (-Key / -Value) |
| Copy-PmUserPreference | Migrate your own preferences to yourself in another organization |
| Get-PmNotificationSubscription | Read your own notification subscriptions (topic × mode) |
| Set-PmNotificationSubscription | Subscribe/unsubscribe yourself to a notification topic for a mode |
| Copy-PmNotificationSubscription | Migrate your own notification subscriptions to another organization |

<a id="20-platform-management-groups"></a>
## 20. Platform Management - Groups

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

<a id="21-platform-management-robot-accounts"></a>
## 21. Platform Management - Robot Accounts

Organization-level robot account management.

| Cmdlet | Description |
|--------|-------------|
| Get-PmRobotAccount | List robot accounts |
| New-PmRobotAccount | Create a robot account (errors if it already exists) |
| Set-PmRobotAccount | Create or update a robot account |
| Copy-PmRobotAccount | Copy robot accounts to another organization |
| Remove-PmRobotAccount | Remove a robot account |

<a id="22-platform-management-licenses"></a>
## 22. Platform Management - Licenses

Organization-level license group management.

| Cmdlet | Description |
|--------|-------------|
| Get-PmLicense | List user license bundles assigned to the organization |
| Get-PmLicenseAllocation | Get per-tenant license allocations (Robots & Services tab) |
| Get-PmLicenseContract | Get the account-level license contract (subscription, products, ML keys) |
| Get-PmLicenseInventory | Get the org-level license inventory dashboard summary |
| Get-PmGroupLicense | List licensed groups |
| Get-PmUserLicense | List licensed users |
| Add-PmGroupLicense | Add a license to a licensed group |
| Remove-PmGroupLicense | Remove a license from a licensed group |
| Remove-PmGroupLicenseAllocation | Remove allocation from a licensed group |
| Remove-PmLicensedGroup | Remove a licensed group |
| Add-PmUserLicense | Add a license to a licensed user |
| Remove-PmUserLicense | Remove a license from a licensed user |
| Remove-PmLicensedUser | Remove a licensed user (drops the row from the licensed-users set) |

<a id="23-platform-management-external-apps-and-other"></a>
## 23. Platform Management - External Apps & Other

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

## 24. Document Understanding

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

## 25. Test Manager

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

Version: 1.0 | Last Updated: March 2026
