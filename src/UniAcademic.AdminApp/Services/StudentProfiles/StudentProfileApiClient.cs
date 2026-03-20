using System.Net.Http.Json;
using UniAcademic.Contracts.StudentProfiles;

namespace UniAcademic.AdminApp.Services.StudentProfiles;

public sealed class StudentProfileApiClient : IStudentProfileApiClient
{
    private readonly HttpClient _httpClient;

    public StudentProfileApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<StudentProfileListItemResponse>> GetListAsync(string? keyword = null, Guid? studentClassId = null, string? gender = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query.Add($"keyword={Uri.EscapeDataString(keyword)}");
        }

        if (studentClassId.HasValue)
        {
            query.Add($"studentClassId={studentClassId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(gender))
        {
            query.Add($"gender={Uri.EscapeDataString(gender)}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var path = "api/studentprofiles";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<StudentProfileListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<StudentProfileResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<StudentProfileResponse>($"api/studentprofiles/{id}", cancellationToken))!;
    }

    public async Task<StudentProfileResponse> CreateAsync(CreateStudentProfileRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/studentprofiles", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentProfileResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<StudentProfileResponse> UpdateAsync(Guid id, UpdateStudentProfileRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/studentprofiles/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentProfileResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/studentprofiles/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
