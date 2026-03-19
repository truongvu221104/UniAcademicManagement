using System.Net.Http;
using System.Net.Http.Json;
using UniAcademic.Contracts.StudentClasses;

namespace UniAcademic.AdminApp.Services.StudentClasses;

public sealed class StudentClassApiClient : IStudentClassApiClient
{
    private readonly HttpClient _httpClient;

    public StudentClassApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<StudentClassListItemResponse>> GetListAsync(string? keyword = null, Guid? facultyId = null, int? intakeYear = null, string? status = null, CancellationToken cancellationToken = default)
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

        if (intakeYear.HasValue)
        {
            query.Add($"intakeYear={intakeYear.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add($"status={Uri.EscapeDataString(status)}");
        }

        var path = "api/studentclasses";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        return (await _httpClient.GetFromJsonAsync<IReadOnlyCollection<StudentClassListItemResponse>>(path, cancellationToken)) ?? [];
    }

    public async Task<StudentClassResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return (await _httpClient.GetFromJsonAsync<StudentClassResponse>($"api/studentclasses/{id}", cancellationToken))!;
    }

    public async Task<StudentClassResponse> CreateAsync(CreateStudentClassRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/studentclasses", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentClassResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task<StudentClassResponse> UpdateAsync(Guid id, UpdateStudentClassRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/studentclasses/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StudentClassResponse>(cancellationToken: cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/studentclasses/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
