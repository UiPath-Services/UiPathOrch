function Get-OrchTestDataQueueItemTable {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $false, ValueFromPipeline = $true)]
        [string]$Path,

        [Switch]$Recurse
    )

    # Invoke the cmdlet
    if ($Path) {
        $data = Get-OrchTestDataQueueItem -Path $Path -Recurse:$Recurse | Group-Object -Property Path
    } else {
        $data = Get-OrchTestDataQueueItem -Recurse:$Recurse | Group-Object -Property Path
    }

    # Process group
    foreach ($group in $data) {
        # Insert empty line before displaying the queue name
        Write-Host "`n   Test Data Queue: $($group.Name)"

        # Parse JSON in items and convert them to table data
        $tableData = $group.Group | ForEach-Object {
            $jsonData = $_.ContentJson | ConvertFrom-Json
            # PSObject Ç∆ÇµÇƒçƒç\ízÅAId Ç∆ IsConsumed Çç≈èâÇ…íuÇ≠
            $row = New-Object -TypeName PSObject
            $row | Add-Member -MemberType NoteProperty -Name 'Id' -Value $_.Id
            $row | Add-Member -MemberType NoteProperty -Name 'IsConsumed' -Value $_.IsConsumed
            foreach ($prop in $jsonData.PSObject.Properties) {
                $row | Add-Member -MemberType NoteProperty -Name $prop.Name -Value $prop.Value
            }
            $row
        }

        # Output in table format
        if ($tableData) {
            $propertiesNames = @('Id', 'IsConsumed') + ($tableData[0].PSObject.Properties | Where-Object {$_.Name -notin @('Id', 'IsConsumed')} | Select-Object -ExpandProperty Name)
            $tableData | Format-Table -Property $propertiesNames -AutoSize
        }
    }
}
