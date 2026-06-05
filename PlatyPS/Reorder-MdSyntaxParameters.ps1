<#
.SYNOPSIS
    Reorders parameters in the SYNTAX code blocks of PlatyPS markdown
    files so that Path, LiteralPath, Recurse, Depth appear first, positional
    parameters follow in position order, and remaining named parameters
    come last in alphabetical order.

.DESCRIPTION
    UiPathOrch's PlatyPS workflow reorders parameters in the built MAML
    XML (see Reorder-SyntaxParameters.ps1). This script applies an
    analogous reorder to the source .md files so the online-viewable
    docs (the help md files are exposed on GitHub) show the recommended
    invocation order.

    Ordering inside each SYNTAX code block:
      1. Path
      2. LiteralPath
      3. Recurse
      4. Depth
      5. Other positional parameters, by Position ascending (read from
         the PARAMETERS yaml of the same md file)
      5. Other named parameters, alphabetical
      6. [<CommonParameters>] (always last)

    Only fenced code blocks inside the SYNTAX section are touched. The
    body PARAMETERS section is left alone.

    A file that already has the expected order is skipped without
    rewriting (mtime preserved).

.PARAMETER MdPath
    One or more .md file paths, or directory paths. If a directory is
    given, all *.md files inside it are processed (non-recursive).

.PARAMETER MaxWidth
    Soft wrap column for the reformatted syntax line. Defaults to 90 to
    match PlatyPS-generated output.

.PARAMETER WhatIf
    Show what would change without writing.

.EXAMPLE
    .\Reorder-MdSyntaxParameters.ps1 ..\docs\help\en-US
    Process every md in the help directory.

.EXAMPLE
    .\Reorder-MdSyntaxParameters.ps1 ..\docs\help\en-US\Add-OrchBucketLink.md -WhatIf
    Preview the change for a single file.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory, Position = 0)]
    [string[]]$MdPath,

    [int]$MaxWidth = 90
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$PriorityNames = @('Path', 'LiteralPath', 'Recurse', 'Depth')

# ---------- token parser ----------

# Splits a syntax body (everything after the cmdlet name) into parameter
# tokens. Handles all PlatyPS shapes:
#   [-Name] <string[]>          mandatory positional with type
#   [[-Name] <string[]>]        optional positional with type
#   [-Path <string[]>]          optional named with type
#   -Name <string[]>            mandatory named with type
#   [-Recurse]                  optional switch
#   -Recurse                    mandatory switch
#   [<CommonParameters>]        special
# Bracket depth is tracked so that '[' and ']' inside type hints like
# <string[]> do not confuse the scanner.
function ConvertTo-SyntaxTokens([string]$text) {
    $tokens = New-Object System.Collections.Generic.List[string]
    $i = 0
    $n = $text.Length
    while ($i -lt $n) {
        while ($i -lt $n -and [char]::IsWhiteSpace($text[$i])) { $i++ }
        if ($i -ge $n) { break }
        $start = $i
        $ch = $text[$i]

        if ($ch -eq '[') {
            $depth = 1
            $i++
            while ($i -lt $n -and $depth -gt 0) {
                if ($text[$i] -eq '[') { $depth++ }
                elseif ($text[$i] -eq ']') { $depth-- }
                $i++
            }
            # Trailing '<type>' (mandatory-positional form: '[-Name] <type>').
            $j = $i
            while ($j -lt $n -and [char]::IsWhiteSpace($text[$j])) { $j++ }
            if ($j -lt $n -and $text[$j] -eq '<') {
                while ($j -lt $n -and $text[$j] -ne '>') { $j++ }
                if ($j -lt $n) { $j++ }
                $i = $j
            }
        }
        elseif ($ch -eq '-') {
            while ($i -lt $n -and -not [char]::IsWhiteSpace($text[$i])) { $i++ }
            $j = $i
            while ($j -lt $n -and [char]::IsWhiteSpace($text[$j])) { $j++ }
            if ($j -lt $n -and $text[$j] -eq '<') {
                while ($j -lt $n -and $text[$j] -ne '>') { $j++ }
                if ($j -lt $n) { $j++ }
                $i = $j
            }
        }
        else {
            return $null  # unexpected; caller will skip this block
        }

        $raw = $text.Substring($start, $i - $start)
        $tok = ($raw -replace '\s+', ' ').Trim()
        $tokens.Add($tok)
    }
    return ,$tokens.ToArray()
}

