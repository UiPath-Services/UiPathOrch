<#
Compares the items the web "Upload Items" created in one queue with the items
Import-OrchQueueItem created in another, from the same CSV file.

  .\Compare-Upload.ps1 -Folder Orch1:\Development -WebQueue QiCsvWeb -CmdQueue QiCsvCmd
  .\Compare-Upload.ps1 -Folder Orch1:\Development -WebQueue QiCsvWeb -CmdQueue QiCsvCmd -Clear
  .\Compare-Upload.ps1 -Folder Orch1:\Development -WebQueue QiCsvWeb -CmdQueue QiCsvCmd -CsvFile .\01_main.csv -Clear

-CsvFile first runs Import-OrchQueueItem for that file into CmdQueue and prints what the
cmdlet returned (success, rejected rows, parse errors), then compares.

Items are paired by the CaseId SpecificContent key when every item has a unique one,
otherwise by creation order. Compared: Reference, Priority, DeferDate, DueDate,
RiskSlaDate, Progress, the SpecificContent keys/values, and the raw SpecificData JSON
(key order included). -Clear deletes the New items of both queues afterwards, so the
next file starts from empty queues.
#>
param(
    [Parameter(Mandatory)][string]$Folder,
    [Parameter(Mandatory)][string]$WebQueue,
    [Parameter(Mandatory)][string]$CmdQueue,
    [string]$CsvFile,
    [string]$CommitType = 'ProcessAllIndependently',
    [switch]$DynamicTyping,
    [switch]$Clear
)

$ErrorActionPreference = 'Stop'

if ($CsvFile) {
    "--- Import-OrchQueueItem $(Split-Path $CsvFile -Leaf) ---"
    $out = Import-OrchQueueItem -Path $Folder -Name $CmdQueue -ImportCsv (Resolve-Path $CsvFile).Path -CommitType $CommitType -DynamicTyping:$DynamicTyping -Confirm:$false -ErrorAction Continue -ErrorVariable importErr 2>$null
    $out | Format-List | Out-String -Width 250
    $importErr | ForEach-Object { "ERROR: $($_.Exception.Message)" }
    Start-Sleep -Seconds 2
}

function Get-Items([string]$queueName) {
    $q = Get-OrchQueue -Path $Folder -Name $queueName
    if (-not $q) { throw "Queue '$queueName' not found in $Folder" }
    $all = [System.Collections.Generic.List[object]]::new()
    $skip = 0
    do {
        $page = @(Invoke-OrchApi -Path $Folder -Uri ("/odata/QueueItems?`$filter=QueueDefinitionId%20eq%20$($q.Id)%20and%20Status%20eq%20%27New%27" +
            "&`$orderby=Id%20asc&`$top=100&`$skip=$skip&`$select=Id,Reference,Priority,DeferDate,DueDate,RiskSlaDate,Progress,SpecificData"))
        $page | ForEach-Object { $all.Add($_) }
        $skip += $page.Count
    } while ($page.Count -eq 100)
    $all
}

# SpecificData is {"DynamicProperties":{...}}; return the inner object's properties in order.
function Get-Props($item) {
    if ([string]::IsNullOrEmpty($item.SpecificData)) { return [ordered]@{} }
    $doc = [System.Text.Json.JsonDocument]::Parse($item.SpecificData)
    $root = $doc.RootElement
    $dp = $root
    foreach ($p in $root.EnumerateObject()) { if ($p.Name -ceq 'DynamicProperties') { $dp = $p.Value } }
    $h = [ordered]@{}
    foreach ($p in $dp.EnumerateObject()) { $h[$p.Name] = $p.Value.GetRawText() }
    $h
}

function Get-Key($props, $index) { if ($props.Contains('CaseId')) { $props['CaseId'] } else { "#$index" } }

$web = @(Get-Items $WebQueue)
$cmd = @(Get-Items $CmdQueue)
$webP = @($web | ForEach-Object { Get-Props $_ })
$cmdP = @($cmd | ForEach-Object { Get-Props $_ })

