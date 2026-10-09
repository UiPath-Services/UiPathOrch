# UiPathOrch roadmap

Status: living document. Last updated 2026-10-09.

Source of most items: a read of the #jp-help-infra Slack channel (485 threads, 2023-01 to
2026-10) for problems customers ran into that a UiPathOrch feature could solve.

## Done (unreleased)

| Item | Commit | Notes |
| --- | --- | --- |
| `Start-OrchJob`: priority, stop/kill, alert parameters; `-Wait` | e4b87ea8, 1bf77c68 | Stop/kill fields dropped below API 15, alerts below 16. JobsCount defaults to 1 (20.10–22.10 refuse it missing). |
| `Measure-OrchLog` | 6b1887b4 | Count per folder from GetTotalCount (Elasticsearch `$count` stops at 10,000). |
| `Get-OrchLog` past 10,000 logs on Cloud | 4ddc54cd | Pages by TimeStamp window. |
| `Get-OrchLog -Machine` / `-WindowsIdentity` | e39a846c | A value that matched nothing used to return every log. |
| `Wait-OrchJob` | f2fe574b | |
| `Test-PmExternalApplication`, `New-PmExternalApplication` | 377936ed, 767be6c8, 18d53841 | For applications registered for other tools. |
| `Find-OrchAccountReference` | c745da54 | Where a Windows account is used, before a password change. |
| Same-tenant user mapping | ab3325c3 | `New-OrchUserMappingCsv -SourceDomain/-DestinationDomain`; `Copy-OrchUser` / `Copy-OrchFolderUser` re-home users within one tenant. |

## Pending verification

### Same-tenant user mapping against a real directory

ab3325c3 was verified on op2510 (25.10) with two local Identity users only. Not yet run against
a real directory:

- **On-premises Active Directory, `OLD\taro` → `NEW\taro`.** Needs a domain controller; none
  exists today. Orch1 is integrated with Entra ID (`kzsai.onmicrosoft.com`, cloud-only), not with
  an on-premises AD, so it has no DC to share. Plan: build a DC, join one op* VM, and configure
  the AD integration (host portal > authentication > Active Directory; IIS Windows
  authentication with `useAppPoolCredentials`). To test the move itself, a second domain with a
  trust (or a second DC) is needed, so that users of both domains can be added. Open questions:
  - How directory users appear in `/odata/Users` (is `UserName` `OLD\taro`, or `taro` with
    `Domain` = `OLD`?). `New-OrchUserMappingCsv` only rewrites names that carry the domain
    (`DOMAIN\name`, `name@domain`); a bare name with a separate `Domain` gets no row.
  - Whether the directory search (`SearchForUsersAndGroups`) resolves `NEW\taro` as typed.
  - Groups (`OLD\RPA Users` → `NEW\RPA Users`).
- **Entra ID.** Connect op2510 (standalone) to the same Entra ID tenant as Orch1; needs an app
  registration (tenant ID, client ID, secret) made by the tenant's admin. Covers mapping between
  real directory users and groups. A domain rewrite (`a@old` → `a@new`) needs a second custom
  domain on the tenant. Also check: a UPN change in Entra keeps the object id, so the
  Orchestrator user may simply follow it with no re-homing; re-homing would then be needed only
  when moving to another Entra tenant (the 2023-09-06 thread). Groups with the same display name
  in the old and new tenant cannot be told apart by name.

References (Slack): AD integration walkthrough, Azure OC study session part 2
(https://uipath-japan.slack.com/archives/C01QYUQ8M40/p1667994529870399); switching the AD on
22.10 (https://uipath-japan.slack.com/archives/CLXREBSCB/p1730877247837979); two-domain trust
setup (https://uipath-japan.slack.com/archives/CLXREBSCB/p1618473013161100).

### Classic folders in `Find-OrchAccountReference`

Robot rows (classic robots) and `ExecutorRobots` on triggers are covered by unit tests only; no
server with classic folders was at hand.

## Backlog

- **Inventory of connected devices** (which machines and users have connected). Survey the data
  sources on a real tenant first: sessions that remain after disconnect, LicensesNamedUser,
  `Jobs.HostMachineName`, the Platform Management audit log. `Get-PmAuditLog`'s date filters are
  still commented out.
- **`Start-OrchJob` target selection** (`MachineRobots` / `MachineSessionIds`). Capture the web
  Start Job payload first.
- **`Get-OrchLog` incremental export** (low priority): Cloud keeps robot logs about 30 days, and
  customers want to archive them periodically.
- **Awareness post**: 36 #jp-help-infra threads are already answered by existing cmdlets; a post
  listing them (links in `jp-help-infra-UiPathOrch.md`) would make that known.

## Decided against

- `Test-OrchConfig`: the configuration file comes from a template, so misspelled keys are rare,
  and a drive that fails to mount cannot be checked against its server anyway.
- Tenant snapshot cmdlet: `Copy-Item -Recurse` to a spare tenant plus `Compare-Orch*` covers it.
- Package usage cmdlet: `Remove-OrchPackage` already refuses a version in use (1013) and names
  what uses it.
- All-in-one same-tenant re-homing that also rewrites per-user asset values and trigger robots in
  place: those PUTs risk clobbering other fields, and passwords cannot be moved anyway.
  `Find-OrchAccountReference` lists what is left to do by hand.
