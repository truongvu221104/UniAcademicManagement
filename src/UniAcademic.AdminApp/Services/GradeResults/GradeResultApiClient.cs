using System.Net.Http.Json;
using UniAcademic.Contracts.GradeResults;

namespace UniAcademic.AdminApp.Services.GradeResults;

public sealed class GradeResultApiClient : IGradeResultApiClient
{
    private readonly HttpClient _httpClient;

    public GradeResultApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<GradeResultListItemResponse>> GetListAsync(Guid? courseOfferingId = null, CancellationToken cancellationToken = default)
    {
        var path = "api/graderesults";
        if (courseOfferingId.HasValue)
        {
            path += $"?courseOfferingId={courseOfferingId.Value}";
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<GradeResultListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<GradeResultResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<GradeResultResponse>($"api/graderesults/{id}", cancellationToken))!;
    }

    public async Task<IReadOnlyCollection<GradeResultResponse>> CalculateAsync(CalculateGradeResultsRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/graderesults/calculate", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IReadOnlyCollection<GradeResultResponse>>(cancellationToken: cancellationToken)) ?? [];
    }
}
