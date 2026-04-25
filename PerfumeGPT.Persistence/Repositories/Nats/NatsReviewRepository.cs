using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories.Nats;

/// <summary>
/// NATS-specific repository implementation for Review operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public sealed class NatsReviewRepository : GenericRepository<Review>, INatsReviewRepository
{
	public NatsReviewRepository(PerfumeDbContext context) : base(context) { }

	public async Task<(List<NatsReviewListItemResponse> Items, int TotalCount)> GetPagedReviewsForNatsAsync(
		int pageNumber,
		int pageSize,
		Guid? variantId = null,
		Guid? userId = null,
		int? minRating = null,
		int? maxRating = null,
		bool? hasImages = null,
		string? sortBy = null,
		bool isDescending = false)
	{
		var query = _context.Reviews
			.Where(r => !r.IsDeleted)
			.AsQueryable();

		// Apply filters
		if (variantId.HasValue)
		{
			query = query.Where(r => r.OrderDetail.VariantId == variantId.Value);
		}

		if (userId.HasValue)
		{
			query = query.Where(r => r.UserId == userId.Value);
		}

		if (minRating.HasValue)
		{
			query = query.Where(r => r.Rating >= minRating.Value);
		}

		if (maxRating.HasValue)
		{
			query = query.Where(r => r.Rating <= maxRating.Value);
		}

		if (hasImages.HasValue)
		{
			if (hasImages.Value)
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
		query = string.IsNullOrWhiteSpace(sortBy) switch
		{
			false when sortBy.Equals("rating", StringComparison.OrdinalIgnoreCase) =>
				isDescending ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
			false when sortBy.Equals("createdAt", StringComparison.OrdinalIgnoreCase) =>
				isDescending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
			_ => query.OrderByDescending(r => r.CreatedAt)
		};

		var items = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.Select(r => new NatsReviewListItemResponse
			{
				Id = r.Id.ToString(),
				UserId = r.UserId.ToString(),
				UserFullName = r.User.FullName,
				UserProfilePictureUrl = r.User.ProfilePicture != null ? r.User.ProfilePicture.Url : null,
				OrderDetailId = r.OrderDetailId.ToString(),
				VariantId = r.OrderDetail.VariantId.ToString(),
				VariantName = r.OrderDetail.ProductVariant.Product.Name + " " + r.OrderDetail.ProductVariant.VolumeMl + "ml " + r.OrderDetail.ProductVariant.Concentration.Name,
				Rating = r.Rating,
				Comment = r.Comment ?? string.Empty,
				StaffFeedbackComment = r.StaffFeedbackComment,
				StaffFeedbackAt = r.StaffFeedbackAt.HasValue ? r.StaffFeedbackAt.Value.ToString("O") : null,
				Images = r.ReviewImages
					.Where(ri => !ri.IsDeleted)
					.Select(ri => new NatsReviewMediaResponse
					{
						Id = ri.Id.ToString(),
						Url = ri.Url,
						ThumbnailUrl = null,
						Type = "Image"
					}).ToList(),
				CreatedAt = r.CreatedAt.ToString("O"),
				UpdatedAt = r.UpdatedAt.HasValue ? r.UpdatedAt.Value.ToString("O") : null
			})
			.AsNoTracking()
			.ToListAsync();

		return (items, totalCount);
	}

	public async Task<List<NatsReviewListItemResponse>> GetReviewsByVariantIdForNatsAsync(Guid variantId)
	{
		return await _context.Reviews
			.Where(r => r.OrderDetail.VariantId == variantId && !r.IsDeleted)
			.Select(r => new NatsReviewListItemResponse
			{
				Id = r.Id.ToString(),
				UserId = r.UserId.ToString(),
				UserFullName = r.User.FullName,
				UserProfilePictureUrl = r.User.ProfilePicture != null ? r.User.ProfilePicture.Url : null,
				OrderDetailId = r.OrderDetailId.ToString(),
				VariantId = r.OrderDetail.VariantId.ToString(),
				VariantName = r.OrderDetail.ProductVariant.Product.Name + " " + r.OrderDetail.ProductVariant.VolumeMl + "ml " + r.OrderDetail.ProductVariant.Concentration.Name,
				Rating = r.Rating,
				Comment = r.Comment ?? string.Empty,
				StaffFeedbackComment = r.StaffFeedbackComment,
				StaffFeedbackAt = r.StaffFeedbackAt.HasValue ? r.StaffFeedbackAt.Value.ToString("O") : null,
				Images = r.ReviewImages
					.Where(ri => !ri.IsDeleted)
					.Select(ri => new NatsReviewMediaResponse
					{
						Id = ri.Id.ToString(),
						Url = ri.Url,
						ThumbnailUrl = null,
						Type = "Image"
					}).ToList(),
				CreatedAt = r.CreatedAt.ToString("O"),
				UpdatedAt = r.UpdatedAt.HasValue ? r.UpdatedAt.Value.ToString("O") : null
			})
			.OrderByDescending(r => r.CreatedAt)
			.AsNoTracking()
			.ToListAsync();
	}

	public async Task<NatsReviewVariantStats> GetVariantReviewStatisticsForNatsAsync(Guid variantId)
	{
		var stats = await _context.Reviews
			.Where(r => r.OrderDetail.VariantId == variantId && !r.IsDeleted)
			.GroupBy(r => r.OrderDetail.VariantId)
			.Select(g => new
			{
				TotalReviews = g.Count(),
				AverageRating = g.Average(r => r.Rating),
				FiveStarCount = g.Count(r => r.Rating == 5),
				FourStarCount = g.Count(r => r.Rating == 4),
				ThreeStarCount = g.Count(r => r.Rating == 3),
				TwoStarCount = g.Count(r => r.Rating == 2),
				OneStarCount = g.Count(r => r.Rating == 1)
			})
			.FirstOrDefaultAsync();

		if (stats == null)
		{
			return new NatsReviewVariantStats
			{
				VariantId = variantId.ToString(),
				TotalReviews = 0,
				AverageRating = 0,
				FiveStarCount = 0,
				FourStarCount = 0,
				ThreeStarCount = 0,
				TwoStarCount = 0,
				OneStarCount = 0
			};
		}

		return new NatsReviewVariantStats
		{
			VariantId = variantId.ToString(),
			TotalReviews = stats.TotalReviews,
			AverageRating = Math.Round(stats.AverageRating, 2),
			FiveStarCount = stats.FiveStarCount,
			FourStarCount = stats.FourStarCount,
			ThreeStarCount = stats.ThreeStarCount,
			TwoStarCount = stats.TwoStarCount,
			OneStarCount = stats.OneStarCount
		};
	}
}
