using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
    public class CartItemRepository : GenericRepository<CartItem>, ICartItemRepository
    {
        public CartItemRepository(PerfumeDbContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<PerfumeGPT.Application.DTOs.Responses.CartItems.GetCartItemResponse> Items, int TotalCount)> GetPagedCartItemsByCartIdAsync(Guid cartId, int pageNumber = 1, int pageSize = 10)
        {
            var query = _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .Select(ci => new PerfumeGPT.Application.DTOs.Responses.CartItems.GetCartItemResponse
                {
                    VariantId = ci.VariantId,
                    Quantity = ci.Quantity
                });

            var total = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<decimal> GetCartTotalByCartIdAsync(Guid cartId)
        {
            // Calculate total based on variant base price and quantity. Join with ProductVariant to get price.
            var total = await _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .Join(_context.ProductVariants, ci => ci.VariantId, pv => pv.Id, (ci, pv) => new { ci.Quantity, pv.BasePrice })
                .SumAsync(x => x.BasePrice * x.Quantity);

            return total;
        }
    }
}
