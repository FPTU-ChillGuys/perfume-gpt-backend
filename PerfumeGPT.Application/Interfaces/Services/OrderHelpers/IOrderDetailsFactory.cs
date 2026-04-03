using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderDetailsFactory
	{
		Task CreateOrderDetailsAsync(Order order,
			List<(Guid VariantId, int Quantity, decimal LineDiscount)> items,
			decimal? finalTotalAmount = null);
	}
}
