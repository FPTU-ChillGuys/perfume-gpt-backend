using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderInventoryManager
	{
		Task<BaseResponse<bool>> ValidateStockAvailabilityAsync(List<(Guid VariantId, int Quantity)> items);
		Task<BaseResponse<bool>> DeductInventoryAsync(List<(Guid VariantId, int Quantity)> items);
		Task<BaseResponse<bool>> RestoreInventoryAsync(List<(Guid VariantId, int Quantity)> items);
	}
}
