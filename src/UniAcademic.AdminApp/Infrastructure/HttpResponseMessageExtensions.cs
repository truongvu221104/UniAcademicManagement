using System.Net.Http;
using System.Text.Json;

namespace UniAcademic.AdminApp.Infrastructure;

public static class HttpResponseMessageExtensions
{
    public static async Task EnsureSuccessWithMessageAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? message = null;

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(content))
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("message", out var messageProperty))
                {
                    message = messageProperty.GetString();
                }
            }
        }
        catch
        {
            // Fall back to default status message.
        }

        throw new InvalidOperationException(message ?? $"{(int)response.StatusCode} {response.ReasonPhrase}");
    }
}
