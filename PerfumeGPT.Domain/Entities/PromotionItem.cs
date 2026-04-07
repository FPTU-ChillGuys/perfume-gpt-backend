using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class PromotionItem : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		protected PromotionItem() { }

		public Guid CampaignId { get; private set; }
		public Guid ProductVariantId { get; private set; }
		public Guid? BatchId { get; private set; }
		public PromotionType ItemType { get; private set; } // Clearance, NewArrival, Regular, etc.
		public bool IsActive { get; private set; }
		public int? MaxUsage { get; private set; }
		public int CurrentUsage { get; private set; }

		// Navigation properties
		public virtual Campaign Campaign { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual Batch? Batch { get; set; }

		// IHasTimestamps  implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// Factory methods
		public static PromotionItem Create(PromotionItemCreationFactor details)
		{
			if (details.CampaignId == Guid.Empty)
				throw DomainException.BadRequest("Campaign ID is required.");

			if (details.ProductVariantId == Guid.Empty)
				throw DomainException.BadRequest("Product variant ID is required.");

			if (details.MaxUsage.HasValue && details.MaxUsage.Value <= 0)
				throw DomainException.BadRequest("Max usage must be greater than 0.");

			return new PromotionItem
			{
				CampaignId = details.CampaignId,
				ProductVariantId = details.ProductVariantId,
				BatchId = details.BatchId,
				ItemType = details.ItemType,
				MaxUsage = details.MaxUsage,
				CurrentUsage = 0,
				IsActive = details.IsActive
			};
		}

		// Business logic methods
		public void UpdateConfiguration(PromotionItemUpdateFactor details)
		{
			if (details.ProductVariantId == Guid.Empty)
				throw DomainException.BadRequest("Product variant ID is required.");

			if (details.MaxUsage.HasValue && details.MaxUsage.Value <= 0)
				throw DomainException.BadRequest("Max usage must be greater than 0.");

			ProductVariantId = details.ProductVariantId;
			BatchId = details.BatchId;
			ItemType = details.ItemType;
			MaxUsage = details.MaxUsage;
			IsActive = details.IsActive;
		}

		public void SetActive(bool isActive)
		{
			IsActive = isActive;
		}

		// Records
		public sealed record PromotionItemCreationFactor
		{
			public required Guid CampaignId { get; init; }
			public required Guid ProductVariantId { get; init; }
			public Guid? BatchId { get; init; }
			public required PromotionType ItemType { get; init; }
			public int? MaxUsage { get; init; }
			public required bool IsActive { get; init; }
		}

		public sealed record PromotionItemUpdateFactor
		{
			public required Guid ProductVariantId { get; init; }
			public Guid? BatchId { get; init; }
			public required PromotionType ItemType { get; init; }
			public int? MaxUsage { get; init; }
			public required bool IsActive { get; init; }
		}
	}
}
