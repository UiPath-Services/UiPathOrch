<#
.SYNOPSIS
    Creates markdown for new cmdlets and validates existing markdown against cmdlet metadata.

.DESCRIPTION
    This script performs the following steps:

      1. Reload the UiPathOrch module to pick up the latest compiled DLL
      2. Create new markdown files for cmdlets that don't have one yet
      3. Validate existing markdown files against cmdlet parameter metadata

    Step 3 reports mismatches (missing/extra parameters, type differences) without
    modifying existing files. Update-MarkdownCommandHelp is NOT used because it
    destructively rewrites parameter order, RELATED LINKS, and duplicates descriptions.

    Typical workflow:
      1. Build and deploy the DLL:  .\Build-Deploy.ps1
      2. Update/validate markdown:  .\Update-Help.ps1
      3. Fix any reported issues manually
      4. Build MAML XML:            .\Build-Help.ps1

.EXAMPLE
    .\Update-Help.ps1
    Reloads the module, creates markdown for new cmdlets, and validates all files.

.EXAMPLE
    .\Update-Help.ps1 -WhatIf
    Shows what would be created without making changes. Validation still runs.
#>
[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'

$scriptDir = $PSScriptRoot
$mdDir     = Join-Path $scriptDir '..\docs\help\en-US'

# --- Validate prerequisites ---

if (-not (Get-Module Microsoft.PowerShell.PlatyPS -ListAvailable)) {
    throw 'PlatyPS v1 (Microsoft.PowerShell.PlatyPS) is not installed. Run: Install-PSResource Microsoft.PowerShell.PlatyPS'
}
Import-Module Microsoft.PowerShell.PlatyPS -ErrorAction Stop

if (-not (Test-Path $mdDir)) {
    throw "Markdown directory not found: $mdDir"
}

# --- Step 1: Reload UiPathOrch module ---

Write-Host '=== Step 1: Reload UiPathOrch module ===' -ForegroundColor Cyan
if ($PSCmdlet.ShouldProcess('UiPathOrch', 'Import-Module -Force')) {
    Import-Module UiPathOrch -Force
    $mod = Get-Module UiPathOrch
    Write-Host "  Loaded: $($mod.Name) v$($mod.Version) ($($mod.ExportedCmdlets.Count + $mod.ExportedFunctions.Count) commands)" -ForegroundColor Green
}

# --- Step 2: Create markdown for new cmdlets ---

Write-Host "`n=== Step 2: Create markdown for new cmdlets ===" -ForegroundColor Cyan

$existingFiles = Get-ChildItem $mdDir -Filter '*.md' | ForEach-Object { $_.BaseName }
$allCommands = @()
$allCommands += (Get-Module UiPathOrch).ExportedCmdlets.Keys
$allCommands += (Get-Module UiPathOrch).ExportedFunctions.Keys

$newCommands = $allCommands | Where-Object { $_ -notin $existingFiles }

if ($newCommands) {
    # New-MarkdownCommandHelp creates a module-name subfolder (e.g., UiPathOrch/) under -OutputFolder.
    # Use a temp folder, then move files to the correct location.
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "PlatyPS_$(Get-Random)"
    foreach ($cmdName in $newCommands) {
        if ($PSCmdlet.ShouldProcess($cmdName, 'New-MarkdownCommandHelp')) {
            Get-Command $cmdName -Module UiPathOrch | New-MarkdownCommandHelp -OutputFolder $tempDir
            $generated = Get-ChildItem $tempDir -Filter "$cmdName.md" -Recurse
            if ($generated) {
                Move-Item $generated.FullName $mdDir -Force
                Write-Host "  Created: $cmdName.md" -ForegroundColor Green
            }
        }
    }
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}
else {
    Write-Host '  No new cmdlets found' -ForegroundColor DarkGray
}

# --- Step 3: Validate markdown against cmdlet metadata ---

Write-Host "`n=== Step 3: Validate markdown parameters ===" -ForegroundColor Cyan

$commonParams = [System.Management.Automation.Cmdlet]::CommonParameters +
                [System.Management.Automation.Cmdlet]::OptionalCommonParameters +
                @('WhatIf', 'Confirm')

# Normalize Nullable type names by stripping assembly-qualified inner type
function NormalizeTypeName([string]$typeName) {
    # System.Nullable`1[[System.Int32, System.Private.CoreLib, ...]] → System.Nullable`1[System.Int32]
    $typeName -replace '\[\[([^,\]]+)[^\]]*\]\]', '[$1]'
}

$issues = [System.Collections.Generic.List[PSCustomObject]]::new()
$files = Get-ChildItem $mdDir -Filter '*.md'

foreach ($f in $files) {
    $cmdName = $f.BaseName
    $cmd = Get-Command $cmdName -Module UiPathOrch -ErrorAction SilentlyContinue
    if (-not $cmd) {
        $issues.Add([PSCustomObject]@{ File = $f.Name; Issue = 'Cmdlet not found in module' })
        continue
    }

    $help = Import-MarkdownCommandHelp -Path $f.FullName

    $cmdParams = $cmd.Parameters.Keys | Where-Object { $_ -notin $commonParams } | Sort-Object
    $mdParams  = $help.Parameters.Name | Where-Object { $_ -notin @('WhatIf', 'Confirm') } | Sort-Object

    # A DontShow parameter is deliberately kept out of help and tab completion -- e.g. the
    # deprecated GroupName0..GroupName9 columns New-/Set-PmRobotAccount still accept so that CSVs
    # exported by older versions keep importing. PowerShell still lists them in .Parameters, so
    # they are treated as NEUTRAL: documenting one is fine, and not documenting one is fine.
    # Requiring them would demand help for exactly the parameters the cmdlet hides; forbidding them
    # would flag the ones that are documented today.
    $dontShow = $cmdParams | Where-Object {
        $cmd.Parameters[$_].Attributes |
            Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] -and $_.DontShow }
    }

    # Check for missing/extra parameters
    foreach ($p in $cmdParams) {
        if ($p -notin $mdParams -and $p -notin $dontShow) {
            $issues.Add([PSCustomObject]@{ File = $f.Name; Issue = "Missing param: $p" })
        }
    }
    foreach ($p in $mdParams) {
        if ($p -notin $cmdParams) {
            $issues.Add([PSCustomObject]@{ File = $f.Name; Issue = "Extra param: $p (not in cmdlet)" })
        }
    }

    # Check parameter types for matching params
    foreach ($p in ($cmdParams | Where-Object { $_ -in $mdParams })) {
        $cmdType = NormalizeTypeName $cmd.Parameters[$p].ParameterType.FullName
        $mdParam = $help.Parameters | Where-Object { $_.Name -eq $p }
        $mdType  = NormalizeTypeName $mdParam.Type
        if ($cmdType -ne $mdType) {
            $issues.Add([PSCustomObject]@{ File = $f.Name; Issue = "Type mismatch: -$p (cmdlet: $cmdType, md: $mdType)" })
        }
    }
}

if ($issues.Count -gt 0) {
    Write-Host "  $($issues.Count) issue(s) found:" -ForegroundColor Yellow
    $issues | Format-Table -AutoSize -Wrap
}
else {
    Write-Host "  All $($files.Count) files match their cmdlet specs" -ForegroundColor Green
}

# --- Summary ---

Write-Host "`n=== Done ===" -ForegroundColor Cyan
if ($newCommands) {
    Write-Host @"

Next steps:
  1. Fill in DESCRIPTION, EXAMPLES, NOTES, RELATED LINKS for new cmdlets
  2. Run .\Build-Help.ps1 to generate MAML XML and deploy
"@ -ForegroundColor Yellow
}
