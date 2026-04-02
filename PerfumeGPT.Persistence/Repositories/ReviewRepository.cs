using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ReviewRepository : GenericRepository<Review>, IReviewRepository
	{
		public ReviewRepository(PerfumeDbContext context) : base(context) { }

		public async Task<ReviewDetailResponse?> GetReviewWithDetailsAsync(Guid reviewId)
		=> await _context.Reviews
			.Where(r => r.Id == reviewId && !r.IsDeleted)
          .Select(r => new ReviewDetailResponse
			{
				Id = r.Id,
				UserId = r.UserId,
				UserFullName = r.User.FullName,
				UserProfilePictureUrl = r.User.ProfilePicture != null ? r.User.ProfilePicture.Url : null,
				OrderDetailId = r.OrderDetailId,
				OrderId = r.OrderDetail.OrderId,
				Quantity = r.OrderDetail.Quantity,
				UnitPrice = r.OrderDetail.UnitPrice,
				VariantId = r.OrderDetail.VariantId,
				VariantName = r.OrderDetail.ProductVariant.Product.Name + " " + r.OrderDetail.ProductVariant.VolumeMl + "ml " + r.OrderDetail.ProductVariant.Concentration.Name,
				ProductName = r.OrderDetail.ProductVariant.Product.Name,
				VolumeMl = r.OrderDetail.ProductVariant.VolumeMl,
				ConcentrationName = r.OrderDetail.ProductVariant.Concentration.Name,
				Rating = r.Rating,
				Comment = r.Comment ?? string.Empty,
				Images = r.ReviewImages
					.Where(ri => !ri.IsDeleted)
					.Select(ri => new MediaResponse
					{
						Id = ri.Id,
						Url = ri.Url,
						AltText = ri.AltText,
						DisplayOrder = ri.DisplayOrder,
						IsPrimary = ri.IsPrimary,
						FileSize = ri.FileSize,
						MimeType = ri.MimeType
					}).ToList(),
				StaffFeedbackComment = r.StaffFeedbackComment,
				StaffFeedbackByStaffId = r.StaffFeedbackByStaffId,
				StaffFeedbackAt = r.StaffFeedbackAt,
				CreatedAt = r.CreatedAt,
				UpdatedAt = r.UpdatedAt
			})
			.AsNoTracking()
			.FirstOrDefaultAsync();

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
                .Select(r => new ReviewListItem
				{
					Id = r.Id,
					UserId = r.UserId,
					UserFullName = r.User.FullName,
					UserProfilePictureUrl = r.User.ProfilePicture != null ? r.User.ProfilePicture.Url : null,
					VariantId = r.OrderDetail.VariantId,
					VariantName = r.OrderDetail.ProductVariant.Product.Name + " " + r.OrderDetail.ProductVariant.VolumeMl + "ml " + r.OrderDetail.ProductVariant.Concentration.Name,
					Rating = r.Rating,
					CommentPreview = r.Comment != null
						? (r.Comment.Length > 100 ? r.Comment.Substring(0, 100) + "..." : r.Comment)
						: string.Empty,
					ImageCount = r.ReviewImages.Count(ri => !ri.IsDeleted),
					CreatedAt = r.CreatedAt
				})
				.AsNoTracking()
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<List<ReviewResponse>> GetReviewsByVariantIdAsync(Guid variantId)
		=> await _context.Reviews
			.Where(r => r.OrderDetail.VariantId == variantId && !r.IsDeleted)
            .Select(r => new ReviewResponse
			{
				Id = r.Id,
				UserId = r.UserId,
				UserFullName = r.User.FullName,
				UserProfilePictureUrl = r.User.ProfilePicture != null ? r.User.ProfilePicture.Url : null,
				OrderDetailId = r.OrderDetailId,
				VariantId = r.OrderDetail.VariantId,
				VariantName = r.OrderDetail.ProductVariant.Product.Name + " " + r.OrderDetail.ProductVariant.VolumeMl + "ml " + r.OrderDetail.ProductVariant.Concentration.Name,
				Rating = r.Rating,
				Comment = r.Comment ?? string.Empty,
				StaffFeedbackComment = r.StaffFeedbackComment,
				StaffFeedbackAt = r.StaffFeedbackAt,
				Images = r.ReviewImages
					.Where(ri => !ri.IsDeleted)
					.Select(ri => new MediaResponse
					{
						Id = ri.Id,
						Url = ri.Url,
						AltText = ri.AltText,
						DisplayOrder = ri.DisplayOrder,
						IsPrimary = ri.IsPrimary,
						FileSize = ri.FileSize,
						MimeType = ri.MimeType
					}).ToList(),
				CreatedAt = r.CreatedAt,
				UpdatedAt = r.UpdatedAt
			})
			.OrderByDescending(r => r.CreatedAt)
			.AsNoTracking()
			.ToListAsync();

		public async Task<List<ReviewResponse>> GetReviewsByUserIdAsync(Guid userId)
		=> await _context.Reviews
			.Where(r => r.UserId == userId && !r.IsDeleted)
            .Select(r => new ReviewResponse
			{
				Id = r.Id,
				UserId = r.UserId,
				UserFullName = r.User.FullName,
				UserProfilePictureUrl = r.User.ProfilePicture != null ? r.User.ProfilePicture.Url : null,
				OrderDetailId = r.OrderDetailId,
				VariantId = r.OrderDetail.VariantId,
				VariantName = r.OrderDetail.ProductVariant.Product.Name + " " + r.OrderDetail.ProductVariant.VolumeMl + "ml " + r.OrderDetail.ProductVariant.Concentration.Name,
				Rating = r.Rating,
				Comment = r.Comment ?? string.Empty,
				StaffFeedbackComment = r.StaffFeedbackComment,
				StaffFeedbackAt = r.StaffFeedbackAt,
				Images = r.ReviewImages
					.Where(ri => !ri.IsDeleted)
					.Select(ri => new MediaResponse
					{
						Id = ri.Id,
						Url = ri.Url,
						AltText = ri.AltText,
						DisplayOrder = ri.DisplayOrder,
						IsPrimary = ri.IsPrimary,
						FileSize = ri.FileSize,
						MimeType = ri.MimeType
					}).ToList(),
				CreatedAt = r.CreatedAt,
				UpdatedAt = r.UpdatedAt
			})
			.OrderByDescending(r => r.CreatedAt)
			.AsNoTracking()
			.ToListAsync();

		public async Task<(int TotalReviews, double AverageRating, int[] StarCounts)> GetVariantReviewStatisticsAsync(Guid variantId)
		{
			var reviews = await _context.Reviews
				.Where(r => r.OrderDetail.VariantId == variantId
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
		=> await _context.Reviews.AnyAsync(r => r.UserId == userId && r.OrderDetailId == orderDetailId && !r.IsDeleted);
	}
}
