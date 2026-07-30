using System.Text.Json;
using System.Text.Json.Serialization;

namespace UberGenius.Api.Json;

// Every DateTime in this app is semantically UTC (see Trip.StartTimeUtc/EndTimeUtc etc.),
// but SQL Server's datetime2 has no timezone concept, so EF Core always reads them back as
// Kind=Unspecified. System.Text.Json only appends the 'Z' suffix for Kind=Utc — without this
// converter, the frontend's `new Date(...)` would silently treat the value as local time
// instead of converting it, breaking the UTC-storage/local-display convention everywhere.
public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
