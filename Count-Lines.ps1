$totalLines = 0
Get-ChildItem -Depth 2 -Filter *.cs | ForEach-Object {
    $lineCount = (Get-Content $_.FullName | Measure-Object -Line).Lines
    $totalLines += $lineCount
    Write-Output "$($_.FullName): $lineCount lines"
}
Write-Output "Total lines in all .cs files: $totalLines"
