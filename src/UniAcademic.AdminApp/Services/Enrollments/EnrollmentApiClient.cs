using System.Net.Http.Json;
using UniAcademic.AdminApp.Infrastructure;
using UniAcademic.Contracts.Enrollments;

namespace UniAcademic.AdminApp.Services.Enrollments;

public sealed class EnrollmentApiClient : IEnrollmentApiClient
{
    private readonly HttpClient _httpClient;

    public EnrollmentApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<EnrollmentListItemResponse>> GetListAsync(string? keyword = null, string? studentCode = null, string? studentFullName = null, Guid? studentProfileId = null, Guid? courseOfferingId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query.Add($"keyword={Uri.EscapeDataString(keyword)}");
        }

        if (!string.IsNullOrWhiteSpace(studentCode))
        {
            query.Add($"studentCode={Uri.EscapeDataString(studentCode)}");
        }

        if (!string.IsNullOrWhiteSpace(studentFullName))
        {
            query.Add($"studentFullName={Uri.EscapeDataString(studentFullName)}");
        }

        if (studentProfileId.HasValue)
        {
            query.Add($"studentProfileId={studentProfileId.Value}");
        }

        if (courseOfferingId.HasValue)
        {
            query.Add($"courseOfferingId={courseOfferingId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var path = "api/enrollments";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<EnrollmentListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<EnrollmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<EnrollmentResponse>($"api/enrollments/{id}", cancellationToken))!;
    }

    public async Task<EnrollmentResponse> CreateAsync(CreateEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/enrollments", request, cancellationToken);
        await response.EnsureSuccessWithMessageAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<EnrollmentResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/enrollments/{id}", cancellationToken);
        await response.EnsureSuccessWithMessageAsync(cancellationToken);
    }
}
