using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderDetailsFactory
	{
		Task CreateOrderDetailsAsync(Order order, List<CartCheckoutItemDto> items);
	}
}
