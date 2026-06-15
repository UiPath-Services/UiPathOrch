# UiPathOrch tests

Two layers:

| Layer | What | Where | Network |
|---|---|---|---|
| **Unit** | Pure C# logic (matchers, CSV parser, path tools, retry policy, address ordering, …) | `Tests/UnitTests/` (xUnit) | none |
| **Integration** | Live `*.Tests.ps1` (Pester) driving real Orchestrator tenants | `Tests/*.Tests.ps1` | hits real tenants |

## Unit tests (fast, offline)

```powershell
dotnet test Tests\UnitTests\UnitTests.csproj
```

No drives, no network. Run these on every change.

## Integration tests (live)

These exercise real cmdlets against real Orchestrator tenants, so they **create,
modify, and delete entities**. They use two drives:

| Role | Meaning | Default |
|---|---|---|
| **Tenant** (`$env:UIPATHORCH_TEST_DRIVE`) | **DISPOSABLE** tenant. Wiped + repopulated from the fixture. **Never point this at a tenant you care about.** | — (must be set) |
| **RefDrive** (`$env:UIPATHORCH_TEST_REF_DRIVE`) | Read-only reference tenant for the two-drive (cross-tenant) tests. Never written. | `Orch1` |

The reference fixture lives in `TestData/Fixture/` (exported from
`Orch1:\TestFixture_Base`). `Import-Fixture.ps1` remaps it onto the target drive.

### Recommended: run the whole suite with the runner

```powershell
Tests\Invoke-AllTests.ps1 -Tenant Orch2 -RefDrive Orch1
```

`Invoke-AllTests.ps1` is the supported way to run everything. For **each** file it
resets the disposable tenant and re-imports the fixture, then runs that file in
isolation and prints a per-file + total summary (exit code 1 if anything is not
clean). Useful switches:

- `-Filter 'Compare*'` — run a subset.
- `-Exclude 'Foo.Tests.ps1','Bar.Tests.ps1'` — skip files.
- `-SkipReset` / `-SkipImport` — trade isolation for speed when iterating on one
  area whose tenant state you know is already good.

### Why the runner exists (don't `Invoke-Pester -Path .` directly)

All files share **one mutable tenant**, but their starting-state contracts
differ: some wipe the tenant (`Reset-Tenant`), some import the fixture, some
create their own uniquely-named scratch, and a few assume the tenant is already
populated. Run together in discovery order, a file that wipes the tenant leaves a
later file's setup to fail, and **every test in that file is reported failed** —
so a bare `Invoke-Pester -Path .` cascades into mass failure even though each
file passes on its own.

The runner fixes this structurally:

1. **Clean state per file** — `Reset-Tenant` + `Import-Fixture` before each file,
   so order no longer matters.
2. **Current location is pinned to the disposable tenant.** A test whose setup
   fails can run a cleanup with an empty `-Path`, which resolves to the *current
   location's* drive (standard provider behavior). Pinning the location to the
   disposable tenant guarantees such a stray operation stays there and can never
   touch the reference drive or any other tenant.
3. **Progress bars and per-drive host warnings are silenced**, so the output is
   just the pass/fail summary.

### Running a single file manually

```powershell
$env:UIPATHORCH_TEST_DRIVE     = 'Orch2'
$env:UIPATHORCH_TEST_REF_DRIVE = 'Orch1'
Tests\Reset-Tenant.ps1   -TargetDrive Orch2 -Confirm:$false
Tests\Import-Fixture.ps1 -TargetDrive Orch2
Set-Location Orch2:\                       # keep stray null-path ops on the disposable tenant
Invoke-Pester -Path Tests\CleanTenant.Tests.ps1 -Output Detailed
```

### Helpers

- `Reset-Tenant.ps1 -TargetDrive <drive>` — **DESTRUCTIVE**: removes triggers,
  queues, processes, buckets, assets, folders, tenant packages/machines, and
  non-system custom roles/users (preserves the current user and system entities).
- `Import-Fixture.ps1 -TargetDrive <drive>` — recreates `TestFixture_Base` and its
  contents from `TestData/Fixture/`, remapping the `Orch1:` path prefix to the
  target drive.

### Known environmental limitations (not regressions)

- **Tenant capability differences.** Some tenants forbid certain operations
  (e.g. creating a test-set schedule on restricted grades). Cases that need such
  a capability will fail on a tenant that lacks it — choose a capable `-Tenant`
  or `-Exclude` those files.
- **Write-only secrets are never compared.** The `Compare-Orch*` secret-bearing
  comparisons warn that credential/secret values, bucket keys, and machine/
  webhook secrets cannot be read back, so drift in those values is not detected.
- The `Reset-Tenant` line `Cannot delete last user/group with Orchestrator
  Administrator role` is expected and harmless (the last admin is preserved).
