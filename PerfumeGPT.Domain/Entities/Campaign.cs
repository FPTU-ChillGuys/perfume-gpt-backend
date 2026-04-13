using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;
using static PerfumeGPT.Domain.Entities.PromotionItem;
using static PerfumeGPT.Domain.Entities.Voucher;

namespace PerfumeGPT.Domain.Entities
{
	public class Campaign : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
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
		public static Campaign Create(CampaignCreationFactor details)
		{
			ValidateName(details.Name);
			ValidateDateRange(details.StartDate, details.EndDate);

			return new Campaign
			{
				Id = Guid.NewGuid(),
				Name = details.Name.Trim(),
				Description = string.IsNullOrWhiteSpace(details.Description) ? null : details.Description.Trim(),
				StartDate = details.StartDate,
				EndDate = details.EndDate,
				Type = details.Type,
				Status = details.Status
			};
		}

		public void UpdateStatus(CampaignStatus newStatus, DateTime nowUtc)
		{
			if (newStatus == CampaignStatus.Active && StartDate > nowUtc)
				throw DomainException.BadRequest("Cannot activate campaign before its start date.");

			if (newStatus < Status && (newStatus != CampaignStatus.Active || Status != CampaignStatus.Paused))
				throw DomainException.BadRequest("Cannot revert campaign to a previous status.");

			Status = newStatus;
		}

		public void UpdateInfo(CampaignUpdateInfoFactor details)
		{
			ValidateName(details.Name);
			ValidateDateRange(details.StartDate, details.EndDate);

			Name = details.Name.Trim();
			Description = string.IsNullOrWhiteSpace(details.Description) ? null : details.Description.Trim();
			StartDate = details.StartDate;
			EndDate = details.EndDate;
			Type = details.Type;
		}

		public PromotionItem AddPromotionItem(PromotionItemConfigFactor details)
		{
			Items ??= [];

			var item = PromotionItem.Create(new PromotionItemCreationFactor
			{
				CampaignId = this.Id,
				ProductVariantId = details.ProductVariantId,
				BatchId = details.BatchId,
				ItemType = details.PromotionType,
				DiscountType = details.DiscountType,
				DiscountValue = details.DiscountValue,
				MaxUsage = details.MaxUsage,
				IsActive = this.Status == CampaignStatus.Active
			});

			Items.Add(item);
			return item;
		}

		public Voucher AddVoucher(VoucherConfigFactor details)
		{
			Vouchers ??= [];

			var voucher = Voucher.CreateCampaign(new VoucherCampaignConfigFactor
			{
				Code = details.Code,
				DiscountValue = details.DiscountValue,
				DiscountType = details.DiscountType,
				ApplyType = details.ApplyType,
				TargetItemType = details.TargetItemType,
				CampaignId = this.Id,
				ExpiryDate = this.EndDate,
				MaxDiscountAmount = details.MaxDiscountAmount,
				MinOrderValue = details.MinOrderValue,
				TotalQuantity = details.TotalQuantity,
				MaxUsagePerUser = details.MaxUsagePerUser,
				IsMemberOnly = details.IsMemberOnly
			});

			Vouchers.Add(voucher);
			return voucher;
		}

		public void UpdatePromotionItem(Guid itemId, PromotionItemConfigFactor details)
		{
			if (itemId == Guid.Empty)
				throw DomainException.BadRequest("Campaign item ID is required.");

			Items ??= [];

			var item = Items.FirstOrDefault(x => x.Id == itemId)
				?? throw DomainException.NotFound("Campaign item not found.");

			item.UpdateConfiguration(new PromotionItemUpdateFactor
			{
				ProductVariantId = details.ProductVariantId,
				BatchId = details.BatchId,
				ItemType = details.PromotionType,
				DiscountType = details.DiscountType,
				DiscountValue = details.DiscountValue,
				MaxUsage = details.MaxUsage,
				IsActive = Status == CampaignStatus.Active
			});
		}

