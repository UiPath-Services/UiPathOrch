# Function to count the number of characters in a text file
function Get-CharacterCount {
    param (
        [string]$FilePath
    )

    # Check if the file exists
    if (-Not (Test-Path -Path $FilePath)) {
        Write-Host "File not found: $FilePath"
        return
    }

    # Read the contents of the file
    $content = Get-Content -Path $FilePath -Raw

    # Count the number of characters
    $charCount = $content.Length

    # Output the character count
    Write-Host "Character count: $charCount"
}

# Example usage
Get-CharacterCount -FilePath ./details.txt
