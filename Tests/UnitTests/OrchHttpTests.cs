using System.Net;
using System.Net.Sockets;
using Xunit;
using static UiPath.OrchAPI.OrchHttp;

namespace UnitTests;

// Unit tests for OrchHttp.OrderByFamilyInterleaved -- the RFC 8305 §4 address-ordering
// helper used by the Happy Eyeballs dialer. The policy pinned down here:
//   - addresses are interleaved by family (IPv6, IPv4, IPv6, IPv4, ...), IPv6 first;
//   - order within a family is preserved as returned by the resolver;
//   - an uneven split appends the remainder of the longer family;
//   - a single-family input is returned in order (no reordering, nothing dropped);
//   - exotic families (neither IPv4 nor IPv6) are kept, appended at the end.
public class OrchHttpTests
{
    private static IPAddress V6(string s) => IPAddress.Parse(s);
    private static IPAddress V4(string s) => IPAddress.Parse(s);

    private static string[] Order(params IPAddress[] addrs) =>
        OrderByFamilyInterleaved(addrs).Select(a => a.ToString()).ToArray();

    [Fact]
    public void InterleavesFamiliesStartingWithIPv6()
    {
        var got = Order(V4("10.0.0.1"), V6("2001:db8::1"), V4("10.0.0.2"), V6("2001:db8::2"));
        Assert.Equal(new[] { "2001:db8::1", "10.0.0.1", "2001:db8::2", "10.0.0.2" }, got);
    }

    [Fact]
    public void PreservesOrderWithinEachFamily()
    {
        var got = Order(V6("2001:db8::a"), V6("2001:db8::b"), V4("10.0.0.9"), V4("10.0.0.8"));
        // v6 in input order, v4 in input order, interleaved v6-first.
        Assert.Equal(new[] { "2001:db8::a", "10.0.0.9", "2001:db8::b", "10.0.0.8" }, got);
    }

    [Fact]
    public void AppendsRemainderWhenMoreIPv6ThanIPv4()
    {
        var got = Order(V6("2001:db8::1"), V6("2001:db8::2"), V6("2001:db8::3"), V4("10.0.0.1"));
        Assert.Equal(new[] { "2001:db8::1", "10.0.0.1", "2001:db8::2", "2001:db8::3" }, got);
    }

    [Fact]
    public void AppendsRemainderWhenMoreIPv4ThanIPv6()
    {
        var got = Order(V4("10.0.0.1"), V4("10.0.0.2"), V4("10.0.0.3"), V6("2001:db8::1"));
        Assert.Equal(new[] { "2001:db8::1", "10.0.0.1", "10.0.0.2", "10.0.0.3" }, got);
    }

    [Fact]
    public void SingleFamilyIPv6IsReturnedInOrder()
    {
        var got = Order(V6("2001:db8::1"), V6("2001:db8::2"));
        Assert.Equal(new[] { "2001:db8::1", "2001:db8::2" }, got);
    }

    [Fact]
    public void SingleFamilyIPv4IsReturnedInOrder()
    {
        var got = Order(V4("10.0.0.1"), V4("10.0.0.2"));
        Assert.Equal(new[] { "10.0.0.1", "10.0.0.2" }, got);
    }

    [Fact]
    public void SingleAddressIsReturnedAsIs()
    {
        var got = Order(V4("10.0.0.1"));
        Assert.Equal(new[] { "10.0.0.1" }, got);
    }

    [Fact]
    public void DropsNothing()
    {
        var input = new[] { V6("2001:db8::1"), V4("10.0.0.1"), V6("2001:db8::2"), V4("10.0.0.2"), V4("10.0.0.3") };
        var got = OrderByFamilyInterleaved(input);
        Assert.Equal(input.Length, got.Count);
        Assert.Equal(input.OrderBy(a => a.ToString()), got.OrderBy(a => a.ToString()));
    }
}
