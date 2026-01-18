using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
    public interface ICartItemRepository : IGenericRepository<CartItem>
    {
        Task<(IEnumerable<PerfumeGPT.Application.DTOs.Responses.CartItems.GetCartItemResponse> Items, int TotalCount)> GetPagedCartItemsByCartIdAsync(Guid cartId, int pageNumber = 1, int pageSize = 10);
        Task<decimal> GetCartTotalByCartIdAsync(Guid cartId);
    }
}
