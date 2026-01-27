using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderDetailsFactory
	{
		Task<BaseResponse<List<OrderDetail>>> CreateOrderDetailsAsync(List<(Guid VariantId, int Quantity)> items);
	}
}
