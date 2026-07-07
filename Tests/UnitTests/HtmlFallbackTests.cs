using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// Pins OrchAPISession.LooksLikeHtml — the sniff that turns an SPA index.html (served with
// HTTP 200 when a portal/identity endpoint is absent on this deployment, e.g. an
// Automation-Cloud-only licensing endpoint hit on-prem) into a clear error instead of a
// cryptic "'<' is an invalid start of a value" JSON parse failure.
public class HtmlFallbackTests
{
    private static readonly string Bom = ((char)0xFEFF).ToString();

    [Theory]
    [InlineData("<!DOCTYPE html><html><head></head></html>")]
    [InlineData("<html lang=\"en\">")]
    [InlineData("   \r\n\t<!DOCTYPE html>")]          // leading whitespace
    public void Detects_html_documents(string body)
        => Assert.True(OrchAPISession.LooksLikeHtml(body));

    [Fact]
    public void Detects_html_after_bom_and_whitespace()
    {
        Assert.True(OrchAPISession.LooksLikeHtml(Bom + "<!DOCTYPE html>"));
        Assert.True(OrchAPISession.LooksLikeHtml(Bom + "  \n <html>"));
    }

    [Theory]
    [InlineData("{\"results\":[]}")]                  // JSON object
    [InlineData("[1,2,3]")]                           // JSON array
    [InlineData("  \n {\"totalCount\":0}")]           // whitespace then JSON
    [InlineData("\"a string\"")]                      // JSON string
    [InlineData("42")]                                // JSON number
    [InlineData("null")]
    [InlineData("")]                                  // empty body
    [InlineData("   ")]                               // whitespace only
    public void Passes_json_and_empty_bodies(string body)
        => Assert.False(OrchAPISession.LooksLikeHtml(body));
}