		public void UpdateVoucher(Guid voucherId, VoucherConfigFactor details)
		{
			if (voucherId == Guid.Empty)
				throw DomainException.BadRequest("Campaign voucher ID is required.");

			Vouchers ??= [];

			var voucher = Vouchers.FirstOrDefault(x => x.Id == voucherId)
				?? throw DomainException.NotFound("Campaign voucher not found.");

			voucher.UpdateCampaign(new VoucherCampaignConfigFactor
			{
				Code = details.Code,
				DiscountValue = details.DiscountValue,
				DiscountType = details.DiscountType,
				ApplyType = details.ApplyType,
				TargetItemType = details.TargetItemType,
				CampaignId = this.Id,
				ExpiryDate = this.EndDate,
				MaxDiscountAmount = details.MaxDiscountAmount,
				MinOrderValue = details.MinOrderValue,
				TotalQuantity = details.TotalQuantity,
				MaxUsagePerUser = details.MaxUsagePerUser,
				IsMemberOnly = details.IsMemberOnly
			});
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

			voucher.IsDeleted = true;
			voucher.DeletedAt ??= DateTime.UtcNow;

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
					existingItem.UpdateConfiguration(new PromotionItemUpdateFactor
					{
						ProductVariantId = req.ProductVariantId,
						BatchId = req.BatchId,
						ItemType = req.PromotionType,
						DiscountType = req.DiscountType,
						DiscountValue = req.DiscountValue,
						MaxUsage = req.MaxUsage,
						IsActive = isActive
					});
				}
				else
				{
					AddPromotionItem(new PromotionItemConfigFactor
					{
						ProductVariantId = req.ProductVariantId,
						BatchId = req.BatchId,
						PromotionType = req.PromotionType,
						DiscountType = req.DiscountType,
						DiscountValue = req.DiscountValue,
						MaxUsage = req.MaxUsage
					});
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

			var vouchersToRemove = Vouchers.Where(v => !requestVoucherById.ContainsKey(v.Id)).ToList();
			foreach (var voucher in vouchersToRemove)
			{
				Vouchers.Remove(voucher);
			}

			foreach (var req in requestVoucherList)
			{
				if (req.Id.HasValue
					   && req.Id.Value != Guid.Empty
					   && existingVouchersById.TryGetValue(req.Id.Value, out var existingVoucher))
				{
					existingVoucher.UpdateCampaign(new VoucherCampaignConfigFactor
					{
						Code = req.Code,
						DiscountValue = req.DiscountValue,
						DiscountType = req.DiscountType,
						ApplyType = req.ApplyType,
						TargetItemType = req.TargetItemType,
						CampaignId = this.Id,
						ExpiryDate = this.EndDate,
						MaxDiscountAmount = req.MaxDiscountAmount,
						MinOrderValue = req.MinOrderValue,
						TotalQuantity = req.TotalQuantity,
						MaxUsagePerUser = req.MaxUsagePerUser,
						IsMemberOnly = req.IsMemberOnly
					});
				}
				else
				{
					AddVoucher(new VoucherConfigFactor
					{
						Code = req.Code,
						DiscountValue = req.DiscountValue,
						DiscountType = req.DiscountType,
						ApplyType = req.ApplyType,
						TargetItemType = req.TargetItemType,
						MaxDiscountAmount = req.MaxDiscountAmount,
						MinOrderValue = req.MinOrderValue,
						TotalQuantity = req.TotalQuantity,
						MaxUsagePerUser = req.MaxUsagePerUser,
						IsMemberOnly = req.IsMemberOnly
					});
				}
			}
		}

		// Business logic methods

		public void EnsureIsNotActive(string action = "modify")
		{
			if (Status == CampaignStatus.Active)
				throw DomainException.BadRequest($"Cannot {action} an active campaign. Please pause the campaign before {action}ing.");
		}

		// Private helpers
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

		// Records
		public sealed record PromotionItemSyncFactor
		{
			public Guid? Id { get; init; }
			public required Guid ProductVariantId { get; init; }
			public Guid? BatchId { get; init; }
			public required PromotionType PromotionType { get; init; }
			public required DiscountType DiscountType { get; init; }
			public required decimal DiscountValue { get; init; }
			public int? MaxUsage { get; init; }
		}

		public sealed record VoucherSyncFactor
		{
			public Guid? Id { get; init; }
			public required string Code { get; init; }
			public required decimal DiscountValue { get; init; }
			public required DiscountType DiscountType { get; init; }
			public required VoucherType ApplyType { get; init; }
			public required PromotionType TargetItemType { get; init; }
			public decimal? MaxDiscountAmount { get; init; }
			public required decimal MinOrderValue { get; init; }
			public int? TotalQuantity { get; init; }
			public int? MaxUsagePerUser { get; init; }
			public bool IsMemberOnly { get; init; }
		}

		public sealed record CampaignCreationFactor
		{
			public required string Name { get; init; }
			public string? Description { get; init; }
			public required DateTime StartDate { get; init; }
			public required DateTime EndDate { get; init; }
			public required CampaignType Type { get; init; }
			public required CampaignStatus Status { get; init; }
		}

		public sealed record CampaignUpdateInfoFactor
		{
			public required string Name { get; init; }
			public string? Description { get; init; }
			public required DateTime StartDate { get; init; }
			public required DateTime EndDate { get; init; }
			public required CampaignType Type { get; init; }
		}

		public sealed record PromotionItemConfigFactor
		{
			public required Guid ProductVariantId { get; init; }
			public Guid? BatchId { get; init; }
			public required PromotionType PromotionType { get; init; }
			public required DiscountType DiscountType { get; init; }
			public required decimal DiscountValue { get; init; }
			public int? MaxUsage { get; init; }
		}

		public sealed record VoucherConfigFactor
		{
			public required string Code { get; init; }
			public required decimal DiscountValue { get; init; }
			public required DiscountType DiscountType { get; init; }
			public required VoucherType ApplyType { get; init; }
			public required PromotionType TargetItemType { get; init; }
			public decimal? MaxDiscountAmount { get; init; }
			public required decimal MinOrderValue { get; init; }
			public int? TotalQuantity { get; init; }
			public int? MaxUsagePerUser { get; init; }
			public bool IsMemberOnly { get; init; }
		}
	}
}
