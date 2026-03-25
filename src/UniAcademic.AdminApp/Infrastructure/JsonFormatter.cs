using System.Text.Json;

namespace UniAcademic.AdminApp.Infrastructure;

public static class JsonFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static string Format(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return JsonSerializer.Serialize(value, Options);
    }

    public static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, Options)!;
}
