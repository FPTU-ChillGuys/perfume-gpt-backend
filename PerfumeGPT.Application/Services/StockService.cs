using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class StockService : IStockService
	{
		private readonly IStockRepository _stockRepository;
		private readonly IBatchRepository _batchRepository;

		public StockService(IStockRepository stockRepository, IBatchRepository batchRepository)
		{
			_stockRepository = stockRepository;
			_batchRepository = batchRepository;
		}

		public async Task<bool> IsValidToCartAsync(Guid variantId, int requiredQuantity)
		{
			return await _stockRepository.IsValidToCart(variantId, requiredQuantity);
		}

		public async Task<bool> IncreaseStockAsync(Guid variantId, int quantity)
		{
			// Validation
			if (quantity <= 0)
			{
				return false;
			}

			var stock = await _stockRepository.FirstOrDefaultAsync(s => s.VariantId == variantId);

			if (stock == null)
			{
				// Create new stock record with default threshold
				stock = new Stock
				{
					VariantId = variantId,
					TotalQuantity = quantity,
					LowStockThreshold = 10
				};
				await _stockRepository.AddAsync(stock);
			}
			else
			{
				// Update existing stock
				stock.TotalQuantity += quantity;
				_stockRepository.Update(stock);
			}

			// Don't call SaveChanges - let the orchestrator/transaction handle it
			return true;
		}

		public async Task<bool> DecreaseStockAsync(Guid variantId, int quantity)
		{
			// Validation
			if (quantity <= 0)
			{
				return false;
			}

			var stock = await _stockRepository.FirstOrDefaultAsync(s => s.VariantId == variantId);

			if (stock == null)
			{
				return false;
			}

			// Update stock and ensure it doesn't go below zero
			stock.TotalQuantity -= quantity;
			if (stock.TotalQuantity < 0)
			{
				stock.TotalQuantity = 0;
			}

			_stockRepository.Update(stock);

			// Don't call SaveChanges - let the orchestrator/transaction handle it
			return true;
		}

		public async Task<BaseResponse<PagedResult<StockResponse>>> GetInventoryAsync(GetInventoryRequest request)
		{
			try
			{
				var (stocks, totalCount) = await _stockRepository.GetPagedInventoryAsync(
					request.VariantId,
					request.SearchTerm,
					request.IsLowStock,
					request.SortBy,
					request.SortOrder,
					request.PageNumber,
					request.PageSize);

				var stockResponses = stocks.Select(s => new StockResponse
				{
					Id = s.Id,
					VariantId = s.VariantId,
					VariantSku = s.ProductVariant.Sku,
					ProductName = s.ProductVariant.Product.Name,
					VolumeMl = s.ProductVariant.VolumeMl,
					ConcentrationName = s.ProductVariant.Concentration.Name,
					TotalQuantity = s.TotalQuantity,
					LowStockThreshold = s.LowStockThreshold,
					IsLowStock = s.TotalQuantity <= s.LowStockThreshold
				}).ToList();

				var pagedResult = new PagedResult<StockResponse>(
					stockResponses,
					request.PageNumber,
					request.PageSize,
					totalCount
				);

				return BaseResponse<PagedResult<StockResponse>>.Ok(pagedResult);
			}
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<StockResponse>>.Fail(
					$"Error retrieving inventory: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<StockResponse>> GetStockByVariantIdAsync(Guid variantId)
		{
			try
			{
				var stock = await _stockRepository.GetStockWithDetailsByVariantIdAsync(variantId);

				if (stock == null)
				{
					return BaseResponse<StockResponse>.Fail(
						"Stock not found for this variant",
						ResponseErrorType.NotFound
					);
				}

				var response = new StockResponse
				{
					Id = stock.Id,
					VariantId = stock.VariantId,
					VariantSku = stock.ProductVariant.Sku,
					ProductName = stock.ProductVariant.Product.Name,
					VolumeMl = stock.ProductVariant.VolumeMl,
					ConcentrationName = stock.ProductVariant.Concentration.Name,
					TotalQuantity = stock.TotalQuantity,
					LowStockThreshold = stock.LowStockThreshold,
					IsLowStock = stock.TotalQuantity <= stock.LowStockThreshold
				};

				return BaseResponse<StockResponse>.Ok(response);
			}
			catch (Exception ex)
			{
				return BaseResponse<StockResponse>.Fail(
					$"Error retrieving stock: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}

		public async Task<BaseResponse<InventorySummaryResponse>> GetInventorySummaryAsync()
		{
			try
			{
				var now = DateTime.UtcNow;
				var expiringSoonDate = now.AddDays(30);

				var (totalVariants, totalStockQuantity, lowStockVariantsCount) = await _stockRepository.GetInventorySummaryDataAsync();

				var allBatches = await _batchRepository.GetAllAsync(
					asNoTracking: true
				);

				var summary = new InventorySummaryResponse
				{
					TotalVariants = totalVariants,
					TotalStockQuantity = totalStockQuantity,
					LowStockVariantsCount = lowStockVariantsCount,
					TotalBatches = allBatches.Count(),
					ExpiredBatchesCount = allBatches.Count(b => b.ExpiryDate < now),
					ExpiringSoonCount = allBatches.Count(b => b.ExpiryDate >= now && b.ExpiryDate <= expiringSoonDate)
				};

				return BaseResponse<InventorySummaryResponse>.Ok(summary);
			}
			catch (Exception ex)
			{
				return BaseResponse<InventorySummaryResponse>.Fail(
					$"Error retrieving inventory summary: {ex.Message}",
					ResponseErrorType.InternalError
				);
			}
		}
	}
}
