using Microsoft.AspNetCore.Mvc;
using ProductFlowIntegration.Models;
using ProductFlowIntegration.Services;
using System.Text;
using System.Text.Json;

namespace ProductFlowIntegration.Controllers
{
    public class ProductFlowController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductFlowController> _logger;

        public ProductFlowController(IConfiguration configuration, IHttpClientFactory httpClientFactory, TokenService tokenService, ILogger<ProductFlowController> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _tokenService = tokenService;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var token = await _tokenService.GetTokenAsync();
                var authToken = token?.AccessToken ?? _configuration["Lightstone:AuthToken"] ?? _configuration["AppSettings:AuthToken"] ?? "";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
                _httpClient.DefaultRequestHeaders.Add("X-Authenticated-Tenantid", AppConfig.TenantId);
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _configuration["Lightstone:ApiKey"] ?? "");
                _httpClient.DefaultRequestHeaders.Add("Referer", AppConfig.Referer);

                var payload = new
                {
                    productCode = AppConfig.ProductCode,
                    productName = AppConfig.ProductName,
                    isUserPresent = true
                };

                var content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(AppConfig.ProductUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call failed with status: {StatusCode}", response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error response: {ErrorContent}", errorContent);
                    throw new HttpRequestException($"API call failed with status: {response.StatusCode}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response: {ResponseBody}", responseBody);

                var result = JsonSerializer.Deserialize<ProductFlowModel>(responseBody, _jsonOptions)!;
                result.ApiKey = _configuration["Lightstone:ApiKey"] ?? "";
                result.AuthToken = authToken;
                result.ProductName = AppConfig.ProductName;
                return View(result);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred");
                return View("Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while starting the product flow");
                return View("Error");
            }
        }

        public IActionResult ProductView(string productFlowInstanceId)
        {
            ViewBag.ProductFlowInstanceId = productFlowInstanceId;
            return View();
        }
    }
}