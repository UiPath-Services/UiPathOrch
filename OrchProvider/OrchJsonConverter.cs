using System.Collections;
using System.Management.Automation;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace UiPath.PowerShell.Entities.JsonConverter;

public class JsonTools
{
    internal static readonly JsonSerializerOptions jsoWhenWritingNull = new()
    {
        //Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // Suppress encoding
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static readonly JsonSerializerOptions jsoOneLine = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    internal static readonly JsonSerializerOptions jsonAllowComments = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static Hashtable? JsonToDictionary(string? jsonText)
    {
        if (jsonText is null) return null;
        try { return JsonNode.Parse(jsonText) is JsonObject obj ? ProcessNode(obj) : null; }
        catch { return null; }
    }

    private static Hashtable ProcessNode(JsonObject obj)
    {
        var result = new Hashtable();
        foreach (var kvp in obj)
        {
            result[kvp.Key] = ConvertValue(kvp.Value);
        }
        return result;
    }

    private static object? ConvertValue(JsonNode? node) => node switch
    {
        JsonObject obj => ProcessNode(obj),
        JsonArray arr => arr.Select(ConvertValue).ToArray(),
        JsonValue val => GetTypedValue(val),
        _ => null
    };

    private static object? GetTypedValue(JsonValue value)
    {
        if (value.TryGetValue<bool>(out var b)) return b;
        if (value.TryGetValue<long>(out var l)) return l;
        if (value.TryGetValue<double>(out var d)) return d;
        if (value.TryGetValue<string>(out var s)) return s;
        return null;
    }
}

// Converter for fields like ResolutionWidth in JSON text returned by OR, which can be
// either strings or numeric values, allowing deserialization to the correct value in either case.
public class StringOrIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            // When the token is a string
            if (int.TryParse(reader.GetString(), out int result))
            {
                return result;
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            // When the token is a number
            return reader.GetInt32();
        }

        throw new JsonException($"Cannot convert {reader.TokenType} to {typeof(int)}.");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

// Converter to append 'Z' to the end when posting DateTime values as JSON to OR.
//public class DateTimeJsonConverter : JsonConverter<DateTime>
//{
//    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//    {
//        return DateTime.Parse(reader.GetString()!);
//    }

//    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
//    {
//        // Convert to UTC and output in "yyyy-MM-ddTHH:mm:ssZ" format
//        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
//    }
//}

// Converter that deserializes to local time.
// Confirmed to work correctly on its own, but problems arise when importing files
// that were exported with local time to CSV, so this has been removed from application.
// Ideally, this should be applied to all DateTime properties on all entities.
public class LocalDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // Convert the UTC DateTime to local time and return
        DateTime utcDateTime = reader.GetDateTime();
        return utcDateTime.ToLocalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            // Convert local time to UTC before writing to JSON
            writer.WriteStringValue(value.Value.ToUniversalTime());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class DateTimeArrayJsonConverter : JsonConverter<DateTime[]>
{
    public override DateTime[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected StartArray token.");
        }

        var dateList = new System.Collections.Generic.List<DateTime>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var dateTime = DateTime.Parse(reader.GetString()!).ToLocalTime();
                dateList.Add(dateTime);
            }
            else
            {
                throw new JsonException($"Unexpected token parsing DateTime. Expected String, got {reader.TokenType}.");
            }
        }

        return dateList.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, DateTime[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var dateTime in value)
        {
            writer.WriteStringValue(dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            //writer.WriteStringValue(dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        writer.WriteEndArray();
    }
}

// Converter for deserializing Member into the appropriate subclass.
public class MemberConverter : JsonConverter<PmGroupMember>
{
    public override PmGroupMember? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        var objectType = root.GetProperty("objectType").GetString();

        return objectType switch
        {
            "DirectoryUser" => JsonSerializer.Deserialize<DirectoryUser>(root.GetRawText(), options),
            "DirectoryGroup" => JsonSerializer.Deserialize<DirectoryGroup>(root.GetRawText(), options),
            "DirectoryRobotUser" => JsonSerializer.Deserialize<DirectoryRobotUser>(root.GetRawText(), options),
            "DirectoryApplication" => JsonSerializer.Deserialize<DirectoryApplication>(root.GetRawText(), options),
            _ => throw new NotSupportedException($"Unknown objectType: {objectType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, PmGroupMember value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

// Converter that swallows exceptions and returns null for array types with detailed unknowns during deserialization.
// For types not documented in the swagger doc, having this safeguard is reassuring.
// However, do not use this for members with known types. If type info is added to swagger later, this safeguard should be removed.
public class SafeArrayConverter<T> : JsonConverter<T[]?>
{
    public override T[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            // Attempt deserialization as a normal array
            return JsonSerializer.Deserialize<T[]>(ref reader, options);
        }
        catch (JsonException)
        {
            // Return null if deserialization fails
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, T[]? value, JsonSerializerOptions options)
    {
        // Serialize the array normally
        JsonSerializer.Serialize(writer, value, options);
    }
}
