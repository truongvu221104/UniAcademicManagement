using System.Net.Http.Json;
using UniAcademic.Contracts.CourseOfferings;

namespace UniAcademic.AdminApp.Services.CourseOfferings;

public sealed class CourseOfferingApiClient : ICourseOfferingApiClient
{
    private readonly HttpClient _httpClient;

    public CourseOfferingApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<CourseOfferingListItemResponse>> GetListAsync(string? keyword = null, Guid? courseId = null, Guid? semesterId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query.Add($"keyword={Uri.EscapeDataString(keyword)}");
        }

        if (courseId.HasValue)
        {
            query.Add($"courseId={courseId.Value}");
        }

        if (semesterId.HasValue)
        {
            query.Add($"semesterId={semesterId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var path = "api/courseofferings";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<CourseOfferingListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<CourseOfferingResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<CourseOfferingResponse>($"api/courseofferings/{id}", cancellationToken))!;
    }

    public async Task<CourseOfferingResponse> CreateAsync(CreateCourseOfferingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/courseofferings", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseOfferingResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<CourseOfferingResponse> UpdateAsync(Guid id, UpdateCourseOfferingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/courseofferings/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseOfferingResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/courseofferings/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
