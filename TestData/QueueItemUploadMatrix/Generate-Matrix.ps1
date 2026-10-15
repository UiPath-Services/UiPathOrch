<#
Generates the CSV matrix used to compare the Orchestrator web "Upload Items" with
Import-OrchQueueItem. Every file is written byte-exact (BOM, line endings, embedded
CR/LF) so the same bytes go to both uploaders. Re-run to regenerate.

  01_main.csv     — everything both uploaders are expected to ACCEPT, one probe per row
  1x_*.csv        — Priority values that may reject the whole file (one value per file)
  2x_*.csv        — header variations
  3x_*.csv        — row shape / CSV syntax
  4x_*.csv        — encodings and line endings
  5x_*.csv        — server-side limits

Every row carries a CaseId column so Compare-Upload.ps1 can pair web and cmdlet items.
#>
param([string]$OutDir = $PSScriptRoot)

$ErrorActionPreference = 'Stop'
[Text.Encoding]::RegisterProvider([Text.CodePagesEncodingProvider]::Instance)

# Quote a field when CSV requires it (comma, quote, CR, LF).
function Q([string]$s) {
    if ($s -match '[,"\r\n]') { '"' + $s.Replace('"', '""') + '"' } else { $s }
}

# rows: array of arrays of RAW field text (already quoted where a case needs a specific form).
function Write-Csv([string]$Name, [object[]]$Rows, [string]$Eol = "`r`n", [string]$Encoding = 'utf8bom', [switch]$NoFinalEol) {
    $text = ($Rows | ForEach-Object { $_ -join ',' }) -join $Eol
    if (-not $NoFinalEol) { $text += $Eol }
    $bytes = switch ($Encoding) {
        'utf8bom' { [byte[]](0xEF, 0xBB, 0xBF) + [Text.Encoding]::UTF8.GetBytes($text) }
        'utf8'    { [Text.Encoding]::UTF8.GetBytes($text) }
        'sjis'    { [Text.Encoding]::GetEncoding(932).GetBytes($text) }
        'utf16le' { [byte[]](0xFF, 0xFE) + [Text.Encoding]::Unicode.GetBytes($text) }
    }
    [IO.File]::WriteAllBytes((Join-Path $OutDir $Name), $bytes)
}

# ---------------------------------------------------------------- 01_main.csv
# Header: Reference / Priority are special; everything else is SpecificContent.
# Includes a Japanese header, a header with a comma, one with quotes, one with
# surrounding spaces (unquoted), to see how each uploader names the key.
$hdr = @('CaseId', 'Reference', 'Priority', 'Value', '顧客名', (Q 'Col,Comma'), (Q 'Col "Q"'), ' Spaced ')
$long = 'x' * 2000
$rows = [System.Collections.Generic.List[object]]::new()
$rows.Add($hdr)
function Row($id, $ref, $pri, $valueRaw) { $rows.Add(@($id, $ref, $pri, $valueRaw, '山田', 'c', 'q', 's')) }

# Priority spellings expected to be accepted
Row 'P01' 'R-P01' 'High'   'v'
Row 'P02' 'R-P02' 'Normal' 'v'
Row 'P03' 'R-P03' 'Low'    'v'
Row 'P04' 'R-P04' 'high'   'v'
Row 'P05' 'R-P05' 'HIGH'   'v'
Row 'P06' 'R-P06' 'nOrMaL' 'v'
Row 'P07' 'R-P07' '1'      'v'
Row 'P08' 'R-P08' '2'      'v'
Row 'P09' 'R-P09' '3'      'v'
Row 'P10' 'R-P10' ''       'v'          # empty priority
Row 'P11' 'R-P11' '""'     'v'          # quoted empty priority

# Reference variations
Row 'R01' ''                 'Normal' 'v'   # empty reference
Row 'R02' '""'               'Normal' 'v'   # quoted empty reference
Row 'R03' 'R-P01'            'Normal' 'v'   # duplicate of P01 (queue must NOT enforce unique refs)
Row 'R04' '参照-日本語'        'Normal' 'v'
Row 'R05' '" R 05 "'         'Normal' 'v'   # quoted, surrounding spaces
Row 'R06' ' R 06 '           'Normal' 'v'   # unquoted, surrounding spaces
Row 'R07' (Q 'R,07')         'Normal' 'v'
Row 'R08' (Q 'R"08')         'Normal' 'v'

