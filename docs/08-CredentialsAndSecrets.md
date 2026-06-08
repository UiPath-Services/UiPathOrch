---
title: Credentials & Secrets
nav_order: 8
permalink: /credentials-secrets/
---

# Credentials & Secrets

## The write-only rule

The Orchestrator and Identity Web APIs **never return stored secrets**. UiPathOrch
surfaces every other field of an entity, but secret fields (passwords, client secrets,
webhook secrets, storage keys) always come back **empty** — they are write-only.

Two consequences follow:

- **Reading** an entity (`Get-*`, `*-ExportCsv`, `Copy-*`) never exposes its secret, even
  for an account connected through a non-confidential app *with* permission to view the
  value. The only consumer that can read a credential value is a **robot at runtime**
  (via the *Get Credential* activity).
- **Copying / migrating** an entity carries everything *except* its secret. After a
  `Copy-*` or an `ExportCsv` → `Import-Csv` round-trip, the secret must be **re-set on the
  destination**.

## Entities that hold a secret

| Entity | Secret field (returned empty) | How to (re-)set it |
|--------|-------------------------------|--------------------|
| **Credential Asset** (`OrchAsset`, Credential type) | `CredentialPassword` | `Set-OrchCredentialAsset` |
| **User / Unattended Robot** (`OrchUser`) | `UR_Password` | `Update-OrchUser -UR_Password …` |
| **Machine — Confidential** (`OrchMachine`) | `ClientSecret` | `Add-OrchMachineClientSecret` (re-issues it) |
| **Storage Bucket** (`OrchBucket`) | `Password` (S3 secret key / Azure storage key, per `Provider`) | re-set in the web UI, or recreate: `Remove-OrchBucket` + `New-OrchBucket -Password …` |
| **Webhook** (`OrchWebhook`) | `Secret` | re-set in the web UI, or recreate: `Remove-OrchWebhook` + `New-OrchWebhook …` |
| **Credential Store** (`OrchCredentialStore`) | secrets in `AdditionalConfiguration` | re-set in the web UI (there is no update or `New-OrchCredentialStore` cmdlet; `Remove-OrchCredentialStore` exists only for deletion) |

Clarifications for the trickier cases:

- **Directory users have no password** — it is managed by the identity provider. The only
  secret on a user is the Unattended Robot password (`UR_Password`). `Copy-OrchUser`
  re-resolves the user in the destination directory and warns you to re-set it.
- **Robot accounts**: in modern Orchestrator robots are user-bound, so the `UR_Password`
  row above covers them. Classic robots are not handled.
- **Machine secrets**: a standard machine's `LicenseKey` is regenerated server-side
  (nothing to migrate); only a **confidential** machine's `ClientSecret` must be
  re-issued, with `Add-OrchMachineClientSecret`.

## Credential assets in depth

A credential asset stores a username plus a password, and has:

- a **ValueScope** of `Global` (one value for everyone) or `PerRobot` (a different
  username/password per user/machine). Expand the per-robot values with
  `Get-OrchAsset -ExpandUserValues` (per-robot assets are then emitted as
  `AssetUserValue` rows carrying `UserName` / `MachineName`).
- a **CredentialStore** reference that determines where the secret is physically kept
  (Orchestrator database, CyberArk, Azure Key Vault, …). List stores with
  `Get-OrchCredentialStore`.

### Re-setting secrets after a copy (CSV round-trip)

The convenient pattern for the **CSV-resettable** entities — credential assets and secret assets — is
to export a CSV on the **destination** tenant, type the secret into its column, and import it back.
The exact commands (`-ExportCredentialCsv` → `Set-OrchCredentialAsset`, and `-ExportCsv` →
`Set-OrchSecretAsset`) are in [CSV Export & Import](05-CsvExportImport.md). For a user's
Unattended-Robot password, use `Update-OrchUser -UR_Password`. Entities without a pipeline re-set
cmdlet (buckets, webhooks, credential stores) must be re-set in the web UI or recreated — see the
table above.

### Folder-copy caveat

Copying a folder (`copy … -Recurse`) **does** bring the credential assets across, but
**not** their passwords — the API has no endpoint that returns them, so there is nothing
to copy. Re-set the passwords on the destination with the CSV workflow above.

## See also

- [CSV Export & Import](05-CsvExportImport.md) — the export/import mechanics for re-setting asset
  secrets in bulk.
- [Migration & Copy Guide](50-MigrationGuide.md) — post-processing for entities that hold
  passwords, in the context of a full tenant migration.
- [Getting Started](00-GettingStarted.md#setting-permissions) — non-confidential app
  sign-in (required for many directory and credential operations).
