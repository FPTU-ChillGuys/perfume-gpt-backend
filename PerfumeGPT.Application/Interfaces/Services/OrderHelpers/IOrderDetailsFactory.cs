using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderDetailsFactory
	{
		Task<List<OrderDetail>> CreateOrderDetailsAsync(List<(Guid VariantId, int Quantity)> items);
	}
}
