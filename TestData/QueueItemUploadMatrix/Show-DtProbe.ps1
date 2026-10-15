<#
Shows, for each 05_dt_probe_*.csv uploaded into the web queue, whether Dynamic typing was
applied ("007" stored as the number 7) or not ("007" kept as a string).

  .\Show-DtProbe.ps1 -Folder Orch1:\Development -Queue QiCsvWeb
#>
param(
    [Parameter(Mandatory)][string]$Folder,
    [Parameter(Mandatory)][string]$Queue
)

$q = Get-OrchQueue -Path $Folder -Name $Queue
Invoke-OrchApi -Path $Folder -Uri ("/odata/QueueItems?`$filter=QueueDefinitionId%20eq%20$($q.Id)%20and%20Status%20eq%20%27New%27" +
    "&`$orderby=Id%20asc&`$top=100&`$select=Id,CreationTime,SpecificData") |
    ForEach-Object {
        $dp = [System.Text.Json.JsonDocument]::Parse($_.SpecificData).RootElement.GetProperty('DynamicProperties')
        $v = $dp.GetProperty('Value')
        [pscustomobject]@{
            Trial         = $dp.GetProperty('CaseId').GetString()
            Uploaded      = $_.CreationTime
            Stored        = $v.GetRawText()
            DynamicTyping = $v.ValueKind -eq 'Number' ? 'applied' : 'NOT applied'
        }
    } | Format-Table -AutoSize
