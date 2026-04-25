using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;

namespace PerfumeGPT.Persistence.Repositories.Nats;

/// <summary>
/// NATS-specific repository implementation for Cart operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public sealed class NatsCartRepository : INatsCartRepository
{
	private readonly PerfumeDbContext _context;

	public NatsCartRepository(PerfumeDbContext context)
	{
		_context = context;
	}

	public async Task<NatsCartResponse?> GetCartByUserIdForNatsAsync(Guid userId)
	{
		var cartItems = await _context.CartItems
			.Where(ci => ci.UserId == userId)
			.Include(ci => ci.ProductVariant)
				.ThenInclude(pv => pv.Product)
			.Include(ci => ci.ProductVariant)
				.ThenInclude(pv => pv.Concentration)
			.Include(ci => ci.ProductVariant)
				.ThenInclude(pv => pv.Media.Where(m => !m.IsDeleted && m.IsPrimary))
			.ToListAsync();

		if (!cartItems.Any())
		{
			return null;
		}

		var items = cartItems
			.Select(ci => new NatsCartItemResponse
			{
				CartItemId = ci.Id.ToString(),
				VariantId = ci.VariantId.ToString(),
				VariantName = $"{ci.ProductVariant.Product.Name} {ci.ProductVariant.VolumeMl}ml {ci.ProductVariant.Concentration.Name}",
				ImageUrl = ci.ProductVariant.Media.FirstOrDefault()?.Url ?? string.Empty,
				VolumeMl = ci.ProductVariant.VolumeMl,
				Type = ci.ProductVariant.Type.ToString(),
				VariantPrice = ci.ProductVariant.RetailPrice ?? ci.ProductVariant.BasePrice,
				Quantity = ci.Quantity,
				IsAvailable = ci.ProductVariant.Status == VariantStatus.Active,
				SubTotal = (ci.ProductVariant.RetailPrice ?? ci.ProductVariant.BasePrice) * ci.Quantity,
				PromotionalQuantity = 0,
				RegularQuantity = ci.Quantity,
				Discount = 0,
				FinalTotal = (ci.ProductVariant.RetailPrice ?? ci.ProductVariant.BasePrice) * ci.Quantity
			})
			.ToList();

		return new NatsCartResponse
		{
			Items = items,
			TotalCount = items.Count,
			TotalAmount = items.Sum(i => i.SubTotal),
			TotalDiscount = items.Sum(i => i.Discount),
			FinalTotal = items.Sum(i => i.FinalTotal)
		};
	}
}
