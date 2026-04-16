using Microsoft.AspNetCore.Identity;
using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class User : IdentityUser<Guid>, IHasTimestamps, ISoftDelete
	{
		public string FullName { get; private set; } = string.Empty;
		public int PointBalance { get; private set; } = 0;
		public bool IsActive { get; private set; } = true;
		public int DeliveryRefusalCount { get; private set; }
		public DateTime? CodBlockedUntil { get; private set; }

		// Navigation properties
		public virtual CustomerProfile? CustomerProfile { get; set; }
		public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = [];
		public virtual ICollection<Address> Addresses { get; set; } = [];
		public virtual ICollection<ImportTicket> ImportTickets { get; set; } = [];
		public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual ICollection<UserNotificationRead> NotificationReadStates { get; set; } = [];
		public virtual ICollection<UserVoucher> UserVouchers { get; set; } = [];
		public virtual ICollection<CartItem> CartItems { get; set; } = [];
		public virtual Media? ProfilePicture { get; set; }
		public virtual ICollection<Order> Orders { get; set; } = [];
		public virtual ICollection<Review> Reviews { get; set; } = [];
		public virtual ICollection<Review> AnswerReviews { get; set; } = [];
		public virtual ICollection<OrderCancelRequest> RequestedCancelRequests { get; set; } = [];
		public virtual ICollection<OrderCancelRequest> ProcessedCancelRequests { get; set; } = [];
		public virtual ICollection<OrderReturnRequest> CustomerReturnRequests { get; set; } = [];
		public virtual ICollection<OrderReturnRequest> ProcessedReturnRequests { get; set; } = [];
		public virtual ICollection<OrderReturnRequest> InspectedReturnRequests { get; set; } = [];

		// IHasTimestamps implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// Factory method
		public static User Create(UserCreationDetails details)
		{
			if (string.IsNullOrWhiteSpace(details.FullName))
				throw DomainException.BadRequest("Full name is required.");

			if (string.IsNullOrWhiteSpace(details.Email))
				throw DomainException.BadRequest("Email is required.");

			return new User
			{
				FullName = details.FullName.Trim(),
				UserName = details.Email.Trim(),
				Email = details.Email.Trim(),
				PhoneNumber = details.PhoneNumber?.Trim(),
				PhoneNumberConfirmed = !string.IsNullOrWhiteSpace(details.PhoneNumber),
				IsActive = true
			};
		}

		// Business logic methods
		public void EnsureActive()
		{
			if (!IsActive)
				throw DomainException.Forbidden("User account is inactive.");
		}

		public void EnsureEmailConfirmed()
		{
			if (!EmailConfirmed)
				throw DomainException.Forbidden("Email has not been confirmed.");
		}

		public void Activate()
		{
			if (IsActive)
				throw DomainException.BadRequest("User is already active.");
			IsActive = true;
		}

		public void Deactivate()
		{
			if (!IsActive)
				throw DomainException.BadRequest("User is already inactive.");
			IsActive = false;
		}

		public void UpdateProfile(string fullName)
		{
			if (string.IsNullOrWhiteSpace(fullName))
				throw DomainException.BadRequest("Full name cannot be empty.");
			FullName = fullName.Trim();
		}

		public void UpdateBasicInfo(string fullName, string phoneNumber)
		{
			if (string.IsNullOrWhiteSpace(fullName))
				throw DomainException.BadRequest("Full name cannot be empty.");

			if (string.IsNullOrWhiteSpace(phoneNumber))
				throw DomainException.BadRequest("Phone number cannot be empty.");

			FullName = fullName.Trim();
			PhoneNumber = phoneNumber.Trim();
		}

		public LoyaltyTransaction EarnPoints(LoyaltyTransaction.EarnTransactionInfo info)
		{
			EnsureActive();

			var transaction = LoyaltyTransaction.CreateEarn(this.Id, info);

			LoyaltyTransactions.Add(transaction);

			PointBalance += transaction.PointsChanged;

			return transaction;
		}

		public LoyaltyTransaction SpendPoints(LoyaltyTransaction.SpendTransactionInfo info)
		{
			EnsureActive();

			if (PointBalance < info.Points)
				throw DomainException.BadRequest($"Insufficient point balance. Current balance is {PointBalance}, but tried to spend {info.Points}.");

			var transaction = LoyaltyTransaction.CreateSpend(this.Id, info);

			LoyaltyTransactions.Add(transaction);

			PointBalance += transaction.PointsChanged;

			return transaction;
		}

		public LoyaltyTransaction AdjustPointsManual(LoyaltyTransaction.ManualTransactionInfo info)
		{
			EnsureActive();

			if (info.TransactionType == LoyaltyTransactionType.Spend && PointBalance < info.Points)
				throw DomainException.BadRequest($"Insufficient point balance for manual deduction. Current: {PointBalance}.");

			var transaction = LoyaltyTransaction.CreateManual(this.Id, info);

			LoyaltyTransactions.Add(transaction);
			PointBalance += transaction.PointsChanged;

			return transaction;
		}

		public void RecordDeliveryRefusal(DateTime nowUtc)
		{
			DeliveryRefusalCount++;

			// Nếu boom 5 lần -> Cấm COD 30 ngày
			if (DeliveryRefusalCount >= 5)
			{
				CodBlockedUntil = nowUtc.AddDays(30);
			}
			// Nếu boom 3 lần -> Cấm COD 7 ngày
			else if (DeliveryRefusalCount >= 3)
			{
				CodBlockedUntil = nowUtc.AddDays(7);
			}
		}

		public bool IsEligibleForCod(DateTime nowUtc)
		{
			// Vẫn đang trong thời gian phạt
			if (CodBlockedUntil.HasValue && CodBlockedUntil.Value > nowUtc)
			{
				return false;
			}

			return true;
		}

		// Record
		public record UserCreationDetails(
			string FullName,
			string Email,
			string? PhoneNumber
		);
	}
}
