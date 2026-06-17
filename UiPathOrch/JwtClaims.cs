using System.Management.Automation;
using System.Text;
using System.Text.Json;

namespace UiPath.PowerShell.Core;

// Decodes a JWT access-token payload into a PSObject of claims for the
// Get-OrchPSDrive .Claims diagnostic. This lives here rather than on the
// OrchPSDrive DTO so the entity layer stays pure data (no token decoding /
// base64 / JSON parsing) — pinned by OrchEntitiesPurityTests. Unix-epoch claims
// (exp/iat/nbf/auth_time) are surfaced as local DateTime; every other value
// keeps its JSON-native shape. Best-effort: returns null for a missing or
// malformed token, never throws.
internal static class JwtClaims
{
    private static readonly HashSet<string> _unixTimestampClaims = ["exp", "iat", "nbf", "auth_time"];

    internal static PSObject? Parse(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var parts = token.Split('.');
        if (parts.Length != 3) return null;

        try
        {
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            payload = payload.Replace('-', '+').Replace('_', '/');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));

            using var doc = JsonDocument.Parse(json);
            var claims = new PSObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                object value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number when _unixTimestampClaims.Contains(prop.Name)
                        => DateTimeOffset.FromUnixTimeSeconds(prop.Value.GetInt64()).LocalDateTime,
                    JsonValueKind.Number => prop.Value.GetInt64(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Array => prop.Value.EnumerateArray().Select(e => e.ToString()).ToArray(),
                    _ => prop.Value.ToString()
                };
                claims.Properties.Add(new PSNoteProperty(prop.Name, value));
            }
            return claims;
        }
        catch
        {
            return null;
        }
    }
}
