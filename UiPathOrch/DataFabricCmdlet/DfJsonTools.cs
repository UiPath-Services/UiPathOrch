using System.Collections;
using System.Management.Automation;
using System.Text.Json;

namespace UiPath.PowerShell.Commands;

internal static class DfJsonTools
{
    // Convert a DataService record (JsonElement object) into a PSObject whose properties
    // are native CLR values, so PowerShell formatting, member access, and ConvertTo-Json
    // round-trips behave as users expect.
    public static PSObject RecordToPSObject(JsonElement record)
    {
        var pso = new PSObject();
        if (record.ValueKind != JsonValueKind.Object) return pso;

        foreach (var prop in record.EnumerateObject())
        {
            pso.Properties.Add(new PSNoteProperty(prop.Name, JsonValueToClr(prop.Value)));
        }
        return pso;
    }

    private static object? JsonValueToClr(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                return el.GetString();
            case JsonValueKind.Number:
                if (el.TryGetInt64(out long l)) return l;
                return el.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;
            case JsonValueKind.Array:
                return el.EnumerateArray().Select(JsonValueToClr).ToArray();
            case JsonValueKind.Object:
                return el.EnumerateObject().ToDictionary(p => p.Name, p => JsonValueToClr(p.Value));
            default:
                return el.ToString();
        }
    }

    // Recursively convert PowerShell-native values (Hashtable, PSObject, IList) into types
    // that System.Text.Json.JsonSerializer handles natively.
    public static object? ToJsonPayload(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case PSObject pso:
                return ToJsonPayload(pso.BaseObject);
            case Hashtable ht:
                {
                    var dict = new Dictionary<string, object?>(ht.Count);
                    foreach (DictionaryEntry entry in ht)
                    {
                        dict[entry.Key.ToString()!] = ToJsonPayload(entry.Value);
                    }
                    return dict;
                }
            case IDictionary<string, object?> gdict:
                return gdict.ToDictionary(kv => kv.Key, kv => ToJsonPayload(kv.Value));
            case string s:
                return s;
            case IList list:
                {
                    var arr = new object?[list.Count];
                    for (int i = 0; i < list.Count; i++)
                    {
                        arr[i] = ToJsonPayload(list[i]);
                    }
                    return arr;
                }
            default:
                return value;
        }
    }
}
