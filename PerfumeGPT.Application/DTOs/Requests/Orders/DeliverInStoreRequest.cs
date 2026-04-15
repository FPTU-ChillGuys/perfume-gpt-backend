namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
    public record DeliverInStoreRequest
    {
        public string? PosSessionId { get; init; }
    }
}
