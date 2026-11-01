using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FastighetsFix.Web.Models.Authentication;
using System.Net.Http.Headers;

namespace FastighetsFix.Web.Services;

public class AuthenticationApiClient
{
    private readonly HttpClient _httpClient;

    public AuthenticationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string[]> RegisterAsync(RegisterForm model)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register",
            model);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            return [];
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            try
            {
                using var document = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync());

                if (document.RootElement.TryGetProperty(
                        "errors",
                        out var errors))
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(field =>
                            field.Value.EnumerateArray())
                        .Select(message =>
                            message.GetString() ?? "Ogiltigt värde.")
                        .ToArray();

                    if (messages.Length > 0)
                    {
                        return messages;
                    }
                }
            }
            catch (JsonException)
            {
                // Använd det generella meddelandet nedan.
            }
        }

        return ["Registreringen misslyckades. Försök igen senare."];
    }
    
    
    public async Task<TokenResponse?> LoginAsync(LoginForm model)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            model);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<TokenResponse>();
    }

    public async Task<UserProfile?> GetCurrentUserAsync(
        string accessToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/auth/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<UserProfile>();
    }
    
    
}