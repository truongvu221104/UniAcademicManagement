using System.Net.Http.Json;
using System.Text;
using UniAcademic.Contracts.GradeResults;

namespace UniAcademic.AdminApp.Services.GradeResults;

public sealed class GradeResultApiClient : IGradeResultApiClient
{
    private readonly HttpClient _httpClient;

    public GradeResultApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<GradeResultListItemResponse>> GetListAsync(
        string? studentCode = null,
        string? studentFullName = null,
        Guid? courseOfferingId = null,
        CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(studentCode))
        {
            queryParts.Add($"studentCode={Uri.EscapeDataString(studentCode)}");
        }

        if (!string.IsNullOrWhiteSpace(studentFullName))
        {
            queryParts.Add($"studentFullName={Uri.EscapeDataString(studentFullName)}");
        }

        if (courseOfferingId.HasValue)
        {
            queryParts.Add($"courseOfferingId={courseOfferingId.Value}");
        }

        var path = new StringBuilder("api/graderesults");
        if (queryParts.Count > 0)
        {
            path.Append('?');
            path.Append(string.Join("&", queryParts));
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<GradeResultListItemResponse>>(path.ToString(), cancellationToken)) ?? [];
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
