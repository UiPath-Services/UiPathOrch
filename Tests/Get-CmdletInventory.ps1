<#
.SYNOPSIS
    Counts the cmdlets UiPathOrch actually exposes, by reflection, and reconciles
    that against the module manifest. Use this instead of grepping for `[Cmdlet]`.

.DESCRIPTION
    A textual `[Cmdlet]` grep over the source over-counts: it also matches abstract
    base classes (RemoveFolderEntityCmdletBase, etc.) and any non-public cmdlet
    types, neither of which PowerShell registers. The only authoritative count is
    the set of types PowerShell would actually load: PUBLIC, concrete (non-abstract),
    derived from System.Management.Automation.Cmdlet, and carrying [CmdletAttribute].

    Reflection.Assembly.GetExportedTypes() returns ONLY public types, so non-public
    cmdlet classes drop out for free; we then filter to concrete Cmdlet subclasses.
    This matches what `Get-Command -Module UiPathOrch` would show, without importing
    the module (no auth, no first-run config, no drive mounts).

    The DLL is copied to a temp file before loading so the real build output is
    never locked (a loaded assembly cannot be unloaded from a PowerShell session).

    It then loads the manifest (Import-PowerShellDataFile) and reports:
      * the true cmdlet count, grouped by noun prefix (Orch / Pm / Du / Tm / Df / Cg)
      * the exported function count
      * DRIFT: cmdlets compiled-but-not-exported, and exported-but-missing.

.PARAMETER DllPath
    Path to UiPathOrch.dll. Default: the newest UiPathOrch.dll under the repo.

.PARAMETER ManifestPath
    Path to UiPathOrch.psd1. Default: Staging\UiPathOrch.psd1.

.EXAMPLE
    .\Tests\Get-CmdletInventory.ps1

.EXAMPLE
    .\Tests\Get-CmdletInventory.ps1 -DllPath .\UiPathOrch\bin\Release\net8.0\UiPathOrch.dll
#>
[CmdletBinding()]
param(
    [string]$DllPath,
    [string]$ManifestPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent

if (-not $DllPath) {
    $DllPath = Get-ChildItem -Path $repoRoot -Recurse -Filter 'UiPathOrch.dll' -File -ErrorAction SilentlyContinue |
        Where-Object FullName -notmatch '\\Tests\\' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $DllPath -or -not (Test-Path $DllPath)) {
    throw "UiPathOrch.dll not found. Build first (.\Build-Deploy.ps1 -BuildOnly) or pass -DllPath."
}
if (-not $ManifestPath) { $ManifestPath = Join-Path $repoRoot 'Staging\UiPathOrch.psd1' }

# Load a temp copy so the real build output is never locked by this session.
$tempDll = Join-Path ([IO.Path]::GetTempPath()) ("UiPathOrch.inventory.{0}.dll" -f [Guid]::NewGuid().ToString('N'))
Copy-Item $DllPath $tempDll -Force
$asm = [System.Reflection.Assembly]::LoadFrom($tempDll)

$cmdletBase = [System.Management.Automation.Cmdlet]

# GetExportedTypes() == public types only -> non-public [Cmdlet] classes are excluded here.
$registrable =
    $asm.GetExportedTypes() |
    Where-Object { -not $_.IsAbstract -and $cmdletBase.IsAssignableFrom($_) } |
    ForEach-Object {
        $attr = $_.GetCustomAttributes($true) |
            Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] } |
            Select-Object -First 1
        if ($attr) {
            [pscustomobject]@{
                Name  = "$($attr.VerbName)-$($attr.NounName)"
                Noun  = $attr.NounName
                Type  = $_.FullName
            }
        }
    } |
    Sort-Object Name

# For contrast: how many [Cmdlet] types exist INCLUDING non-public/abstract (the over-count).
$allCmdletAttrTypes =
    $asm.GetTypes() |
    Where-Object {
        $_.GetCustomAttributes($true) |
        Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
    }

# Noun-prefix grouping.
function Get-Prefix([string]$noun) {
    if ($noun -match '^(Orch|Pm|Du|Tm|Df|Cg)') { return $Matches[1] }
    return '(other)'
}
$byPrefix = $registrable |
    Group-Object { Get-Prefix $_.Noun } |
    Sort-Object Name |
    ForEach-Object { [pscustomobject]@{ Prefix = $_.Name; Count = $_.Count } }

# Reconcile against the manifest.
$manifest   = Import-PowerShellDataFile -Path $ManifestPath
$exported    = @($manifest.CmdletsToExport)   | Where-Object { $_ }
$funcs       = @($manifest.FunctionsToExport) | Where-Object { $_ }
$reflectedNames = $registrable.Name

$compiledNotExported = $reflectedNames | Where-Object { $_ -notin $exported } | Sort-Object
$exportedNotCompiled = $exported       | Where-Object { $_ -notin $reflectedNames } | Sort-Object

Remove-Item $tempDll -Force -ErrorAction SilentlyContinue

# ---- Report ----
Write-Host ""
Write-Host "UiPathOrch cmdlet inventory" -ForegroundColor Cyan
Write-Host ("  DLL:      {0}" -f $DllPath)
Write-Host ("  Manifest: {0}" -f $ManifestPath)
Write-Host ""
Write-Host ("  Registrable cmdlets (public, concrete, [Cmdlet]): {0}" -f $registrable.Count) -ForegroundColor Green
Write-Host ("  Exported functions (manifest):                    {0}" -f $funcs.Count) -ForegroundColor Green
Write-Host ("  TOTAL user-facing commands:                       {0}" -f ($registrable.Count + $funcs.Count)) -ForegroundColor Green
Write-Host ""
Write-Host ("  (For contrast, raw [Cmdlet] types incl. non-public/abstract: {0})" -f $allCmdletAttrTypes.Count) -ForegroundColor DarkGray
Write-Host ""
Write-Host "  By noun prefix:" -ForegroundColor Cyan
$byPrefix | Format-Table -AutoSize | Out-String | Write-Host

if ($compiledNotExported) {
    Write-Host ("  DRIFT - compiled but NOT in manifest CmdletsToExport ({0}):" -f $compiledNotExported.Count) -ForegroundColor Yellow
    $compiledNotExported | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
    Write-Host ""
}
if ($exportedNotCompiled) {
    Write-Host ("  DRIFT - exported in manifest but NO matching compiled cmdlet ({0}):" -f $exportedNotCompiled.Count) -ForegroundColor Red
    $exportedNotCompiled | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    Write-Host ""
}
if (-not $compiledNotExported -and -not $exportedNotCompiled) {
    Write-Host "  Manifest exports match compiled cmdlets exactly. No drift." -ForegroundColor Green
    Write-Host ""
}

# Emit the inventory object for programmatic use / piping.
[pscustomobject]@{
    CmdletCount        = $registrable.Count
    FunctionCount      = $funcs.Count
    TotalCommands      = $registrable.Count + $funcs.Count
    RawCmdletAttrTypes = $allCmdletAttrTypes.Count
    ByPrefix           = $byPrefix
    CompiledNotExported = $compiledNotExported
    ExportedNotCompiled = $exportedNotCompiled
    Cmdlets            = $reflectedNames
}
