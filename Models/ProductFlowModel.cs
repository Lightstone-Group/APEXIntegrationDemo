using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductFlowIntegration.Models
{
    public class ProductFlowModel
    {
        [JsonPropertyName("productFlowInstanceId")]
        public string ProductFlowInstanceId { get; set; } = string.Empty;
        
        [JsonPropertyName("popupOnly")]
        public bool? PopupOnly { get; set; } = false;
        
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;
        
        [JsonPropertyName("authToken")]
        public string AuthToken { get; set; } = string.Empty;

        [JsonIgnore]
        public string ProductName { get; set; } = string.Empty;
    }
}