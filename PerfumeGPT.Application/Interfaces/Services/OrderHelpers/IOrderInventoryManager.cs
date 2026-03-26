namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderInventoryManager
	{
		Task<bool> ValidateStockAvailabilityAsync(List<(Guid VariantId, int Quantity)> items);
		Task<bool> DeductInventoryAsync(List<(Guid VariantId, int Quantity)> items);
	}
}
