using System.Net;
using System.Net.Http;
using UiPath.OrchAPI;
using UiPath.PowerShell.Commands;
using Xunit;

namespace UnitTests;

// Automation Suite 24.10.11 does not serve /odata/HttpTriggers: the route answers
// 404 {"message":"Invalid request!","errorCode":1000} in every folder while its OData neighbours
// answer 200, and the web UI offers only Time and Queue triggers where Automation Cloud also has
// Event and API triggers. Get-OrchApiTrigger and Copy-Item's API-trigger stage recognise that
// answer and skip once per drive instead of repeating the server's bare "Invalid request!" for
// every folder of a -Recurse run. These pin the recogniser.
public class IsEndpointNotFoundTests
{
    private static HttpResponseException Http(HttpStatusCode code)
        => new("body", new HttpResponseMessage(code));

    [Fact]
    public void NotFound_IsRecognised()
        => Assert.True(OrchAPISession.IsEndpointNotFound(Http(HttpStatusCode.NotFound)));

    // The neighbouring failures on the same route must NOT latch the feature off: 400 is what an
    // older Orchestrator answers for a bad OData query, and 401/403/500 are situational.
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void OtherStatusCodes_AreNotRecognised(HttpStatusCode code)
        => Assert.False(OrchAPISession.IsEndpointNotFound(Http(code)));

    // The cache and cmdlet layers wrap the HTTP failure, so the 404 is reached through InnerException.
    [Fact]
    public void NotFound_IsFoundThroughAWrapper()
    {
        var wrapped = new OrchException("AS:\\Shared", "Copying API triggers", Http(HttpStatusCode.NotFound));
        Assert.True(OrchAPISession.IsEndpointNotFound(wrapped));
    }

    [Fact]
    public void UnrelatedException_IsNotRecognised()
        => Assert.False(OrchAPISession.IsEndpointNotFound(new InvalidOperationException("nope")));

    [Fact]
    public void Null_IsNotRecognised()
        => Assert.False(OrchAPISession.IsEndpointNotFound(null));

    // The notice names the drive and says what is skipped, so a migration log shows which side
    // lacked the feature rather than an unattributed "Invalid request!".
    [Fact]
    public void Warning_NamesTheDriveAndTheConsequence()
    {
        var w = OrchAPISession.ApiTriggersUnavailableWarning("AS:");
        Assert.StartsWith("AS:", w, StringComparison.Ordinal);
        Assert.Contains("/odata/HttpTriggers", w, StringComparison.Ordinal);
        Assert.Contains("Clear-OrchCache", w, StringComparison.Ordinal);
    }
}