function Get-ParamName([string]$token) {
    # Accept any number of leading '[' so '[[-Name]' (optional positional)
    # also yields 'Name'.
    if ($token -match '^\[*-(?<n>[A-Za-z][A-Za-z0-9_]*)') {
        return $Matches.n
    }
    return $null
}

# Parse the PARAMETERS section yaml blocks to extract Position for each
# parameter. Returns a hashtable: paramName -> [int] minimum position
# across parameter sets, or $null for non-positional parameters.
function Get-ParamPositions([string]$mdText) {
    $positions = @{}
    $rxHeading = [regex]'(?m)^###\s+-(?<n>[A-Za-z][A-Za-z0-9_]*)\s*$'
    $headings  = $rxHeading.Matches($mdText)
    for ($i = 0; $i -lt $headings.Count; $i++) {
        $h = $headings[$i]
        $name = $h.Groups['n'].Value
        $bodyStart = $h.Index + $h.Length
        if ($i + 1 -lt $headings.Count) {
            $bodyEnd = $headings[$i + 1].Index
        } else {
            # Up to next '## ' (top-level) heading or EOF.
            $sub = $mdText.Substring($bodyStart)
            $m = [regex]::Match($sub, '(?m)^##\s+\S')
            $bodyEnd = if ($m.Success) { $bodyStart + $m.Index } else { $mdText.Length }
        }
        $body = $mdText.Substring($bodyStart, $bodyEnd - $bodyStart)
        $posMatches = [regex]::Matches($body, '(?m)^\s*Position:\s*(?<v>\S+)\s*$')
        $minPos = [int]::MaxValue
        $isPositional = $false
        $intHolder = 0
        foreach ($pm in $posMatches) {
            $v = $pm.Groups['v'].Value
            if ([int]::TryParse($v, [ref]$intHolder)) {
                if ($intHolder -lt $minPos) { $minPos = $intHolder }
                $isPositional = $true
            }
        }
        $positions[$name] = if ($isPositional) { $minPos } else { $null }
    }
    return $positions
}

