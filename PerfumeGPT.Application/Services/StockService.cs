using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class StockService : IStockService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;

		public StockService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		#endregion Dependencies



		public async Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity)
			=> await _unitOfWork.Stocks.HasSufficientStockAsync(variantId, requiredQuantity);

		public async Task<BaseResponse<PagedResult<StockResponse>>> GetInventoryAsync(GetPagedInventoryRequest request)
		{
			var (stockResponses, totalCount) = await _unitOfWork.Stocks.GetPagedInventoryAsync(request);

			var pagedResult = new PagedResult<StockResponse>(
				stockResponses,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<StockResponse>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<StockResponse>> GetStockByVariantIdAsync(Guid variantId)
		{
			var response = await _unitOfWork.Stocks.GetStockWithDetailsByVariantIdAsync(variantId)
			 ?? throw AppException.NotFound($"Không tìm thấy tồn kho cho biến thể {variantId}");

			return BaseResponse<StockResponse>.Ok(response);
		}

		public async Task<BaseResponse<InventorySummaryResponse>> GetInventorySummaryAsync()
		{
			var now = DateTime.UtcNow;
			var expiringSoonDate = now.AddDays(30);

			var (totalVariants, totalStockQuantity, lowStockVariantsCount, outOfStockVariantsCount) = await _unitOfWork.Stocks.GetInventorySummaryDataAsync();

			var allBatches = await _unitOfWork.Batches.GetAllAsync(asNoTracking: true);

			var summary = new InventorySummaryResponse
			{
				TotalVariants = totalVariants,
				TotalStockQuantity = totalStockQuantity,
				LowStockVariantsCount = lowStockVariantsCount,
				OutOfStockVariantsCount = outOfStockVariantsCount,
				TotalBatches = allBatches.Count(),
				ExpiredBatchesCount = allBatches.Count(b => b.ExpiryDate < now),
				ExpiringSoonCount = allBatches.Count(b => b.ExpiryDate >= now && b.ExpiryDate <= expiringSoonDate)
			};

			return BaseResponse<InventorySummaryResponse>.Ok(summary);
		}

		public async Task InitStockAsync(Guid variantId, int initialQuantity, int lowThreshold)
		{
			var exists = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (exists != null)
			{
				throw AppException.Conflict($"Tồn kho cho biến thể {variantId} đã tồn tại.");
			}

			var newStock = new Stock(variantId, initialQuantity, lowThreshold);
			await _unitOfWork.Stocks.AddAsync(newStock);
		}
	}
}
