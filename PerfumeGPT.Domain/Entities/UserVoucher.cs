using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class UserVoucher : BaseEntity<Guid>, IHasTimestamps
	{
		protected UserVoucher() { }

		public Guid? UserId { get; private set; }
		public Guid VoucherId { get; private set; }
		public Guid? OrderId { get; private set; }
		public string? GuestIdentifier { get; private set; }
		public UsageStatus Status { get; private set; }

		// Navigation properties
		public virtual User? User { get; set; }
		public virtual Voucher Voucher { get; set; } = null!;
		public virtual Order? Order { get; set; } = null!;

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static UserVoucher CreateAvailable(Guid? userId, Guid voucherId, string? guestIdentifier = null)
		{
			if (voucherId == Guid.Empty)
				throw DomainException.BadRequest("Voucher ID is required.");

			return new UserVoucher
			{
				UserId = userId,
				VoucherId = voucherId,
				GuestIdentifier = string.IsNullOrWhiteSpace(guestIdentifier) ? null : guestIdentifier.Trim(),
				Status = UsageStatus.Available
			};
		}

		public static UserVoucher CreateReserved(Guid? userId, Guid voucherId, Guid orderId, string? guestEmailOrPhone = null)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			var userVoucher = CreateAvailable(userId, voucherId, guestEmailOrPhone);
			userVoucher.Reserve(orderId);
			return userVoucher;
		}

		// Business logic methods
		public void Reserve(Guid orderId)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			if (Status == UsageStatus.Used)
				throw DomainException.BadRequest("Cannot reserve a used voucher.");

			if (Status != UsageStatus.Available)
				throw DomainException.BadRequest("Voucher must be available before reserving.");

			OrderId = orderId;
			Status = UsageStatus.Reserved;
		}

		public void MarkUsed()
		{
			if (Status != UsageStatus.Reserved)
				throw DomainException.BadRequest("Voucher must be reserved before marking as used.");

			Status = UsageStatus.Used;
		}

		public void ReleaseReservation()
		{
			if (Status != UsageStatus.Reserved)
				throw DomainException.BadRequest("Voucher is not reserved.");

			OrderId = null;
			Status = UsageStatus.Available;
		}

		public void RevertUsed()
		{
			if (Status != UsageStatus.Used)
				throw DomainException.BadRequest("Cannot revert a voucher that is not used.");

			OrderId = null;
			Status = UsageStatus.Available;
		}

		public void AssignToUser(Guid userId)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID is required.");

			UserId = userId;
		}
	}
}

