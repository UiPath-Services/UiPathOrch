# Release Guide

This document describes how to cut a release of **UiPathOrch** and publish it to the [PowerShell Gallery](https://www.powershellgallery.com/packages/UiPathOrch).

> **Audience**: repo maintainers with PSGallery co-owner rights.
> Secrets (API keys, signing certificate passwords, vault paths) are **never** stored in this repo. They are shared out-of-band among maintainers.

---

## Overview

A release consists of:

1. Version bump in the module manifest + `.csproj` + `CHANGELOG.md`
2. Build and run all [release acceptance gates](#4-release-acceptance-gates)
3. (Optional) Sign the module — see [Code signing](#code-signing-optional)
4. Tag the commit and push
5. Publish the module to the PowerShell Gallery
6. Create a GitHub Release with notes

The recommended path is **GitHub Actions-based automated publish** (see [Automated release](#automated-release-recommended)). Manual publish is documented as a fallback.

---

## Prerequisites

Each maintainer who can publish needs:

- **GitHub permissions**: Write or Admin on this repo
- **PSGallery account** listed as a co-owner of the `UiPathOrch` package
  - Request via the [Manage Owners](https://www.powershellgallery.com/packages/UiPathOrch/Manage) page
  - Each owner generates their own API key at [PSGallery API Keys](https://www.powershellgallery.com/account/apikeys)
- **(Optional) Code signing certificate** — only if shipping signed releases
  - The certificate itself and its password are distributed out-of-band
  - Do **not** commit `.pfx` files or passwords to this repo
  - See [Code signing](#code-signing-optional) for when this is worth the effort
- Local toolchain:
  - PowerShell 7.4.2+
  - .NET SDK 8.0+
  - Git

---

## Versioning

UiPathOrch follows [Semantic Versioning](https://semver.org/):

- **MAJOR** — breaking changes to cmdlet parameters, provider paths, or pipeline behavior
- **MINOR** — new cmdlets, new parameters (backward-compatible)
- **PATCH** — bug fixes, documentation, performance

The current version lives in `Staging/UiPathOrch.psd1` (`ModuleVersion`).

---

## Release procedure

### 1. Prepare the release branch

```powershell
git switch master
git pull
git switch -c release/vX.Y.Z
```

### 2. Bump version and update changelogs

Edit the following files (the two version fields must match):

- `UiPathOrch/UiPathOrch.csproj` — update `<Version>`
- `Staging/UiPathOrch.psd1` — update `ModuleVersion`
- `CHANGELOG.md` — move items from `Unreleased` to a new `## [X.Y.Z] - YYYY-MM-DD` section. This block doubles as the GitHub Release body (the release workflow extracts the matching `## [X.Y.Z]` section) and as the `ReleaseNotes` text in `Staging/UiPathOrch.psd1` (kept in sync by hand at version-bump time)

### 3. Build and deploy locally

```powershell
.\Build-Deploy.ps1              # Build + deploy to C:\Program Files\PowerShell\7\Modules\UiPathOrch
Import-Module UiPathOrch -Force

Get-Module UiPathOrch            # Confirm the new version is loaded
Get-Command -Module UiPathOrch   # All expected cmdlets present
```

### 4. Release acceptance gates

All of the following must pass before tagging. See `CONTRIBUTING.md` for setup (Orchestrator test drives, PSScriptAnalyzer installation, etc.).

| Gate | Command | Pass criteria |
| --- | --- | --- |
| .NET build | `dotnet build --configuration Release` | Exit 0, no errors |
| Format check | `dotnet format --verify-no-changes` | Exit 0 |
| Unit tests | `dotnet test --configuration Release` | All pass |
| Manifest validity | `Test-ModuleManifest "$env:ProgramFiles\PowerShell\7\Modules\UiPathOrch\UiPathOrch.psd1"` | Returns module info, no errors |
| Lint (Errors) | `Invoke-ScriptAnalyzer -Path Staging -Recurse -Severity Error` | No output |
| Lint (Warnings) | `Invoke-ScriptAnalyzer -Path Staging -Recurse -Severity Warning` | Reviewed; new warnings justified in PR |
| Pester — self-contained | `Invoke-Pester -Path Tests\SelfContained.Tests.ps1` | 0 failures |
| Pester — completer | `Invoke-Pester -Path Tests\Completer.Tests.ps1` | 0 failures |
| Publish dry-run | `Publish-Module -Path <modulePath> -NuGetApiKey <key> -WhatIf` | No validation errors |

Notes on these gates:

- **`Test-ModuleManifest` needs the DLL adjacent to the psd1.** It tries to open `RootModule` to confirm the module loads, so running it against `Staging\UiPathOrch.psd1` fails (DLL lives under `UiPathOrch\bin\Release\net8.0\`). Run it against the deployed module path (above) or `Import-PowerShellDataFile` the psd1 if you only need to peek at the version. The CI release workflow does the latter for exactly this reason.
- **Completer + Fixture round-trip tests reset the target tenant.** Both `Tests\Completer.Tests.ps1` and `Tests\Fixture.RoundTrip.Tests.ps1` run `Reset-Tenant.ps1 -TargetDrive OrchTest` in `BeforeAll`, then `Import-Fixture.ps1` to land at a deterministic state. The target drive is destroyed and re-seeded each run — make sure `OrchTest:` points at a disposable tenant before running.
- **Known-issue Pester tests** are tagged `KnownIssue` and can be excluded with `-ExcludeTagFilter KnownIssue` when the OData Queue-items intermittent is flaky on the tenant.

Quick one-liner for the Pester gates (requires connected `OrchTest:` and `Orch1:` drives):

```powershell
Import-OrchConfig
Invoke-Pester -Path Tests\SelfContained.Tests.ps1,Tests\Completer.Tests.ps1 `
    -ExcludeTagFilter KnownIssue -Output Detailed
```

### 5. Sign the module (optional)

> Skip this step if shipping an unsigned release. See [Code signing](#code-signing-optional) below for when signing matters.

Sign the following files with the project's code signing certificate:

- `Staging\UiPathOrch.dll` (the built binary from `UiPathOrch\bin\Release\net8.0\`)
- `Staging\UiPathOrch.psd1`
- `Staging\UiPathOrch.psm1`
- All `.ps1` files under `Staging\Functions\`
- `Staging\UiPathOrch.Format.ps1xml`

```powershell
$cert = Get-PfxCertificate -FilePath <path-to-pfx>   # out-of-band path
$files = @(
    'Staging\UiPathOrch.dll',
    'Staging\UiPathOrch.psd1',
    'Staging\UiPathOrch.psm1',
    'Staging\UiPathOrch.Format.ps1xml'
) + (Get-ChildItem Staging\Functions\*.ps1 | ForEach-Object FullName)

Set-AuthenticodeSignature -FilePath $files -Certificate $cert `
    -TimestampServer http://timestamp.digicert.com
```

Verify all signatures are `Valid`:

```powershell
Get-AuthenticodeSignature $files | Format-Table Status, Path
```

> **Self-signed certs yield `Status = UnknownError`** on machines where the signing cert is not installed in the trust store (the CI runner, or any user machine that hasn't imported the cert). The signature itself is cryptographically intact — only chain validation is failing. The CI release workflow (`.github/workflows/release.yml`) therefore accepts `Valid` or `UnknownError` **and also verifies the signer thumbprint matches the expected cert**, which rules out `NotSigned` / `HashMismatch` / `NotTrusted`. On the signer's own machine (where the cert was originally created) the status will be `Valid` and the standard check works as-is.

### 6. Open a pull request and merge

```powershell
git add Staging/UiPathOrch.psd1 CHANGELOG.md
git commit -m "Release vX.Y.Z"
git push -u origin release/vX.Y.Z
gh pr create --fill
```

After CI passes and review, merge into `master`.

### 7. Tag the release

```powershell
git switch master
git pull
git tag vX.Y.Z              # lightweight tag — matches v0.9.x convention
git push origin vX.Y.Z      # push triggers release.yml
```

> **Tag style.** Past releases (v0.9.x) all use **lightweight** tags, so
> `git tag vX.Y.Z` (no `-a`, no `-s`) is the established convention.
> The release workflow does not depend on tag annotations or signatures —
> only on the tag name matching the `v*.*.*` glob and the version
> embedded in `Staging/UiPathOrch.psd1` + `UiPathOrch/UiPathOrch.csproj`
> matching the tag (after stripping the `v`).
>
> Signed tags (`git tag -s …`) are fine if GPG is configured locally,
> but require a secret key in the keyring for `user.email`. If you see
> `gpg: skipped "<email>": No secret key`, fall back to a lightweight tag —
> the workflow will publish identically.

> **Version style.** Starting with **1.0.0**, UiPathOrch uses 3-part
> SemVer (`MAJOR.MINOR.PATCH`). The pre-1.0 history retained 4-part
> tags (`v0.9.18.0` etc.); the discontinuity at 1.0 is intentional.
> The `v*.*.*` glob in `release.yml` accepts both shapes, but new
> releases should be 3-part.

### 8. Publish to PSGallery

#### Automated release (recommended)

Pushing a `vX.Y.Z` tag triggers the `release.yml` workflow, which:

1. Builds and tests the module
2. (Optional) Signs it using the certificate stored in repo secrets (`CODE_SIGNING_PFX_BASE64`, `CODE_SIGNING_PFX_PASSWORD`) — skipped if the secrets are not configured
3. Publishes to PSGallery using `PSGALLERY_API_KEY`
4. Creates a GitHub Release containing **only the section of `CHANGELOG.md` that matches the tag's version** (the workflow extracts the `## [X.Y.Z]` block up to the next version heading; if the section is missing the workflow fails before publishing)

Maintainers only need to push the tag — the workflow handles the rest.

> **Workflow gotchas encountered (and already mitigated) in v0.9.16.5's first run:**
> - `Test-ModuleManifest` was replaced with `Import-PowerShellDataFile` for the tag-vs-manifest check, because the DLL is not adjacent to the psd1 at that point in the CI pipeline.
> - The post-sign signature check accepts `Valid` or `UnknownError` (self-signed certs are not in the runner's trust store) and additionally asserts that the signer thumbprint matches the expected cert. `NotSigned` / `HashMismatch` / `NotTrusted` still fail.
> - The sign step runs conditionally via an explicit `Probe signing secret` step (`steps.sign_probe.outputs.present == 'true'`); `if: ${{ secrets.X != '' }}` alone is not reliable across all GHA contexts.

#### Manual publish (fallback)

If the workflow is broken or a maintainer needs to publish manually:

```powershell
# Stage a clean copy (the deployed module directory is suitable)
$modulePath = 'C:\Program Files\PowerShell\7\Modules\UiPathOrch'

# If this is a signed release, verify signatures one more time
Get-ChildItem $modulePath -Recurse -Include *.dll,*.psd1,*.psm1,*.ps1,*.ps1xml |
    Get-AuthenticodeSignature | Where-Object Status -ne 'Valid'
# (Above should output nothing for a signed release. Skip for unsigned releases.)

# Publish — use your own PSGallery API key
Publish-Module -Path $modulePath -NuGetApiKey <your-api-key>
```

### 9. Create a GitHub Release

If publishing manually (the automated workflow does this for you):

```powershell
# Extract the matching `## [X.Y.Z]` section from CHANGELOG.md (same logic the
# release workflow runs automatically).
$version = 'X.Y.Z'
$lines = Get-Content CHANGELOG.md
$section = New-Object System.Collections.Generic.List[string]
$inSection = $false
foreach ($line in $lines) {
    if ($line -match '^##\s+\[(\S+?)\]') {
        if ($matches[1] -eq $version) { $inSection = $true; continue }
        elseif ($inSection)            { break }
    }
    if ($inSection) { $section.Add($line) }
}
Set-Content release-notes.md (($section -join "`n").Trim())
gh release create "v$version" --title "v$version" --notes-file release-notes.md
```

### 10. Post-release verification

From a clean machine (or different user):

```powershell
Find-Module UiPathOrch                    # Should show the new version
Install-Module UiPathOrch -Scope CurrentUser
Import-Module UiPathOrch
Get-Module UiPathOrch | Select-Object Version, Path
```

---

## Rollback

PSGallery **does not allow deleting or overwriting** a published version. If a release is broken:

1. Publish a new patch version (`X.Y.Z+1`) with the fix
2. In the PSGallery UI, **unlist** the broken version (Manage Package → Unlist)
   - Unlisted versions remain installable by exact version but are hidden from `Find-Module`
3. Update `CHANGELOG.md` to note the pulled version

Never try to force-push over a release tag — create a new tag instead.

---

## Code signing (optional)

Signing a PowerShell module is **not required** to publish it to PSGallery or for users to install and import it. Many popular community modules (Pester, PSScriptAnalyzer, etc.) ship unsigned.

Sign the module when:

- Users may run under `AllSigned` execution policy (common in enterprise environments with GPO-enforced code restrictions)
- You want end users to be able to verify publisher authenticity via Authenticode
- You want to reduce SmartScreen / antivirus friction on download

Skip signing when:

- The additional maintainer overhead (certificate rotation, secret management, per-release signing) outweighs the above benefits for your user base

If you do sign, see step 4 of the release procedure for the commands, and configure the GHA secrets below.

---

## GitHub Actions secrets

The `release.yml` workflow requires the following repo secrets (configured in Settings → Secrets and variables → Actions):

| Secret | Required? | Purpose |
| --- | --- | --- |
| `PSGALLERY_API_KEY` | Required | API key with `Push` scope for the `UiPathOrch` package |
| `CODE_SIGNING_PFX_BASE64` | Optional | Base64-encoded `.pfx` certificate (signed releases only) |
| `CODE_SIGNING_PFX_PASSWORD` | Optional | Password for the `.pfx` (signed releases only) |

Rotation procedure and certificate lifecycle are documented out-of-band among maintainers.

---

## Maintainers / Co-owners

The current list of maintainers with publish rights is tracked in the GitHub repo's **Settings → Collaborators** page and on the PSGallery **Manage Owners** page. Adding or removing a co-owner requires consensus among current maintainers.

To request co-owner access, open an issue or contact an existing maintainer directly.

---

## Checklist (quick reference)

- [ ] Version bumped in `UiPathOrch/UiPathOrch.csproj` and `Staging/UiPathOrch.psd1` (values match)
- [ ] `CHANGELOG.md` updated (and the matching `ReleaseNotes` block in `Staging/UiPathOrch.psd1` copied from it)
- [ ] `.\Build-Deploy.ps1` clean
- [ ] All [release acceptance gates](#4-release-acceptance-gates) pass
- [ ] (If signing) Module files signed, all signatures `Valid`
- [ ] PR merged to `master`
- [ ] Tag `vX.Y.Z` pushed
- [ ] Automated workflow succeeded (or manual publish completed)
- [ ] GitHub Release created with notes
- [ ] `Find-Module UiPathOrch` shows the new version from a clean machine
