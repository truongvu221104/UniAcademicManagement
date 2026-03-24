using System.Net.Http.Json;
using UniAcademic.Contracts.Grades;

namespace UniAcademic.AdminApp.Services.Grades;

public sealed class GradeApiClient : IGradeApiClient
{
    private readonly HttpClient _httpClient;

    public GradeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<GradeCategoryListItemResponse>> GetListAsync(Guid? courseOfferingId = null, CancellationToken cancellationToken = default)
    {
        var path = "api/grades";
        if (courseOfferingId.HasValue)
        {
            path += $"?courseOfferingId={courseOfferingId.Value}";
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<GradeCategoryListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<GradeCategoryResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<GradeCategoryResponse>($"api/grades/{id}", cancellationToken))!;
    }

    public async Task<GradeCategoryResponse> CreateCategoryAsync(CreateGradeCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/grades", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GradeCategoryResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<GradeCategoryResponse> UpdateCategoryAsync(Guid id, UpdateGradeCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/grades/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GradeCategoryResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<GradeCategoryResponse> UpdateEntriesAsync(Guid id, UpdateGradeEntriesRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/grades/{id}/entries", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GradeCategoryResponse>(cancellationToken: cancellationToken))!;
    }
}
