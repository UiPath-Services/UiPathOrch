# Migration Test Matrix

## Test Combinations

### By Version Direction

| # | Source | Destination | Status |
|---|---|---|---|
| 1 | Automation Cloud (API v20) | On-prem MSI 21.10.4 (API v13) | Done (2026-03-16) |
| 2 | On-prem MSI 21.10.4 (API v13) | Automation Cloud (API v20) | Done (2026-03-16) |
| 3 | Automation Cloud (API v20) | Automation Cloud (API v20) | Not tested |
| 4 | On-prem (same version) | On-prem (same version) | Not tested |
| 5 | Automation Cloud | Automation Cloud (same org, different tenant) | Not tested |
| 6 | Automation Cloud | Automation Cloud (cross-org) | Not tested |

### Priority

**High** (common scenarios):
- #3: Tenant consolidation (Cloud → Cloud, same version)
- #5: Tenant reorganization (same org)

**Medium**:
- #6: Cross-org migration (Case B user mapping)
- On-prem with other versions (23.4, 23.10, 24.10, etc.)

**Low** (already covered by hardest cases):
- #1, #2: Tested. The largest version gap (v13 ↔ v20).

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

## Notes

- `copy -Recurse` is idempotent: re-running skips already-copied entities.
- HTTP logging (Verbose level) is essential for diagnosing failures.
- Swagger version comparison is the definitive way to determine when
  properties were introduced.
- `Set-OrchSetting` can configure the destination tenant's library feed
  before migration.
