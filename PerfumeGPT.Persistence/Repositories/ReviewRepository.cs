using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ReviewRepository : GenericRepository<Review>, IReviewRepository
	{
		public ReviewRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<Review?> GetReviewWithDetailsAsync(Guid reviewId)
		{
			return await _context.Reviews
				.Include(r => r.User)
				.Include(r => r.OrderDetail)
					.ThenInclude(od => od.ProductVariant)
						.ThenInclude(v => v.Product)
				.Include(r => r.OrderDetail)
					.ThenInclude(od => od.ProductVariant)
						.ThenInclude(v => v.Concentration)
				.Include(r => r.OrderDetail)
					.ThenInclude(od => od.Order)
				.Include(r => r.ModeratedByStaff)
				.Include(r => r.ReviewImages.Where(m => !m.IsDeleted))
				.AsNoTracking()
				.FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted);
		}

		public async Task<(List<Review> Items, int TotalCount)> GetPagedReviewsAsync(GetPagedReviewsRequest request)
		{
			var query = _context.Reviews
				.Include(r => r.User)
				.Include(r => r.OrderDetail)
					.ThenInclude(od => od.ProductVariant)
				.Include(r => r.ReviewImages.Where(m => !m.IsDeleted))
				.Where(r => !r.IsDeleted)
				.AsQueryable();

			// Apply filters
			if (request.VariantId.HasValue)
			{
				query = query.Where(r => r.OrderDetail.VariantId == request.VariantId.Value);
			}

			if (request.UserId.HasValue)
			{
				query = query.Where(r => r.UserId == request.UserId.Value);
			}

			if (request.Status.HasValue)
			{
				query = query.Where(r => r.Status == request.Status.Value);
			}

			if (request.MinRating.HasValue)
			{
				query = query.Where(r => r.Rating >= request.MinRating.Value);
			}

			if (request.MaxRating.HasValue)
			{
				query = query.Where(r => r.Rating <= request.MaxRating.Value);
			}

			if (request.HasImages.HasValue)
			{
				if (request.HasImages.Value)
				{
					query = query.Where(r => r.ReviewImages.Any(m => !m.IsDeleted));
				}
				else
				{
					query = query.Where(r => !r.ReviewImages.Any(m => !m.IsDeleted));
				}
			}

			var totalCount = await query.CountAsync();

			// Apply sorting
			query = string.IsNullOrWhiteSpace(request.SortBy) switch
			{
				false when request.SortBy.Equals("rating", StringComparison.OrdinalIgnoreCase) =>
					request.IsDescending ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
				false when request.SortBy.Equals("createdAt", StringComparison.OrdinalIgnoreCase) =>
					request.IsDescending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
				_ => query.OrderByDescending(r => r.CreatedAt) // Default sort
			};

			var items = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.AsNoTracking()
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<bool> CanUserReviewOrderDetailAsync(Guid userId, Guid orderDetailId)
		{
			// Check if the order detail exists and belongs to the user
			var orderDetail = await _context.OrderDetails
				.Include(od => od.Order)
				.FirstOrDefaultAsync(od => od.Id == orderDetailId);

			if (orderDetail == null || orderDetail.Order.CustomerId != userId)
			{
				return false;
			}

			// Check if the order is delivered
			if (orderDetail.Order.Status != OrderStatus.Delivered)
			{
				return false;
			}

			// Check if user has already reviewed this order detail
			var hasReviewed = await HasUserReviewedOrderDetailAsync(userId, orderDetailId);
			return !hasReviewed;
		}

		public async Task<bool> HasUserReviewedOrderDetailAsync(Guid userId, Guid orderDetailId)
		{
			return await _context.Reviews
				.AnyAsync(r => r.UserId == userId && r.OrderDetailId == orderDetailId && !r.IsDeleted);
		}

		public async Task<List<Review>> GetReviewsByVariantIdAsync(Guid variantId, ReviewStatus? status = null)
		{
			var query = _context.Reviews
				.Include(r => r.User)
				.Include(r => r.ReviewImages.Where(m => !m.IsDeleted))
				.Where(r => r.OrderDetail.VariantId == variantId && !r.IsDeleted);

			if (status.HasValue)
			{
				query = query.Where(r => r.Status == status.Value);
			}

			return await query
				.OrderByDescending(r => r.CreatedAt)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<Review>> GetReviewsByUserIdAsync(Guid userId)
		{
			return await _context.Reviews
				.Include(r => r.OrderDetail)
					.ThenInclude(od => od.ProductVariant)
						.ThenInclude(v => v.Product)
				.Include(r => r.ReviewImages.Where(m => !m.IsDeleted))
				.Where(r => r.UserId == userId && !r.IsDeleted)
				.OrderByDescending(r => r.CreatedAt)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<(int TotalReviews, double AverageRating, int[] StarCounts)> GetVariantReviewStatisticsAsync(Guid variantId)
		{
			var reviews = await _context.Reviews
				.Where(r => r.OrderDetail.VariantId == variantId 
					&& r.Status == ReviewStatus.Approved 
					&& !r.IsDeleted)
				.Select(r => r.Rating)
				.ToListAsync();

			if (!reviews.Any())
			{
				return (0, 0, new int[5]);
			}

			var totalReviews = reviews.Count;
			var averageRating = reviews.Average();

			var starCounts = new int[5];
			for (int i = 1; i <= 5; i++)
			{
				starCounts[i - 1] = reviews.Count(r => r == i);
			}

			return (totalReviews, averageRating, starCounts);
		}

		public async Task<(List<Review> Items, int TotalCount)> GetPendingReviewsAsync(int pageNumber, int pageSize)
		{
			var query = _context.Reviews
				.Include(r => r.User)
				.Include(r => r.OrderDetail)
					.ThenInclude(od => od.ProductVariant)
				.Include(r => r.ReviewImages.Where(m => !m.IsDeleted))
				.Where(r => r.Status == ReviewStatus.Pending && !r.IsDeleted);

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderBy(r => r.CreatedAt)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.AsNoTracking()
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<bool> IsOrderDetailDeliveredToUserAsync(Guid userId, Guid orderDetailId)
		{
			return await _context.OrderDetails
				.Include(od => od.Order)
				.AnyAsync(od => od.Id == orderDetailId 
					&& od.Order.CustomerId == userId 
					&& od.Order.Status == OrderStatus.Delivered);
		}
	}
}
