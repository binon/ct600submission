using System.Text;
using System.Text.Json;
using CT600Submission.Models;

namespace CT600Submission.Services;

public class HmrcService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HmrcService> _logger;
    private HmrcAuthToken? _currentToken;
    private readonly object _tokenLock = new object();

    public HmrcService(HttpClient httpClient, IConfiguration configuration, ILogger<HmrcService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = configuration["Hmrc:BaseUrl"] ?? "https://test-api.service.hmrc.gov.uk";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        // Add security headers
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CT600-Submission/1.0");
    }

    public Task<string> GetAuthorizationUrlAsync()
    {
        var clientId = _configuration["Hmrc:ClientId"];
        var redirectUri = _configuration["Hmrc:RedirectUri"];
        var scope = "read:corporation-tax write:corporation-tax";

        var authUrl = $"{_httpClient.BaseAddress}oauth/authorize" +
               $"?client_id={clientId}" +
               $"&response_type=code" +
               $"&scope={Uri.EscapeDataString(scope)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri ?? "")}";
        
        return Task.FromResult(authUrl);
    }

    public async Task<HmrcAuthToken> ExchangeCodeForTokenAsync(string authorizationCode)
    {
        try
        {
            var clientId = _configuration["Hmrc:ClientId"];
            var clientSecret = _configuration["Hmrc:ClientSecret"];
            var redirectUri = _configuration["Hmrc:RedirectUri"];

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("client_id", clientId ?? ""),
                new KeyValuePair<string, string>("client_secret", clientSecret ?? ""),
                new KeyValuePair<string, string>("redirect_uri", redirectUri ?? "")
            });

            var response = await _httpClient.PostAsync("/oauth/token", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);

            if (tokenData == null)
            {
                throw new InvalidOperationException("Failed to deserialize token response");
            }

            var newToken = new HmrcAuthToken
            {
                AccessToken = tokenData["access_token"]?.ToString() ?? "",
                TokenType = tokenData["token_type"]?.ToString() ?? "Bearer",
                ExpiresIn = int.Parse(tokenData["expires_in"]?.ToString() ?? "3600"),
                ExpiresAt = DateTime.UtcNow.AddSeconds(int.Parse(tokenData["expires_in"]?.ToString() ?? "3600")),
                RefreshToken = tokenData.ContainsKey("refresh_token") ? tokenData["refresh_token"]?.ToString() : null
            };

            lock (_tokenLock)
            {
                _currentToken = newToken;
            }

            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exchanging authorization code for token");
            throw;
        }
    }

    public async Task<string> SubmitCT600Async(CT600Data data)
    {
        try
        {
            HmrcAuthToken? token;
            lock (_tokenLock)
            {
                token = _currentToken;
            }

            if (token == null || DateTime.UtcNow >= token.ExpiresAt)
            {
                throw new InvalidOperationException("Authentication token is missing or expired. Please authenticate first.");
            }

            var submissionPayload = new
            {
                companyRegistrationNumber = data.CompanyRegistrationNumber,
                taxReference = data.TaxReference,
                periodStart = data.PeriodStart.ToString("yyyy-MM-dd"),
                periodEnd = data.PeriodEnd.ToString("yyyy-MM-dd"),
                turnover = data.Turnover,
                taxableProfit = data.TaxableProfit,
                taxDue = data.TaxDue
            };

            var json = JsonSerializer.Serialize(submissionPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

            var response = await _httpClient.PostAsync("/corporation-tax/ct600/submit", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);

            var submissionReference = responseData?["submissionReference"]?.ToString() ?? 
                                     $"CT600-{DateTime.UtcNow:yyyyMMddHHmmss}";

            _logger.LogInformation("CT600 submitted successfully with reference: {Reference}", submissionReference);
            return submissionReference;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting CT600 to HMRC");
            throw;
        }
    }

    public async Task<SubmissionStatus> GetSubmissionStatusAsync(string submissionReference)
    {
        try
        {
            HmrcAuthToken? token;
            lock (_tokenLock)
            {
                token = _currentToken;
            }

            if (token == null || DateTime.UtcNow >= token.ExpiresAt)
            {
                throw new InvalidOperationException("Authentication token is missing or expired. Please authenticate first.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

            var response = await _httpClient.GetAsync($"/corporation-tax/ct600/status/{submissionReference}");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var statusData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);

            return new SubmissionStatus
            {
                Reference = submissionReference,
                Status = statusData?["status"]?.ToString() ?? "Unknown",
                StatusDate = DateTime.TryParse(statusData?["statusDate"]?.ToString(), out var date) ? date : null,
                Message = statusData?.ContainsKey("message") == true ? statusData["message"]?.ToString() : null
            };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new SubmissionStatus
            {
                Reference = submissionReference,
                Status = "Not Found",
                Message = "Submission reference not found in HMRC system"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submission status from HMRC");
            throw;
        }
    }

    public bool IsAuthenticated()
    {
        lock (_tokenLock)
        {
            return _currentToken != null && DateTime.UtcNow < _currentToken.ExpiresAt;
        }
    }
}
