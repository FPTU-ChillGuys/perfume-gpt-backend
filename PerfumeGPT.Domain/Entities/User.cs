using Microsoft.AspNetCore.Identity;
using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class User : IdentityUser<Guid>, IHasTimestamps, ISoftDelete
	{
		public string FullName { get; private set; } = string.Empty;
		public int PointBalance { get; private set; } = 0;
		public bool IsActive { get; private set; } = true;

		// Navigation properties
		public virtual CustomerProfile? CustomerProfile { get; set; }
		public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = [];
		public virtual ICollection<Address> Addresses { get; set; } = [];
		public virtual ICollection<ImportTicket> ImportTickets { get; set; } = [];
		public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
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
		public static User Create(string fullName, string email, string? phoneNumber)
		{
			if (string.IsNullOrWhiteSpace(fullName))
				throw DomainException.BadRequest("Full name is required.");

			if (string.IsNullOrWhiteSpace(email))
				throw DomainException.BadRequest("Email is required.");

			return new User
			{
				FullName = fullName.Trim(),
				UserName = email.Trim(),
				Email = email.Trim(),
				PhoneNumber = phoneNumber?.Trim(),
				PhoneNumberConfirmed = !string.IsNullOrWhiteSpace(phoneNumber),
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
	}
}
