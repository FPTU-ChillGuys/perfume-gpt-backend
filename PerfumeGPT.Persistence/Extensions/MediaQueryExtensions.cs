using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Persistence.Extensions
{
	public static class MediaQueryExtensions
	{
		public static IQueryable<Media> WhereEntity(this IQueryable<Media> query, EntityType entityType, Guid entityId)
		{
			return entityType switch
			{
				EntityType.Product => query.Where(m => m.ProductId == entityId),
				EntityType.ProductVariant => query.Where(m => m.ProductVariantId == entityId),
				EntityType.User => query.Where(m => m.UserId == entityId),
				EntityType.Review => query.Where(m => m.ReviewId == entityId),
				_ => query.Where(m => false)
			};
		}

		public static IQueryable<Media> WhereEntityNotDeleted(this IQueryable<Media> query, EntityType entityType, Guid entityId)
		{
			return query.WhereEntity(entityType, entityId)
				.Where(m => !m.IsDeleted);
		}

		public static IQueryable<Media> WherePrimaryForEntity(this IQueryable<Media> query, EntityType entityType, Guid entityId)
		{
			return query.WhereEntityNotDeleted(entityType, entityId)
				.Where(m => m.IsPrimary);
		}
	}
}
