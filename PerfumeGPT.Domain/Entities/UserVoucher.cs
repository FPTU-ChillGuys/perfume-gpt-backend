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
		public string? GuestEmailOrPhone { get; private set; }
		public bool IsUsed { get; private set; }
		public UsageStatus Status { get; private set; }

		// Navigation properties
		public virtual User? User { get; set; }
		public virtual Voucher Voucher { get; set; } = null!;
		public virtual Order? Order { get; set; } = null!;

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static UserVoucher CreateAvailable(Guid? userId, Guid voucherId, string? guestEmailOrPhone = null)
		{
			if (voucherId == Guid.Empty)
				throw DomainException.BadRequest("Voucher ID is required.");

			return new UserVoucher
			{
				UserId = userId,
				VoucherId = voucherId,
				GuestEmailOrPhone = guestEmailOrPhone,
				IsUsed = false,
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

			if (IsUsed)
				throw DomainException.BadRequest("Cannot reserve a used voucher.");

			if (Status != UsageStatus.Available)
				throw DomainException.BadRequest("Voucher must be available before reserving.");

			OrderId = orderId;
			Status = UsageStatus.Reserved;
		}

		public void MarkUsed(Guid orderId)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			if (Status != UsageStatus.Reserved)
				throw DomainException.BadRequest("Voucher must be reserved before marking as used.");

			OrderId = orderId;
			IsUsed = true;
			Status = UsageStatus.Used;
		}

		public void ReleaseReservation()
		{
			if (Status != UsageStatus.Reserved)
				throw DomainException.BadRequest("Voucher is not reserved.");

			if (IsUsed)
				throw DomainException.BadRequest("Cannot release a used voucher.");

			OrderId = null;
			Status = UsageStatus.Available;
		}

		public void MarkAsRefunded()
		{
			if (Status != UsageStatus.Used && Status != UsageStatus.Reserved)
				throw DomainException.BadRequest("Only reserved or used vouchers can be refunded.");

			Status = UsageStatus.Refunded;
		}

		public UserVoucher CreateReplacement()
		{
			if (Status != UsageStatus.Refunded)
				throw DomainException.BadRequest("Can only create a replacement for a refunded voucher.");

			return CreateAvailable(this.UserId, this.VoucherId, this.GuestEmailOrPhone);
		}

		public void AssignToUser(Guid userId)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID is required.");

			UserId = userId;
		}
	}
}

