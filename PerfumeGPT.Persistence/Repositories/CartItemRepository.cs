using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class CartItemRepository : GenericRepository<CartItem>, ICartItemRepository
	{
		public CartItemRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<GetCartItemResponse>> GetCartItemsByUserIdAsync(Guid userId, List<Guid>? itemIds = null)
		{
			var query = _context.CartItems.Where(ci => ci.UserId == userId);

			if (itemIds != null && itemIds.Count > 0)
				query = query.Where(ci => itemIds.Contains(ci.Id) && ci.ProductVariant.Stock.TotalQuantity - ci.ProductVariant.Stock.ReservedQuantity > 0);

			return await query
			   .Select(ci => new GetCartItemResponse
			   {
				   CartItemId = ci.Id,
				   VariantId = ci.VariantId,
				   VariantName = $"{ci.ProductVariant.Product.Name} - {ci.ProductVariant.Concentration.Name} - {ci.ProductVariant.VolumeMl}ml",
				   ImageUrl = ci.ProductVariant.Media != null
						? (ci.ProductVariant.Media.Where(m => m.IsPrimary).Select(m => m.Url).FirstOrDefault()
							?? ci.ProductVariant.Media.Select(m => m.Url).FirstOrDefault()
							?? string.Empty)
						: string.Empty,
				   VolumeMl = ci.ProductVariant.VolumeMl,
				   Type = ci.ProductVariant.Type,
				   VariantPrice = ci.ProductVariant.BasePrice,
				   Quantity = ci.Quantity,
				   IsAvailable = ci.ProductVariant.Stock.TotalQuantity - ci.ProductVariant.Stock.ReservedQuantity > 0
			   })
				.ToListAsync();
		}

		public async Task<List<CartCheckoutItemDto>> GetCartCheckoutItemsAsync(Guid userId, List<Guid>? itemIds = null)
		{
			var query = _context.CartItems.Where(ci => ci.UserId == userId);

			if (itemIds != null && itemIds.Count > 0)
				query = query.Where(ci => itemIds.Contains(ci.Id) && ci.ProductVariant.Stock.TotalQuantity - ci.ProductVariant.Stock.ReservedQuantity > 0);

			return await query
			   .Select(ci => new CartCheckoutItemDto
			   {
				   VariantId = ci.VariantId,
				   VariantName = $"{ci.ProductVariant.Product.Name} - {ci.ProductVariant.Concentration.Name} - {ci.ProductVariant.VolumeMl}ml",
				   Quantity = ci.Quantity,
				   UnitPrice = ci.ProductVariant.BasePrice,
				   SubTotal = ci.ProductVariant.BasePrice * ci.Quantity,
				   Discount = 0m,
				   FinalTotal = ci.ProductVariant.BasePrice * ci.Quantity
			   })
				.ToListAsync();
		}

		public async Task<List<CartItemPriceDto>> GetCartItemPricesAsync(Guid userId, List<Guid>? itemIds)
		{
			var query = _context.CartItems.Where(ci => ci.UserId == userId);

			if (itemIds != null && itemIds.Count > 0)
				query = query.Where(ci => itemIds.Contains(ci.Id) && ci.ProductVariant.Stock.TotalQuantity - ci.ProductVariant.Stock.ReservedQuantity > 0);

			return await query
			  .Select(ci => new CartItemPriceDto
			  {
				  VariantId = ci.VariantId,
				  VariantName = $"{ci.ProductVariant.Product.Name} - {ci.ProductVariant.Concentration.Name} - {ci.ProductVariant.VolumeMl}ml",
				  VariantPrice = ci.ProductVariant.BasePrice,
				  Quantity = ci.Quantity
			  })
				.ToListAsync();
		}

		public async Task<bool> HasItemsAsync(Guid userId)
		{
			return await _context.CartItems
				.AnyAsync(ci => ci.UserId == userId);
		}

		public async Task ClearCartByUserIdAsync(Guid userId, List<Guid>? itemIds)
		{
			var query = _context.CartItems.Where(ci => ci.UserId == userId);

			if (itemIds != null && itemIds.Count > 0)
				query = query.Where(ci => itemIds.Contains(ci.Id) && ci.ProductVariant.Stock.TotalQuantity - ci.ProductVariant.Stock.ReservedQuantity > 0);

			_context.CartItems.RemoveRange(query);
		}
	}
}
