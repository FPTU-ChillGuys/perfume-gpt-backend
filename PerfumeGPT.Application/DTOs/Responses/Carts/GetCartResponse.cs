using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
    public class GetCartResponse
    {
        public PagedResult<GetCartItemResponse> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
