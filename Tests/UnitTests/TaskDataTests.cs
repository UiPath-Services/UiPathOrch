using System.Text.Json;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// GET /tasks/GenericTasks/GetTaskDataById answers in camelCase with a numeric status, unlike OData.
// The payload is the shape measured on Cloud (2026-11-05), trimmed.
public class TaskDataTests
{
    private const string Payload = """
        {"status":2,"data":{"Customer":"ACME","Amount":1200,"Items":["a","b"],"Nested":{"ok":true}},
         "action":"Approve","organizationUnitFullyQualifiedName":"Shared","tags":[],
         "taskAssignmentCriteria":"SingleUser","title":"zzTaskDataTest","type":"ExternalTask",
         "priority":"Low","organizationUnitId":1438561,"key":"1e5521e4-ff51-4c5f-9cf6-409e78aaae6f",
         "creationTime":"2025-12-17T01:29:34.943Z","id":13601113}
        """;

    [Fact]
    public void Reads_the_camelCase_fields()
    {
        var d = JsonSerializer.Deserialize<OrchTaskData>(Payload)!;
        Assert.Equal(13601113, d.Id);
        Assert.Equal("zzTaskDataTest", d.Title);
        Assert.Equal("ExternalTask", d.Type);
        Assert.Equal("Approve", d.Action);
        Assert.Equal(1438561, d.OrganizationUnitId);
        Assert.Equal("Shared", d.OrganizationUnitFullyQualifiedName);
        Assert.NotNull(d.CreationTime);
    }

    [Theory]
    [InlineData(0, "Unassigned")]
    [InlineData(1, "Pending")]
    [InlineData(2, "Completed")]
    [InlineData(7, "7")]
    public void Status_number_reads_as_its_name(int status, string expected)
    {
        var d = JsonSerializer.Deserialize<OrchTaskData>($$"""{"status":{{status}},"id":1}""")!;
        Assert.Equal(expected, d.Status);
    }

    [Fact]
    public void Data_becomes_native_values()
    {
        var d = JsonSerializer.Deserialize<OrchTaskData>(Payload)!;
        Assert.NotNull(d.DataJson);
        var data = DfJsonTools.RecordToPSObject(d.DataJson!.Value);
        Assert.Equal("ACME", data.Properties["Customer"].Value);
        Assert.Equal(1200L, data.Properties["Amount"].Value);
        Assert.Equal(new object?[] { "a", "b" }, (object?[])data.Properties["Items"].Value);
        Assert.Equal(true, ((Dictionary<string, object?>)data.Properties["Nested"].Value)["ok"]);
    }
}
