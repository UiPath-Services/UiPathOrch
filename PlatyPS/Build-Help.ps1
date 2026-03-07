<#
.SYNOPSIS
    Builds MAML XML help files from PlatyPS v1 markdown and deploys to the module directory.

.DESCRIPTION
    This script performs the full help build pipeline:
      1. Import PlatyPS v1 markdown files (.md) into CommandHelp objects
      2. Export MAML XML help files from the CommandHelp objects
      3. Reorder syntax parameters (Path, Recurse, Depth first)
      4. Deploy XML files to the module installation directory

    The script handles two separate help files:
      - UiPath.PowerShell.OrchProvider.dll-Help.xml (C# cmdlets, ~230 cmdlets)
      - UiPathOrch-Help.xml (PS1 function cmdlets, 7 functions)

.PARAMETER DeployPath
    The module installation directory. Defaults to the installed UiPathOrch module path.

.PARAMETER SkipDeploy
    Builds and reorders XML but does not copy to the module directory.

.PARAMETER SkipReorder
    Skips the parameter reorder step.

.EXAMPLE
    .\Build-Help.ps1
    Full build + reorder + deploy.

.EXAMPLE
    .\Build-Help.ps1 -SkipDeploy
    Build and reorder only (does not deploy).

.EXAMPLE
    .\Build-Help.ps1 -WhatIf
    Shows what would be done without making changes.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$DeployPath = 'C:\Program Files\PowerShell\7\Modules\UiPathOrch',

    [switch]$SkipDeploy,

    [switch]$SkipReorder
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir  = $PSScriptRoot
$mdDir      = Join-Path $scriptDir 'v1-US\UiPathOrch'
$stagingDir = Join-Path $scriptDir '..\Staging\en-US'
$enUSDir    = Join-Path $DeployPath 'en-US'

# --- Validate prerequisites ---

if (-not (Get-Module Microsoft.PowerShell.PlatyPS -ListAvailable)) {
    throw 'PlatyPS v1 (Microsoft.PowerShell.PlatyPS) is not installed. Run: Install-PSResource Microsoft.PowerShell.PlatyPS'
}
Import-Module Microsoft.PowerShell.PlatyPS -ErrorAction Stop

if (-not (Test-Path $mdDir)) {
    throw "Markdown directory not found: $mdDir"
}
if (-not (Test-Path $stagingDir)) {
    New-Item -Path $stagingDir -ItemType Directory -Force | Out-Null
}

# --- Step 1 & 2: Import markdown and export MAML XML ---

Write-Host '=== Step 1: Import markdown files ===' -ForegroundColor Cyan
$helpObjects = Import-MarkdownCommandHelp -Path (Join-Path $mdDir '*.md')
Write-Host "  Imported $($helpObjects.Count) command help objects" -ForegroundColor Green

Write-Host "`n=== Step 2: Export MAML XML ===" -ForegroundColor Cyan

# Export to a temp directory (Export-MamlCommandHelp creates a module-name subdirectory)
$tempOutDir = Join-Path $stagingDir 'temp_export'
if (Test-Path $tempOutDir) { Remove-Item $tempOutDir -Recurse -Force }
New-Item -Path $tempOutDir -ItemType Directory -Force | Out-Null

# PlatyPS automatically splits into separate XML files based on 'external help file' metadata
$helpObjects | Export-MamlCommandHelp -OutputFolder $tempOutDir -Force

# Move generated XML files from subdirectory to staging
$generatedFiles = Get-ChildItem $tempOutDir -Filter '*.xml' -Recurse
foreach ($xmlFile in $generatedFiles) {
    $destXml = Join-Path $stagingDir $xmlFile.Name
    Copy-Item $xmlFile.FullName $destXml -Force
    Write-Host "  $($xmlFile.Name) ($($xmlFile.Length) bytes)" -ForegroundColor Green
}

Remove-Item $tempOutDir -Recurse -Force

# --- Step 3: Reorder syntax parameters ---

if (-not $SkipReorder) {
    Write-Host "`n=== Step 3: Reorder syntax parameters (Path, Recurse, Depth first) ===" -ForegroundColor Cyan
    $reorderScript = Join-Path $scriptDir 'Reorder-SyntaxParameters.ps1'
    if (-not (Test-Path $reorderScript)) {
        Write-Warning "Reorder script not found: $reorderScript (skipping)"
    }
    else {
        $xmlFiles = Get-ChildItem $stagingDir -Filter '*.xml'
        foreach ($xmlFile in $xmlFiles) {
            & $reorderScript $xmlFile.FullName
        }
    }
}
else {
    Write-Host "`n=== Step 3: Skipped (parameter reorder) ===" -ForegroundColor DarkGray
}

# --- Step 4: Deploy to module directory ---

if (-not $SkipDeploy) {
    Write-Host "`n=== Step 4: Deploy to $enUSDir ===" -ForegroundColor Cyan
    if (-not (Test-Path $enUSDir)) {
        throw "Deploy target directory not found: $enUSDir"
    }

    $xmlFiles = Get-ChildItem $stagingDir -Filter '*.xml'
    foreach ($xmlFile in $xmlFiles) {
        $dest = Join-Path $enUSDir $xmlFile.Name
        if ($PSCmdlet.ShouldProcess($dest, "Copy $($xmlFile.Name)")) {
            Copy-Item $xmlFile.FullName $dest -Force
            Write-Host "  Deployed: $($xmlFile.Name) ($($xmlFile.Length) bytes)" -ForegroundColor Green
        }
    }

    # Reload module to pick up new help
    Write-Host "`n=== Step 5: Reload module ===" -ForegroundColor Cyan
    if ($PSCmdlet.ShouldProcess('UiPathOrch', 'Import-Module -Force')) {
        Import-Module UiPathOrch -Force
        Write-Host '  Module reloaded' -ForegroundColor Green
    }
}
else {
    Write-Host "`n=== Step 4: Skipped (deploy) ===" -ForegroundColor DarkGray
}

# --- Summary ---

Write-Host "`n=== Done ===" -ForegroundColor Cyan
$xmlFiles = Get-ChildItem $stagingDir -Filter '*.xml'
foreach ($xmlFile in $xmlFiles) {
    Write-Host "  $($xmlFile.Name): $($xmlFile.Length) bytes" -ForegroundColor White
}
