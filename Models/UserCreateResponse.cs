using System.Text.Json.Serialization;

namespace ProductFlowIntegration.Models
{
    public class UserCreateResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
