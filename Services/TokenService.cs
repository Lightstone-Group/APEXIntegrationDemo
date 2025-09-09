using System.Text;
using System.Text.Json;
using ProductFlowIntegration.Models;

namespace ProductFlowIntegration.Services
{
    public class TokenService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<TokenService> _logger;
        private readonly KeyVaultService _keyVaultService;
        private readonly IConfiguration _configuration;

        public TokenService(IHttpClientFactory httpClientFactory, ILogger<TokenService> logger, KeyVaultService keyVaultService, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _keyVaultService = keyVaultService;
            _configuration = configuration;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<TokenResponse?> GetTokenAsync()
        {
            try
            {
                // Try to get auth settings from Key Vault first, fallback to configuration
                var authTokenUrl = await GetConfigValueAsync("Lightstone--TokenUrl", "Lightstone:TokenUrl");
                var authUserEmail = await GetConfigValueAsync("Lightstone--UserEmail", "Lightstone:UserEmail");
                var authUserPassword = await GetConfigValueAsync("Lightstone--UserPassword", "Lightstone:UserPassword");
                var clientId = await GetConfigValueAsync("Lightstone--ClientId", "Lightstone:ClientId");

                if (string.IsNullOrEmpty(authTokenUrl) || string.IsNullOrEmpty(authUserEmail) ||
                    string.IsNullOrEmpty(authUserPassword) || string.IsNullOrEmpty(clientId))
                {
                    _logger.LogError("Missing required authentication configuration");   
                    return null;
                }

                var formData = new List<KeyValuePair<string, string>>
                {
                    new("client_id", clientId),
                    new("grant_type", "password"),
                    new("scope", $"openid {clientId} offline_access"),
                    new("response_type", "token id_token"),
                    new("username", authUserEmail),
                    new("password", authUserPassword)
                };

                var content = new FormUrlEncodedContent(formData);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await _httpClient.PostAsync(authTokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Token request failed: Status {StatusCode}, Response: {Response}",
                        response.StatusCode, errorContent);
                    return null;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Token response received");

                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody, _jsonOptions);
                return tokenResponse;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error occurred while requesting token");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred while processing token response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while requesting token");
                return null;
            }
        }

        private async Task<string?> GetConfigValueAsync(string keyVaultSecretName, string configurationKey)
        {
            try
            {
                // Try Key Vault first
                var keyVaultValue = await _keyVaultService.GetSecretAsync(keyVaultSecretName);
                if (!string.IsNullOrEmpty(keyVaultValue))
                {
                    _logger.LogDebug("Retrieved config value from Key Vault: {SecretName}", keyVaultSecretName);
                    return keyVaultValue;
                }

                // Fallback to configuration
                var configValue = _configuration[configurationKey];
                if (!string.IsNullOrEmpty(configValue))
                {
                    _logger.LogDebug("Retrieved config value from configuration: {ConfigKey}", configurationKey);
                    return configValue;
                }

                _logger.LogWarning("Config value not found in Key Vault or configuration: {SecretName}/{ConfigKey}",
                    keyVaultSecretName, configurationKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving config value: {SecretName}/{ConfigKey}",
                    keyVaultSecretName, configurationKey);

                // Fallback to configuration on error
                return _configuration[configurationKey];
            }
        }
    }
}