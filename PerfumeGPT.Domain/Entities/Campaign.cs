using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Campaign : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		public sealed record PromotionItemSyncFactor(
			Guid? Id,
			Guid ProductVariantId,
			Guid? BatchId,
			PromotionType PromotionType,
			int? MaxUsage);

		public sealed record VoucherSyncFactor(
			Guid? Id,
			string Code,
			decimal DiscountValue,
			DiscountType DiscountType,
			VoucherType ApplyType,
			PromotionType TargetItemType);

		protected Campaign() { }

		public string Name { get; private set; } = null!;
		public string? Description { get; private set; }
		public DateTime StartDate { get; private set; }
		public DateTime EndDate { get; private set; }
		public CampaignType Type { get; private set; } // Flash Sale, Clearance, Seasonal, etc.
		public CampaignStatus Status { get; private set; } // Upcoming, Active, Paused, Completed, Cancelled

		// Navigation properties
		public virtual ICollection<PromotionItem> Items { get; set; } = [];
		public virtual ICollection<Voucher> Vouchers { get; set; } = [];

		// IHasTimestamps  implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// Factory methods
		public static Campaign Create(string name, string? description, DateTime startDate, DateTime endDate, CampaignType type, CampaignStatus status)
		{
			ValidateName(name);
			ValidateDateRange(startDate, endDate);

			return new Campaign
			{
				Id = Guid.NewGuid(),
				Name = name.Trim(),
				Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
				StartDate = startDate,
				EndDate = endDate,
				Type = type,
				Status = status
			};
		}

		public PromotionItem AddPromotionItem(Guid variantId, Guid? batchId, PromotionType type, int? maxUsage)
		{
			Items ??= [];

			var item = PromotionItem.Create(
				this.Id,
				variantId,
				batchId,
				type,
				maxUsage,
				this.Status == CampaignStatus.Active);

			Items.Add(item);
			return item;
		}

		public Voucher AddVoucher(string code, decimal discountValue, DiscountType discountType, VoucherType applyType, PromotionType targetItemType)
		{
			Vouchers ??= [];

			var voucher = Voucher.CreateCampaign(
				code,
				discountValue,
				discountType,
				applyType,
				targetItemType,
				this.Id,
				this.EndDate);

			Vouchers.Add(voucher);
			return voucher;
		}

		public void UpdatePromotionItem(Guid itemId, Guid productVariantId, Guid? batchId, PromotionType itemType, int? maxUsage)
		{
			if (itemId == Guid.Empty)
				throw DomainException.BadRequest("Campaign item ID is required.");

			Items ??= [];

			var item = Items.FirstOrDefault(x => x.Id == itemId)
				?? throw DomainException.NotFound("Campaign item not found.");

			item.UpdateConfiguration(productVariantId, batchId, itemType, maxUsage, Status == CampaignStatus.Active);
		}

		public void UpdateVoucher(Guid voucherId, string code, decimal discountValue, DiscountType discountType, VoucherType applyType, PromotionType targetItemType)
		{
			if (voucherId == Guid.Empty)
				throw DomainException.BadRequest("Campaign voucher ID is required.");

			Vouchers ??= [];

			var voucher = Vouchers.FirstOrDefault(x => x.Id == voucherId)
				?? throw DomainException.NotFound("Campaign voucher not found.");

			voucher.UpdateCampaign(code, discountValue, discountType, applyType, targetItemType, Id, EndDate);
		}

		public void RemovePromotionItem(Guid itemId)
		{
			if (itemId == Guid.Empty)
				throw DomainException.BadRequest("Campaign item ID is required.");

			Items ??= [];

			var item = Items.FirstOrDefault(x => x.Id == itemId)
				?? throw DomainException.NotFound("Campaign item not found.");

			Items.Remove(item);
		}

		public void RemoveVoucher(Guid voucherId)
		{
			if (voucherId == Guid.Empty)
				throw DomainException.BadRequest("Campaign voucher ID is required.");

			Vouchers ??= [];

			var voucher = Vouchers.FirstOrDefault(x => x.Id == voucherId)
				?? throw DomainException.NotFound("Campaign voucher not found.");

			Vouchers.Remove(voucher);
		}

		public void SyncPromotionItems(IEnumerable<PromotionItemSyncFactor> requestItems, bool isActive)
		{
			Items ??= [];

			var requestItemList = requestItems.ToList();
			var requestItemById = requestItemList
				.Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty)
				.ToDictionary(x => x.Id!.Value, x => x);
			var existingItemsById = Items.ToDictionary(x => x.Id, x => x);

			var itemsToRemove = Items.Where(i => !requestItemById.ContainsKey(i.Id)).ToList();
			foreach (var item in itemsToRemove)
			{
				Items.Remove(item);
			}

			foreach (var req in requestItemList)
			{
				if (req.Id.HasValue
					 && req.Id.Value != Guid.Empty
					 && existingItemsById.TryGetValue(req.Id.Value, out var existingItem))
				{
					existingItem.UpdateConfiguration(req.ProductVariantId, req.BatchId, req.PromotionType, req.MaxUsage, isActive);
				}
				else
				{
					AddPromotionItem(req.ProductVariantId, req.BatchId, req.PromotionType, req.MaxUsage);
				}
			}
		}

		public void SyncVouchers(IEnumerable<VoucherSyncFactor> requestVouchers)
		{
			Vouchers ??= [];

			var requestVoucherList = requestVouchers.ToList();
			var requestVoucherById = requestVoucherList
				.Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty)
				.ToDictionary(x => x.Id!.Value, x => x);
			var existingVouchersById = Vouchers.ToDictionary(x => x.Id, x => x);

			// 1. Remove vouchers not present in the request
			var vouchersToRemove = Vouchers.Where(v => !requestVoucherById.ContainsKey(v.Id)).ToList();
			foreach (var voucher in vouchersToRemove)
			{
				Vouchers.Remove(voucher);
			}

			// 2. Update existing & add new
			foreach (var req in requestVoucherList)
			{
				if (req.Id.HasValue
					   && req.Id.Value != Guid.Empty
					   && existingVouchersById.TryGetValue(req.Id.Value, out var existingVoucher))
				{
					existingVoucher.UpdateCampaign(
						req.Code,
						req.DiscountValue,
						req.DiscountType,
						req.ApplyType,
						req.TargetItemType,
						this.Id,
						this.EndDate);
				}
				else
				{
					AddVoucher(
						req.Code,
						req.DiscountValue,
						req.DiscountType,
						req.ApplyType,
						req.TargetItemType);
				}
			}
		}

		// Business logic methods
		public void UpdateInfo(string name, string? description, DateTime startDate, DateTime endDate, CampaignType type)
		{
			ValidateName(name);
			ValidateDateRange(startDate, endDate);

			Name = name.Trim();
			Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
			StartDate = startDate;
			EndDate = endDate;
			Type = type;
		}

		public void UpdateStatus(CampaignStatus newStatus, DateTime nowUtc)
		{
			if (newStatus == CampaignStatus.Active && StartDate > nowUtc)
				throw DomainException.BadRequest("Cannot activate campaign before its start date.");

			if (newStatus < Status && (newStatus != CampaignStatus.Active || Status != CampaignStatus.Paused))
				throw DomainException.BadRequest("Cannot revert campaign to a previous status.");

			Status = newStatus;
		}

		public void EnsureUpdatable()
		{
			if (Status == CampaignStatus.Active)
				throw DomainException.BadRequest("Cannot update an active campaign. Please pause the campaign before updating.");
		}

		public void EnsureDeletable()
		{
			if (Status == CampaignStatus.Active)
				throw DomainException.BadRequest("Cannot delete an active campaign. Please pause the campaign before deleting.");
		}

		private static void ValidateName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw DomainException.BadRequest("Campaign name is required.");
		}

		private static void ValidateDateRange(DateTime startDate, DateTime endDate)
		{
			if (endDate <= startDate)
				throw DomainException.BadRequest("Campaign end date must be after start date.");
		}
	}
}
