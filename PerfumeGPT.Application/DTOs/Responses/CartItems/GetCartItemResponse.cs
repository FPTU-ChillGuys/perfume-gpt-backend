namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
    public class GetCartItemResponse
    {
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
