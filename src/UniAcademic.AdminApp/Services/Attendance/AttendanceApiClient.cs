using System.Net.Http.Json;
using UniAcademic.Contracts.Attendance;

namespace UniAcademic.AdminApp.Services.Attendance;

public sealed class AttendanceApiClient : IAttendanceApiClient
{
    private readonly HttpClient _httpClient;

    public AttendanceApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<AttendanceSessionListItemResponse>> GetListAsync(Guid? courseOfferingId = null, CancellationToken cancellationToken = default)
    {
        var path = "api/attendance";
        if (courseOfferingId.HasValue)
        {
            path += $"?courseOfferingId={courseOfferingId.Value}";
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<AttendanceSessionListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<AttendanceSessionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<AttendanceSessionResponse>($"api/attendance/{id}", cancellationToken))!;
    }

    public async Task<AttendanceSessionResponse> CreateAsync(CreateAttendanceSessionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/attendance", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AttendanceSessionResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<AttendanceSessionResponse> UpdateRecordsAsync(Guid id, UpdateAttendanceRecordsRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/attendance/{id}/records", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AttendanceSessionResponse>(cancellationToken: cancellationToken))!;
    }
}
