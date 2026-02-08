using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
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

		public async Task<ReviewDetailResponse?> GetReviewWithDetailsAsync(Guid reviewId)
		{
			return await _context.Reviews
				.Where(r => r.Id == reviewId && !r.IsDeleted)
				.ProjectToType<ReviewDetailResponse>()
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}

		public async Task<(List<ReviewListItem> Items, int TotalCount)> GetPagedReviewsAsync(GetPagedReviewsRequest request)
		{
			var query = _context.Reviews
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
				.ProjectToType<ReviewListItem>()
				.AsNoTracking()
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<List<ReviewResponse>> GetReviewsByVariantIdAsync(Guid variantId, ReviewStatus? status = null)
		{
			var query = _context.Reviews
				.Where(r => r.OrderDetail.VariantId == variantId && !r.IsDeleted)
				.ProjectToType<ReviewResponse>();

			if (status.HasValue)
			{
				query = query.Where(r => r.Status == status.Value);
			}

			return await query
				.OrderByDescending(r => r.CreatedAt)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<ReviewResponse>> GetReviewsByUserIdAsync(Guid userId)
		{
			return await _context.Reviews
				.Where(r => r.UserId == userId && !r.IsDeleted)
				.ProjectToType<ReviewResponse>()
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

			if (reviews.Count == 0)
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

		public async Task<bool> CanUserReviewOrderDetailAsync(Guid userId, Guid orderDetailId)
		{
			var orderDetail = await _context.OrderDetails
				.Include(od => od.Order)
				.FirstOrDefaultAsync(od => od.Id == orderDetailId);

			if (orderDetail == null || orderDetail.Order.CustomerId != userId)
			{
				return false;
			}

			if (orderDetail.Order.Status != OrderStatus.Delivered)
			{
				return false;
			}

			var hasReviewed = await HasUserReviewedOrderDetailAsync(userId, orderDetailId);
			return !hasReviewed;
		}

		public async Task<bool> HasUserReviewedOrderDetailAsync(Guid userId, Guid orderDetailId)
		{
			return await _context.Reviews
				.AnyAsync(r => r.UserId == userId && r.OrderDetailId == orderDetailId && !r.IsDeleted);
		}
	}
}
