using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VeggieLife.API.Extensions;

/// <summary>
/// Đảm bảo tất cả giá trị DateTime khi serialize ra JSON đều theo chuẩn ISO 8601 UTC kết thúc bằng 'Z'.
/// Giúp Frontend và trình duyệt hiểu chính xác múi giờ UTC và convert sang Asia/Ho_Chi_Minh (+07:00).
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}

/// <summary>
/// Converter cho DateTime? dạng có thể null.
/// </summary>
public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return string.IsNullOrEmpty(str) ? null : DateTime.Parse(str);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            var utc = value.Value.Kind == DateTimeKind.Utc
                ? value.Value
                : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
            writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

