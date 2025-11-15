using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WMS.Api.Common
{
    /// <summary>
    /// Global DateTime converter to ensure all API DateTimes are correctly mapped 
    /// from client-local time to UTC for storage and marked as UTC on output.
    /// This resolves the time zone offset problem.
    /// </summary>
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if (DateTime.TryParse(reader.GetString(), out DateTime dateTime))
                {
                    // FIX (INBOUND): If Kind is Unspecified (which is the problem state when Angular sends local time), 
                    // assume it is the server's local time (UTC+8) and convert it to UTC (UTC).
                    if (dateTime.Kind == DateTimeKind.Unspecified || dateTime.Kind == DateTimeKind.Local)
                    {
                        return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
                    }

                    // Already UTC/Offset, just normalize to UTC.
                    return dateTime.ToUniversalTime();
                }
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // FIX (OUTBOUND): Always write the UTC value with the 'Z' suffix.
            // This forces Angular to correctly shift the time to the user's local timezone (e.g., 1:00 AM UTC -> 9:00 AM Local).
            writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffZ"));
        }
    }
}