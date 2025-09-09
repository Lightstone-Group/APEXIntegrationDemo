namespace ProductFlowIntegration.Models
{
    public class UserOnboardingResult
    {
        public string PartyId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}