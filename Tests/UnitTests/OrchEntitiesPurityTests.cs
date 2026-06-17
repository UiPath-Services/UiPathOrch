using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace UnitTests;

// Guards the entity layer's cohesion. OrchEntities.cs is a large but deliberately
// flat registry of wire DTOs (it is NOT split, by design). The risk of a giant
// DTO file is not its size — it is that behavior silently creeps into the data
// classes (e.g. a JWT decoder that used to live on OrchPSDrive). This test pins
// the invariant: DTOs stay pure data + self-shaping; token/blob decoding, JSON
// PARSING, HTTP/transport, crypto, and cmdlet host output belong in the core/
// auth/cmdlet layers, not here.
//
// Deliberately ALLOWED (so the guard stays correct, not cargo-cult):
//   * JsonSerializer.Serialize(this, ...)  — a DTO rendering ITSELF to JSON for
//     display is a legitimate self-shaping concern.
//   * DateTimeOffset.FromUnixTimeSeconds(...) — used by computed display
//     properties on time-bearing DTOs (e.g. license expiry).
// FORBIDDEN is the inbound/active logic a pure DTO should never contain.
public class OrchEntitiesPurityTests
{
    private static readonly (Regex pattern, string why)[] Forbidden =
    [
        (new Regex(@"\bConvert\.(From|To)Base64"), "base64 decode/encode — decode tokens/blobs in the core/auth layer"),
        (new Regex(@"JsonDocument\.Parse"),        "parsing raw JSON — DTOs are deserialization targets, not parsers"),
        (new Regex(@"JsonSerializer\.Deserialize"),"deserializing JSON — belongs in the API/session layer"),
        (new Regex(@"\bHttpClient\b"),             "HTTP transport must not live on a DTO"),
        (new Regex(@"\.SendApiRequest\b"),         "API calls must not be issued from a DTO"),
        (new Regex(@"\bRandomNumberGenerator\b"),  "crypto/randomness belongs in AuthManager/core"),
        (new Regex(@"System\.Security\.Cryptography"), "crypto belongs in AuthManager/core"),
        (new Regex(@"\bSHA(1|256|384|512)\b"),     "hashing belongs in AuthManager/core"),
        (new Regex(@"\bWriteObject\s*\("),         "host/cmdlet output belongs in the cmdlet layer"),
        (new Regex(@"\bProcess\.Start\s*\("),      "launching processes must not happen from a DTO"),
    ];

    private static string FindEntitiesFile()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "UiPathOrch", "OrchEntities.cs");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Could not locate OrchEntities.cs from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Entity_layer_stays_pure_data_no_decode_transport_crypto_or_host_logic()
    {
        var file = FindEntitiesFile();
        var lines = File.ReadAllLines(file);

        var hits = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.TrimStart().StartsWith("//")) continue; // ignore comments
            foreach (var (pattern, why) in Forbidden)
            {
                if (pattern.IsMatch(line))
                {
                    hits.Add($"  OrchEntities.cs:{i + 1}  [{why}]\n    {line.Trim()}");
                }
            }
        }

        Assert.True(hits.Count == 0,
            "OrchEntities.cs (the DTO layer) must stay pure data. Found active logic that " +
            "belongs in the core/auth/session/cmdlet layers — move it out (see JwtClaims.cs " +
            "for the pattern) rather than letting the entity file accrete behavior:\n\n" +
            string.Join("\n", hits));
    }
}
