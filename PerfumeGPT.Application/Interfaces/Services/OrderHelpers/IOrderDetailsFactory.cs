using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderDetailsFactory
	{
		Task CreateOrderDetailsAsync(Order order,
	   List<(Guid VariantId, Guid? BatchId, int Quantity, decimal LineDiscount, decimal? LineFinalTotal)> items,
		decimal? finalTotalAmount = null);
	}
}
