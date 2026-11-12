using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManager.Api.Tests.Common;

/// <summary>The same JSON conventions the API uses: camelCase and enums as text.</summary>
internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
