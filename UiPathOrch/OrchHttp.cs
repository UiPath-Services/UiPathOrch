using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using UiPath.PowerShell.Core;

namespace UiPath.OrchAPI;

/// <summary>
/// Factory for the <see cref="SocketsHttpHandler"/> shared by every Orchestrator-bound
/// <see cref="HttpClient"/> (API calls, auth token exchange, and bucket-item transfers all
/// route through <c>OrchAPISession.InitializeHttpClient</c>, so configuring it here covers
/// all paths).
///
/// Centralizes two things:
///   - proxy configuration (parsed from the PSDrive's <see cref="ProxySettings"/>), and
///   - an RFC 8305-style "Happy Eyeballs" dialer that races a full TCP+TLS handshake across
///     every resolved address, to survive IPv6-only / NAT64+DNS64 networks.
/// </summary>
internal static class OrchHttp
{
    // Recycle pooled connections so a backend that goes bad (or a stale DNS answer) is
    // re-dialed instead of being pinned for the life of the session.
    private static readonly TimeSpan PooledConnectionLifetime = TimeSpan.FromMinutes(2);

    // RFC 8305 §5 "Connection Attempt Delay": how long to wait before launching the next
    // address's attempt (the RFC's recommended default; it requires >= 100 ms and <= 2 s).
    private static readonly TimeSpan ConnectionAttemptDelay = TimeSpan.FromMilliseconds(250);

