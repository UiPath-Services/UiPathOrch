# Web-upload equivalence fixtures

These fixtures prove that the `Import-Orch*Item` cmdlets build the **same
item payload** the Orchestrator web UI builds from the **same CSV**. Because
the web and the cmdlet POST to the identical server endpoint, matching the
payload guarantees identical created items — no live tenant is needed to
prove it, so the check runs as an offline unit test
(`WebUploadEquivalenceTests`).

The only piece that must come from the real web is the **golden** payload.
Capture it once and commit it; the test then runs forever (until the web
changes its conversion, at which point the test fires and we re-sync).

## Files

| File | Source | Committed? |
| ---- | ------ | ---------- |
| `tdq_triangle_sample.csv` | hand-authored sample (this repo) | yes |
| `tdq_triangle_schema.json` | the queue's `ContentJsonSchema` | yes |
| `tdq_triangle_web_golden.json` | **captured from the web** (below) | once captured |

The sample CSV columns match a test data queue named **三角形のテストキュー**
with the schema in `tdq_triangle_schema.json` (`番号` integer; `辺A`/`辺B`/
`辺C`/`コメント`/`期待値` string; `additionalProperties:false`). Create that
queue if it does not exist, or point the fixtures at an equivalent queue.

## Capturing the golden (one-time, ~2 min)

1. Open the Orchestrator web UI, go to the **三角形のテストキュー** test data
   queue, choose **Upload Items**, and select `tdq_triangle_sample.csv`.
2. Before clicking the final upload, open browser **devtools → Network**.
3. Complete the upload. Find the `POST .../api/TestDataQueueActions/BulkAddItems`
   request.
4. Copy its **request payload** (the JSON body — either the whole
   `{"queueName":...,"items":[...]}` object or just the `items` array).
5. Save it verbatim as `tdq_triangle_web_golden.json` in this folder.

That's it — the previously-skipped
`TestDataQueueItem_TriangleSchema_MatchesWebUpload` fact now runs and compares
the cmdlet's generated items against this golden (object-key-order and
number-formatting insensitive). If it fails, the cmdlet's CSV→JSON conversion
diverges from the web and must be reconciled.

## Kitchen-sink (type / format coverage)

The triangle fixtures only exercise `integer` + `string`. The kitchen-sink
fixtures pin the remaining types and the date string-formats, which is where
the cmdlet is most likely to diverge from the web:

| File | Source | Committed? |
| ---- | ------ | ---------- |
| `kitchensink_sample.csv` | hand-authored (this repo) | yes |
| `kitchensink_schema.json` | the queue's `ContentJsonSchema` | yes |
| `kitchensink_web_golden.json` | **captured from the web** | once captured |

`kitchensink_schema.json` declares one property per type:
`intField` (integer), `numField` (number), `boolField` (boolean),
`strField` (string), `dateField` (string/`format:date`),
`dateTimeField` (string/`format:date-time`). The sample rows probe:

- **number**: `3.14`, `100.50` (trailing zero), `100` (integer-looking) —
  does the web preserve `100.50` or emit `100.5`? `100` or `100.0`?
- **date / date-time**: ISO values — does the web pass them through verbatim
  (as the cmdlet does) or reformat them?
- **boolean**: `true` / `false`.
- **string**: ASCII, a quoted comma field, and Japanese.

### Capturing the kitchen-sink golden

1. Create a test data queue whose `ContentJsonSchema` is the contents of
   `kitchensink_schema.json`.
2. Upload `kitchensink_sample.csv` via **Upload Items**, capture the
   `BulkAddItems` request body (devtools → Network), and save it as
   `kitchensink_web_golden.json` here.
3. Remove the `Skip` on `TestDataQueueItem_KitchenSinkSchema_MatchesWebUpload`.
   If it fails, reconcile the cmdlet's `Coerce` (e.g. number formatting, or
   date handling) to match the web.

> Note: every CSV value must satisfy the schema or the web upload is rejected
> (test data queue schemas are server-enforced). Keep dates ISO and numbers
> dot-decimal in the committed sample so the golden is capturable; probe
> reject-behaviour (non-ISO dates, empty integer cells) with separate one-off
> CSVs rather than the committed sample.

## Import-OrchQueueItem

Same pattern for regular queue items (no schema). The cmdlet's conversion
rules: `Priority` maps `1`/`low`→`Low`, `2`/`normal`→`Normal`, `3`/`high`→`High`;
`Reference` is always set; every other column becomes a `SpecificContent`
string value.

| File | Source | Committed? |
| ---- | ------ | ---------- |
| `queueitem_sample.csv` | hand-authored (this repo) | yes |
| `queueitem_web_golden.json` | **captured from the web** | once captured |

### Capturing

1. Upload `queueitem_sample.csv` to any regular queue via the web
   (Queues → a queue → **Upload Items** / bulk add).
2. Capture the `POST .../odata/Queues.../BulkAddQueueItems` (or equivalent)
   request body in devtools → Network, save as `queueitem_web_golden.json`.
3. Remove the `Skip` on `QueueItem_MatchesWebUpload`. The test compares the
   cmdlet's `queueItems` array (via `ImportQueueItemCmdlet.BuildQueueItemsArrayJson`)
   against the golden's `queueItems`.

If it fails, the cmdlet's CSV→queue-item conversion diverges from the web
(e.g. Priority spelling, SpecificContent value types, or extra columns the web
treats specially like DeferDate/DueDate) and must be reconciled.
