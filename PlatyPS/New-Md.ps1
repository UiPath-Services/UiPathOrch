$OutputFolder = "new"

# Get all commands from the module
$allCommands = Get-Command -Module UiPathOrch | Select-Object -ExpandProperty Name

# Get existing .md file names (without extension)
$existingMd = Get-ChildItem -Path $OutputFolder -Filter "*.md" -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -ne "UiPathOrch.md" } |  # Exclude module page
    ForEach-Object { $_.BaseName }

# Filter to only new commands
$newCommands = $allCommands | Where-Object { $_ -notin $existingMd }

if ($newCommands) {
    $parameters = @{
        Command = $newCommands
        OutputFolder = $OutputFolder
        AlphabeticParamsOrder = $false
        ExcludeDontShow = $true
        Encoding = [System.Text.Encoding]::UTF8
    }
    New-MarkdownHelp @parameters
    Write-Host "Generated: $($newCommands -join ', ')" -ForegroundColor Green
} else {
    Write-Host "No new commands to document." -ForegroundColor Yellow
}

# Generate About help only if it does not exist
$aboutPath = Join-Path $OutputFolder "about_UiPathOrch.md"
if (-not (Test-Path $aboutPath)) {
    New-MarkdownAboutHelp -OutputFolder $OutputFolder -AboutName "UiPathOrch"
}