# Value (SpecificContent) probes
$values = [ordered]@{
    V01 = ''                              # empty
    V02 = '""'                            # quoted empty
    V03 = '   '                           # spaces only, unquoted
    V04 = '"   "'                         # spaces only, quoted
    V05 = '  lead and trail  '            # unquoted surrounding spaces
    V06 = (Q 'a,b')
    V07 = (Q 'say "hi"')
    V08 = (Q "line1`nline2")              # embedded LF
    V09 = (Q "line1`r`nline2")            # embedded CRLF
    V10 = (Q "line1`rline2")              # embedded bare CR
    V11 = "a`tb"                          # tab
    V12 = '007'
    V13 = '1.50'
    V14 = '1e3'
    V15 = '-0'
    V16 = '12345678901234567890'          # beyond Int64
    V17 = 'true'
    V18 = 'FALSE'
    V19 = 'null'
    V20 = (Q '{"a":1,"b":[1,2]}')         # JSON object text
    V21 = (Q '[1,2,3]')
    V22 = '2026-10-15'
    V23 = '2026-10-15T10:00:00+09:00'
    V24 = '10/15/2026'
    V25 = '😀'
    V26 = '👨‍👩‍👧'                        # ZWJ sequence
    V27 = "e$([char]0x0301)"              # combining accent (NFD)
    V28 = 'é'                             # precomposed (NFC)
    V29 = 'C:\temp\a.txt'
    V30 = '=SUM(A1:A2)'
    V31 = "'leading apostrophe"
    V32 = '<b>&amp;</b>'
    V33 = "nbsp$([char]0xA0)here"
    V34 = 'full　width　space'             # U+3000
    V35 = '全角ＡＢＣ１２３'
    V36 = $long                           # 2,000 chars
    V37 = '\u0041'                        # JSON escape text, must stay literal
    V38 = '%20%27'
    V39 = '{{curly}}'
    V40 = 'ends with backslash\'
}
foreach ($k in $values.Keys) { Row $k "R-$k" 'Normal' $values[$k] }
Write-Csv '01_main.csv' $rows

# ---------------------------------------------------------------- 02_dynamic_typing.csv
# Upload with "Dynamic typing" ON. In 01_main.csv it turned empty into null, 007 / 1.50 /
# 1e3 / -0 into numbers, true / FALSE into booleans, and 2026-10-15T10:00:00+09:00 into
# "2026-10-15T01:00:00Z"; 12345678901234567890, "null", "   " and 2026-10-15 stayed strings.
# These probes pin down where each rule starts and stops.
$dyn = [ordered]@{
    # numbers
    N01 = '0';  N02 = '-5';  N03 = '+5';  N04 = '1.0';  N05 = '.5';  N06 = '5.'
    N07 = '1E3';  N08 = '-1e-3';  N09 = '1e400';  N10 = '0x1F';  N11 = (Q '1,000')
    N12 = ' 5';  N13 = '5 ';  N14 = 'Infinity';  N15 = '-Infinity';  N16 = 'NaN'
    N17 = '123abc';  N18 = '1_000';  N19 = '５';  N20 = '00';  N21 = '-007'
    N22 = '9007199254740991';  N23 = '9007199254740992';  N24 = '9007199254740993'
    N25 = '1234567890123456';  N26 = '12345678901234567';  N27 = '0.1'
    N28 = '3.14159265358979323846';  N29 = '100.50';  N30 = '$5';  N31 = '5%'
    N32 = '1/2';  N33 = '0b101';  N34 = '0o17';  N35 = '1e';  N36 = '-'
    N37 = '0.0000001';  N38 = '123456789012345678901234567890';  N39 = "`t5"
    # booleans
    B01 = 'True';  B02 = 'tRuE';  B03 = 'yes';  B04 = 'no';  B05 = 'on';  B06 = 'off'
    B07 = 'true ';  B08 = ' false';  B09 = 't';  B10 = 'TRUE1'
    # null-like
    U01 = 'null';  U02 = 'NULL';  U03 = 'undefined';  U04 = 'None';  U05 = 'nil'
    # dates
    D01 = '2026-10-15T10:00:00Z';  D02 = '2026-10-15T10:00:00';  D03 = '2026-10-15T10:00:00.123+09:00'
    D04 = '2026-10-15T10:00';  D05 = '2026-10-15 10:00';  D06 = '2026-10-15 10:00:00'
    D07 = '2026/10/15';  D08 = '2026/10/15 10:00:00';  D09 = '2026 10 15';  D10 = '20261015'
    D11 = '2026-10-15T10:00:00+0900';  D12 = '2026-10-15T10:00:00-05:00';  D13 = '2026-13-01T00:00:00Z'
    D14 = '2026-02-30T00:00:00Z';  D15 = (Q 'Thu, 15 Oct 2026 10:00:00 GMT');  D16 = '10:00'
    D17 = '2026-10-15t10:00:00z';  D18 = '1970-01-01T00:00:00Z';  D19 = '0001-01-01T00:00:00Z'
    D20 = '2026-10-15T24:00:00Z';  D21 = '2026-10-15T10:00:00.1234567Z';  D22 = '2026-10'
    D23 = '2026';  D24 = '2026-10-15T10:00:00+09';  D25 = '2026-10-15 10:00:00+09:00'
}
$drows = [System.Collections.Generic.List[object]]::new()
$drows.Add(@('CaseId', 'Reference', 'Priority', 'Value'))
foreach ($k in $dyn.Keys) { $drows.Add(@($k, "R-$k", 'Normal', $dyn[$k])) }
# Does dynamic typing touch Reference / an empty Priority?
$drows.Add(@('X01', '007', 'Normal', 'v'))
$drows.Add(@('X02', 'true', '', 'v'))
$drows.Add(@('X03', '2026-10-15T10:00:00+09:00', 'Normal', 'v'))
Write-Csv '02_dynamic_typing.csv' $drows