function ConvertTo-OrderedSyntax(
    [string[]]$tokens,
    [string[]]$priorityNames,
    [hashtable]$positions
) {
    $priority = @{}
    $commonParams = $null
    $others = New-Object System.Collections.Generic.List[pscustomobject]
    foreach ($tok in $tokens) {
        if ($tok -eq '[<CommonParameters>]') {
            $commonParams = $tok
            continue
        }
        $name = Get-ParamName $tok
        if ($name -and ($priorityNames -contains $name)) {
            $priority[$name] = $tok
            continue
        }
        $pos = if ($name -and $positions.ContainsKey($name)) { $positions[$name] } else { $null }
        $others.Add([pscustomobject]@{ Name = $name; Token = $tok; Position = $pos })
    }

    # Stable sort: positional bucket first (sorted by Position asc, then Name
    # alphabetical as tiebreaker), then named bucket (Name alphabetical).
    $sortedOthers = @($others | Sort-Object `
        @{ Expression = { if ($null -ne $_.Position) { 0 } else { 1 } } },
        @{ Expression = { if ($null -ne $_.Position) { $_.Position } else { 0 } } },
        @{ Expression = { $_.Name } })

    $new = New-Object System.Collections.Generic.List[string]
    foreach ($p in $priorityNames) {
        if ($priority.ContainsKey($p)) { $new.Add($priority[$p]) }
    }
    foreach ($o in $sortedOthers) { $new.Add($o.Token) }
    if ($commonParams) { $new.Add($commonParams) }
    return ,$new.ToArray()
}

function Format-Syntax([string]$cmdName, [string[]]$tokens, [int]$maxWidth) {
    $lines = New-Object System.Collections.Generic.List[string]
    $current = $cmdName
    foreach ($tok in $tokens) {
        $candidate = "$current $tok"
        if ($candidate.Length -le $maxWidth) {
            $current = $candidate
        } else {
            $lines.Add($current)
            $current = " $tok"  # continuation: one leading space (PlatyPS convention)
        }
    }
    $lines.Add($current)
    return ($lines -join "`n")
}

# ---------- file processor ----------

function Update-MdFile([string]$file, [string[]]$priorityNames, [int]$maxWidth) {
    $bytes  = [System.IO.File]::ReadAllBytes($file)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $orig   = [System.IO.File]::ReadAllText($file)
    $useCrLf = $orig.Contains("`r`n")
    # Normalize to LF for regex processing; restore at write time.
    $text = $orig -replace "`r`n", "`n"

    $positions = Get-ParamPositions $text

    $rxSyntax = [regex]'(?ms)^(##\s+SYNTAX[ \t]*\n)(.*?)(?=^##\s+\S|\z)'
    $m = $rxSyntax.Match($text)
    if (-not $m.Success) {
        return @{ Changed = $false; Reason = 'no SYNTAX section' }
    }
    $sectionStart = $m.Index + $m.Groups[1].Length
    $sectionLen   = $m.Groups[2].Length
    $sectionBody  = $m.Groups[2].Value

    $rxFence = [regex]'(?ms)^```[A-Za-z]*[ \t]*\n(.+?)\n```'

    $script:positionsForCallback = $positions
    $newSection = $rxFence.Replace($sectionBody, {
        param($fm)
        $inner = $fm.Groups[1].Value
        $combined = (($inner -split "`n" | ForEach-Object { $_.Trim() }) -join ' ')
        $combined = ($combined -replace '\s+', ' ').Trim()

        $firstSpace = $combined.IndexOf(' ')
        if ($firstSpace -lt 0) { return $fm.Value }  # only cmdlet name in block
        $cmdName = $combined.Substring(0, $firstSpace)
        $rest    = $combined.Substring($firstSpace + 1)

        $tokens = ConvertTo-SyntaxTokens $rest
        if (-not $tokens -or $tokens.Length -le 1) { return $fm.Value }

        $newTokens = ConvertTo-OrderedSyntax `
            $tokens `
            $script:PriorityNames `
            $script:positionsForCallback

        # Compare token sequences; if identical, no change.
        $same = $tokens.Length -eq $newTokens.Length
        if ($same) {
            for ($k = 0; $k -lt $tokens.Length; $k++) {
                if ($tokens[$k] -ne $newTokens[$k]) { $same = $false; break }
            }
        }
        if ($same) { return $fm.Value }

        $newSyntax = Format-Syntax $cmdName $newTokens $script:MaxWidth
        $script:blocksTouchedInFile++
        return "``````" + "`n" + $newSyntax + "`n" + "``````"
    })

    if ($newSection -eq $sectionBody) {
        return @{ Changed = $false; Reason = 'already ordered' }
    }

    $newText = $text.Substring(0, $sectionStart) + $newSection + $text.Substring($sectionStart + $sectionLen)
    if ($useCrLf) { $newText = $newText -replace "`n", "`r`n" }

    if ($PSCmdlet.ShouldProcess($file, "Reorder SYNTAX parameters")) {
        $encoding = if ($hasBom) {
            [System.Text.UTF8Encoding]::new($true)
        } else {
            [System.Text.UTF8Encoding]::new($false)
        }
        [System.IO.File]::WriteAllText($file, $newText, $encoding)
    }
    return @{ Changed = $true; Reason = "$script:blocksTouchedInFile block(s) reordered" }
}

# ---------- main ----------

$script:PriorityNames = $PriorityNames
$script:MaxWidth      = $MaxWidth

$files = New-Object System.Collections.Generic.List[string]
foreach ($p in $MdPath) {
    if (Test-Path $p -PathType Container) {
        Get-ChildItem $p -Filter '*.md' -File | ForEach-Object { $files.Add($_.FullName) }
    } elseif (Test-Path $p -PathType Leaf) {
        $files.Add((Resolve-Path $p).Path)
    } else {
        Write-Warning "Not found: $p"
    }
}

$changedCount = 0
$unchangedCount = 0
foreach ($f in $files) {
    $script:blocksTouchedInFile = 0
    $res = Update-MdFile $f $PriorityNames $MaxWidth
    if ($res.Changed) {
        $changedCount++
        Write-Host "  reordered: $f  ($($res.Reason))" -ForegroundColor Green
    } else {
        $unchangedCount++
        Write-Verbose "  unchanged: $f  ($($res.Reason))"
    }
}

Write-Host ""
Write-Host "Done. $changedCount reordered, $unchangedCount unchanged." -ForegroundColor Cyan
