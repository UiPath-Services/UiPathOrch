using System.Net;
using System.Management.Automation;
using System.Text.Json;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// Wraps POST /odata/ProcessSchedules/.../ValidateProcessSchedule. Sends an existing trigger
// (fetched via Get-OrchTrigger or named via -Name) to the server's pre-flight validator and
// returns a ValidationResult with IsValid + Errors + ErrorCodes (e.g. RobotNotFound,
// TemplateNoLicense, RobotConcurrencyLimit, ...). Useful for "would this schedule actually run?"
// preview without enabling/firing it.
[Cmdlet(VerbsDiagnostic.Test, "OrchTrigger")]
[OutputType(typeof(ValidationResult))]
public class TestTriggerCmdlet : OrchestratorPSCmdlet
{
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(TriggerNameCompleter))]
    [SupportsWildcards]
    public string[]? Name { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [SupportsWildcards]
    public string[]? Path { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath")]
    public string[]? LiteralPath { get; set; }

    [Parameter]
    public SwitchParameter Recurse { get; set; }

    [Parameter]
    public uint Depth { get; set; }

    protected override void ProcessRecord()
    {
        var drivesFolders = SessionState.EnumFolders(EffectivePath(Path, LiteralPath), Recurse.IsPresent, Depth);

        using var cancelHandler = new ConsoleCancelHandler();
        foreach (var (drive, folder) in drivesFolders)
        {
            try
            {
                var triggers = drive.GetTriggers(folder)
                    .FilterByNames(t => t?.Name, Name)
                    .OrderBy(t => t.Name)
                    .ToList();

                foreach (var trigger in triggers.WithCancellation(cancelHandler.Token))
                {
                    try
                    {
                        var result = drive.OrchAPISession.ValidateProcessSchedule(folder.Id ?? 0, trigger);
                        if (result is not null)
                        {
                            result.Path = folder.GetPSPath();
                            result.Name = trigger.Name;
                            WriteObject(result);
                        }
                    }
                    catch (HttpResponseException hex)
                    {
                        // Server rejected the schedule (model-validation 400, the API's own
                        // ValidationResult, or some other 4xx). Surface as IsValid=false so
                        // callers can iterate uniformly across triggers without try/catch.
                        var fallback = TryBuildValidationResultFromError(hex);
                        if (fallback is not null)
                        {
                            fallback.Path = folder.GetPSPath();
                            fallback.Name = trigger.Name;
                            WriteObject(fallback);
                        }
                        else
                        {
                            WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), hex), "TestTriggerError", ErrorCategory.InvalidOperation, trigger));
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(new OrchException(trigger.GetPSPath(), ex), "TestTriggerError", ErrorCategory.InvalidOperation, trigger));
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(folder.GetPSPath(), ex), "GetTriggerError", ErrorCategory.InvalidOperation, folder));
            }
        }
    }

    // Converts an HTTP error response into a ValidationResult{IsValid=false}. Handles:
    //   1. ValidateProcessSchedule's own shape:  { "isValid": false, "errors": [...], "errorCodes": [...] }
    //   2. ASP.NET model-binding errors object:  { "errors": { "FieldName": ["msg1", ...], ... } }
    //   3. Single-message envelopes:             { "message": "msg1; msg2; ..." } (also "error", "errorMessage", "title")
    //   4. Plain text body
    // Returns null only if no body text is present.
    private static ValidationResult? TryBuildValidationResultFromError(HttpResponseException hex)
    {
        // HttpResponseException is constructed with the body string as its message in
        // OrchAPISession.EnsureSuccessStatusCode, so hex.Message already carries the body.
        // Don't re-read hex.Response.Content — the response is disposed by the caller's `using`.
        string body = hex.Message ?? "";
        if (string.IsNullOrEmpty(body)) return null;

        // Try structured JSON shapes first.
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Shape 1: ValidationResult passthrough
            if (root.TryGetProperty("isValid", out var isValidEl) && isValidEl.ValueKind == JsonValueKind.False)
            {
                var errs = new List<string>();
                if (root.TryGetProperty("errors", out var errArr) && errArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in errArr.EnumerateArray())
                        if (e.ValueKind == JsonValueKind.String) errs.Add(e.GetString() ?? "");
                }
                string[]? codes = null;
                if (root.TryGetProperty("errorCodes", out var codeArr) && codeArr.ValueKind == JsonValueKind.Array)
                {
                    codes = codeArr.EnumerateArray()
                        .Where(c => c.ValueKind == JsonValueKind.String)
                        .Select(c => c.GetString() ?? "").ToArray();
                }
                return new ValidationResult { IsValid = false, Errors = errs.ToArray(), ErrorCodes = codes };
            }

            // Shape 2: ASP.NET model-binding errors object
            if (root.TryGetProperty("errors", out var errObj) && errObj.ValueKind == JsonValueKind.Object)
            {
                var errs = new List<string>();
                foreach (var prop in errObj.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var msg in prop.Value.EnumerateArray())
                            if (msg.ValueKind == JsonValueKind.String) errs.Add(msg.GetString() ?? "");
                    }
                }
                if (errs.Count > 0)
                {
                    return new ValidationResult { IsValid = false, Errors = errs.ToArray(), ErrorCodes = null };
                }
            }
        }
        catch { /* not JSON, fall through */ }

        // Shape 3 & 4: let OrchException.ExtractMessage do the message-field digging,
        // then split combined-message strings on common separators so each becomes its own
        // entry in Errors[].
        string message = OrchException.ExtractMessage(body) ?? body;
        var split = message
            .Split(new[] { "; ", ";\n", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().TrimEnd(';'))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
        return new ValidationResult
        {
            IsValid = false,
            Errors = split.Length > 0 ? split : new[] { message },
            ErrorCodes = null,
        };
    }
}