# ---------------------------------------------------------------- 03_dynamic_typing_followup.csv
# Follow-ups to 02 (also Dynamic typing ON): which boolean spellings convert (true and
# FALSE did, True did not), the integer precision cut-off, how the date rewrite treats
# fractions and out-of-range fields, and what happens to Reference (007 became "7", a
# date with offset became the browser's Date.toString()).
$dyn3 = [ordered]@{
    B11 = 'false';  B12 = 'TRUE';  B13 = 'False';  B14 = 'fAlSe'
    N40 = '1e20';  N41 = '-9007199254740991';  N42 = '-9007199254740992';  N43 = '9007199254740991.5'
    N44 = '1e15';  N45 = '1e16';  N46 = '0.1e1';  N47 = '1.';  N48 = '-.5';  N49 = '1e+3'
    D26 = '2026-04-31T00:00:00Z';  D27 = '2026-02-29T00:00:00Z';  D28 = '2026-10-32T00:00:00Z'
    D29 = '2026-10-15T25:00:00Z';  D30 = '2026-10-15T10:60:00Z';  D31 = '2026-10-15T10:00:60Z'
    D32 = '2026-10-15T10:00:00.999Z';  D33 = '2026-10-15T23:30:00-01:00';  D34 = '2026-10-15T10:00:00+14:00'
    D35 = '2026-00-10T00:00:00Z';  D36 = '+002026-10-15T10:00:00Z';  D37 = '2026-10-15T10:00:00.5+09:00'
}
$d3 = [System.Collections.Generic.List[object]]::new()
$d3.Add(@('CaseId', 'Reference', 'Priority', 'Value'))
foreach ($k in $dyn3.Keys) { $d3.Add(@($k, "R-$k", 'Normal', $dyn3[$k])) }
$d3.Add(@('X04', '1.50', 'Normal', 'v'))
$d3.Add(@('X05', 'TRUE', 'Normal', 'v'))
$d3.Add(@('X06', '2026-10-15T10:00:00Z', 'Normal', 'v'))
$d3.Add(@('X07', ' 5 ', 'Normal', 'v'))
$d3.Add(@('X08', '', 'Normal', 'v'))
$d3.Add(@('X09', '9007199254740993', 'Normal', 'v'))
Write-Csv '03_dynamic_typing_followup.csv' $d3

# ---------------------------------------------------------------- 05_dt_probe_*.csv
# One file per upload attempt, to find when the web's Dynamic typing check box is not
# applied. "007" is stored as 7 when it is, as "007" when it is not.
#   A1-A3: drop the file into the dialog, THEN check Dynamic typing
#   B1-B3: check Dynamic typing, THEN drop the file
foreach ($t in 'A1', 'A2', 'A3', 'B1', 'B2', 'B3') {
    Write-Csv "05_dt_probe_$t.csv" @(@('CaseId', 'Value'), @($t, '007'))
}

# ---------------------------------------------------------------- 1x priority (one value per file)
$pri = [ordered]@{
    '11_priority_medium.csv'      = 'Medium'
    '12_priority_zero.csv'        = '0'
    '13_priority_four.csv'        = '4'
    '14_priority_spaced.csv'      = ' High '
    '15_priority_quoted_spaced.csv' = '" High "'
    '16_priority_japanese.csv'    = '高'
}
foreach ($f in $pri.Keys) {
    Write-Csv $f @(@('CaseId', 'Reference', 'Priority', 'Value'), @('A1', 'R-A1', 'Normal', 'ok'), @('A2', 'R-A2', $pri[$f], 'probe'), @('A3', 'R-A3', 'Low', 'ok'))
}

