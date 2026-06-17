using System.Management.Automation;
using System.Text;
using System.Text.Json;

namespace UiPath.PowerShell.Core;

// JWT helper. DecodePayloadJson is the single base64url payload decoder shared by
// the auth layer (OrchestratorAuthManager.ParseJwtPayload / DebugJwtToken) and
// the Get-OrchPSDrive .Claims diagnostic (ReadClaims), so the base64url padding
// and char translation live in exactly one place.
internal static class Jwt
{
    private static readonly HashSet<string> _unixTimestampClaims = ["exp", "iat", "nbf", "auth_time"];

    // Decodes a JWT segment (the base64url-encoded middle part) to its UTF-8 JSON
    // string. Throws on a malformed segment (invalid base64); callers that need
    // null-safety wrap this in try/catch (see ReadClaims).
    internal static string DecodePayloadJson(string segment)
    {
        segment = segment.PadRight(segment.Length + (4 - segment.Length % 4) % 4, '=');
        segment = segment.Replace('-', '+').Replace('_', '/');
        return Encoding.UTF8.GetString(Convert.FromBase64String(segment));
    }

    // Decodes a full JWT access token into a PSObject of claims for the
    // Get-OrchPSDrive .Claims diagnostic. Unix-epoch claims (exp/iat/nbf/
    // auth_time) are surfaced as local DateTime; every other value keeps its
    // JSON-native shape. Best-effort: returns null for a missing or malformed
    // token, never throws.
    internal static PSObject? ReadClaims(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var parts = token.Split('.');
        if (parts.Length != 3) return null;

        try
        {
            using var doc = JsonDocument.Parse(DecodePayloadJson(parts[1]));
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
