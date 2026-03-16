# Migration Test Matrix

## Available Test Environments

| Drive | Type | API Version | Root |
|---|---|---|---|
| local: | On-premises MSI | v13 | orchestrator.local |
| Orch1: | Automation Cloud | v20 | cloud.uipath.com/yotsuda/svc1 |
| Orch2: | Automation Cloud | v20 | cloud.uipath.com/yotsuda/svc3 |
| Orch4: | Automation Cloud | v20 | cloud.uipath.com/yotsuda/svc4 |
| Orch5: | Automation Cloud | v20 | cloud.uipath.com/yotsuda/svc5 |
| stage: | Staging Cloud | TBD | staging.uipath.com |
| AraiStg: | Staging Cloud | TBD | staging.uipath.com |

## Test Combinations

### By Version Direction

| # | Source | Destination | Direction | Status |
|---|---|---|---|---|
| 1 | Cloud (v20) | On-prem (v13) | New → Old | Done (2026-03-16) |
| 2 | On-prem (v13) | Cloud (v20) | Old → New | Done (2026-03-16) |
| 3 | Cloud (v20) | Cloud (v20) | Same version | Not tested |
| 4 | On-prem (v13) | On-prem (v13) | Same version | Not tested |

### By Organization

| # | Source | Destination | Same Org? | Status |
|---|---|---|---|---|
| 5 | Orch2: (yotsuda) | Orch1: (yotsuda) | Yes (same org, different tenant) | Not tested |
| 6 | Orch2: (yotsuda) | kzsai: (kzsai) | No (cross-org) | Not tested |

### By Platform

| # | Source | Destination | Platforms | Status |
|---|---|---|---|---|
| 7 | Cloud | Cloud | AC → AC | Not tested |
| 8 | Cloud | On-prem | AC → MSI | Done (2026-03-16) |
| 9 | On-prem | Cloud | MSI → AC | Done (2026-03-16) |
| 10 | On-prem | On-prem | MSI → MSI | Not tested (need 2nd on-prem) |

## Priority for Testing

### High Priority (common migration scenarios)
- **#3 Cloud → Cloud (same version)**: Most common migration scenario
  (tenant consolidation). Use Orch2: → Orch1: or Orch4:.
- **#5 Same org, different tenant**: Common for tenant reorganization.

### Medium Priority
- **#6 Cross-org**: Covered by Case B user mapping. Test with
  Orch2: → kzsai: if available.
- **#4 On-prem → On-prem**: Requires 2nd on-prem environment (myonprem:
  uses same server as local:, so may not be a valid test).

### Low Priority (already tested)
- **#1 Cloud → On-prem** and **#2 On-prem → Cloud**: The most difficult
  combinations due to version differences. Already tested and bugs fixed.

## Issues Found and Fixed

### Test #1: Cloud (v20) → On-prem (v13)

| Entity | Issue | Fix |
|---|---|---|
| Release | Tags, ResourceOverwrites, FeedId, ProcessSettings, HiddenForAttendedUser, RemoteControlAccess, EnvironmentVariables | PostRelease: strip by API version |
| Machine | AutomationCloudSlots, AutomationType, TargetFramework, Tags | AddMachine: strip by API version |
| Webhook | Name, Description, Key (added in v16) | CreateWebhook: strip for < v16 |
| Queue | Tags, Encrypted, RetryAbandonedItems | CreateQueue: strip for < v16 |
| Asset | Key, Tags | AddAsset: strip for < v15 |
| Bucket | Tags | PostBucket: strip for < v15 |
| Trigger | ActivateOnJobComplete, ItemsActivationThreshold; missing StartProcessCronDetails and ExternalJobKey | PostProcessSchedule: strip and default |

### Test #2: On-prem (v13) → Cloud (v20)

| Entity | Issue | Fix |
|---|---|---|
| Webhook | No Name field in v13 | CopyWebhook: auto-generate from URL host |
| Queue | ReleaseId requires SLA on v19+ | CopyQueues: auto-set 24h SLA when needed |
| Role | "Custom mixed roles can no longer be created" | Server constraint, not fixable |
| User | On-prem local users don't exist in Cloud | Expected (Case B / manual) |

## Notes

- `copy -Recurse` is idempotent: re-running skips already-copied entities.
- HTTP logging (Verbose level) is essential for diagnosing failures.
- Swagger version comparison is the definitive way to determine when
  properties were introduced.
- The `Set-OrchSetting` cmdlet can be used to configure the destination
  tenant's library feed before migration.