# ---------------------------------------------------------------- 2x headers
Write-Csv '21_header_case.csv'      @(@('CaseId', 'reference', 'PRIORITY', 'Value'), @('H1', 'R-H1', 'High', 'v'))
Write-Csv '22_header_spaced_special.csv' @(@('CaseId', ' Reference ', ' Priority ', 'Value'), @('H2', 'R-H2', 'High', 'v'))
Write-Csv '23_header_duplicate.csv' @(@('CaseId', 'Value', 'Value'), @('H3', 'first', 'second'))
# 23 showed the web renaming a duplicate header to Value_1. These pin the scheme down:
# a third duplicate, duplicated special columns, and a collision with a literal Value_1.
Write-Csv '2g_header_duplicate_three_and_special.csv' @(@('CaseId', 'Value', 'Value', 'Value', 'Reference', 'Reference', 'Priority', 'Priority'),
    @('G1', 'a', 'b', 'c', 'R-1', 'R-2', 'High', 'Low'))
Write-Csv '2h_header_duplicate_collides_later.csv' @(@('CaseId', 'Value', 'Value_1', 'Value'), @('G2', 'a', 'b', 'c'))
Write-Csv '2i_header_duplicate_collides_earlier.csv' @(@('CaseId', 'Value_1', 'Value', 'Value'), @('G3', 'a', 'b', 'c'))
# Two empty header names: does the repeat become "_1" (a real key) as duplicate renaming would give?
Write-Csv '2j_header_two_empty_names.csv' @(@('CaseId', '', '', 'Value'), @('J1', 'x', 'y', 'v'))
Write-Csv '24_header_empty_name.csv' @(@('CaseId', '', 'Value'), @('H4', 'under-empty-header', 'v'))
Write-Csv '25_header_trailing_comma.csv' @(@('CaseId', 'Value', ''), @('H5', 'v', ''))
Write-Csv '26_header_reserved_names.csv' @(@('CaseId', 'Name', 'Source', 'Progress', 'DeferDate', 'DueDate', 'RiskSlaDate', 'SpecificContent', 'Status'),
    @('H6', 'n', 's', 'p', '2030-01-01T00:00:00Z', '2030-01-02T00:00:00Z', '2030-01-03T00:00:00Z', 'sc', 'Successful'))
# The web's rejection message lists date formats for "column values": the nine formats
# below, one per row, in DeferDate and DueDate.
Write-Csv '2b_dates_all_formats.csv' @(@('CaseId', 'Reference', 'DeferDate', 'DueDate'),
    @('D1', 'R-D1', '2030-01-02', '2030-02-02'),
    @('D2', 'R-D2', '2030/01/02', '2030/02/02'),
    @('D3', 'R-D3', '2030 01 02', '2030 02 02'),
    @('D4', 'R-D4', '2030-01-02 13:45', '2030-02-02 13:45'),
    @('D5', 'R-D5', '2030/01/02 13:45', '2030/02/02 13:45'),
    @('D6', 'R-D6', '2030 01 02 13:45', '2030 02 02 13:45'),
    @('D7', 'R-D7', '2030-01-02 13:45:30', '2030-02-02 13:45:30'),
    @('D8', 'R-D8', '2030/01/02 13:45:30', '2030/02/02 13:45:30'),
    @('D9', 'R-D9', '2030 01 02 13:45:30', '2030 02 02 13:45:30'),
    @('DA', 'R-DA', '', ''))
Write-Csv '2c_date_iso_t.csv'        @(@('CaseId', 'DeferDate'), @('D10', '2030-01-02T13:45:30'))
Write-Csv '2d_date_slash_mdy.csv'    @(@('CaseId', 'DueDate'), @('D11', '01/02/2030'))
Write-Csv '2e_risksladate.csv'       @(@('CaseId', 'RiskSlaDate'), @('D12', '2030-01-02 13:45'))
Write-Csv '2f_date_header_case.csv'  @(@('CaseId', 'deferdate', 'DUEDATE'), @('D13', '2030-01-02', '2030-02-02'))
Write-Csv '27_no_reference_no_priority.csv' @(@('CaseId', 'Value'), @('H7', 'v'))
Write-Csv '28_only_specific_one_column.csv' @(@('CaseId'), @('H8'))
Write-Csv '29_header_only.csv'      @(, @('CaseId', 'Reference', 'Priority', 'Value'))

