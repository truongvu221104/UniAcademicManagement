using System.Net.Http.Json;
using UniAcademic.Contracts.LecturerAssignments;

namespace UniAcademic.AdminApp.Services.LecturerAssignments;

public sealed class LecturerAssignmentApiClient : ILecturerAssignmentApiClient
{
    private readonly HttpClient _httpClient;

    public LecturerAssignmentApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<LecturerAssignmentResponse>> GetListAsync(Guid? courseOfferingId = null, Guid? lecturerProfileId = null, CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>();
        if (courseOfferingId.HasValue)
        {
            queryParts.Add($"courseOfferingId={courseOfferingId.Value}");
        }

        if (lecturerProfileId.HasValue)
        {
            queryParts.Add($"lecturerProfileId={lecturerProfileId.Value}");
        }

        var path = "api/lecturerassignments";
        if (queryParts.Count > 0)
        {
            path += "?" + string.Join("&", queryParts);
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<LecturerAssignmentResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<LecturerAssignmentResponse> AssignAsync(AssignLecturerRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/lecturerassignments", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LecturerAssignmentResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task UnassignAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/lecturerassignments/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
