# Build-Xml.ps1 - Help XML Build and Parameter Order Optimization
# Generates XML help files from Markdown using PlatyPS and
# reorders -Path, -Recurse, -Depth parameters to optimal positions

param(
    [switch]$SkipParameterReorder,  # Skip parameter reordering if specified
    [switch]$Verbose,               # Enable verbose output
    [string]$ExclusionFile = "excluded-cmdlets.txt"  # Path to exclusion file
)

# Global variable to store excluded cmdlets
$script:ExcludedCmdlets = @()

# Load excluded cmdlets from file
function Load-ExcludedCmdlets {
    param([string]$FilePath)
    
    $excluded = @()
    
    if (Test-Path $FilePath) {
        try {
            $content = Get-Content $FilePath -Encoding UTF8 -ErrorAction Stop
            foreach ($line in $content) {
                $line = $line.Trim()
                # Skip empty lines and comments
                if ($line -and -not $line.StartsWith("#")) {
                    $excluded += $line
                }
            }
            
            if ($excluded.Count -gt 0) {
                Write-Host "  📋 Loaded $($excluded.Count) excluded cmdlets from $FilePath" -ForegroundColor Cyan
                if ($Verbose) {
                    Write-Host "    Excluded: $($excluded -join ', ')" -ForegroundColor Gray
                }
            } else {
                Write-Host "  ℹ No cmdlets excluded (file is empty or contains only comments)" -ForegroundColor Gray
            }
        } catch {
            Write-Warning "Failed to load exclusion file $FilePath : $($_.Exception.Message)"
        }
    } else {
        Write-Host "  ℹ Exclusion file not found: $FilePath (all cmdlets will be processed)" -ForegroundColor Gray
    }
    
    return $excluded
}

# Check if a cmdlet should be excluded from parameter reordering
function Test-CmdletExcluded {
    param([string]$CmdletName)
    
    if (-not $CmdletName) {
        return $false
    }
    
    # Check exact match first
    if ($script:ExcludedCmdlets -contains $CmdletName) {
        return $true
    }
    
    # Check wildcard patterns
    foreach ($excluded in $script:ExcludedCmdlets) {
        if ($excluded -match '\*' -and $CmdletName -like $excluded) {
            return $true
        }
    }
    
    return $false
}

# Parameter reordering function definitions
function Get-ParameterPriority {
    param([System.Xml.XmlElement]$Parameter, [System.Xml.XmlNamespaceManager]$NsManager)
    
    $paramName = $Parameter.SelectSingleNode(".//maml:name", $NsManager)?.InnerText
    
    if (-not $paramName) {
        return 999  # Unknown parameters go last
    }
    
    switch ($paramName) {
        "Path" { return 1 }
        "Recurse" { return 2 }
        "Depth" { return 3 }
        default { return 100 }
    }
}

function Reorder-SyntaxParameters {
    param(
        [System.Xml.XmlElement]$SyntaxItem,
        [System.Xml.XmlNamespaceManager]$NsManager,
        [string]$CmdletName
    )
    
    # Check if this cmdlet should be excluded
    if (Test-CmdletExcluded -CmdletName $CmdletName) {
        return $false
    }
    
    # Get all parameter nodes
    $parameterNodes = $SyntaxItem.SelectNodes(".//command:parameter", $NsManager)
    
    if ($parameterNodes.Count -eq 0) {
        return $false
    }
    
    # Check if any target parameters (Path, Recurse, Depth) exist
    $hasTargetParams = $false
    foreach ($param in $parameterNodes) {
        $paramName = $param.SelectSingleNode(".//maml:name", $NsManager)?.InnerText
        if ($paramName -in @("Path", "Recurse", "Depth")) {
            $hasTargetParams = $true
            break
        }
    }
    
    if (-not $hasTargetParams) {
        return $false
    }
    
    # Convert NodeList to Array to avoid live collection issues
    $parameterArray = @()
    foreach ($param in $parameterNodes) {
        $parameterArray += $param
    }
    
    # Sort parameters by priority
    $sortedParameters = $parameterArray | Sort-Object { Get-ParameterPriority $_ $NsManager }
    
    # Get parent node of the original parameter nodes
    $parentNode = $parameterArray[0].ParentNode
    
    # Remove all existing parameter nodes (reverse order to avoid index issues)
    for ($j = $parameterArray.Count - 1; $j -ge 0; $j--) {
        $parentNode.RemoveChild($parameterArray[$j]) | Out-Null
    }
    
    # Add parameter nodes in sorted order
    foreach ($param in $sortedParameters) {
        $parentNode.AppendChild($param) | Out-Null
    }
    return $true
}

