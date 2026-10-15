# Queue item CSV upload matrix

Checks that `Import-OrchQueueItem` creates the same queue items as the Orchestrator web
**Queues > Upload Items** from the same CSV file.

- `Generate-Matrix.ps1` writes the CSV files byte-exact (BOM, line endings, embedded CR/LF).
- `Compare-Upload.ps1` imports a file with the cmdlet and compares the two queues.

## Setup

Two queues in one folder, both with **Enforce unique references off** (`01_main.csv`
has a deliberate duplicate reference): `QiCsvWeb` (web upload) and `QiCsvCmd` (cmdlet).

## Per file

1. Web: upload the file into `QiCsvWeb`. Note any error message the web shows.
2. Run:

   ```powershell
   .\Compare-Upload.ps1 -Folder Orch1:\Development -WebQueue QiCsvWeb -CmdQueue QiCsvCmd -CsvFile .\<file>.csv -Clear
   ```

3. Record: web message, cmdlet output, and the comparison (`IDENTICAL` or the diff table).

For files the web rejects entirely, the comparison shows 0 web items: check that the
cmdlet rejects it as well, and compare the two messages.

## Findings: web "Dynamic typing" (2026-10-15, Automation Cloud)

Observed on `01_main.csv` and `02_dynamic_typing.csv` with Dynamic typing ON:

- **Empty → `null`** (empty and `""`). Whitespace-only stays a string. `null`, `NULL`,
  `undefined`, `None`, `nil` stay strings.
- **Numbers**: decimal only: optional `-` (no `+`), digits with an optional `.` (`.5`, `5.`),
  optional exponent (`1e3`, `1E3`, `-1e-3`). Surrounding whitespace, tabs included, is allowed
  (` 5`, `5 `, `\t5` → 5). Emitted as the JS number (`007` → 7, `1.50` → 1.5, `-0` → 0,
  `3.14159265358979323846` → 3.141592653589793). Stay strings: `+5`, `0x1F`, `0b101`, `0o17`,
  `1,000`, `1_000`, `５`, `Infinity`, `NaN`, `1e400`, `123abc`, `1e`, `-`, `$5`, `5%`, `1/2`,
  and integers beyond 2^53−1 (`9007199254740991` → number, `9007199254740992` → string,
  `12345678901234567` → string). `20261015` and `2026` become numbers.
- **Booleans**: `true` and `FALSE` converted; `True`, `tRuE`, `yes`, `on`, `t`, `true ` (trailing
  space), ` false` did not.
- **Dates**: only `YYYY-MM-DDTHH:mm:ss[.fff](Z|±HH:MM)` is rewritten, to UTC
  `YYYY-MM-DDTHH:mm:ssZ` (fraction dropped): `…10:00:00+09:00` → `…01:00:00Z`,
  `…10:00:00.1234567Z` → `…10:00:00Z`. JS Date rollover applies (`2026-02-30` → `2026-03-02`,
  `T24:00:00Z` → next day); an invalid month (`2026-13-01T00:00:00Z`) gives the string
  `undefinedZ`. Unchanged: no offset, no seconds, `+0900`, `+09`, space separator, lower-case
  `t`/`z`, `2026-10-15`, RFC 1123.
- **Reference is converted too**: `007` → `7`; `2026-10-15T10:00:00+09:00` →
  `Thu Oct 15 2026 10:00:00 GMT+0900 (Japan Standard Time)` (browser-local `Date.toString()`).
- Priority unaffected.

`03_dynamic_typing_followup.csv` settled the edges:

- Booleans: exactly `true`, `false`, `TRUE`, `FALSE` (`False`, `fAlSe` stay strings).
- Number cut-off is `Number.isSafeInteger` on integral results: `1e15` → 1000000000000000,
  `1e16` / `1e20` / `-9007199254740992` / `9007199254740991.5` (rounds to 2^53) stay strings.
  `0.1e1`, `1.`, `-.5`, `1e+3` are numbers.
- Date rewrite: the pattern needs minutes and seconds `00`–`59` (`T10:60:00Z`, `T10:00:60Z`
  are left alone). Day up to 31 rolls over (`04-31` → `05-01`, `02-29` in 2026 → `03-01`);
  day 32, hour 25, month 00/13 give `undefinedZ`. Fractions are truncated (`.999Z` → `:00Z`).
  Offsets up to `+14:00` work. `+002026-…` is left alone.
