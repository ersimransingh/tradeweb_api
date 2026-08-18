using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradeWeb.API.Models
{
    // Accepts either a plain JSON array, or a DataSet-style object wrapping it
    // (e.g. {"rs0": [...]}), which is how most of this codebase serializes SQL
    // result sets. Falls back to the first array-valued property if "rs0" is absent.
    public class FlexibleArrayConverter<T> : JsonConverter<List<T>>
    {
        public override List<T> Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            JsonElement arrayElement = default;
            bool found = false;

            if (root.ValueKind == JsonValueKind.Array)
            {
                arrayElement = root;
                found = true;
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("rs0", out var rs0) && rs0.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = rs0;
                    found = true;
                }
                else
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            arrayElement = prop.Value;
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                // A single bare object (e.g. "dateFormat": { "key": ..., "format": ... },
                // which the tradewebx tab Setting uses alongside the array form) is treated
                // as a one-element list.
                if (root.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        var single = JsonSerializer.Deserialize<T>(root.GetRawText(), options);
                        return single != null ? new List<T> { single } : new List<T>();
                    }
                    catch (JsonException)
                    {
                        return new List<T>();
                    }
                }
                return new List<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(arrayElement.GetRawText(), options) ?? new List<T>();
        }

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
