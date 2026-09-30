namespace UiPath.PowerShell.Commands;

/// <summary>
/// The Orchestrator API version the newest machine runtime slots were introduced in, and the
/// check the machine cmdlets run before sending one.
///
/// A field the server does not model is not ignored: the PATCH endpoint answers a body it
/// cannot bind with <c>machineDto must not be null</c>, which tells the user nothing. Since
/// the drive already knows the server's API version, the cmdlets drop such a slot and say why.
///
/// Only slots whose absence was confirmed against a real server are listed here. The older
/// fields are deliberately NOT version-gated: probing a 21.10.4 (API 13.0) Orchestrator showed
/// the inherited "added in v15" notes in OrchAPISession.AddMachine to be wrong for at least
/// AutomationType, TargetFramework and AutomationCloudSlots — that server accepts and applies
/// them — while MaintenanceWindow, which the same notes place at v13, is rejected there. A
/// wrong constant blocks a field that works, so those are sent and the server's answer is
/// translated instead (see UpdateMachineCmdlet.DescribeUnboundPayload).
///
/// AgentSlots and HeadlessSlots stay unguarded for a different reason: they have been sent to
/// older servers unchanged since they were modelled, and zeroing them on a server that does
/// know them would be the worse surprise (same reasoning as AddMachine).
/// </summary>
internal static class MachineFieldVersions
{
    // Verified: a 24.10.11 Automation Suite (API 18.0) rejects a PATCH carrying AppTestSlots
    // and does not return the property at all; cloud (API 20) accepts and applies it.
    public const int AppTestSlots = 19;
    public const int HostingSlots = 19;
    public const int PerformanceTestSlots = 20;
    public const int FunctionSlots = 20;

    /// <summary>
    /// True when the server is new enough for the field, or when its API version is unknown
    /// (a drive not signed in yet) — matching how AddMachine's version strips read a null
    /// ApiVersion. Otherwise reports the drop through <paramref name="warn"/> and returns false.
    /// </summary>
    public static bool Supported(int introducedIn, double? apiVersion, string parameterName, string target, Action<string> warn)
    {
        if (apiVersion is null || apiVersion >= introducedIn) return true;

        warn($"\"{target}\": -{parameterName} requires Orchestrator API {introducedIn}.0 or newer; this server reports API {apiVersion}. It was not sent.");
        return false;
    }

    /// <summary>Returns the value to send, or null once the field is reported as unsupported.</summary>
    public static int? SupportedOrNull(int? value, int introducedIn, double? apiVersion, string parameterName, string target, Action<string> warn) =>
        value is null || Supported(introducedIn, apiVersion, parameterName, target, warn) ? value : null;
}
