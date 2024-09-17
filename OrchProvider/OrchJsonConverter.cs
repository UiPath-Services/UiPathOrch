using System.Text.Json;
using System.Text.Json.Serialization;

namespace UiPath.PowerShell.Entities.JsonConverter
{
    // OR から返される JSON テキストの ResolutionWidth などが、文字列だったり数値だったりするので
    // どちらの場合でも数値にデシリアライズできるようにするためのコンバータ。
    public class StringOrIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // トークンが文字列の場合
                if (int.TryParse(reader.GetString(), out int result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                // トークンが数値の場合
                return reader.GetInt32();
            }

            throw new JsonException($"Cannot convert {reader.TokenType} to {typeof(int)}.");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    // OR に DateTime を JSON でポストするとき、末尾に Z を付加するためのコンバータ。
    //public class DateTimeJsonConverter : JsonConverter<DateTime>
    //{
    //    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        return DateTime.Parse(reader.GetString()!);
    //    }

    //    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    //    {
    //        // UTC に変換し、"yyyy-MM-ddTHH:mm:ssZ" 形式で出力
    //        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
    //    }
    //}

    // ローカル時刻にデシリアライズするコンバータ。
    // これ自体は正しく動作することを確認しているが、CSV にローカル時刻で出力したファイルを
    // インポートするときに問題が出そうなので、適用を躊躇している。
    // 本当はすべてのエンティティの DataTime 型プロパティに指定したいが。。
    public class LocalDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // UTC DateTime をローカル時刻に変換して返す
            DateTime utcDateTime = reader.GetDateTime();
            return utcDateTime.ToLocalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                // ローカル時刻を UTC に変換してから JSON に書き込む
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

    // Member を、適切なサブクラスでデシリアライズするためのコンバータ。
    public class MemberConverter : JsonConverter<Member>
    {
        public override Member? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        public override void Write(Utf8JsonWriter writer, Member value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
