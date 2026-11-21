using System.ComponentModel;
using System.Diagnostics;

namespace UiPath.PowerShell.Core;

/// <summary>
/// Opens an Orchestrator page in the default browser, and says where it is when that
/// cannot be done.
/// </summary>
/// <remarks>
/// The cmdlets that open a page (Invoke-Item on a folder, Open-OrchJob, Edit-OrchProcess,
/// Invoke-Item on a Du/Tm project) called Process.Start(url) and nothing else. Over SSH to
/// a Linux server -- no X11, no Wayland -- xdg-open failed on its own terminal and the
/// cmdlet returned without a word (seen 2026-11-21 on RHEL 8.5), and with no xdg-open at all
/// the Win32Exception reached the user raw. On Linux and the BSDs the URL is also shown after
/// a launch that seemed to work: xdg-open returns before any browser runs, so one that fails
/// afterwards (ssh -X to a server without a browser) cannot be detected -- the same reasoning
/// as the PKCE sign-in (OrchestratorAuthManager.LaunchSignInBrowser).
/// </remarks>
internal static class BrowserLauncher
{
    /// <param name="warn">Shown when no browser could be opened: the URL to open by hand.</param>
    /// <param name="info">Shown on Linux/BSD after a launch: the URL, in case no browser appears.</param>
    internal static void Open(string url, Action<string> warn, Action<string> info)
    {
        bool desktopOs = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        if (!desktopOs && !HasGraphicalSession(
                Environment.GetEnvironmentVariable("DISPLAY"),
                Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
        {
            warn(BuildNoBrowserMessage(url, "neither DISPLAY nor WAYLAND_DISPLAY is set"));
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // No handler for http URLs: xdg-open missing, no default browser registered.
            warn(BuildNoBrowserMessage(url, ex.Message));
            return;
        }

        if (!desktopOs) info($"Opening in a browser (if none appears, open it yourself): {url}");
    }

    /// <summary>X11 or Wayland is what xdg-open needs to show a browser on Linux and the BSDs.</summary>
    internal static bool HasGraphicalSession(string? display, string? waylandDisplay)
        => !string.IsNullOrEmpty(display) || !string.IsNullOrEmpty(waylandDisplay);

    internal static string BuildNoBrowserMessage(string url, string reason)
        => $"No browser could be opened ({reason}). Open this URL in a browser: {url}";
}
