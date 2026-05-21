<#
.SYNOPSIS
    Reorders parameters in MAML XML <command:syntaxItem> so that
    Path, Recurse, Depth appear first, followed by the remaining parameters
    in their original order.

.DESCRIPTION
    PlatyPS exports parameters in alphabetical order within <command:syntaxItem>.
    This script reorders them so that Path, Recurse, Depth come first,
    matching the recommendation in the help documentation:
    "place -Path, -Recurse, and -Depth immediately after the cmdlet name."

    The <command:parameters> section (full parameter descriptions) is left unchanged.

.PARAMETER XmlPath
    Path to the MAML XML file to process.

.PARAMETER WhatIf
    Shows what changes would be made without actually modifying the file.

.EXAMPLE
    .\Reorder-SyntaxParameters.ps1 ..\Staging\en-US\UiPathOrch.dll-Help.xml
    .\Reorder-SyntaxParameters.ps1 ..\Staging\en-US\UiPathOrch-Help.xml
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory, Position = 0)]
    [string[]]$XmlPath
)

# Priority parameters in desired display order
$PriorityNames = @('Path', 'Recurse', 'Depth')

foreach ($path in $XmlPath) {
    $resolvedPath = Resolve-Path $path -ErrorAction Stop
    [xml]$xml = Get-Content $resolvedPath -Raw -Encoding utf8

    $ns = New-Object Xml.XmlNamespaceManager $xml.NameTable
    $ns.AddNamespace('command', 'http://schemas.microsoft.com/maml/dev/command/2004/10')
    $ns.AddNamespace('maml', 'http://schemas.microsoft.com/maml/2004/10')

    $reorderedCount = 0

    foreach ($syntaxItem in $xml.SelectNodes('//command:syntaxItem', $ns)) {
        $cmdName = $syntaxItem.SelectSingleNode('maml:name', $ns).'#text'
        $paramNodes = $syntaxItem.SelectNodes('command:parameter', $ns)
        $params = @(foreach ($p in $paramNodes) { $p })

        if ($params.Length -le 1) { continue }

        # Find priority parameters that exist in this syntaxItem
        $priorityParams = [System.Collections.Generic.List[System.Xml.XmlElement]]::new()
        foreach ($name in $PriorityNames) {
            $found = $params | Where-Object {
                $_.SelectSingleNode('maml:name', $ns).'#text' -eq $name
            }
            if ($found) { $priorityParams.Add($found) }
        }

        # Skip if no priority parameters found
        if ($priorityParams.Count -eq 0) { continue }

        # Partition non-priority parameters into positional vs named.
        # MAML stores per-set position in the 'position' attribute
        # ("0", "1", ..., or "named").
        $prioritySet = [System.Collections.Generic.HashSet[System.Xml.XmlElement]]::new()
        foreach ($pp in $priorityParams) { [void]$prioritySet.Add($pp) }

        $positionalEntries = [System.Collections.Generic.List[psobject]]::new()
        $namedEntries      = [System.Collections.Generic.List[psobject]]::new()
        $intHolder = 0
        foreach ($p in $params) {
            if ($prioritySet.Contains($p)) { continue }
            $paramName = $p.SelectSingleNode('maml:name', $ns).'#text'
            $posAttr   = $p.GetAttribute('position')
            if ([int]::TryParse($posAttr, [ref]$intHolder)) {
                $positionalEntries.Add([pscustomobject]@{
                    Node = $p; Name = $paramName; Position = $intHolder
                })
            } else {
                $namedEntries.Add([pscustomobject]@{
                    Node = $p; Name = $paramName
                })
            }
        }
        $sortedPositional = @($positionalEntries | Sort-Object Position, Name)
        $sortedNamed      = @($namedEntries      | Sort-Object Name)

        # Build canonical desired order: priority + positional + named
        $newOrder = [System.Collections.Generic.List[System.Xml.XmlElement]]::new()
        $newOrder.AddRange($priorityParams)
        foreach ($e in $sortedPositional) { $newOrder.Add($e.Node) }
        foreach ($e in $sortedNamed)      { $newOrder.Add($e.Node) }

        # Idempotent: skip if current order already matches desired.
        $alreadyOrdered = ($params.Length -eq $newOrder.Count)
        if ($alreadyOrdered) {
            for ($i = 0; $i -lt $params.Length; $i++) {
                if (-not [object]::ReferenceEquals($params[$i], $newOrder[$i])) {
                    $alreadyOrdered = $false; break
                }
            }
        }
        if ($alreadyOrdered) { continue }

        $nameList = ($priorityParams | ForEach-Object { $_.SelectSingleNode('maml:name', $ns).'#text' }) -join ', '

        if ($PSCmdlet.ShouldProcess($cmdName, "Reorder syntax (priority [$nameList] + positional + named)")) {
            # Remove all parameter nodes
            foreach ($p in $params) {
                [void]$syntaxItem.RemoveChild($p)
            }
            # Re-add in new order
            foreach ($p in $newOrder) {
                [void]$syntaxItem.AppendChild($p)
            }
            $reorderedCount++
        }
    }

    if ($reorderedCount -gt 0) {
        # Save with UTF-8 (with BOM) to match PlatyPS output
        $settings = [System.Xml.XmlWriterSettings]::new()
        $settings.Indent = $true
        $settings.IndentChars = '  '
        $settings.Encoding = [System.Text.UTF8Encoding]::new($true)
        $settings.OmitXmlDeclaration = $false

        $writer = [System.Xml.XmlWriter]::Create($resolvedPath.Path, $settings)
        try {
            $xml.Save($writer)
        }
        finally {
            $writer.Dispose()
        }
        Write-Host "$resolvedPath : $reorderedCount cmdlet(s) reordered" -ForegroundColor Green
    }
    else {
        Write-Host "$resolvedPath : no changes needed" -ForegroundColor DarkGray
    }
}
