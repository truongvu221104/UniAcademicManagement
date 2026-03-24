using System.Net.Http.Headers;
using System.Net.Http.Json;
using UniAcademic.Contracts.Materials;

namespace UniAcademic.AdminApp.Services.Materials;

public sealed class CourseMaterialApiClient : ICourseMaterialApiClient
{
    private readonly HttpClient _httpClient;

    public CourseMaterialApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<CourseMaterialListItemResponse>> GetListAsync(Guid? courseOfferingId = null, CancellationToken cancellationToken = default)
    {
        var path = "api/coursematerials";
        if (courseOfferingId.HasValue)
        {
            path += $"?courseOfferingId={courseOfferingId.Value}";
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<CourseMaterialListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<CourseMaterialResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<CourseMaterialResponse>($"api/coursematerials/{id}", cancellationToken))!;
    }

    public async Task<CourseMaterialResponse> UploadAsync(UploadCourseMaterialRequest request, string filePath, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(request.CourseOfferingId.ToString()), "courseOfferingId");
        content.Add(new StringContent(request.Title), "title");
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            content.Add(new StringContent(request.Description), "description");
        }

        content.Add(new StringContent(request.MaterialType.ToString()), "materialType");
        content.Add(new StringContent(request.SortOrder.ToString()), "sortOrder");
        content.Add(new StringContent(request.IsPublished.ToString()), "isPublished");

        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var response = await _httpClient.PostAsync("api/coursematerials", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseMaterialResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<CourseMaterialResponse> UpdateAsync(Guid id, UpdateCourseMaterialRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/coursematerials/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseMaterialResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<CourseMaterialResponse> SetPublishStateAsync(Guid id, SetCourseMaterialPublishStateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/coursematerials/{id}/publish-state", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CourseMaterialResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<byte[]> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetByteArrayAsync($"api/coursematerials/{id}/download", cancellationToken);
    }
}
