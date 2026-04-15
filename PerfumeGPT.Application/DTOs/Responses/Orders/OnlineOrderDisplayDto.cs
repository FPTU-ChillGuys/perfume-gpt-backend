namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
 public record OnlineOrderDisplayDto
    {
        public required UserOrderResponse Order { get; init; }
    }
}
