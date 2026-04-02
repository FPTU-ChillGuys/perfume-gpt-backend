using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;

namespace PerfumeGPT.Application.Services.Helpers.OrderHelpers
{
	public class OrderInventoryManager : IOrderInventoryManager
	{
		private readonly IStockService _stockService;
		private readonly IBatchService _batchService;
		private readonly IVariantService _variantService;

		public OrderInventoryManager(
			IStockService stockService,
			IBatchService batchService,
			IVariantService variantService)
		{
			_stockService = stockService;
			_batchService = batchService;
			_variantService = variantService;
		}

		public async Task<bool> ValidateStockAvailabilityAsync(List<(Guid VariantId, int Quantity)> items)
		{
			foreach (var (VariantId, Quantity) in items)
			{
				// Use StockService to validate stock
				var isStockValid = await _stockService.HasSufficientStockAsync(VariantId, Quantity);
				if (!isStockValid)
				{
					var variantResponse = await _variantService.GetVariantByIdAsync(VariantId);
					var productName = variantResponse.Payload != null ? $"Variant {variantResponse.Payload.Sku}" : "Unknown product";
					throw AppException.BadRequest($"Insufficient stock for {productName}.");
				}

				// Use BatchService to validate batch availability
				var isBatchValid = await _batchService.ValidateBatchAvailabilityAsync(VariantId, Quantity);
				if (!isBatchValid)
				{
					var variantResponse = await _variantService.GetVariantByIdAsync(VariantId);
					var productName = variantResponse.Payload != null ? $"Variant {variantResponse.Payload.Sku}" : "Unknown product";
					throw AppException.BadRequest($"Insufficient batch quantity for {productName}.");
				}
			}

			return true;
		}

		public async Task DeductInventoryAsync(List<(Guid VariantId, int Quantity)> items)
		{
			var aggregatedItems = items
				   .GroupBy(i => i.VariantId)
				   .Select(g => (VariantId: g.Key, Quantity: g.Sum(x => x.Quantity)));

			foreach (var (VariantId, Quantity) in aggregatedItems)
			{
				// Use BatchService to deduct batches (FIFO) - this will also recalculate stock automatically
				var batchDeducted = await _batchService.DeductBatchesByVariantIdAsync(VariantId, Quantity);
				if (!batchDeducted)
				{
					throw AppException.Internal($"Failed to deduct batch quantity for variant {VariantId}.");
				}
			}
		}
	}
}
