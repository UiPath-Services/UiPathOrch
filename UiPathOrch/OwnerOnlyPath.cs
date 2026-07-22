namespace UiPath.OrchAPI;

/// <summary>
/// Owner-only (0600 / 0700) permissions for the paths this module writes that can carry
/// credentials: the config file (AppSecret / PAT / Password) and the HTTP log files -- request
/// and response bodies are recorded at Trace/Verbose and on every error, which is where a cmdlet
/// submitting a credential lands (see OrchAPISession.EnsureLoggingWarningEmitted, which tells
/// the user exactly that).
///
/// On Unix a plain file/directory create takes its mode from the process umask -- commonly 022,
/// so 0644 / 0755, readable by every other account on the host. On Windows the per-user profile
/// paths these live under already carry a restrictive ACL, and the SetUnixFileMode /
/// CreateDirectory(mode) APIs throw PlatformNotSupportedException, hence the guards.
///
/// The config file has been created 0600 since the mode was first applied there; this type
/// exists so the log paths get the same treatment from the same place, rather than the rule
/// living at one call site and being absent at the others.
///
/// Applied at CREATE time only. Re-tightening something that already exists would silently
/// override a mode the user set deliberately (e.g. to let a log-collection agent read a log), so
/// files and directories written by earlier versions keep whatever mode they already have.
/// </summary>
internal static class OwnerOnlyPath
{
    private const UnixFileMode OwnerReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private const UnixFileMode OwnerReadWriteTraverse =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;

    /// <summary>
    /// Restrict an existing file to 0600. No-op on Windows. Call only for a file this process
    /// just created -- see the type remarks on why existing files are left alone.
    /// </summary>
    internal static void RestrictFile(string path)
    {
        if (OperatingSystem.IsWindows()) return;

        try
        {
            File.SetUnixFileMode(path, OwnerReadWrite);
        }
        catch (Exception ex)
        {
            // Best effort. An exotic filesystem (a mounted share, a container overlay, a FAT
            // volume) can reject chmod; losing the tightening must not take down the cmdlet, and
            // for the log path in particular must not take down the write it was protecting.
            System.Diagnostics.Debug.WriteLine(
                $"Could not restrict file permissions on '{path}': {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Create the directory if absent, with 0700 on Unix. Existing directories are returned
    /// untouched (including their mode), matching Directory.CreateDirectory's own semantics.
    /// </summary>
    internal static void CreateRestrictedDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(path);
            return;
        }

        try
        {
            // The mode overload applies only to directories this call actually creates, which is
            // precisely the create-time-only rule above.
            Directory.CreateDirectory(path, OwnerReadWriteTraverse);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Could not create '{path}' with restricted permissions: {ex.GetType().Name}: {ex.Message}");
            // Fall back to an unrestricted create: a log directory we cannot chmod is still far
            // better than a drive that cannot log at all.
            Directory.CreateDirectory(path);
        }
    }
}
