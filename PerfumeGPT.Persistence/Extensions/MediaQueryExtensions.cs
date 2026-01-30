using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using System.Linq.Expressions;

namespace PerfumeGPT.Persistence.Extensions
{
	/// <summary>
	/// Extension methods for querying Media entities with the new foreign key structure
	/// </summary>
	public static class MediaQueryExtensions
	{
		/// <summary>
		/// Creates a predicate to filter Media by EntityType and entity ID
		/// </summary>
		public static Expression<Func<Media, bool>> ByEntity(EntityType entityType, Guid entityId)
		{
			return entityType switch
			{
				EntityType.Product => m => m.ProductId == entityId,
				EntityType.ProductVariant => m => m.ProductVariantId == entityId,
				_ => m => false
			};
		}

		/// <summary>
		/// Filters Media by EntityType and entity ID
		/// </summary>
		public static IQueryable<Media> WhereEntity(this IQueryable<Media> query, EntityType entityType, Guid entityId)
		{
			return entityType switch
			{
				EntityType.Product => query.Where(m => m.ProductId == entityId),
				EntityType.ProductVariant => query.Where(m => m.ProductVariantId == entityId),
				_ => query.Where(m => false)
			};
		}

		/// <summary>
		/// Filters Media by EntityType, entity ID, and ensures not deleted
		/// </summary>
		public static IQueryable<Media> WhereEntityNotDeleted(this IQueryable<Media> query, EntityType entityType, Guid entityId)
		{
			return query.WhereEntity(entityType, entityId)
				.Where(m => !m.IsDeleted);
		}

		/// <summary>
		/// Gets the primary media for an entity
		/// </summary>
		public static IQueryable<Media> WherePrimaryForEntity(this IQueryable<Media> query, EntityType entityType, Guid entityId)
		{
			return query.WhereEntityNotDeleted(entityType, entityId)
				.Where(m => m.IsPrimary);
		}
	}
}
