using System;
using System.Net;
using System.Net.Http;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

// Get-OrchLicenseRuntime / Get-OrchLicenseNamedUser query every robot type UiPathOrch knows when
// -RobotType is not given, and older servers reject the newer types. The table skips the types
// measured missing (2026-10-07); a type the user names is always sent.
public class LicenseRobotTypeSupportTests
{
    [Theory]
    // 20.10.16 = 11.1
    [InlineData("AttendedStudioWeb", 11.1, true)]
    [InlineData("AutomationCloud", 11.1, true)]
    [InlineData("AutomationKit", 11.1, true)]
    [InlineData("Unattended", 11.1, false)]
    // 22.4.4 / 22.10.0 = 15: AutomationKit is missing on 22.4 but present on 22.10, so not skipped
    [InlineData("AutomationKit", 15, false)]
    [InlineData("AutomationCloud", 15, false)]
    [InlineData("ServerlessTestAutomation", 15, true)]
    // 23.4.0 = 16
    [InlineData("AttendedStudioWeb", 16, true)]
    [InlineData("ServerlessTestAutomation", 16, false)]
    // 24.10 / 25.10.2 = 17
    [InlineData("AttendedStudioWeb", 17, false)]
    public void Skips_only_measured_absences(string type, double api, bool absent)
    {
        Assert.Equal(absent, LicenseRobotTypeSupport.KnownAbsent(type, api, runtime: false));
        Assert.Equal(absent, LicenseRobotTypeSupport.KnownAbsent(type, api, runtime: true));
    }

    [Theory]
    [InlineData(17, true)]    // every standalone server measured: 500
    [InlineData(18, false)]   // Automation Suite 24.10.11: empty list
    [InlineData(20, false)]   // Automation Cloud
    public void StudioPro_is_skipped_for_runtimes_on_standalone_only(double api, bool absent)
    {
        Assert.Equal(absent, LicenseRobotTypeSupport.KnownAbsent("StudioPro", api, runtime: true));
        Assert.False(LicenseRobotTypeSupport.KnownAbsent("StudioPro", api, runtime: false));
    }

    [Fact]
    public void An_unknown_version_skips_nothing()
        => Assert.False(LicenseRobotTypeSupport.KnownAbsent("AttendedStudioWeb", null, runtime: true));

    [Fact]
    public void Recognises_a_404_and_the_OData_enum_refusal_but_not_a_server_error()
    {
        static OrchException Wrap(HttpStatusCode code, string message)
            => new("op2210:\\", new HttpResponseException(message, new HttpResponseMessage(code)));

        Assert.True(LicenseRobotTypeSupport.IsUnknownTypeResponse(Wrap(HttpStatusCode.NotFound, "Not Found")));
        Assert.True(LicenseRobotTypeSupport.IsUnknownTypeResponse(Wrap(HttpStatusCode.BadRequest, "Invalid OData query options.")));
        Assert.False(LicenseRobotTypeSupport.IsUnknownTypeResponse(Wrap(HttpStatusCode.InternalServerError, "An error has occurred.")));
        Assert.False(LicenseRobotTypeSupport.IsUnknownTypeResponse(Wrap(HttpStatusCode.BadRequest, "Something else.")));
    }

    [Fact]
    public void Only_literal_names_count_as_named()
    {
        var named = LicenseRobotTypeSupport.NamedTypes(["Unattended", "Attended*", "studiopro"]);
        Assert.Contains("unattended", named);
        Assert.Contains("StudioPro", named);
        Assert.DoesNotContain("AttendedStudioWeb", named);
    }

    [Theory]
    [InlineData("User", "DirectoryUser")]
    [InlineData("Robot", "DirectoryRobot")]
    public void A_local_account_maps_to_its_directory_type(string local, string directory)
        => Assert.Equal(directory, OrchProvider.LocalToDirectoryType(local));
}
