using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

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
