# Migration Test Matrix

## Test Combinations

### By Version Direction

| # | Source | Destination | Status |
|---|---|---|---|
| 1 | Automation Cloud (API v20) | On-prem MSI 21.10.4 (API v13) | Done (2026-03-16) |
| 2 | On-prem MSI 21.10.4 (API v13) | Automation Cloud (API v20) | Done (2026-03-16) |
| 3 | Automation Cloud (API v20) | On-prem MSI 20.10.16 (API v11) | Done (2026-03-17). 97 "must not be null" errors. Many v11-incompatible properties remain. Low priority fix (v11 is EOL). |
| 4 | Automation Cloud (API v20) | Automation Cloud (API v20) | Not tested |
| 5 | On-prem (same version) | On-prem (same version) | Not tested |
| 6 | Automation Cloud | Automation Cloud (same org, different tenant) | Not tested |
| 7 | Automation Cloud | Automation Cloud (cross-org) | Not tested |

### By AD Integration and User Mapping

| # | Source | Destination | User Mapping | Status |
|---|---|---|---|---|
| 8 | No AD (local users) | No AD (local users) | Case A (same usernames) | Done (2026-03-16, #1 #2) |
| 9 | No AD (local users) | No AD or AD | Case B (username format changes, e.g. `admin` → `admin@company.com`) | Not tested |
| 10 | AD (Entra ID) | AD (Entra ID), same directory | Case A (same usernames) | Not tested |
| 11 | AD (Entra ID) | AD (Entra ID), different directory | Case B (different email domains) | Not tested |
| 12 | AD (on-prem AD) | AD (Entra ID) | Case B (`DOMAIN\user` → `user@company.com`) | Not tested |
| 13 | No AD (local users) | AD (Entra ID) | Case B (local names → email addresses) | Not tested |

**Note**: `copy -Recurse Source:\ Destination:\` copies folder-level user
assignments but does NOT create organization-level users. When migrating
to a different identity provider, users must be created in the destination
organization first (via `Copy-PmUser` or manually), then use
`-UserMappingCsv` to map old usernames to new ones.

### Priority

**High** (common scenarios):
- #4: Tenant consolidation (Cloud → Cloud, same version)
- #6: Tenant reorganization (same org)
- #9: On-prem to Cloud with username changes (most common real migration)

**Medium**:
- #7: Cross-org migration (Case B user mapping)
- #12: On-prem AD to Entra ID (common modernization path)
- On-prem with other versions (23.4, 23.10, 24.10, etc.)

**Low** (already covered or rare):
- #1, #2: Tested. The largest supported version gap (v13 ↔ v20).
- #3: v11 is EOL. Many incompatible properties. Fix on demand.
- #10: Same directory, usernames match. Unlikely to have issues.
- #5: Same version on-prem. Requires 2nd on-prem environment.

## Issues Found and Fixed

### Test #1: Automation Cloud (v20) → On-prem MSI 21.10.4 (v13)

| Entity | Issue | Fix |
|---|---|---|
| Release | Unsupported properties: Tags, ResourceOverwrites, FeedId, ProcessSettings, HiddenForAttendedUser (v17+), RemoteControlAccess (v16+), EnvironmentVariables (v19+) | PostRelease: strip by API version |
| Machine | AutomationCloudSlots (v15+), AutomationType (v15+), TargetFramework (v15+), Tags (v15+) | AddMachine: strip by API version |
| Webhook | Name, Description, Key (v16+) | CreateWebhook: strip for < v16 |
| Queue | Tags, Encrypted, RetryAbandonedItems (not in v13) | CreateQueue: strip for < v16 |
| Asset | Key, Tags (v15+) | AddAsset: strip for < v15 |
| Bucket | Tags (v15+) | PostBucket: strip for < v15 |
| Trigger | ActivateOnJobComplete, ItemsActivationThreshold (not in v13); missing StartProcessCronDetails and ExternalJobKey (required by v13) | PostProcessSchedule: strip and add defaults |

### Test #2: On-prem MSI 21.10.4 (v13) → Automation Cloud (v20)

| Entity | Issue | Fix |
|---|---|---|
| Webhook | No Name field in v13 | CopyWebhook: auto-generate from URL host for v16+ destination |
| Queue | ReleaseId requires SLA on v19+ | CopyQueues: auto-set 24h SLA for v19+ destination |
| Role | "Custom mixed roles can no longer be created" | Server constraint, not fixable |
| User | On-prem local users don't exist in Cloud | Expected (Case B / manual) |

## UserMappingCsv Testing Notes

The `-UserMappingCsv` parameter has NOT been tested in actual migration.
It is used by the following cmdlets:
- `Copy-PmUser`
- `Copy-OrchUser`
- `Copy-OrchAsset`
- `Copy-OrchFolderUser`
- `copy` (`Copy-Item`)

Key scenarios to test:
- On-prem local user `admin` → Cloud email `admin@company.com`
- On-prem AD user `DOMAIN\jsmith` → Entra ID `jsmith@company.com`
- Generate CSV with `New-OrchUserMappingCsv`, validate with
  `Test-OrchUserMappingCsv`, then migrate with `-UserMappingCsv`

## Notes

- `copy -Recurse` is idempotent: re-running skips already-copied entities.
- HTTP logging (Verbose level) is essential for diagnosing failures.
- Swagger version comparison is the definitive way to determine when
  properties were introduced.
- `Set-OrchSetting` can configure the destination tenant's library feed
  before migration.
