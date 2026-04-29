using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Korendzh.Mobile.Services;

/// <summary>
/// Клиент к Korendzh REST API. Базовый URL берётся из переменных окружения сборки или
/// (для dev) хардкодится в App constants. Все запросы — с JWT-bearer.
/// </summary>
public class KorendzhApiClient
{
    // В реальном приложении базовый URL должен быть конфигурируемым.
    public const string BaseUrl = "https://бокатюк.бел";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AuthState _auth;
    private readonly HttpClient _http;

    public KorendzhApiClient(AuthState auth)
    {
        _auth = auth;
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string Token, DateTime ExpiresAtUtc, string FullName, string[] Roles);
    public record TimeEntryDto(
        Guid Id, Guid WorkerId, DateOnly WorkDate, decimal Hours,
        string TaskName, Guid? CarId, string? CarName, string? LicensePlate, string? Description,
        DateTime CreatedAt, DateTime? UpdatedAt);
    public record CreateEntryRequest(
        Guid? WorkerId, DateOnly WorkDate, decimal Hours, string TaskName,
        string? CarName, string? LicensePlate, string? Description);

    public async Task<LoginResponse?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password), JsonOpts, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts, ct);
        if (body is not null)
        {
            _auth.Set(body.Token, body.ExpiresAtUtc, body.FullName, body.Roles);
            await _auth.SaveAsync();
        }
        return body;
    }

    public async Task<List<TimeEntryDto>> GetMyEntriesAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        SetAuthHeader();
        var qs = new List<string>();
        if (from.HasValue) qs.Add($"from={from:yyyy-MM-dd}");
        if (to.HasValue) qs.Add($"to={to:yyyy-MM-dd}");
        var url = "/api/timeentries" + (qs.Count > 0 ? "?" + string.Join('&', qs) : "");
        var list = await _http.GetFromJsonAsync<List<TimeEntryDto>>(url, JsonOpts, ct);
        return list ?? new List<TimeEntryDto>();
    }

    public async Task<bool> CreateEntryAsync(CreateEntryRequest req, CancellationToken ct = default)
    {
        SetAuthHeader();
        var resp = await _http.PostAsJsonAsync("/api/timeentries", req, JsonOpts, ct);
        return resp.IsSuccessStatusCode;
    }

    private void SetAuthHeader()
    {
        if (!string.IsNullOrEmpty(_auth.Token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        }
    }
}