function Optimize-XmlParameterOrder {
    param([string]$XmlFilePath)
    
    if (-not (Test-Path $XmlFilePath)) {
        Write-Warning "XML file not found: $XmlFilePath"
        return $false
    }
    
    try {
        Write-Host "    🔧 Optimizing parameter order..." -ForegroundColor Cyan
        
        # Load XML file
        [xml]$xml = Get-Content $XmlFilePath -Encoding UTF8
        
        # Setup namespace manager
        $nsManager = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $nsManager.AddNamespace("maml", "http://schemas.microsoft.com/maml/2004/10")
        $nsManager.AddNamespace("command", "http://schemas.microsoft.com/maml/dev/command/2004/10")
        
        # Get all command elements
        $commands = $xml.SelectNodes("//command:command", $nsManager)
        $totalModified = 0
        $totalSkipped = 0
        
        foreach ($command in $commands) {
            $cmdletName = $command.SelectSingleNode(".//command:details/command:name", $nsManager)?.InnerText
            
            # Process all syntaxItem elements
            $syntaxItems = $command.SelectNodes(".//command:syntax/command:syntaxItem", $nsManager)
            
            $cmdletProcessed = $false
            foreach ($syntaxItem in $syntaxItems) {
                if (Test-CmdletExcluded -CmdletName $cmdletName) {
                    if (-not $cmdletProcessed) {  # Only count once per cmdlet
                        $totalSkipped++
                        $cmdletProcessed = $true
                    }
                    continue
                }
                
                if (Reorder-SyntaxParameters -SyntaxItem $syntaxItem -NsManager $nsManager -CmdletName $cmdletName) {
                    $totalModified++
                    $cmdletProcessed = $true
                }
            }
        }
        
        # Save modified XML
        if ($totalModified -gt 0) {
            $xml.Save($XmlFilePath)
            Write-Host "    ✅ Optimized $totalModified syntax items" -ForegroundColor Green
            if ($totalSkipped -gt 0) {
                Write-Host "    ⏭ Skipped $totalSkipped excluded cmdlets" -ForegroundColor Yellow
            }
        } else {
            Write-Host "    ℹ No optimization needed" -ForegroundColor Gray
            if ($totalSkipped -gt 0) {
                Write-Host "    ⏭ $totalSkipped cmdlets were excluded from processing" -ForegroundColor Yellow
            }
        }
        
        return $true
        
    } catch {
        Write-Error "Error optimizing XML parameter order: $($_.Exception.Message)"
        return $false
    }
}

function Build-HelpXml {
    param(
        [string]$SourcePath,
        [string]$OutputPath,
        [string]$Language,
        [string[]]$CopyDestinations
    )
    
    Write-Host "🚀 Building XML help for $Language..." -ForegroundColor Yellow
    Write-Host "  📂 Source: $SourcePath" -ForegroundColor Gray
    Write-Host "  📁 Output: $OutputPath" -ForegroundColor Gray
    
    try {
        # Generate XML help using PlatyPS
        Write-Host "  📖 Generating XML help files..." -ForegroundColor Cyan
        New-ExternalHelp $SourcePath -OutputPath $OutputPath -Force
        
        if (-not $SkipParameterReorder) {
            # Load excluded cmdlets list
            $script:ExcludedCmdlets = Load-ExcludedCmdlets -FilePath $ExclusionFile
            
            # Optimize parameter order for generated XML files
            $xmlFiles = Get-ChildItem -Path $OutputPath -Filter "*.xml"
            foreach ($xmlFile in $xmlFiles) {
                Optimize-XmlParameterOrder -XmlFilePath $xmlFile.FullName
            }
        } else {
            Write-Host "  ⏭ Parameter reordering skipped" -ForegroundColor Yellow
        }
        
        # Copy files to specified destinations
        foreach ($destination in $CopyDestinations) {
            Write-Host "  📋 Copying to: $destination" -ForegroundColor Cyan
            
            # Create directory if it doesn't exist
            if (-not (Test-Path $destination)) {
                New-Item -Path $destination -ItemType Directory -Force | Out-Null
                Write-Host "    📁 Created directory: $destination" -ForegroundColor Green
            }
            
            try {
                Copy-Item "$OutputPath\*" $destination -Force
                Write-Host "    ✅ Copy completed" -ForegroundColor Green
                
                # Remove any .backup files created during the process
                $backupFiles = Get-ChildItem -Path $destination -Filter "*.backup" -ErrorAction SilentlyContinue
                if ($backupFiles) {
                    foreach ($backupFile in $backupFiles) {
                        Remove-Item -Path $backupFile.FullName -Force -ErrorAction SilentlyContinue
                        Write-Host "    🗑  Removed backup file: $($backupFile.Name)" -ForegroundColor DarkGray
                    }
                }
            } catch {
                Write-Warning "Failed to copy to $destination : $($_.Exception.Message)"
            }
        }
        
        Write-Host "  🎉 $Language build completed successfully!" -ForegroundColor Green
        return $true
        
    } catch {
        Write-Error "Failed to build $Language help: $($_.Exception.Message)"
        return $false
    }
}

