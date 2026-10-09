using System.Management.Automation;
using System.Net;

namespace UiPath.PowerShell.Commands;

/// <summary>
/// Which license robot types a server can answer for, so Get-OrchLicenseRuntime and
/// Get-OrchLicenseNamedUser don't fill the screen with errors for types the user never asked
/// about. Without -RobotType (or with a wildcard) they query every type UiPathOrch knows, and an
/// older server rejects the newer ones: 404 on 20.10 and 22.4, 400 "Invalid OData query options."
/// on 22.10 and 23.4 (measured 2026-10-07).
/// </summary>
/// <remarks>
/// Two layers, both only for types the user did not name:
///   1. <see cref="KnownAbsent"/> skips the request where a type was measured missing at or below
///      the server's API version.
///   2. <see cref="IsUnknownTypeResponse"/> recognizes the server's "no such type" answer for the
///      rest, which the cmdlets report as verbose instead of as an error. The version alone
///      cannot decide every case: 22.4 and 22.10 both report API 15, yet only 22.10 knows
///      AutomationKit; 21.10 (API 13) was not measured at all.
/// A type the user names is always sent, and its error shown: the table records measurements,
/// not the product's rules, and a guess must never stop a request that would have worked.
/// </remarks>
internal static class LicenseRobotTypeSupport
{
    // Type -> the highest API version it was measured MISSING on (absent there and below).
    // 20.10.16 = 11.1, 22.4.4 = 15, 22.10.0 = 15, 23.4.0 = 16, 24.10.x / 25.10.2 = 17,
    // Automation Suite 24.10.11 = 18, Automation Cloud = 20.
    private static readonly Dictionary<string, double> MissingThrough = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AttendedStudioWeb"] = 16,                // missing 11.1, 15, 15, 16; answers on 17
        ["AutomationCloudTestAutomation"] = 15,    // missing 11.1, 15, 15; answers on 16
        ["ServerlessTestAutomation"] = 15,         // missing 11.1, 15, 15; answers on 16
        ["AutomationCloud"] = 11.1,                // missing 11.1; answers on 15
        ["Serverless"] = 11.1,                     // missing 11.1; answers on 15
        ["AutomationKit"] = 11.1,                  // missing 11.1 and on 22.4 (15), answers on 22.10 (15)
    };

    // Runtime only: StudioPro is a named-user license, and the runtime listing answers it with
    // 500 "An error has occurred." on every standalone server measured (20.10 to 25.10.2, API 17),
    // while Automation Suite (18) and Cloud (20) return an empty list.
    private static readonly Dictionary<string, double> RuntimeMissingThrough = new(StringComparer.OrdinalIgnoreCase)
    {
        ["StudioPro"] = 17,
    };

    // The drive's API version for the table: signing in first, which learns it on most drives.
    // Still null (a drive that learns it only from a first API response) means no skipping,
    // and the second layer handles that drive's unknown types.
    internal static double? ApiVersionOf(Core.OrchDriveInfo drive)
    {
        // A Ctrl+C during the sign-in ends the command rather than moving on to the next drive's.
        try { drive.OrchAPISession.EnsureAuthenticated(); }
        catch (Exception ex) when (ex is not OperationCanceledException and not System.Management.Automation.PipelineStoppedException)
        { /* the listing request reports any sign-in failure itself */ }
        return drive.OrchAPISession.ApiVersion;
    }

    internal static bool KnownAbsent(string robotType, double? apiVersion, bool runtime)
    {
        if (apiVersion is null) return false;
        if (MissingThrough.TryGetValue(robotType, out var through) && apiVersion <= through) return true;
        return runtime && RuntimeMissingThrough.TryGetValue(robotType, out through) && apiVersion <= through;
    }

    // The server's answer to a robot type it does not have: 404, or 400 "Invalid OData query
    // options." (the robotType parameter is an enum the older server cannot parse).
    internal static bool IsUnknownTypeResponse(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is HttpResponseException { StatusCode: HttpStatusCode.NotFound }) return true;
            if (e.Message.Contains("Invalid OData query options", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    // The robot types the user named outright in -RobotType (no wildcard); only these are sent
    // regardless of the table and have their errors shown.
    internal static HashSet<string> NamedTypes(string[]? robotType)
        => new((robotType ?? []).Where(t => !WildcardPattern.ContainsWildcardCharacters(t)), StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Reports one license-listing failure per (drive, robot type): an error, unless the type was
/// not named by the user and the server answered "no such type" -- then verbose. If a drive
/// answered no type at all, the first of its quieted failures is raised as an error in
/// <see cref="Finish"/>, so a server without the endpoint is not reported as an empty success.
/// </summary>
internal sealed class UnknownRobotTypeReporter(PSCmdlet cmdlet, string errorId, HashSet<string> named)
{
    private readonly HashSet<Core.OrchDriveInfo> _answered = [];
    private readonly Dictionary<Core.OrchDriveInfo, OrchException> _quieted = [];

    internal void Answered(Core.OrchDriveInfo drive) => _answered.Add(drive);

    internal void Failed(Core.OrchDriveInfo drive, string robotType, OrchException ex)
    {
        if (named.Contains(robotType) || !LicenseRobotTypeSupport.IsUnknownTypeResponse(ex))
        {
            cmdlet.WriteError(new ErrorRecord(ex, errorId, ErrorCategory.InvalidOperation, ex.Target));
            return;
        }
        cmdlet.WriteVerbose($"{drive.NameColonSeparator}: robot type '{robotType}' is not available on this server ({ex.Message}).");
        _quieted.TryAdd(drive, ex);
    }

    internal void Finish()
    {
        foreach (var (drive, ex) in _quieted)
        {
            if (!_answered.Contains(drive))
            {
                cmdlet.WriteError(new ErrorRecord(ex, errorId, ErrorCategory.InvalidOperation, ex.Target));
            }
        }
    }
}
