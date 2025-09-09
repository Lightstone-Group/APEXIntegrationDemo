using Microsoft.AspNetCore.Mvc;
using ProductFlowIntegration.Models;
using ProductFlowIntegration.Services;
using System.Text;
using System.Text.Json;

namespace ProductFlowIntegration.Controllers
{
    public class UserOnboardingController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserOnboardingController> _logger;
        private readonly TokenService _tokenService;

        public UserOnboardingController(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<UserOnboardingController> logger, TokenService tokenService)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _tokenService = tokenService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public IActionResult Index()
        {
            return View(new UserInputModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(UserInputModel input)
        {
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var result = new UserOnboardingResult();

            try
            {
                var token = await _tokenService.GetTokenAsync();
                var authToken = token?.AccessToken ?? _configuration["Lightstone:AuthToken"] ?? _configuration["AppSettings:AuthToken"] ?? "";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("CorrelationId", Guid.NewGuid().ToString());
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
                _httpClient.DefaultRequestHeaders.Add("X-Authenticated-TenantId", AppConfig.TenantId);
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _configuration["Lightstone:ApiKey"] ?? "");
                _httpClient.DefaultRequestHeaders.Add("Referer", AppConfig.Referer);

                // Step 1: Create User
                var userObject = new
                {
                    signInNames = new[] { new { value = input.Email } },
                    givenName = input.FirstName,
                    surname = input.LastName
                };

                var content = new StringContent(JsonSerializer.Serialize(userObject, _jsonOptions), Encoding.UTF8, "application/json");
                content = new StringContent(JsonSerializer.Serialize(userObject), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(AppConfig.CreateUserUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Create User API failed: Status {StatusCode}, Response: {Response}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Create User API failed with status: {response.StatusCode}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<UserCreateResponse>(responseBody)!;
                var user2 = JsonSerializer.Deserialize<dynamic>(responseBody)!;

                if (string.IsNullOrEmpty(user.Id))
                {
                    throw new InvalidOperationException("Failed to get Party ID from Create User response");
                }
                var partyId = user.Id;

                // Step 2: Onboard User
                var onboardingObject = new
                {
                    name = input.FirstName,
                    surname = input.LastName,
                    contactNumber = "",
                    options = new
                    {
                        associatePartyWithTenant = true,
                        sendWelcomeEmail = false,
                        async = true
                    },
                    accountType = 863480000,
                };
                _httpClient.DefaultRequestHeaders.Add("X-Ls-Party-Id", partyId);
                var onboardingContent = new StringContent(JsonSerializer.Serialize(onboardingObject, _jsonOptions), Encoding.UTF8, "application/json");
                var onboardingResponse = await _httpClient.PostAsync(AppConfig.OnboardingUrl, onboardingContent);

                if (!onboardingResponse.IsSuccessStatusCode)
                {
                    var errorContent = await onboardingResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Onboarding API failed: Status {StatusCode}, Response: {Response}", onboardingResponse.StatusCode, errorContent);
                    throw new HttpRequestException($"Onboarding API failed with status: {onboardingResponse.StatusCode}");
                }

                result.PartyId = partyId;
                result.Success = true;
                result.Message = $"Successfully onboarded user {input.Email} into APEX.";

                _logger.LogInformation("Successfully onboarded user {Email} with Party ID {PartyId}", input.Email, partyId);
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.Message = $"API request failed: {ex.Message}";
                _logger.LogError(ex, "HTTP request failed during user onboarding");
            }
            catch (JsonException ex)
            {
                result.Success = false;
                result.Message = "Failed to process API response";
                _logger.LogError(ex, "JSON processing failed during user onboarding");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"An unexpected error occurred: {ex.Message}";
                _logger.LogError(ex, "Unexpected error during user onboarding");
            }

            return View("Result", result);
        }
    }
}