using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace ProductFlowIntegration.Services
{
    public class KeyVaultService
    {
        private readonly SecretClient? _secretClient;
        private readonly ILogger<KeyVaultService> _logger;

        public KeyVaultService(IConfiguration configuration, ILogger<KeyVaultService> logger)
        {
            _logger = logger;
            
            try
            {
                var keyVaultUrl = configuration["KeyVault:VaultUrl"];
                
                if (!string.IsNullOrEmpty(keyVaultUrl))
                {
                    var clientId = configuration["KeyVault:ClientId"];
                    var clientSecret = configuration["KeyVault:ClientSecret"];
                    var tenantId = configuration["KeyVault:TenantId"];

                    if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(tenantId))
                    {
                        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                        _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
                        _logger.LogInformation("Azure Key Vault client initialized successfully");
                    }
                    else
                    {
                        // Fall back to DefaultAzureCredential for managed identity or other auth methods
                        _secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
                        _logger.LogInformation("Azure Key Vault client initialized with DefaultAzureCredential");
                    }
                }
                else
                {
                    _logger.LogWarning("Key Vault URL not configured, will use fallback configuration");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Key Vault client");
                _secretClient = null;
            }
        }

        public async Task<string?> GetSecretAsync(string secretName)
        {
            if (_secretClient == null)
            {
                _logger.LogDebug("Key Vault client not available for secret: {SecretName}", secretName);
                return null;
            }

            try
            {
                var response = await _secretClient.GetSecretAsync(secretName);
                _logger.LogDebug("Successfully retrieved secret: {SecretName}", secretName);
                return response.Value.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve secret from Key Vault: {SecretName}", secretName);
                return null;
            }
        }
    }
}