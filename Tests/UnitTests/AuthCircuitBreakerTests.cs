using System;
using UiPath.OrchAPI;
using Xunit;

namespace UnitTests;

// The auth circuit breaker latches a hard auth failure so subsequent calls fail fast
// instead of re-authenticating + retrying per item. It must re-throw the SAME cause,
// stay tripped until explicitly reset, and start un-tripped.
public class AuthCircuitBreakerTests
{
    [Fact]
    public void Starts_untripped_and_does_not_throw()
    {
        var breaker = new AuthCircuitBreaker();
        Assert.False(breaker.IsTripped);
        breaker.ThrowIfTripped(); // no-op
    }

    [Fact]
    public void Trip_latches_and_rethrows_the_same_cause()
    {
        var breaker = new AuthCircuitBreaker();
        var cause = new InvalidOperationException("auth broken");
        breaker.Trip(cause);

        Assert.True(breaker.IsTripped);
        var thrown1 = Assert.Throws<InvalidOperationException>(() => breaker.ThrowIfTripped());
        var thrown2 = Assert.Throws<InvalidOperationException>(() => breaker.ThrowIfTripped());
        Assert.Same(cause, thrown1);
        Assert.Same(cause, thrown2); // stays tripped across calls
    }

    [Fact]
    public void Reset_clears_the_latch()
    {
        var breaker = new AuthCircuitBreaker();
        breaker.Trip(new Exception("x"));
        breaker.Reset();

        Assert.False(breaker.IsTripped);
        breaker.ThrowIfTripped(); // no-op after reset
    }
}