- Reference: `1.50` → `1.5`, ` 5 ` → `5`, `TRUE` → `true`, `9007199254740993` unchanged,
  empty stays empty, `2026-10-15T10:00:00Z` → `Thu Oct 15 2026 19:00:00 GMT+0900 (Japan
  Standard Time)`: the value goes through the same typing and is then stringified with JS
  `String()`, so a date comes out in the browser's local time zone.

**Web defect: check the box BEFORE adding the file.** Checking Dynamic typing after the CSV
is dropped into the dialog uploads it untyped, although the box shows as checked
(`05_dt_probe_*.csv`: drop-then-check 3/3 untyped, check-then-drop 3/3 typed). Use
`Show-DtProbe.ps1` to see which trials were typed.

**Large files.** The web sends a whole file as one `BulkAddQueueItems` request. For
`51_rows_15000.csv` it took 5.5 minutes. The same single request from the cmdlet first hit
the HttpClient 100 s timeout (5,508 items created), then with a longer timeout got a 500
after 373 s (10,903 created). `Import-OrchQueueItem` therefore sends 1,000 rows per request
(15,000 rows: 297 s, all created), except AllOrNothing.

## Files

| File | What it probes |
|---|---|
| `01_main.csv` | Everything expected to be accepted: Priority spellings (High/high/HIGH/1/2/3/empty), Reference variations (empty, duplicate, Japanese, spaces, comma, quote), 40 value probes (empty, spaces, comma, quotes, LF/CRLF/CR in a cell, tab, numeric-looking, true/null, JSON text, dates, emoji, NFC/NFD, backslash, formula, NBSP, full-width, 2,000 chars), and headers with Japanese, comma, quotes and surrounding spaces |
| `02_dynamic_typing.csv` | Upload with **Dynamic typing ON**: number spellings, boolean spellings, null-like words, date/time formats, and whether Reference is touched |
| `03_dynamic_typing_followup.csv` | Dynamic typing edges: boolean case, the safe-integer cut-off, date range and rollover, Reference stringification |
| `05_dt_probe_A1`–`B3` | The web defect: Dynamic typing checked before (A) or after (B) adding the file, 3 trials each (`Show-DtProbe.ps1`) |
| `11`–`16` | One questionable Priority each (`Medium`, `0`, `4`, ` High `, `" High "`, `高`), between two valid rows: does the whole file fail, or one row? |
| `21` | Lower/upper-case `reference` / `PRIORITY` headers |
| `22` | ` Reference ` / ` Priority ` headers with spaces |
| `23` | Duplicate header |
| `2g`–`2i` | Duplicate headers: three of a name and a repeated Reference/Priority; a rename that collides with a later or an earlier header |
| `24`, `25` | Empty header name, trailing comma in the header |
| `2j` | Two empty header names |
| `26` | Headers that are queue item fields (`Name`, `Source`, `Progress`, `DeferDate`, `DueDate`, `RiskSlaDate`, `SpecificContent`, `Status`): special or SpecificContent? |
| `2b` | `DeferDate` / `DueDate` in each of the nine formats the web lists as valid, plus empty |
| `2c`, `2d` | A date the web does not list (`2030-01-02T13:45:30`, `01/02/2030`) |
| `2e` | `RiskSlaDate`: a date column too? |
| `2f` | `deferdate` / `DUEDATE` header case |
| `27`, `28` | No Reference/Priority columns; a single column |
| `29` | Header only, no rows |
| `31`, `32` | Row shorter / longer than the header |
| `33`–`35` | Blank line in the middle, trailing blank lines, no final newline |
| `36`–`39` | Bare quote inside a field, unterminated quote, space before the opening quote, text after the closing quote |
| `3a`–`3c` | Semicolon, tab and pipe delimiters |
| `3d`–`3g` | Delimiter detection: semicolons only inside values, a comma/semicolon tie, more semicolon fields, a quoted comma in a semicolon file |
| `41`–`44` | UTF-8 with BOM, UTF-8 without BOM, Shift_JIS, UTF-16 LE |
| `45`, `46` | LF-only and CR-only line endings |
| `51`, `52` | 15,000 rows (limit) and 15,001 rows |
| `53` | 129-character Reference between two valid rows |
| `54` | 100,000-character value |
