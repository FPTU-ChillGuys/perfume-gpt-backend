using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class StockService : IStockService
	{
		private readonly IStockRepository _stockRepository;

		public StockService(IStockRepository stockRepository)
		{
			_stockRepository = stockRepository;
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
	}
}