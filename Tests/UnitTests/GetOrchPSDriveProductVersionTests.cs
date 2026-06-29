using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Regression guard for the Get-OrchPSDrive ProductVersion bug: an already-connected
// drive showed a blank ProductVersion because the cmdlet only fetched
// /api/Status/Version under -Force, while the OrchPSDrive ctor otherwise reads the
// org-global version cache passively (CachedValue) — which normal drive use never fills.
//
// The fetch itself needs a live authenticated session (no fixture in the unit env),
// so we pin the decision rule that governs it. ShouldFetchProductVersion must:
//   - fetch under -Force (the full connect authenticates the drive),
//   - fetch for an already-authenticated drive without -Force (cheap, fills the column),
//   - NOT fetch for a cold drive without -Force (a fetch would trigger auth/PKCE for a
//     drive the user merely listed).
public class GetOrchPSDriveProductVersionTests
{
    [Theory]
    [InlineData(true, true, true)]    // -Force, authenticated      -> fetch
    [InlineData(true, false, true)]   // -Force, cold               -> fetch (connect authenticates)
    [InlineData(false, true, true)]   // connected, no -Force       -> fetch (the regressed case)
    [InlineData(false, false, false)] // cold, no -Force            -> do NOT fetch (no PKCE)
    public void ShouldFetchProductVersion_Rule(bool force, bool isAuthenticated, bool expected)
    {
        Assert.Equal(expected, GetOrchPSDriveCmdlet.ShouldFetchProductVersion(force, isAuthenticated));
    }
}
