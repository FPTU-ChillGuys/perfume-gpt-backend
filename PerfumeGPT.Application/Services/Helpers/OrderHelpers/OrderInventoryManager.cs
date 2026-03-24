using PerfumeGPT.Application.DTOs.Responses.Base;
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

		public async Task<BaseResponse<bool>> ValidateStockAvailabilityAsync(List<(Guid VariantId, int Quantity)> items)
		{
			foreach (var item in items)
			{
				// Use StockService to validate stock
				var isStockValid = await _stockService.HasSufficientStockAsync(item.VariantId, item.Quantity);
				if (!isStockValid)
				{
					var variantResponse = await _variantService.GetVariantByIdAsync(item.VariantId);
					var productName = variantResponse.Payload != null ? $"Variant {variantResponse.Payload.Sku}" : "Unknown product";
					return BaseResponse<bool>.Fail($"Insufficient stock for {productName}.", ResponseErrorType.BadRequest);
				}

				// Use BatchService to validate batch availability
				var isBatchValid = await _batchService.ValidateBatchAvailabilityAsync(item.VariantId, item.Quantity);
				if (!isBatchValid)
				{
					var variantResponse = await _variantService.GetVariantByIdAsync(item.VariantId);
					var productName = variantResponse.Payload != null ? $"Variant {variantResponse.Payload.Sku}" : "Unknown product";
					return BaseResponse<bool>.Fail($"Insufficient batch quantity for {productName}.", ResponseErrorType.BadRequest);
				}
			}

			return BaseResponse<bool>.Ok(true);
		}

		public async Task<BaseResponse<bool>> DeductInventoryAsync(List<(Guid VariantId, int Quantity)> items)
		{
			foreach (var item in items)
			{
				// Use BatchService to deduct batches (FIFO) - this will also recalculate stock automatically
				var batchDeducted = await _batchService.DeductBatchesByVariantIdAsync(item.VariantId, item.Quantity);
				if (!batchDeducted)
				{
					return BaseResponse<bool>.Fail($"Failed to deduct batch quantity for variant {item.VariantId}.", ResponseErrorType.InternalError);
				}
			}

			return BaseResponse<bool>.Ok(true);
		}

		public async Task<BaseResponse<bool>> RestoreInventoryAsync(List<(Guid VariantId, int Quantity)> items)
		{
			foreach (var item in items)
			{
				// Use StockService to increase stock
             await _stockService.IncreaseStockAsync(item.VariantId, item.Quantity);

				// Note: Batches are not restored as they follow FIFO and have already been consumed
				// New batches should be created through the normal batch creation process
			}

			return BaseResponse<bool>.Ok(true);
		}
	}
}
