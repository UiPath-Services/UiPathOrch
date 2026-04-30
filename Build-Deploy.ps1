# Build-Deploy.ps1
# Builds UiPathOrch.dll and deploys it with module files and Docs to the module directory.
# Does NOT deploy Get-Help XML files (use a separate script for those).
#
# Usage:
#   .\Build-Deploy.ps1           # Build and deploy
#   .\Build-Deploy.ps1 -BuildOnly    # Build only, no deploy
#   .\Build-Deploy.ps1 -DeployOnly   # Deploy only, no build

param(
    [switch]$BuildOnly,
    [switch]$DeployOnly
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$csprojPath  = Join-Path $projectRoot 'UiPathOrch\UiPathOrch.csproj'
$buildOutput = Join-Path $projectRoot 'UiPathOrch\bin\Release\net8.0'
$stagingDocs = Join-Path $projectRoot 'Staging\Docs'
$modulePath  = Join-Path $env:ProgramFiles 'PowerShell\7\Modules\UiPathOrch'

$dllName = 'UiPathOrch.dll'

# --- Build ---
if (-not $DeployOnly) {
    Write-Host '=== Building ===' -ForegroundColor Cyan
    dotnet build $csprojPath --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error 'Build failed.'
        return
    }

    $dll = Join-Path $buildOutput $dllName
    if (-not (Test-Path $dll)) {
        Write-Error "DLL not found: $dll"
        return
    }
    Write-Host "Built: $dll" -ForegroundColor Green
}

if ($BuildOnly) { return }

# --- Deploy ---
Write-Host '=== Deploying ===' -ForegroundColor Cyan

if (-not (Test-Path $modulePath)) {
    Write-Error "Module directory not found: $modulePath"
    return
}

# Deploy DLL
$srcDll = Join-Path $buildOutput $dllName
$dstDll = Join-Path $modulePath $dllName
Write-Host "  DLL: $srcDll -> $dstDll"
Copy-Item $srcDll $dstDll -Force

# Deploy module files (psd1, psm1, ps1xml, releasenotes.txt)
$stagingDir = Join-Path $projectRoot 'Staging'
$moduleFiles = @('UiPathOrch.psd1', 'UiPathOrch.psm1', 'UiPathOrch.Format.ps1xml', 'ReleaseNotes.md')
foreach ($fileName in $moduleFiles) {
    $src = Join-Path $stagingDir $fileName
    if (Test-Path $src) {
        $dst = Join-Path $modulePath $fileName
        Write-Host "  Mod: $src -> $dst"
        Copy-Item $src $dst -Force
    }
}

# Deploy Functions (ps1 files)
$stagingFunctions = Join-Path $stagingDir 'Functions'
$functionsDir = Join-Path $modulePath 'Functions'
if (-not (Test-Path $functionsDir)) {
    New-Item -ItemType Directory -Path $functionsDir | Out-Null
}
$funcFiles = Get-ChildItem $stagingFunctions -Filter '*.ps1' | Sort-Object Name
foreach ($func in $funcFiles) {
    $dst = Join-Path $functionsDir $func.Name
    Write-Host "  Func: $($func.FullName) -> $dst"
    Copy-Item $func.FullName $dst -Force
}

# Deploy Docs (01, 02, 03 .md files)
$docsDir = Join-Path $modulePath 'Docs'
if (-not (Test-Path $docsDir)) {
    New-Item -ItemType Directory -Path $docsDir | Out-Null
}

$docFiles = Get-ChildItem $stagingDocs -Filter '*.md' | Sort-Object Name
foreach ($doc in $docFiles) {
    $dst = Join-Path $docsDir $doc.Name
    Write-Host "  Doc: $($doc.FullName) -> $dst"
    Copy-Item $doc.FullName $dst -Force
}

Write-Host '=== Done ===' -ForegroundColor Green
Write-Host ''
Write-Host 'Reload the module with:' -ForegroundColor Yellow
Write-Host '  Import-Module UiPathOrch -Force'
