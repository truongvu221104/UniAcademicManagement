using System.Net.Http.Json;
using UniAcademic.AdminApp.Infrastructure;
using UniAcademic.Contracts.Rosters;

namespace UniAcademic.AdminApp.Services.Rosters;

public sealed class CourseOfferingRosterApiClient : ICourseOfferingRosterApiClient
{
    private readonly HttpClient _httpClient;

    public CourseOfferingRosterApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CourseOfferingRosterResponse> GetAsync(Guid courseOfferingId, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<CourseOfferingRosterResponse>($"api/courseofferings/{courseOfferingId}/roster", cancellationToken))!;
    }

    public async Task<CourseOfferingRosterResponse> FinalizeAsync(Guid courseOfferingId, FinalizeCourseOfferingRosterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/courseofferings/{courseOfferingId}/roster/finalize", request, cancellationToken);
        await response.EnsureSuccessWithMessageAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CourseOfferingRosterResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<CourseOfferingRosterResponse> ReopenAsync(Guid courseOfferingId, ReopenCourseOfferingRosterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/courseofferings/{courseOfferingId}/roster/reopen", request, cancellationToken);
        await response.EnsureSuccessWithMessageAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<CourseOfferingRosterResponse>(cancellationToken: cancellationToken))!;
    }
}
