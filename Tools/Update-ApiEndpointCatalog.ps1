<#
.SYNOPSIS
    Regenerates UiPathOrch\Resources\ApiEndpoints.txt -- the known-endpoint catalog that
    backs Invoke-OrchApi's -Uri tab completion.

.DESCRIPTION
    Reads a folder of UiPath swagger / OpenAPI documents and flattens them into one
    tab-separated line per endpoint. The catalog is embedded in the module (see
    UiPathOrch.csproj) and parsed lazily on the first <Tab>; nothing here runs at
    module runtime.

    Column layout (see ApiEndpointCatalog.cs, which must stay in sync):

        base <TAB> path <TAB> methods <TAB> versions <TAB> summary

    base      O = Orchestrator   (relative to the drive's Orchestrator base URL -- no switch)
              I = Identity       (Invoke-OrchApi -Identity)
              P = Portal         (Invoke-OrchApi -Portal)
              S = other service  (tenant-root relative; the path carries its own
                                  /testmanager_ , /du_ , /aifabric_ ... prefix)
    path      endpoint path, placeholders left as {name}
    methods   comma-separated, uppercase, e.g. GET,POST
    versions  Orchestrator only: the Web API version range the path appears in
              across the vNN swagger docs, e.g. "11-20" or "18-20". Empty elsewhere.
    summary   short operation summary for the completion tooltip (may be empty)

    Orchestrator entries are the UNION of every vNN swagger doc, tagged with the
    first/last version the path appears in, so the completer can filter by the drive's
    ApiVersion (the api-supported-versions header) and never offer an endpoint the
    target tenant does not have.

    Portal has no swagger document, so its endpoints are harvested from the
    HttpRequestPortal / GetEnumerablePortal call sites in OrchAPISession.cs.

.PARAMETER SwaggerDir
    Folder holding the swagger corpus (the vNN\swagger.json, IS4 *\swagger.json, ... tree).

.PARAMETER SessionPath
    OrchAPISession.cs, used to harvest the Portal endpoints.

.PARAMETER OutFile
    Catalog to write. Defaults to UiPathOrch\Resources\ApiEndpoints.txt.

.EXAMPLE
    .\Tools\Update-ApiEndpointCatalog.ps1 -SwaggerDir 'C:\...\Downloads\swagger'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $SwaggerDir,

    [string] $SessionPath = (Join-Path $PSScriptRoot '..\UiPathOrch\OrchAPISession.cs'),

    [string] $OutFile = (Join-Path $PSScriptRoot '..\UiPathOrch\Resources\ApiEndpoints.txt')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Non-Orchestrator services are reached through the tenant root, so each swagger doc's
# paths must be re-rooted under the service prefix the gateway routes on. The prefixes
# come from each document's own `servers` entry (e.g. /{org}/{tenant}/testmanager_),
# except Document Understanding, whose Cloud doc is rooted at the framework API
# (module call sites use /du_/api/framework/... -- see OrchAPISession.GetDuProjects).
$ServiceSources = [ordered]@{
    'TestManagementHub2.0\V2\swagger.json'          = '/testmanager_'
    'TestManagementHub2.0\swagger.json'             = '/testmanager_'
    'DocumentUnderstandingCloud1.0\swagger.json'    = '/du_/api/framework'
    'OpenAPI definition v0\ai-deployer_v3.json'     = '/aifabric_/ai-deployer'
    'OpenAPI definition v0\ai-trainer_v3.json'      = '/aifabric_/ai-trainer'
    'OpenAPI definition v0\ai-pkgmanager_v3.json'   = '/aifabric_/ai-pkgmanager'
}

# Identity: every doc we have. The union is intentional -- Identity exposes no
# version header we could filter on, so an endpoint present in ANY doc is offered.
$IdentitySources = @(
    'IS4 Internal V1\swagger.json'
    'IS4 External V1\swagger.json'
    'IdentityServerV3.2.41\swagger.json'
    'IdentityServerV3.2.1\swagger.json'
    'IdentityServerV3.1.10\swagger.json.txt'
)

$HttpVerbs = @('get', 'put', 'post', 'delete', 'patch', 'head', 'options')

function Read-Swagger([string] $relative) {
    $full = Join-Path $SwaggerDir $relative
    if (-not (Test-Path -LiteralPath $full)) {
        Write-Warning "missing swagger doc, skipped: $relative"
        return $null
    }
    Get-Content -LiteralPath $full -Raw | ConvertFrom-Json
}

# Some UiPath swagger paths escape their placeholders as {{name}} (an artifact of the
# generator). Normalize to a single brace so the catalog has one placeholder spelling.
function Normalize-Path([string] $p) {
    $p = $p -replace '\{\{', '{' -replace '\}\}', '}'
    $p = $p.TrimEnd('/')
    if (-not $p.StartsWith('/')) { $p = '/' + $p }
    return $p
}

# key -> record. Records accumulate methods (union) and, for Orchestrator, the set of
# Web API versions the path was seen in.
$catalog = [ordered]@{}

function Add-Endpoint([string] $base, [string] $path, [string[]] $methods, [string] $summary, [Nullable[int]] $version) {
    $path = Normalize-Path $path
    $key = "$base`t$path"
    if (-not $catalog.Contains($key)) {
        $catalog[$key] = [pscustomobject]@{
            Base     = $base
            Path     = $path
            Methods  = [System.Collections.Generic.SortedSet[string]]::new()
            Versions = [System.Collections.Generic.SortedSet[int]]::new()
            Summary  = ''
        }
    }
    $rec = $catalog[$key]
    foreach ($m in $methods) { [void]$rec.Methods.Add($m.ToUpperInvariant()) }
    if ($null -ne $version) { [void]$rec.Versions.Add([int]$version) }
    # Prefer the first non-empty summary we see (docs are processed newest-first).
    if (-not $rec.Summary -and $summary) {
        $rec.Summary = ($summary -replace '\s+', ' ').Trim()
        if ($rec.Summary.Length -gt 90) { $rec.Summary = $rec.Summary.Substring(0, 89) + [char]0x2026 }
    }
}

function Import-SwaggerDoc($doc, [string] $base, [string] $prefix, [Nullable[int]] $version) {
    if ($null -eq $doc) { return }
    foreach ($p in $doc.paths.PSObject.Properties) {
        $methods = @()
        $summary = ''
        foreach ($op in $p.Value.PSObject.Properties) {
            if ($HttpVerbs -notcontains $op.Name.ToLowerInvariant()) { continue }
            $methods += $op.Name
            if (-not $summary) {
                $s = $op.Value.PSObject.Properties['summary']
                if ($s -and $s.Value) { $summary = [string]$s.Value }
                else {
                    $o = $op.Value.PSObject.Properties['operationId']
                    if ($o -and $o.Value) { $summary = [string]$o.Value }
                }
            }
        }
        if (-not $methods) { continue }
        Add-Endpoint -base $base -path ($prefix + $p.Name) -methods $methods -summary $summary -version $version
    }
}

# ---- Orchestrator: union of every vNN doc, newest first so summaries come from the newest ----
$orchDocs = Get-ChildItem -LiteralPath $SwaggerDir -Directory |
    Where-Object { $_.Name -match '^v(\d+)\.0$' } |
    Sort-Object { [int]($_.Name -replace '^v(\d+)\.0$', '$1') } -Descending

foreach ($d in $orchDocs) {
    $ver = [int]($d.Name -replace '^v(\d+)\.0$', '$1')
    $doc = Read-Swagger (Join-Path $d.Name 'swagger.json')
    Import-SwaggerDoc -doc $doc -base 'O' -prefix '' -version $ver
    Write-Verbose "Orchestrator v$ver : $(@($doc.paths.PSObject.Properties).Count) paths"
}

# ---- Identity ----
foreach ($rel in $IdentitySources) {
    Import-SwaggerDoc -doc (Read-Swagger $rel) -base 'I' -prefix '' -version $null
}

# ---- Other services (tenant-root relative, each under its own gateway prefix) ----
foreach ($rel in $ServiceSources.Keys) {
    Import-SwaggerDoc -doc (Read-Swagger $rel) -base 'S' -prefix $ServiceSources[$rel] -version $null
}

# ---- Portal: no swagger, harvest the call sites in OrchAPISession.cs ----
# Matches the first string / interpolated-string argument of the portal helper families.
# Interpolation holes ({partitionGlobalId}, {groupId}, ...) survive verbatim, which is
# exactly the placeholder spelling the catalog wants.
$portalRegex = [regex] @'
(?x)
(?: HttpRequestPortal | GetEnumerablePortal | GetEnumerableWithoutPagingPortal | HttpGetPortalWithJsonContentType )
(?: < [^>]+ > )? \s* \(
[^"\r\n]*?
\$? " (?<path> / [^"\r\n]* ) "
'@

$sessionText = Get-Content -LiteralPath $SessionPath -Raw
$portalCount = 0
foreach ($line in ($sessionText -split "`r?`n")) {
    if ($line.TrimStart().StartsWith('//')) { continue }   # commented-out call sites
    foreach ($m in $portalRegex.Matches($line)) {
        $path = $m.Groups['path'].Value
        # Drop the query string -- the catalog stores paths; the caller appends filters.
        $path = ($path -split '\?', 2)[0]
        if (-not $path -or $path -eq '/') { continue }
        # The portal helpers already prepend the portal base; a few call sites spell the
        # /portal_ prefix anyway. Strip it so every P entry is portal-base relative.
        $path = $path -replace '^/portal_', ''
        Add-Endpoint -base 'P' -path $path -methods @('GET') -summary '' -version $null
        $portalCount++
    }
}
Write-Verbose "Portal call sites harvested: $portalCount"

# ---- Emit ----
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('# UiPathOrch known-API-endpoint catalog. GENERATED -- DO NOT EDIT BY HAND.')
[void]$sb.AppendLine('# Regenerate: Tools\Update-ApiEndpointCatalog.ps1 -SwaggerDir <swagger corpus>')
[void]$sb.AppendLine('# base<TAB>path<TAB>methods<TAB>versions<TAB>summary')
[void]$sb.AppendLine('#   base: O=Orchestrator  I=Identity(-Identity)  P=Portal(-Portal)  S=service(tenant-root relative)')
[void]$sb.AppendLine('#   versions: Orchestrator Web API version range the path appears in (empty = not version-tagged)')

foreach ($rec in ($catalog.Values | Sort-Object Base, Path)) {
    $versions = ''
    if ($rec.Versions.Count -gt 0) {
        $min = ($rec.Versions | Select-Object -First 1)
        $max = ($rec.Versions | Select-Object -Last 1)
        $versions = if ($min -eq $max) { "$min" } else { "$min-$max" }
    }
    $methods = ($rec.Methods -join ',')
    [void]$sb.AppendLine("$($rec.Base)`t$($rec.Path)`t$methods`t$versions`t$($rec.Summary)")
}

$outDir = Split-Path -Parent $OutFile
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }
# LF + UTF-8 (no BOM): the parser is newline-agnostic, but a stable byte layout keeps diffs clean.
[System.IO.File]::WriteAllText($OutFile, ($sb.ToString() -replace "`r`n", "`n"), [System.Text.UTF8Encoding]::new($false))

$byBase = $catalog.Values | Group-Object Base | Sort-Object Name
Write-Host "Wrote $OutFile"
Write-Host ("  total {0} endpoints  ({1})" -f $catalog.Count, (($byBase | ForEach-Object { "$($_.Name)=$($_.Count)" }) -join '  '))
