using System.Net.Http.Json;
using UniAcademic.Contracts.Courses;

namespace UniAcademic.AdminApp.Services.Courses;

public sealed class CourseApiClient : ICourseApiClient
{
    private readonly HttpClient _httpClient;

    public CourseApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<CourseListItemResponse>> GetListAsync(string? keyword = null, Guid? facultyId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query.Add($"keyword={Uri.EscapeDataString(keyword)}");
        }

        if (facultyId.HasValue)
        {
            query.Add($"facultyId={facultyId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var path = "api/courses";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<CourseListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<CourseResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<CourseResponse>($"api/courses/{id}", cancellationToken))!;
    }

    public async Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/courses", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<CourseResponse> UpdateAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/courses/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/courses/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
