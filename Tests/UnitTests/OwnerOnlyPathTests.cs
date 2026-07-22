using System.Runtime.Versioning;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Owner-only permissions for the paths that can carry credentials -- the config file and the
// HTTP log files.
//
// The assertions are meaningful on Linux and macOS only; on Windows the APIs under test are
// no-ops by design (the profile paths carry an ACL instead, and SetUnixFileMode /
// CreateDirectory(mode) throw PlatformNotSupportedException there). Rather than pull in a
// Skippable-fact package for one file, each test returns early on Windows -- CI runs the full
// unit suite on ubuntu-latest and macos-latest, so the real assertions do execute.
public class OwnerOnlyPathTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "UiPathOrchTests_" + Guid.NewGuid().ToString("N")[..12]);

    public OwnerOnlyPathTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"temp cleanup: {ex.Message}"); }
        GC.SuppressFinalize(this);
    }

    // Annotated so the platform analyzer (CA1416) propagates the Unix-only requirement to the
    // call sites instead of flagging it here; every caller is behind an IsWindows() early return,
    // which the analyzer recognizes.
    [UnsupportedOSPlatform("windows")]
    private static UnixFileMode PermissionBits(string path) =>
        File.GetUnixFileMode(path) & ~UnixFileMode.StickyBit;

    [Fact]
    public void RestrictFile_leaves_only_owner_read_write()
    {
        if (OperatingSystem.IsWindows()) return;

        var path = Path.Combine(_root, "log.txt");
        File.WriteAllText(path, "secret");
        // Start from a world-readable mode so the assertion cannot pass by accident on a host
        // with an already-restrictive umask.
        File.SetUnixFileMode(path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite |
            UnixFileMode.GroupRead | UnixFileMode.OtherRead);

        OwnerOnlyPath.RestrictFile(path);

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, PermissionBits(path));
    }

    [Fact]
    public void CreateRestrictedDirectory_creates_it_owner_only()
    {
        var path = Path.Combine(_root, "Logs");

        OwnerOnlyPath.CreateRestrictedDirectory(path);

        // The directory must exist on every platform -- that part is not Unix-specific, and a
        // regression there would stop logging outright.
        Assert.True(Directory.Exists(path));

        if (OperatingSystem.IsWindows()) return;

        Assert.Equal(
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
            PermissionBits(path));
    }

    // Create-time only: an operator who widened an existing log directory (to let a collection
    // agent read it) must not have that silently reverted on the next drive mount.
    [Fact]
    public void CreateRestrictedDirectory_does_not_retighten_an_existing_directory()
    {
        if (OperatingSystem.IsWindows()) return;

        var path = Path.Combine(_root, "Existing");
        Directory.CreateDirectory(path);
        var widened =
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute;
        File.SetUnixFileMode(path, widened);

        OwnerOnlyPath.CreateRestrictedDirectory(path);

        Assert.Equal(widened, PermissionBits(path));
    }

    // A path that cannot be chmod'ed must not throw: losing the tightening is acceptable,
    // taking down the cmdlet (or the log write it was protecting) is not.
    [Fact]
    public void RestrictFile_on_a_missing_path_is_swallowed()
    {
        OwnerOnlyPath.RestrictFile(Path.Combine(_root, "does-not-exist"));
    }
}
