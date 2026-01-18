namespace PerfumeGPT.Application.DTOs.Requests.LoyaltyPoints
{
    public class CreateLoyaltyPointRequest : UpdateLoyaltyPointRequest
    {
        public Guid UserId { get; set; }
    }
}
