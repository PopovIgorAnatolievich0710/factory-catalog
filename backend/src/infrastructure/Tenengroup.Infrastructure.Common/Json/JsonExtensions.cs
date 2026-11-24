using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tenengroup.Infrastructure.Common.Json;

public static class JsonExtensions
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ToJsonString<T>(this T value) => JsonSerializer.Serialize(value, Options);

    public static T? FromJsonString<T>(this string json) => JsonSerializer.Deserialize<T>(json, Options);
}