$byCase = ($webP.Count -gt 0) -and
    @($webP | Where-Object { -not $_.Contains('CaseId') }).Count -eq 0 -and
    @($cmdP | Where-Object { -not $_.Contains('CaseId') }).Count -eq 0 -and
    @($webP | ForEach-Object { $_['CaseId'] } | Group-Object | Where-Object Count -gt 1).Count -eq 0 -and
    @($cmdP | ForEach-Object { $_['CaseId'] } | Group-Object | Where-Object Count -gt 1).Count -eq 0

$pairs = [System.Collections.Generic.List[object]]::new()
if ($byCase) {
    $cmdIndex = @{}; for ($i = 0; $i -lt $cmd.Count; $i++) { $cmdIndex[$cmdP[$i]['CaseId']] = $i }
    $seen = @{}
    for ($i = 0; $i -lt $web.Count; $i++) {
        $k = $webP[$i]['CaseId']; $j = $cmdIndex[$k]; $seen[$k] = $true
        $pairs.Add([pscustomobject]@{ Key = $k; W = $web[$i]; WP = $webP[$i]; C = ($j -ne $null ? $cmd[$j] : $null); CP = ($j -ne $null ? $cmdP[$j] : $null) })
    }
    for ($j = 0; $j -lt $cmd.Count; $j++) {
        if (-not $seen.ContainsKey($cmdP[$j]['CaseId'])) { $pairs.Add([pscustomobject]@{ Key = $cmdP[$j]['CaseId']; W = $null; WP = $null; C = $cmd[$j]; CP = $cmdP[$j] }) }
    }
} else {
    $n = [Math]::Max($web.Count, $cmd.Count)
    for ($i = 0; $i -lt $n; $i++) {
        $pairs.Add([pscustomobject]@{ Key = "#$i"; W = $web[$i]; WP = $webP[$i]; C = $cmd[$i]; CP = $cmdP[$i] })
    }
}

$diffs = foreach ($p in $pairs) {
    if (-not $p.W) { [pscustomobject]@{ Key = $p.Key; Field = '(item)'; Web = '<missing>'; Cmdlet = 'present' }; continue }
    if (-not $p.C) { [pscustomobject]@{ Key = $p.Key; Field = '(item)'; Web = 'present'; Cmdlet = '<missing>' }; continue }
    foreach ($f in 'Reference', 'Priority', 'DeferDate', 'DueDate', 'RiskSlaDate', 'Progress') {
        if ("$($p.W.$f)" -cne "$($p.C.$f)") { [pscustomobject]@{ Key = $p.Key; Field = $f; Web = $p.W.$f; Cmdlet = $p.C.$f } }
    }
    $keys = @($p.WP.Keys) + @($p.CP.Keys) | Select-Object -Unique
    foreach ($k in $keys) {
        $wv = $p.WP.Contains($k) ? $p.WP[$k] : '<absent>'
        $cv = $p.CP.Contains($k) ? $p.CP[$k] : '<absent>'
        if ($wv -cne $cv) { [pscustomobject]@{ Key = $p.Key; Field = "SC[$k]"; Web = $wv; Cmdlet = $cv } }
    }
    if ((@($p.WP.Keys) -join "`0") -cne (@($p.CP.Keys) -join "`0")) {
        [pscustomobject]@{ Key = $p.Key; Field = 'SC key order'; Web = (@($p.WP.Keys) -join ', '); Cmdlet = (@($p.CP.Keys) -join ', ') }
    }
}

"Web: $($web.Count) items   Cmdlet: $($cmd.Count) items   Paired by: $($byCase ? 'CaseId' : 'creation order')"
if ($diffs) { $diffs | Format-Table -Wrap -AutoSize | Out-String -Width 250 } else { 'IDENTICAL' }

if ($Clear) {
    foreach ($qn in $WebQueue, $CmdQueue) {
        Clear-OrchCache -Path $Folder
        Get-OrchQueueItem -Path $Folder -Name $qn -Status New | Remove-OrchQueueItem -Confirm:$false | Out-Null
    }
    'Cleared New items of both queues.'
}