# ---------------------------------------------------------------- 3x row shape / syntax
Write-Csv '31_ragged_short_row.csv' @(@('CaseId', 'Reference', 'Priority', 'Value', 'Extra'), @('S1', 'R-S1', 'High', 'v'))
Write-Csv '32_ragged_long_row.csv'  @(@('CaseId', 'Reference', 'Priority', 'Value'), @('S2', 'R-S2', 'High', 'v', 'overflow'))
Write-Csv '33_blank_line_middle.csv' @(@('CaseId', 'Value'), @('S3a', 'v'), @(''), @('S3b', 'v'))
Write-Csv '34_trailing_blank_lines.csv' @(@('CaseId', 'Value'), @('S4', 'v'), @(''), @(''))
Write-Csv '35_no_final_newline.csv' @(@('CaseId', 'Value'), @('S5', 'v')) -NoFinalEol
Write-Csv '36_bare_quote_inside.csv' @(@('CaseId', 'Value'), @('S6', 'a"b'))
Write-Csv '37_unterminated_quote.csv' @(@('CaseId', 'Value'), @('S7', '"abc'), @('S7b', 'v'))
Write-Csv '38_space_before_quote.csv' @(@('CaseId', 'Value'), @('S8', ' "x"'))
Write-Csv '39_text_after_closing_quote.csv' @(@('CaseId', 'Value'), @('S9', '"x"y'))
Write-Csv '3a_semicolon_delimited.csv' @(@('CaseId;Value'), @('SA;v'))
# 3a showed the web detecting ';'. These pin down the delimiter guess (Papa Parse style:
# the candidate with consistent field counts and the most fields wins, comma on a tie).
Write-Csv '3b_tab_delimited.csv'  @(@("CaseId`tValue"), @("SB`tv"))
Write-Csv '3c_pipe_delimited.csv' @(@('CaseId|Value'), @('SC|v'))
# Comma file whose values contain semicolons: comma counts are consistent, semicolon counts are not.
Write-Csv '3d_comma_with_semicolons_in_values.csv' @(@('CaseId', 'Value'), @('SD1', 'a;b;c'), @('SD2', 'd;e;f'))
# Tie: both ',' and ';' give 2 consistent fields.
Write-Csv '3e_tie_comma_vs_semicolon.csv' @(@('CaseId;Value', 'X'), @('SE;v', 'x'))
# ';' gives more consistent fields (3) than ',' (2).
Write-Csv '3f_semicolon_more_fields.csv' @(@('CaseId;Value;Other', 'X'), @('SF;v;o', 'x'))
# Semicolon file with a quoted value containing a comma.
Write-Csv '3g_semicolon_quoted_comma.csv' @(@('CaseId;Value'), @('SG;"a,b"'))

# ---------------------------------------------------------------- 4x encodings / line endings
$enc = @(@('CaseId', 'Value', '列名'), @('E1', '日本語の値', 'あ'))
Write-Csv '41_utf8_bom.csv'   $enc -Encoding utf8bom
Write-Csv '42_utf8_nobom.csv' $enc -Encoding utf8
Write-Csv '43_shift_jis.csv'  $enc -Encoding sjis
Write-Csv '44_utf16le_bom.csv' $enc -Encoding utf16le
Write-Csv '45_lf_only.csv'    @(@('CaseId', 'Value'), @('E5a', 'v'), @('E5b', (Q "in`ncell"))) -Eol "`n"
Write-Csv '46_cr_only.csv'    @(@('CaseId', 'Value'), @('E6a', 'v'), @('E6b', 'v')) -Eol "`r"

# ---------------------------------------------------------------- 5x limits
$big = [System.Collections.Generic.List[object]]::new(); $big.Add(@('CaseId', 'Value'))
1..15000 | ForEach-Object { $big.Add(@("L$_", 'v')) }
Write-Csv '51_rows_15000.csv' $big
$big.Add(@('L15001', 'v'))
Write-Csv '52_rows_15001.csv' $big
Write-Csv '53_reference_129_chars.csv' @(@('CaseId', 'Reference', 'Value'), @('L3a', 'ok', 'v'), @('L3b', ('r' * 129), 'v'), @('L3c', 'ok2', 'v'))
Write-Csv '54_value_100k_chars.csv' @(@('CaseId', 'Value'), @('L4', ('y' * 100000)))

Get-ChildItem $OutDir -Filter *.csv | Sort-Object Name | Format-Table Name, Length
