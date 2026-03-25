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
		public bool AutoStopWhenBatchEmpty { get; private set; }
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
		public static PromotionItem Create(Guid campaignId, Guid productVariantId, Guid? batchId, PromotionType itemType, int? maxUsage, bool isActive)
		{
			if (campaignId == Guid.Empty)
				throw DomainException.BadRequest("Campaign ID is required.");

			if (productVariantId == Guid.Empty)
				throw DomainException.BadRequest("Product variant ID is required.");

			if (maxUsage.HasValue && maxUsage.Value <= 0)
				throw DomainException.BadRequest("Max usage must be greater than 0.");

			return new PromotionItem
			{
				CampaignId = campaignId,
				ProductVariantId = productVariantId,
				BatchId = batchId,
				ItemType = itemType,
				MaxUsage = maxUsage,
				CurrentUsage = 0,
				AutoStopWhenBatchEmpty = batchId.HasValue,
				IsActive = isActive
			};
		}

		// Business logic methods
		public void UpdateConfiguration(Guid productVariantId, Guid? batchId, PromotionType itemType, int? maxUsage, bool isActive)
		{
			if (productVariantId == Guid.Empty)
				throw DomainException.BadRequest("Product variant ID is required.");

			if (maxUsage.HasValue && maxUsage.Value <= 0)
				throw DomainException.BadRequest("Max usage must be greater than 0.");

			ProductVariantId = productVariantId;
			BatchId = batchId;
			ItemType = itemType;
			MaxUsage = maxUsage;
			AutoStopWhenBatchEmpty = batchId.HasValue;
			IsActive = isActive;
		}

		public void SetActive(bool isActive)
		{
			IsActive = isActive;
		}
	}
}
