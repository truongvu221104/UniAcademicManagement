using System.Net.Http;
using System.Net.Http.Json;
using UniAcademic.Contracts.Faculties;

namespace UniAcademic.AdminApp.Services.Faculties;

public sealed class FacultyApiClient : IFacultyApiClient
{
    private readonly HttpClient _httpClient;

    public FacultyApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<FacultyListItemResponse>> GetListAsync(string? keyword = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query.Add($"keyword={Uri.EscapeDataString(keyword)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var path = "api/faculties";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<FacultyListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<FacultyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<FacultyResponse>($"api/faculties/{id}", cancellationToken))!;
    }

    public async Task<FacultyResponse> CreateAsync(CreateFacultyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/faculties", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FacultyResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<FacultyResponse> UpdateAsync(Guid id, UpdateFacultyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/faculties/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FacultyResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/faculties/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
