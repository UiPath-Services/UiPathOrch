using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins SetPmLicenseAllocationCmdlet.ComputeServicePayloads — the pure merge behind the
// per-tenant allocation write. The PUT replaces one service's whole allocation
// (a code left out of a non-empty products array is zeroed), so every payload the
// cmdlet builds has to carry the service's untouched codes as well. These tests
// exist because sending a partial list once wiped a live App Test Robot allocation.
public class ComputeServicePayloadsTests
{
    private static ServiceLicense Service(string serviceType, params (string code, double quantity)[] products) =>
        new()
        {
            serviceType = serviceType,
            products = products.Select(p => new ServiceLicenseProduct { code = p.code, quantity = p.quantity }).ToArray(),
        };

    private static Dictionary<string, double> Requested(params (string code, double quantity)[] items) =>
        items.ToDictionary(i => i.code, i => i.quantity, StringComparer.OrdinalIgnoreCase);

    private static double QuantityOf(ProductQuantity[] payload, string code) =>
        payload.Single(p => string.Equals(p.code, code, StringComparison.OrdinalIgnoreCase)).quantity!.Value;

    [Fact]
    public void UntouchedCodes_OfTheSameService_AreResent()
    {
        var current = new[] { Service("orchestrator", ("APPTESTR", 1), ("NONPR", 2)) };

        var payloads = SetPmLicenseAllocationCmdlet.ComputeServicePayloads(
            current, Requested(("UNATT", 0)), null, out var unplaced);

        Assert.Empty(unplaced);
        var orchestrator = payloads["orchestrator"];
        Assert.Equal(1d, QuantityOf(orchestrator, "APPTESTR"));
        Assert.Equal(2d, QuantityOf(orchestrator, "NONPR"));
        Assert.Equal(0d, QuantityOf(orchestrator, "UNATT"));
    }

    [Fact]
    public void RequestedQuantity_OverridesCurrent()
    {
        var current = new[] { Service("orchestrator", ("APPTESTR", 1)) };

        var payloads = SetPmLicenseAllocationCmdlet.ComputeServicePayloads(
            current, Requested(("APPTESTR", 3)), null, out _);

        Assert.Equal(3d, QuantityOf(payloads["orchestrator"], "APPTESTR"));
        Assert.Single(payloads);
    }

    [Fact]
    public void ServicesWithoutARequestedCode_AreNotWritten()
    {
        var current = new[]
        {
            Service("orchestrator", ("APPTESTR", 1)),
            Service("dataservice", ("DSU", 2)),
            Service("tenant", ("PLTU", 3000)),
        };

        var payloads = SetPmLicenseAllocationCmdlet.ComputeServicePayloads(
            current, Requested(("PLTU", 5000)), null, out _);

        Assert.Single(payloads);
        Assert.True(payloads.ContainsKey("tenant"));
    }

    [Fact]
    public void CodesSpanningServices_ProduceOnePayloadEach()
    {
        var current = new[]
        {
            Service("orchestrator", ("APPTESTR", 1)),
            Service("tenant", ("PLTU", 3000), ("SPR", 50000)),
        };

        var payloads = SetPmLicenseAllocationCmdlet.ComputeServicePayloads(
            current, Requested(("APPTESTR", 2), ("SPR", 60000)), null, out _);

        Assert.Equal(2, payloads.Count);
        Assert.Equal(2d, QuantityOf(payloads["orchestrator"], "APPTESTR"));
        Assert.Equal(60000d, QuantityOf(payloads["tenant"], "SPR"));
        // The tenant service's other consumable must survive the replace-all PUT.
        Assert.Equal(3000d, QuantityOf(payloads["tenant"], "PLTU"));
    }

    [Fact]
    public void CodeAtZero_IsPlacedByTheStaticGrouping()
    {
        // The service-licenses endpoint omits codes allocated 0, so raising one from
        // 0 has to fall back to the known grouping instead of failing.
        var current = new[] { Service("orchestrator", ("UNATT", 1)) };

        var payloads = SetPmLicenseAllocationCmdlet.ComputeServicePayloads(
            current, Requested(("APPTESTR", 1), ("PLTU", 100), ("DSU", 1)), null, out var unplaced);

        Assert.Empty(unplaced);
        Assert.Equal(1d, QuantityOf(payloads["orchestrator"], "APPTESTR"));
        Assert.Equal(1d, QuantityOf(payloads["orchestrator"], "UNATT"));
        Assert.Equal(100d, QuantityOf(payloads["tenant"], "PLTU"));
        Assert.Equal(1d, QuantityOf(payloads["dataservice"], "DSU"));
    }

    [Fact]
    public void UnknownCode_IsReportedNotGuessed()
    {
        var payloads = SetPmLicenseAllocationCmdlet.ComputeServicePayloads(
            [], Requested(("MADEUPCODE", 1)), null, out var unplaced);

        Assert.Empty(payloads);
        Assert.Equal("MADEUPCODE", Assert.Single(unplaced));
    }

    [Fact]
    public void ForcedServiceType_WinsOverBothSources()
    {
        var current = new[] { Service("orchestrator", ("APPTESTR", 1)), Service("tenant", ("PLTU", 10)) };

        var payloads = SetPmLicenseAllocationCmdlet.ComputeServicePayloads(
            current, Requested(("APPTESTR", 2)), "testmanager", out var unplaced);

        Assert.Empty(unplaced);
        Assert.Single(payloads);
        Assert.True(payloads.ContainsKey("testmanager"));
        Assert.Equal(2d, QuantityOf(payloads["testmanager"], "APPTESTR"));
    }

    [Fact]
    public void ServiceTypeOfProductCode_GroupsTheKnownCodes()
    {
        Assert.Equal("orchestrator", SetPmLicenseAllocationCmdlet.ServiceTypeOfProductCode("UNATT"));
        Assert.Equal("orchestrator", SetPmLicenseAllocationCmdlet.ServiceTypeOfProductCode("apptestr"));
        Assert.Equal("dataservice", SetPmLicenseAllocationCmdlet.ServiceTypeOfProductCode("DSU"));
        Assert.Equal("tenant", SetPmLicenseAllocationCmdlet.ServiceTypeOfProductCode("HEALTEST"));
        Assert.Null(SetPmLicenseAllocationCmdlet.ServiceTypeOfProductCode("NOSUCHCODE"));
    }
}