    public static SocketsHttpHandler CreateHandler(ProxySettings? proxy, bool ignoreSslErrors)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = PooledConnectionLifetime,
        };

        bool proxyConfigured = ConfigureProxy(handler, proxy);

        if (ignoreSslErrors)
        {
            // Proxy path: the handler performs TLS, so the override goes on its SslOptions.
            // Direct path: the dialer performs TLS, so the override is also applied there
            // (see ConnectAndAuthenticateAsync). Setting both keeps the two paths in sync.
            handler.SslOptions.RemoteCertificateValidationCallback = AcceptAnyCertificate;
        }

        // Only take over dialing for direct connections. With a proxy in play the handler
        // must connect to the proxy host itself (and CONNECT-tunnel HTTPS), so leave its
        // dialer alone.
        if (!proxyConfigured)
        {
            handler.ConnectCallback = (context, ct) => HappyEyeballsConnectAsync(context, ignoreSslErrors, ct);
        }

        return handler;
    }

    private static bool AcceptAnyCertificate(object sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors) => true;

    /// <summary>
    /// A sentence naming the proxy a failed request went through, or "" when none was in play.
    /// </summary>
    /// <remarks>
    /// A connection failure names only the address it could not reach. When that address is a
    /// proxy the reader is left with no way to tell a proxy is involved at all -- and least of
    /// all when it was never configured here: ConfigureProxy leaves the handler alone unless the
    /// drive enables one, and .NET's default is then to use the machine's proxy. So the drive's
    /// configuration says nothing about a proxy while every request goes through one. Reported
    /// from the field as "Could not connect to 127.0.0.1:10000", read -- reasonably -- as a port
    /// this module wanted to listen on.
    ///
    /// Best-effort: any failure to work out the answer yields "", because an explanation must
    /// never replace the error it explains.
    /// </remarks>
    // Marks an exception this class has already explained, so it is explained once. The two send
    // sites nest: SendOnce reads the HttpClient property inside its own try, and that getter runs
    // EnsureAuthenticated -> the auth send -- so a proxy failure during token acquisition is
    // caught, annotated, and then caught again one frame out.
    private const string ProxyNotedKey = "UiPathOrch.ProxyNoted";

    /// <summary>
    /// The same exception carrying an explanation of the proxy it went through, or null when
    /// there is nothing to add (no proxy, no request URI, or it has been said already).
    /// </summary>
    internal static Exception? AnnotateProxyFailure(HttpRequestException ex, Uri? requestUri, ProxySettings? configured)
    {
        if (requestUri is null || ex.Data.Contains(ProxyNotedKey)) return null;

        string note = DescribeEffectiveProxy(requestUri, configured);
        if (note.Length == 0) return null;

        var annotated = new HttpRequestException(ex.Message + note, ex);
        annotated.Data[ProxyNotedKey] = true;
        return annotated;
    }

    /// <summary>
    /// A configuration snippet that would make this connection direct, or null when it would not
    /// help (no proxy, or one the drive asked for).
    /// </summary>
    /// <remarks>
    /// For a surface that can show a code block -- the sign-in page, not a console warning. Naming
    /// the key is not enough for the reader who most needs it: a configuration written before
    /// UseProxy existed already HAS a Proxy block, so "set it in that block" leaves them looking
    /// for a key that is not there. The snippet shows the block as they will find it, with the new
    /// key in place and the rest of it elided so it is clear nothing else has to change.
    /// </remarks>
    internal static string? BuildDirectConnectionConfigSnippet(Uri requestUri, ProxySettings? configured, IWebProxy? systemProxy = null)
    {
        if (DescribeEffectiveProxy(requestUri, configured, systemProxy).Length == 0) return null;
        if (configured is not null && configured.Enabled == true) return null;

        return """
              "Proxy": {
                "UseProxy": false,

                "Enabled": false,
                ...
              },
            """;
    }

    internal static string DescribeEffectiveProxy(Uri requestUri, ProxySettings? configured, IWebProxy? systemProxy = null)
    {
        try
        {
            // Turned off explicitly: there is no proxy to name, whatever the machine says.
            if (configured?.UseProxy == false) return "";

            if (configured is not null && configured.Enabled == true)
            {
                string where = configured.UseDefaultWebProxy.GetValueOrDefault()
                    ? "the machine's Internet Options, selected by this drive's Proxy block (UseDefaultWebProxy)"
                    : $"this drive's Proxy block ({configured.Url})";
                return $" The request went through a proxy, configured by {where}.";
            }

            // No proxy of ours, which is not the same as no proxy: null Proxy + UseProxy left at
            // its default means .NET resolves one from the environment.
            Uri? via = (systemProxy ?? HttpClient.DefaultProxy)?.GetProxy(requestUri);

            // Null means "no proxy for this destination"; a hand-built WebProxy answers with the
            // destination itself for the same. Treat both as none.
            if (via is null || via == requestUri) return "";

            // The remedy belongs with the diagnosis, and belongs on both surfaces -- the console
            // reader needs it as much as the one looking at the sign-in page. Kept to one clause,
            // and phrased as the direct-connection option rather than as the answer: a proxy that
            // is meant to be used should be repaired, not bypassed, and only the reader knows which
            // case they are in.
            return $" The request went through the proxy {via}, which UiPathOrch inherited from this"
                + " machine rather than from the drive's configuration (HTTPS_PROXY / HTTP_PROXY, or"
                + " Windows Internet Options). The drive's Proxy block being absent or disabled does"
                + " not by itself stop a proxy being used: to connect directly instead, set"
                + " \"UseProxy\": false in that block (Edit-OrchConfig opens the configuration file).";
        }
        catch
        {
            return "";
        }
    }

    // Returns whether a proxy is in play -- which is what the caller needs in order to decide
    // whether to take over dialing, not whether this method touched the handler.
    private static bool ConfigureProxy(SocketsHttpHandler handler, ProxySettings? proxy)
    {
        // "Use no proxy" is the more specific instruction, so it wins over Enabled, and it has to
        // be answered before the Enabled gate: the entire point is that leaving the block
        // disabled does NOT stop .NET falling back to the machine's proxy. Returning false is
        // correct -- the connection is direct, so the direct-connection dialer applies.
        if (proxy?.UseProxy == false)
        {
            handler.UseProxy = false;
            return false;
        }

        if (proxy is null || proxy.Enabled != true) return false;

        try
        {
            if (proxy.UseDefaultWebProxy.GetValueOrDefault())
            {
                handler.Proxy = WebRequest.DefaultWebProxy;
                handler.UseProxy = true;
                return true;
            }

            var webProxy = new WebProxy
            {
                Address = new Uri(proxy.Url ?? ""),
                BypassProxyOnLocal = proxy.BypassProxyOnLocal ?? true,
                UseDefaultCredentials = proxy.UseDefaultCredentials ?? false,
            };

            if (proxy.Credentials is not null && !webProxy.UseDefaultCredentials)
            {
                webProxy.Credentials = new NetworkCredential(
                    userName: proxy.Credentials.Username,
                    password: proxy.Credentials.Password);
            }

            handler.Proxy = webProxy;
            handler.UseProxy = true;
            return true;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Proxy: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// RFC 8305 "Happy Eyeballs v2"-style connect, adapted for a TLS black-hole failure mode.
    ///
    /// Implements the parts of RFC 8305 that matter here:
    ///   - §4 address ordering: interleave the resolved addresses by family, IPv6 first, so
    ///     attempts alternate families instead of exhausting every (dead) IPv6 before any IPv4.
    ///     Full RFC 6724 destination sorting is not done — .NET does not expose source-address
    ///     selection — but the family interleave is the part that matters here.
    ///   - §5 staggered attempts: launch the next address after a Connection Attempt Delay
    ///     (250 ms) instead of flooding every address at once, and advance immediately when an
    ///     attempt fails.
    /// Not implemented: §3 asynchronous A/AAAA queries — we take whatever a single getaddrinfo
    /// (Dns.GetHostAddressesAsync) returns.
    ///
    /// One deliberate deviation from RFC 8305: an attempt counts as "succeeded" only once its
    /// *TLS handshake* completes, not when the TCP connection is established. This is essential
    /// on IPv6-only / NAT64+DNS64 networks (common in enterprises): a synthesized 64:ff9b::
    /// backend completes the TCP handshake (small packets) but black-holes the TLS Certificate
    /// flight (broken Path-MTU discovery). A strict RFC 8305 implementation races the TCP
    /// connect, so it would pick the black-holed backend (its TCP connects fastest) and then
    /// hang in TLS. Racing the TLS handshake routes around the dead paths; because it runs on
    /// the async path it also honors cancellation, so HttpClient.Timeout / Ctrl+C work (the
    /// sync HttpClient.Send path did not reliably abort a wedged TLS handshake, which is why
    /// such a call hung indefinitely instead of timing out).
    ///
    /// This is not an ad-hoc hack: it is exactly the behavior the Happy Eyeballs v3 draft
    /// (draft-ietf-happy-happyeyeballs-v3) standardizes — clients MAY wait for the TLS handshake
    /// to complete before cancelling other attempts, for "networks in which a TCP-terminating
    /// proxy might be causing TCP handshakes to succeed quickly, even though end-to-end
    /// connectivity with the TLS-terminating server will fail" (the same shape as the NAT64
    /// black-hole here). So this dialer is RFC 8305 §4/§5 plus the HE v3 TLS-completion-as-success
    /// rule. (HE is only a mitigation; the root cause is broken Path-MTU discovery on the NAT64
    /// path, whose real fix is PLPMTUD / RFC 8899 or a network-side correction.)
    /// </summary>
    private static async ValueTask<Stream> HappyEyeballsConnectAsync(
        SocketsHttpConnectionContext context, bool ignoreSslErrors, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;

        var resolved = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        if (resolved.Length == 0)
            throw new IOException($"No addresses resolved for {host}");

        var addresses = OrderByFamilyInterleaved(resolved);

        // Cancelled once a winner is found, to tear down the slower / black-holed attempts.
        using var winnerFound = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, winnerFound.Token);

        var pending = new List<Task<(SslStream? stream, Exception? error)>>();
        int nextIndex = 0;
        Exception? lastError = null;

        while (nextIndex < addresses.Count || pending.Count > 0)
        {
            if (nextIndex < addresses.Count)
            {
                pending.Add(ConnectAndAuthenticateAsync(addresses[nextIndex++], host, port, ignoreSslErrors, linked.Token));
            }

            Task completed;
            if (nextIndex < addresses.Count)
            {
                // More addresses remain: wake on whichever comes first — a pending attempt
                // finishing, or the stagger delay elapsing (then loop to launch the next).
                // The delay carries no token; an abandoned one simply elapses harmlessly.
                var stagger = Task.Delay(ConnectionAttemptDelay);
                var all = new List<Task>(pending) { stagger };
                completed = await Task.WhenAny(all).ConfigureAwait(false);
                if (completed == stagger)
                    continue; // time to launch the next address
            }
            else
            {
                // Every address has been launched: just wait for the remaining attempts.
                completed = await Task.WhenAny(pending).ConfigureAwait(false);
            }

            var attempt = (Task<(SslStream? stream, Exception? error)>)completed;
            pending.Remove(attempt);

            var (stream, error) = attempt.Result;
            if (stream is not null)
            {
                winnerFound.Cancel(); // tear down the slower / black-holed attempts
                return stream;
            }

            // A cancellation is our own teardown / the caller's timeout, not a diagnosis;
            // keep the last real socket/TLS error so e.g. an untrusted-certificate failure
            // isn't reported as a generic connect failure.
            if (error is not null and not OperationCanceledException)
                lastError = error;
        }

        // If we got here because the caller cancelled (e.g. HttpClient.Timeout), surface that
        // rather than a generic connect failure.
        cancellationToken.ThrowIfCancellationRequested();
        string lastErrorSummary = lastError is null ? "" : $" Last error: {lastError.Message}";
        throw new IOException($"Could not connect to {host}:{port} on any of {addresses.Count} address(es).{lastErrorSummary}", lastError);
    }

    // RFC 8305 §4: interleave addresses by family so attempts alternate IPv6 / IPv4 instead of
    // exhausting one family first; IPv6 goes first (the typical preference). Order within a
    // family is preserved as returned by the resolver. Any other family (rare) is appended so
    // nothing is dropped.
    internal static List<IPAddress> OrderByFamilyInterleaved(IPAddress[] addresses)
    {
        var v6 = addresses.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6).ToList();
        var v4 = addresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToList();

        var ordered = new List<IPAddress>(addresses.Length);
        for (int i = 0; i < v6.Count || i < v4.Count; i++)
        {
            if (i < v6.Count) ordered.Add(v6[i]);
            if (i < v4.Count) ordered.Add(v4[i]);
        }

        foreach (var a in addresses)
            if (a.AddressFamily is not AddressFamily.InterNetworkV6 and not AddressFamily.InterNetwork)
                ordered.Add(a);

        return ordered;
    }

    // Never faults: a failed attempt returns its exception in the tuple so the racing
    // loop can harvest it for the final "could not connect" diagnosis without leaving
    // abandoned faulted tasks behind (the losers are dropped when a winner returns).
    private static async Task<(SslStream? stream, Exception? error)> ConnectAndAuthenticateAsync(
        IPAddress ip, string host, int port, bool ignoreSslErrors, CancellationToken cancellationToken)
    {
        var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(ip, port), cancellationToken).ConfigureAwait(false);
            var ssl = new SslStream(new NetworkStream(socket, ownsSocket: true), leaveInnerStreamOpen: false);
            var sslOptions = new SslClientAuthenticationOptions { TargetHost = host };
            if (ignoreSslErrors)
                sslOptions.RemoteCertificateValidationCallback = AcceptAnyCertificate;
            await ssl.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);
            return (ssl, null);
        }
        catch (Exception ex)
        {
            socket.Dispose();
            return (null, ex);
        }
    }
}
