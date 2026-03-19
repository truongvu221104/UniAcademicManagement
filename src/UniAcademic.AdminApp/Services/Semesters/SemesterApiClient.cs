using System.Net.Http.Json;
using UniAcademic.Contracts.Semesters;

namespace UniAcademic.AdminApp.Services.Semesters;

public sealed class SemesterApiClient : ISemesterApiClient
{
    private readonly HttpClient _httpClient;

    public SemesterApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<SemesterListItemResponse>> GetListAsync(string? keyword = null, string? academicYear = null, int? termNo = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query.Add($"keyword={Uri.EscapeDataString(keyword)}");
        }

        if (!string.IsNullOrWhiteSpace(academicYear))
        {
            query.Add($"academicYear={Uri.EscapeDataString(academicYear)}");
        }

        if (termNo.HasValue)
        {
            query.Add($"termNo={termNo.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var path = "api/semesters";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<SemesterListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<SemesterResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<SemesterResponse>($"api/semesters/{id}", cancellationToken))!;
    }

    public async Task<SemesterResponse> CreateAsync(CreateSemesterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/semesters", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SemesterResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<SemesterResponse> UpdateAsync(Guid id, UpdateSemesterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/semesters/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SemesterResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/semesters/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
