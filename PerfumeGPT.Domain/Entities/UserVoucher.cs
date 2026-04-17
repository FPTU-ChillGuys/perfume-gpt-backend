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
				throw DomainException.BadRequest("Voucher ID là bắt buộc.");

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
				throw DomainException.BadRequest("Order ID là bắt buộc.");

			var userVoucher = CreateAvailable(userId, voucherId, guestEmailOrPhone);
			userVoucher.Reserve(orderId);
			return userVoucher;
		}

		// Business logic methods
		public void Reserve(Guid orderId)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID là bắt buộc.");

			if (Status == UsageStatus.Used)
				throw DomainException.BadRequest("Không thể giữ chỗ cho mã giảm giá đã dùng.");

			if (Status != UsageStatus.Available)
				throw DomainException.BadRequest("Mã giảm giá phải ở trạng thái khả dụng trước khi giữ chỗ.");

			OrderId = orderId;
			Status = UsageStatus.Reserved;
		}

		public void MarkUsed()
		{
			if (Status != UsageStatus.Reserved)
				throw DomainException.BadRequest("Mã giảm giá phải được giữ chỗ trước khi đánh dấu đã sử dụng.");

			Status = UsageStatus.Used;
		}

		public void ReleaseReservation()
		{
			if (Status != UsageStatus.Reserved)
				throw DomainException.BadRequest("Mã giảm giá chưa được giữ chỗ.");

			OrderId = null;
			Status = UsageStatus.Available;
		}

		public void RevertUsed()
		{
			if (Status != UsageStatus.Used)
				throw DomainException.BadRequest("Không thể hoàn tác mã giảm giá chưa được sử dụng.");

			OrderId = null;
			Status = UsageStatus.Available;
		}

		public void AssignToUser(Guid userId)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID là bắt buộc.");

			UserId = userId;
		}
	}
}

