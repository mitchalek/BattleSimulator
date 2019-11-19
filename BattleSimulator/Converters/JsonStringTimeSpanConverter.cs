using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BattleSimulator.Converters
{
    public class JsonStringTimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (TimeSpan.TryParse(reader.GetString(), out TimeSpan result))
            {
                return result;
            }
            return TimeSpan.Zero;
        }
        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
