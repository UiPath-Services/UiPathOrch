namespace UiPath.PowerShell.Core;

/// <summary>
/// The time zones Orchestrator accepts, on every OS. Orchestrator takes WINDOWS time-zone
/// ids only ("Tokyo Standard Time"); an IANA id ("Asia/Tokyo", "Etc/UTC") is rejected with
/// "not a valid windows time zone id".
/// </summary>
/// <remarks>
/// On Linux and macOS the OS time-zone database is IANA, so the cmdlets that used
/// TimeZoneInfo directly failed there (seen 2026-11-21 on RHEL 8.5): New-OrchTrigger with
/// no time zone sent the local "Etc/UTC" and was rejected; -TimeZone with a Windows display
/// name ("(UTC+09:00) Osaka, Sapporo, Tokyo") was not found, and with one of the OS's own
/// names resolved to an IANA id and was rejected; its completer offered the 425 OS names,
/// none of which could succeed. Only -TimeZoneId's completer already used the embedded
/// Windows table. Everything that lists, resolves or defaults a time zone goes through here.
/// </remarks>
internal static class OrchTimeZones
{
    /// <summary>
    /// Every time zone as (Windows id, display name): the live OS list on Windows, the
    /// embedded Windows table (<see cref="WindowsTimeZones"/>) elsewhere.
    /// </summary>
    internal static IEnumerable<(string Id, string DisplayName)> All() =>
        OperatingSystem.IsWindows()
            ? TimeZoneInfo.GetSystemTimeZones().Select(t => (t.Id, t.DisplayName))
            : WindowsTimeZones.All;

    /// <summary>The local time zone as a Windows id, the default where none is given.</summary>
    internal static string LocalId() => ToWindowsId(TimeZoneInfo.Local.Id, OperatingSystem.IsWindows());

    /// <summary>
    /// A local time-zone id as Orchestrator takes it: as is on Windows; elsewhere the IANA
    /// id converted to its Windows equivalent ("Etc/UTC" → "UTC", "Asia/Tokyo" → "Tokyo
    /// Standard Time"), or "UTC" for one with no Windows equivalent.
    /// </summary>
    internal static string ToWindowsId(string localId, bool isWindows)
    {
        if (isWindows) return localId;
        return TimeZoneInfo.TryConvertIanaIdToWindowsId(localId, out var windowsId) && !string.IsNullOrEmpty(windowsId)
            ? windowsId
            : "UTC";
    }
}
