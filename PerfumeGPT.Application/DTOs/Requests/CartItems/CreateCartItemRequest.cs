namespace PerfumeGPT.Application.DTOs.Requests.CartItems
{
    public class CreateCartItemRequest : UpdateCartItemRequest
    {
        public Guid CartId { get; set; }
        public Guid VariantId { get; set; }
    }
}