# Main processing begins
Write-Host "===========================================" -ForegroundColor Magenta
Write-Host "     PowerShell Help XML Builder v2.1      " -ForegroundColor Magenta  
Write-Host "===========================================" -ForegroundColor Magenta
Write-Host "Prerequisites:" -ForegroundColor Yellow
Write-Host "  ⚠  Please run ./Update-Md.ps1 before executing this script" -ForegroundColor Red
Write-Host ""
Write-Host "Features:" -ForegroundColor White
Write-Host "  • PlatyPS XML generation" -ForegroundColor Cyan
Write-Host "  • Parameter order optimization" -ForegroundColor Cyan
Write-Host "  • Cmdlet exclusion support" -ForegroundColor Cyan
Write-Host "  • Automatic deployment" -ForegroundColor Cyan
Write-Host ""

$startTime = Get-Date

# Build English help
$success1 = Build-HelpXml -SourcePath "md-US" -OutputPath "xml-US" -Language "English (US)" -CopyDestinations @(
    "C:\Program Files\PowerShell\7\Modules\UiPathOrch\en-US",
    "C:\MyProj\OrchProvider\OrchProvider\bin\Debug\net8.0\en-US"
)

Write-Host ""

# Build Japanese help
$success2 = Build-HelpXml -SourcePath "md-JP" -OutputPath "xml-JP" -Language "Japanese (JP)" -CopyDestinations @(
    "C:\Program Files\PowerShell\7\Modules\UiPathOrch\ja-JP",
    "C:\MyProj\OrchProvider\OrchProvider\bin\Debug\net8.0\ja-JP"
)

# Calculate execution time
$endTime = Get-Date
$duration = $endTime - $startTime

# Display final results
Write-Host ""
Write-Host "===========================================" -ForegroundColor Magenta
Write-Host "              Build Summary               " -ForegroundColor Magenta
Write-Host "===========================================" -ForegroundColor Magenta

if ($success1 -and $success2) {
    Write-Host "🎉 All builds completed successfully!" -ForegroundColor Green
} elseif ($success1 -or $success2) {
    Write-Host "⚠ Some builds completed with warnings" -ForegroundColor Yellow
} else {
    Write-Host "❌ Build failed" -ForegroundColor Red
}

Write-Host ""
Write-Host "📊 Build Statistics:" -ForegroundColor White
Write-Host "  • English (US): $(if ($success1) { '✅ Success' } else { '❌ Failed' })" -ForegroundColor $(if ($success1) { 'Green' } else { 'Red' })
Write-Host "  • Japanese (JP): $(if ($success2) { '✅ Success' } else { '❌ Failed' })" -ForegroundColor $(if ($success2) { 'Green' } else { 'Red' })
Write-Host "  • Parameter optimization: $(if ($SkipParameterReorder) { '⏭ Skipped' } else { '✅ Applied' })" -ForegroundColor $(if ($SkipParameterReorder) { 'Yellow' } else { 'Green' })
Write-Host "  • Exclusion file: $(if (Test-Path $ExclusionFile) { "✅ $ExclusionFile" } else { "❌ Not found" })" -ForegroundColor $(if (Test-Path $ExclusionFile) { 'Green' } else { 'Red' })
Write-Host "  • Total duration: $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Cyan

Write-Host ""
Write-Host "📝 Usage Tips:" -ForegroundColor Yellow
Write-Host "  • Test with: Get-Help Get-OrchBucket -Full" -ForegroundColor Gray
Write-Host "  • Skip optimization: .\Build-Xml.ps1 -SkipParameterReorder" -ForegroundColor Gray
Write-Host "  • Verbose output: .\Build-Xml.ps1 -Verbose" -ForegroundColor Gray
Write-Host "  • Custom exclusion file: .\Build-Xml.ps1 -ExclusionFile 'custom.txt'" -ForegroundColor Gray
Write-Host "  • Edit exclusions: notepad excluded-cmdlets.txt" -ForegroundColor Gray

if ($Verbose) {
    Write-Host ""
    Write-Host "🔍 Generated Files:" -ForegroundColor Yellow
    
    @("xml-US", "xml-JP") | ForEach-Object {
        if (Test-Path $_) {
            $files = Get-ChildItem -Path $_ -Filter "*.xml"
            Write-Host "  📁 $_/:" -ForegroundColor Cyan
            foreach ($file in $files) {
                Write-Host "    📄 $($file.Name) ($($file.Length) bytes)" -ForegroundColor Gray
            }
        }
    }
    
    # Show exclusion file content if verbose
    if (Test-Path $ExclusionFile) {
        Write-Host ""
        Write-Host "🚫 Exclusion File Content:" -ForegroundColor Yellow
        $content = Get-Content $ExclusionFile -Encoding UTF8
        $activeCmdlets = $content | Where-Object { $_ -and -not $_.StartsWith("#") }
        if ($activeCmdlets) {
            foreach ($cmdlet in $activeCmdlets) {
                Write-Host "    • $cmdlet" -ForegroundColor Red
            }
        } else {
            Write-Host "    (No active exclusions)" -ForegroundColor Gray
        }
    }
}

Write-Host ""

