using Microsoft.Extensions.Configuration;

namespace ProductFlowIntegration
{
    public static class AppConfig
    {
        private static IConfiguration? _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string ProductCode => _configuration?["AppSettings:ProductCode"] ?? string.Empty;
        public static string ProductName => _configuration?["AppSettings:ProductName"] ?? string.Empty;
        public static string TenantId => _configuration?["AppSettings:TenantId"] ?? string.Empty;
        public static string Referer => _configuration?["AppSettings:Referer"] ?? string.Empty;
        public static string ProductUrl => _configuration?["AppSettings:ProductUrl"] ?? string.Empty;
        public static string CreateUserUrl => _configuration?["AppSettings:CreateUserUrl"] ?? string.Empty;
        public static string OnboardingUrl => _configuration?["AppSettings:OnboardingUrl"] ?? string.Empty;

    }
}