using System.Net.Http.Json;
using UniAcademic.Contracts.LecturerProfiles;

namespace UniAcademic.AdminApp.Services.LecturerProfiles;

public sealed class LecturerProfileApiClient : ILecturerProfileApiClient
{
    private readonly HttpClient _httpClient;

    public LecturerProfileApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<LecturerProfileListItemResponse>> GetListAsync(Guid? facultyId = null, bool? isActive = null, string? keyword = null, CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>();
        if (facultyId.HasValue)
        {
            queryParts.Add($"facultyId={facultyId.Value}");
        }

        if (isActive.HasValue)
        {
            queryParts.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            queryParts.Add($"keyword={Uri.EscapeDataString(keyword)}");
        }

        var path = "api/lecturerprofiles";
        if (queryParts.Count > 0)
        {
            path += "?" + string.Join("&", queryParts);
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<LecturerProfileListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<LecturerProfileResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<LecturerProfileResponse>($"api/lecturerprofiles/{id}", cancellationToken))!;
    }

    public async Task<LecturerProfileResponse> CreateAsync(CreateLecturerProfileRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/lecturerprofiles", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LecturerProfileResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<LecturerProfileResponse> UpdateAsync(Guid id, UpdateLecturerProfileRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/lecturerprofiles/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LecturerProfileResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/lecturerprofiles/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
