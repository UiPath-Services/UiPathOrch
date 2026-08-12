using System;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Completer;
using Xunit;

namespace UnitTests;

// Pins which completer is wired to each time-zone parameter across the whole
// module. The two types are interchangeable at the compiler level and differ
// only in what they emit:
//   TimeZoneCompleter   -> DisplayName ("(UTC+09:00) Osaka, Sapporo, Tokyo")
//   TimeZoneIdCompleter -> Id          ("Tokyo Standard Time")
// A `-TimeZoneId` parameter binds the Id and a `-TimeZone` parameter binds the
// display name (resolved name->id at submit time), so swapping them produces
// completions the server rejects rather than a compile error. Update-OrchTestSetSchedule
// already pinned its own pair; this covers the rest of the family so an
// attribute dropped in a refactor fails here instead of in a user's console.
public class TimeZoneCompleterWiringShapeTests
{
    public static TheoryData<Type, string, Type> TimeZoneParameters => new()
    {
        // -TimeZoneId / -...TimeZoneId : bind the Id
        { typeof(NewTriggerCmdlet),          "TimeZoneId",             typeof(TimeZoneIdCompleter) },
        { typeof(UpdateTriggerCmdlet),       "TimeZoneId",             typeof(TimeZoneIdCompleter) },
        { typeof(UpdateMachineCmdlet),       "MaintenanceTimeZoneId",  typeof(TimeZoneIdCompleter) },
        { typeof(NewTestSetScheduleCmdlet),  "TimeZoneId",             typeof(TimeZoneIdCompleter) },

        // -TimeZone / -...TimeZone : bind the display name
        { typeof(NewTriggerCmdlet),          "TimeZone",               typeof(TimeZoneCompleter) },
        { typeof(UpdateTriggerCmdlet),       "TimeZone",               typeof(TimeZoneCompleter) },
        { typeof(UpdateMachineCmdlet),       "MaintenanceTimeZone",    typeof(TimeZoneCompleter) },
    };

    [Theory]
    [MemberData(nameof(TimeZoneParameters))]
    public void TimeZoneParameter_uses_the_completer_matching_what_it_binds(
        Type cmdletType, string parameterName, Type expectedCompleter)
    {
        var prop = cmdletType.GetProperty(parameterName);
        Assert.NotNull(prop);

        var completer = prop!.GetCustomAttribute<ArgumentCompleterAttribute>();
        Assert.NotNull(completer);
        Assert.Equal(expectedCompleter, completer!.Type);
    }

    [Theory]
    [MemberData(nameof(TimeZoneParameters))]
    public void TimeZoneParameter_is_a_cmdlet_parameter(Type cmdletType, string parameterName, Type _)
    {
        // A completer on a property that is not a [Parameter] would never run.
        var prop = cmdletType.GetProperty(parameterName);
        Assert.NotNull(prop);
        Assert.NotNull(prop!.GetCustomAttribute<ParameterAttribute>());
    }

    [Fact]
    public void Hidden_id_parameters_stay_hidden()
    {
        // The -TimeZoneId variants are DontShow: they exist for CSV / pipeline
        // binding, so they are kept out of parameter-name completion while the
        // friendlier -TimeZone stays visible. DontShow does not suppress
        // *argument* completion, which is why wiring a completer onto a hidden
        // parameter is still worth doing -- that is the contract pinned here.
        foreach (var (cmdletType, parameterName) in new[]
        {
            (typeof(NewTriggerCmdlet),    "TimeZoneId"),
            (typeof(UpdateTriggerCmdlet), "TimeZoneId"),
            (typeof(UpdateMachineCmdlet), "MaintenanceTimeZoneId"),
        })
        {
            var attr = cmdletType.GetProperty(parameterName)!.GetCustomAttribute<ParameterAttribute>();
            Assert.True(attr!.DontShow, $"{cmdletType.Name}.{parameterName} should stay DontShow");
        }
    }
}
